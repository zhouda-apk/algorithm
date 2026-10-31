using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using AlgorithmLab.Core.Models;

namespace AlgorithmLab.Data.FileStorage
{
    public sealed class JsonDataStore : IDisposable
    {
        public const int MaximumFileBytes = 20 * 1024 * 1024;
        private static readonly HashSet<string> ActivePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly object gate = new object();
        private readonly IFileCommitter committer;
        private readonly Mutex mutex;
        private bool disposed;
        private bool changing;
        private StoreDocument current;
        private string fingerprint;
        public string FilePath { get; }
        public string BackupPath { get { return FilePath + ".bak"; } }
        public static string DefaultPath { get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AlgorithmLab", "store.json"); } }

        public JsonDataStore(string filePath = null) : this(filePath ?? DefaultPath, new AtomicFileCommitter()) { }
        internal JsonDataStore(string filePath, IFileCommitter committer)
        {
            FilePath = Path.GetFullPath(filePath);
            this.committer = committer ?? throw new ArgumentNullException(nameof(committer));
            lock (ActivePaths)
            {
                if (!ActivePaths.Add(FilePath)) throw new AlgorithmLabException(ErrorKind.Conflict, "This store is already open in this process.");
                try
                {
                    mutex = new Mutex(false, "Local\\AlgorithmLab-" + DocumentValidator.Hash(Encoding.UTF8.GetBytes(FilePath.ToUpperInvariant())));
                    bool acquired;
                    try { acquired = mutex.WaitOne(0); } catch (AbandonedMutexException) { acquired = true; }
                    if (!acquired) throw new AlgorithmLabException(ErrorKind.Conflict, "Another process is using this store.");
                }
                catch { if (mutex != null) mutex.Dispose(); ActivePaths.Remove(FilePath); throw; }
            }
        }
        private static DataContractJsonSerializer Serializer()
        {
            return new DataContractJsonSerializer(typeof(StoreDocument), new DataContractJsonSerializerSettings { MaxItemsInObjectGraph = 2000000 });
        }
        private static byte[] Encode(StoreDocument value)
        {
            using (var stream = new MemoryStream())
            {
                try { Serializer().WriteObject(stream, value); }
                catch (SerializationException e) { throw new AlgorithmLabException(ErrorKind.Validation, "JSON serialization or object graph limit failed.", e); }
                if (stream.Length > MaximumFileBytes) throw new AlgorithmLabException(ErrorKind.Validation, "JSON exceeds the 20 MiB limit.");
                return stream.ToArray();
            }
        }
        private static StoreDocument Decode(byte[] bytes)
        {
            try
            {
                VerifySingleObject(new UTF8Encoding(false, true).GetString(bytes));
                DocumentValidator.Require(Array.IndexOf(bytes, (byte)0) < 0, "JSON must be UTF-8 without raw NUL bytes.");
                DocumentValidator.Require(bytes.Length < 3 || bytes[0] != 239 || bytes[1] != 187 || bytes[2] != 191, "Unexpected UTF-8 BOM.");
                using (var reader = JsonReaderWriterFactory.CreateJsonReader(bytes, new System.Xml.XmlDictionaryReaderQuotas
                { MaxDepth = 64, MaxStringContentLength = MaximumFileBytes, MaxArrayLength = 2000000, MaxBytesPerRead = 4096, MaxNameTableCharCount = 16384 }))
                {
                    var document = (StoreDocument)Serializer().ReadObject(reader);
                    DocumentValidator.Require(reader.MoveToContent() == System.Xml.XmlNodeType.None, "Unexpected trailing JSON content.");
                    DocumentValidator.Validate(document);
                    return document;
                }
            }
            catch (Exception e) when (e is SerializationException || e is System.Xml.XmlException || e is DecoderFallbackException)
            { throw new AlgorithmLabException(ErrorKind.DataIntegrity, "Invalid JSON document.", e); }
        }
        private static StoreDocument Copy(StoreDocument value) { return Decode(Encode(value)); }
        private static void VerifySingleObject(string text)
        {
            text = text.TrimStart(' ', '\r', '\n', '\t');
            DocumentValidator.Require(text.Length > 0 && text[0] == '{', "JSON root must be an object.");
            bool quoted = false, escaped = false;
            int depth = 0;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (quoted)
                {
                    if (escaped) escaped = false;
                    else if (c == '\\') escaped = true;
                    else if (c == '"') quoted = false;
                    continue;
                }
                if (c == '"') { quoted = true; continue; }
                if (c == '{' || c == '[') depth++;
                if (c == '}' || c == ']') depth--;
                if (depth == 0)
                {
                    for (int j = i + 1; j < text.Length; j++)
                        DocumentValidator.Require(text[j] == ' ' || text[j] == '\r' || text[j] == '\n' || text[j] == '\t', "Unexpected trailing JSON content.");
                    return;
                }
            }
            throw new AlgorithmLabException(ErrorKind.DataIntegrity, "Unterminated JSON object.");
        }
        private byte[] ReadBytes(string path)
        {
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                DocumentValidator.Require(stream.Length <= MaximumFileBytes, "JSON exceeds the 20 MiB limit.");
                using (var buffer = new MemoryStream()) { stream.CopyTo(buffer); DocumentValidator.Require(buffer.Length <= MaximumFileBytes, "JSON too large."); return buffer.ToArray(); }
            }
        }
        private void Reload()
        {
            var bytes = ReadBytes(FilePath); var valid = Decode(bytes);
            current = valid; fingerprint = DocumentValidator.Hash(bytes);
        }
        private void CheckOpen()
        {
            if (disposed) throw new ObjectDisposedException(nameof(JsonDataStore));
            if (changing) throw new AlgorithmLabException(ErrorKind.Conflict, "A commit callback cannot re-enter the store.");
        }
        public StoreDocument Initialize()
        {
            lock (gate)
            {
                CheckOpen();
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
                    if (File.Exists(FilePath)) Reload();
                    else
                    {
                        if (File.Exists(BackupPath) || Directory.GetFiles(Path.GetDirectoryName(FilePath), Path.GetFileName(FilePath) + ".*.tmp").Length > 0)
                            throw new AlgorithmLabException(ErrorKind.DataIntegrity, "Main file missing; recovery is required.");
                        Publish(StoreDocument.CreateEmpty(), true);
                    }
                    return Copy(current);
                }
                catch (Exception e) when (e is IOException || e is UnauthorizedAccessException) { throw StorageError(e); }
            }
        }
        public StoreDocument Load()
        {
            lock (gate)
            {
                CheckOpen();
                try { Reload(); return Copy(current); }
                catch (Exception e) when (e is IOException || e is UnauthorizedAccessException) { throw StorageError(e); }
            }
        }
        public StoreDocument Commit(Action<StoreDocument> change)
        {
            if (change == null) throw new ArgumentNullException(nameof(change));
            lock (gate)
            {
                CheckOpen();
                if (current == null) throw new AlgorithmLabException(ErrorKind.Conflict, "Initialize or load a valid store before committing.");
                try
                {
                    if (DocumentValidator.Hash(ReadBytes(FilePath)) != fingerprint) throw new AlgorithmLabException(ErrorKind.Conflict, "Store changed outside this instance; reload first.");
                    var candidate = Copy(current);
                    changing = true;
                    try { change(candidate); } finally { changing = false; }
                    candidate.FileFormatVersion = current.FileFormatVersion;
                    candidate.DocumentRevision = checked(current.DocumentRevision + 1);
                    candidate.SavedAtUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
                    DocumentValidator.Validate(candidate);
                    Publish(candidate, true);
                    return Copy(current);
                }
                catch (Exception e) when (e is IOException || e is UnauthorizedAccessException) { throw StorageError(e); }
            }
        }
        private void Publish(StoreDocument candidate, bool keepBackup)
        {
            var bytes = Encode(candidate); var hash = DocumentValidator.Hash(bytes);
            var temporary = FilePath + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                { stream.Write(bytes, 0, bytes.Length); stream.Flush(true); }
                var readBack = ReadBytes(temporary); Decode(readBack);
                DocumentValidator.Require(DocumentValidator.Hash(readBack) == hash, "Temporary file verification failed.");
                try { committer.Publish(temporary, FilePath, keepBackup ? BackupPath : null); }
                catch (IOException)
                {
                    // A publication can complete before its caller observes an error.
                    // Only accept the exact intended bytes as successful.
                    if (!File.Exists(FilePath) || DocumentValidator.Hash(ReadBytes(FilePath)) != hash) throw;
                }
                Reload();
            }
            catch
            {
                current = null; fingerprint = null;
                try { if (File.Exists(FilePath)) Reload(); } catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is AlgorithmLabException) { }
                throw;
            }
            finally { try { if (File.Exists(temporary)) File.Delete(temporary); } catch (IOException) { } catch (UnauthorizedAccessException) { } }
        }
        public StoreDocument InspectBackup()
        {
            lock (gate)
            {
                CheckOpen();
                try { return Decode(ReadBytes(BackupPath)); }
                catch (Exception e) when (e is IOException || e is UnauthorizedAccessException) { throw StorageError(e); }
            }
        }
        // Explicit recovery only: the caller must show backup time/revision and get confirmation.
        public StoreDocument RestoreBackup()
        {
            lock (gate)
            {
                CheckOpen();
                try
                {
                    var backup = InspectBackup();
                    if (File.Exists(FilePath)) File.Copy(FilePath, FilePath + ".corrupt." + Guid.NewGuid().ToString("N"));
                    long revision = backup.DocumentRevision;
                    if (File.Exists(FilePath))
                    {
                        try { revision = Math.Max(revision, Decode(ReadBytes(FilePath)).DocumentRevision); }
                        catch (AlgorithmLabException e) when (e.Kind == ErrorKind.DataIntegrity) { }
                    }
                    backup.DocumentRevision = checked(revision + 1);
                    backup.SavedAtUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
                    Publish(backup, false);
                    return Copy(current);
                }
                catch (Exception e) when (e is IOException || e is UnauthorizedAccessException) { throw StorageError(e); }
            }
        }
        private AlgorithmLabException StorageError(Exception inner) { return new AlgorithmLabException(ErrorKind.Persistence, "Cannot access store: " + FilePath, inner); }
        public void Dispose()
        {
            lock (gate)
            {
                if (disposed) return;
                CheckOpen();
                // Dispose on the same thread that acquired the lifetime mutex.
                mutex.ReleaseMutex(); mutex.Dispose(); disposed = true;
                lock (ActivePaths) ActivePaths.Remove(FilePath);
            }
        }
    }
}

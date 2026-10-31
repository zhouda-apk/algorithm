using System.IO;
namespace AlgorithmLab.Data.FileStorage
{
    internal interface IFileCommitter { void Publish(string temporary, string destination, string backup); }
    internal sealed class AtomicFileCommitter : IFileCommitter
    {
        public void Publish(string temporary, string destination, string backup)
        {
            if (File.Exists(destination)) File.Replace(temporary, destination, backup);
            else File.Move(temporary, destination);
        }
    }
}

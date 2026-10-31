using System;
using System.Globalization;
using System.IO;
namespace ConsoleApp1.Presentation
{
    internal sealed class ConsoleInputReader
    {
        private readonly TextReader input;
        private readonly TextWriter output;
        internal ConsoleInputReader(TextReader input, TextWriter output) { this.input = input; this.output = output; }
        internal string Read(string prompt)
        {
            output.Write(prompt); var value = input.ReadLine();
            if (value == null) throw new EndOfStreamException();
            if (value.Trim() == ":q") throw new OperationCanceledException();
            return value;
        }
        internal int Integer(string prompt)
        {
            int value;
            if (!int.TryParse(Read(prompt).Trim(), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out value))
                throw new AlgorithmLab.Core.Models.AlgorithmLabException(AlgorithmLab.Core.Models.ErrorKind.Validation, "請輸入 Int32 整數。");
            return value;
        }
        internal bool Confirm(string prompt) { return string.Equals(Read(prompt).Trim(), "Y", StringComparison.OrdinalIgnoreCase); }
    }
}

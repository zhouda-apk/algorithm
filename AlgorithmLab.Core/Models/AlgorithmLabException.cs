using System;
namespace AlgorithmLab.Core.Models
{
    public class AlgorithmLabException : Exception
    {
        public ErrorKind Kind { get; }
        public AlgorithmLabException(ErrorKind kind, string message, Exception inner = null) : base(message, inner) { Kind = kind; }
    }
}

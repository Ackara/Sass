namespace Acklann.Sassin
{
    public readonly struct CompilerError
    {
        public CompilerError(string message, string file, int line, int column, ErrorLevel severity = ErrorLevel.Error, string code = null)
        {
            Severity = severity;
            Message = message;
            StatusCode = code;
            File = file;
            Line = line;
            Column = column;
        }

        public ErrorLevel Severity { get; }

        public string StatusCode { get; }

        public string Message { get; }

        public string File { get; }

        public int Line { get; }

        public int Column { get; }
    }
}
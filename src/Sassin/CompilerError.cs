namespace Acklann.Sassin
{
    public readonly struct CompilerError
    {
        public CompilerError(string message, string file, int line, int column, ErrorSeverity severity = ErrorSeverity.Error, int code = default)
        {
            Severity = severity;
            Message = message;
            StatusCode = code;
            File = file;
            Line = line;
            Column = column;
        }

        public ErrorSeverity Severity { get; }

        public int StatusCode { get; }

        public string Message { get; }

        public string File { get; }

        public int Line { get; }

        public int Column { get; }
    }
}
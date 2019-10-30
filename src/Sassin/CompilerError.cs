namespace Acklann.Sassin
{
    public class CompilerError
    {
        public string Message { get; set; }

        public string File { get; set; }

        public int Line { get; set; }

        public int Column { get; set; }

        public int StatusCode { get; set; }
    }
}
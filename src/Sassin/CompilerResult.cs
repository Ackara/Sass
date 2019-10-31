namespace Acklann.Sassin
{
    public class CompilerResult
    {
        public bool Success { get; set; }

        public string SourceFile { get; set; }

        public string OutputFile
        {
            get
            {
                if (GeneratedFiles?.Length > 0)
                    return GeneratedFiles[GeneratedFiles.Length - 1];
                else
                    return null;
            }
        }

        public string[] GeneratedFiles { get; set; }

        public CompilerError[] Errors { get; set; }
    }
}
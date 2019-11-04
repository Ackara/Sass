namespace Acklann.Sassin
{
    public class CompilerOptions
    {
        public CompilerOptions()
        {
            Suffix = ".min";
            Minify = GenerateSourceMaps = AddSourceComments = true;
        }

        public bool Minify { get; set; }
        public string Suffix { get; set; }
        public string OutputDirectory { get; set; }
        public bool KeepIntermediateFiles { get; set; }

        public string SourceMapDirectory { get; set; }
        public bool GenerateSourceMaps { get; set; }
        public bool AddSourceComments { get; set; }

        public string ToArgs()
        {
            string toJs(bool bit) => (bit ? "true" : "false");
            string escape(object obj) => string.Concat('"', obj, '"');

            return string.Concat(
                escape(OutputDirectory), " ",
                escape(SourceMapDirectory), " ",
                
                escape(Suffix), " ",
                toJs(Minify), " ",

                toJs(KeepIntermediateFiles), " ",
                toJs(GenerateSourceMaps), " ",
                toJs(AddSourceComments)
                );
        }
    }
}
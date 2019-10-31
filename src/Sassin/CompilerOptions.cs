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

        public bool KeepIntermediateFiles { get; set; }

        public bool AddSourceComments { get; set; }

        public bool GenerateSourceMaps { get; set; }

        public string SourceMapDirectory { get; set; }

        public string Suffix { get; set; }

        public override string ToString()
        {
            string quote(object obj) => string.Concat('"', obj, '"');
            string format(bool bit) => (bit ? "true" : "false");

            return string.Concat(Minify);
        }
    }
}
namespace Acklann.Sassin
{
    public class CompilerOptions
    {
        public CompilerOptions()
        {
            Minify = GenerateSourceMaps = AddSourceComments = true;
        }

        public const string DEFAULT_NAME = "sassconfig.json";

        public string OutputDirectory { get; set; }
        public string ConfigurationFile { get; set; }
        public string SourceMapDirectory { get; set; }

        public bool Minify { get; set; }
        public bool AddSourceComments { get; set; }
        public bool GenerateSourceMaps { get; set; }

        public string ToArgs()
        {
            string toJs(bool bit) => (bit ? "true" : "false");
            string escape(object obj) => string.Concat('"', obj, '"');

            return string.Concat(
                /* 3 */escape(ConfigurationFile), ' ',
                /* 4 */escape(OutputDirectory), ' ',
                /* 5 */escape(SourceMapDirectory), ' ',

                /* 6 */toJs(Minify), ' ',
                /* 7 */toJs(GenerateSourceMaps), ' ',
                /* 8 */toJs(AddSourceComments)
                );
        }
    }
}
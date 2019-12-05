using Microsoft.VisualStudio.Shell;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Acklann.Sassin
{
    [Guid("d89d3fa9-f433-473f-9675-b6062c5d56b1")]
    public class ConfigurationPage : DialogPage
    {
        public ConfigurationPage()
        {
            GenerateSourceMap = Minify = AddSourceMapComments = ShouldShowErrors = true;
        }

        public const string Catagory = "General";
        public static string ConfigurationFileDefaultName = CompilerOptions.DEFAULT_NAME;
        public static bool ShouldGenerateSourceMap, ShouldMinifyFile, ShouldAddSourceMapComment, ShouldShowErrors;

        [Category(Catagory)]
        [DisplayName("Conguration File Default Name")]
        [Description("The default name of the configuration file.")]
        public string ConfigDefaultName
        {
            get => ConfigurationFileDefaultName;
            set { ConfigurationFileDefaultName = value; }
        }

        [Category(Catagory)]
        [DisplayName("Add Source Map Comments")]
        [Description("Determines whether to add the line number and file where a selector is defined to be emitted into the compiled CSS as a comment. Useful for debugging, especially when using imports and mixins.")]
        public bool AddSourceMapComments
        {
            get => ShouldAddSourceMapComment;
            set { ShouldAddSourceMapComment = value; }
        }

        [Category(Catagory)]
        [DisplayName("Generate Source Map")]
        [Description("Determines whether to create a source-map (.map) file for debugging.")]
        public bool GenerateSourceMap
        {
            get => ShouldGenerateSourceMap;
            set { ShouldGenerateSourceMap = value; }
        }

        [Category(Catagory)]
        [DisplayName("Minify")]
        [Description("Determines whether to optimize the .css file after compilation.")]
        public bool Minify
        {
            get => ShouldMinifyFile;
            set { ShouldMinifyFile = value; }
        }

        [Category(Catagory)]
        [DisplayName("Show Error")]
        [Description("When enabled compilation errors will appear in the error list.")]
        public bool ShowErrors
        {
            get => ShouldShowErrors;
            set { ShouldShowErrors = true; }
        }
    }
}
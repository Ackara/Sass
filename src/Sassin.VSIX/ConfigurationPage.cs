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
            GenerateSourceMap = Minify = AddSourceMapComments = true;
        }

        public static bool ShouldGenerateSourceMap, ShouldMinifyFile, ShouldAddSourceMapComment;

        [Category(General)]
        [DisplayName("Generate Source Map")]
        [Description("")]
        public bool GenerateSourceMap
        {
            get => ShouldGenerateSourceMap;
            set { ShouldGenerateSourceMap = value; }
        }

        [Category(General)]
        [DisplayName("Minify")]
        [Description("")]
        public bool Minify
        {
            get => ShouldMinifyFile;
            set { ShouldMinifyFile = value; }
        }

        [Category(General)]
        [DisplayName("Add Source Map Comments")]
        [Description("")]
        public bool AddSourceMapComments
        {
            get => ShouldAddSourceMapComment;
            set { ShouldAddSourceMapComment = value; }
        }

        #region Backing Members

        private const string General = "General";

        #endregion Backing Members
    }
}
using System;
using System.IO;
using System.Linq;

namespace Acklann.Sassin
{
	internal static partial class Sample
	{
		public const string FOLDER_NAME = "samples";

		public static string DirectoryName => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, FOLDER_NAME);
		
		public static FileInfo GetFile(string fileName, string directory = null)
        {
            fileName = Path.GetFileName(fileName);
            string searchPattern = $"*{Path.GetExtension(fileName)}";

            string targetDirectory = directory?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, FOLDER_NAME);
            return new DirectoryInfo(targetDirectory).EnumerateFiles(searchPattern, SearchOption.AllDirectories)
                .First(x => x.Name.Equals(fileName, StringComparison.CurrentCultureIgnoreCase));
        }

		public static FileInfo GetAppSCSS() => GetFile(@"app.scss");
		public static FileInfo GetButtonsSCSS() => GetFile(@"base\_buttons.scss");
		public static FileInfo GetLayoutSCSS() => GetFile(@"base\_layout.scss");
		public static FileInfo GetResetSCSS() => GetFile(@"base\_reset.scss");
		public static FileInfo GetStatesSCSS() => GetFile(@"base\_states.scss");
		public static FileInfo GetTypographySCSS() => GetFile(@"base\_typography.scss");
		public static FileInfo GetAllSCSS() => GetFile(@"modules\_all.scss");
		public static FileInfo GetColorsSCSS() => GetFile(@"modules\_colors.scss");
		public static FileInfo GetMixinsSCSS() => GetFile(@"modules\_mixins.scss");

		public struct File
		{
			public const string AppSCSS = @"app.scss";
			public const string ButtonsSCSS = @"base\_buttons.scss";
			public const string LayoutSCSS = @"base\_layout.scss";
			public const string ResetSCSS = @"base\_reset.scss";
			public const string StatesSCSS = @"base\_states.scss";
			public const string TypographySCSS = @"base\_typography.scss";
			public const string AllSCSS = @"modules\_all.scss";
			public const string ColorsSCSS = @"modules\_colors.scss";
			public const string MixinsSCSS = @"modules\_mixins.scss";
		}
	}	
}

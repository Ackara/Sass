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

		public static FileInfo GetBasicSCSS() => GetFile(@"basic.scss");
		public static FileInfo GetError7SCSS() => GetFile(@"error-7.scss");
		public static FileInfo GetIndexHTML() => GetFile(@"index.html");

		public struct File
		{
			public const string BasicSCSS = @"basic.scss";
			public const string Error7SCSS = @"error-7.scss";
			public const string IndexHTML = @"index.html";
		}
	}	
}

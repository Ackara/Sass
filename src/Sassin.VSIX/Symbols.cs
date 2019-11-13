using System;

namespace Acklann.Sassin
{
	public static class Symbols
	{
		public const string ProductName = "$(Product)";

		public struct Package
		{
			public const string GuidString = "235471ab-a770-4d0e-b00b-4f80bb01f200";
			public static readonly Guid Guid = new Guid("235471ab-a770-4d0e-b00b-4f80bb01f200");
		}
		public struct CmdSet
		{
			public const string GuidString = "f5f3a3ba-35a2-4f98-a0f3-bd33eefeb63d";
			public static readonly Guid Guid = new Guid("f5f3a3ba-35a2-4f98-a0f3-bd33eefeb63d");
			public const int VSMainMenuGroup = 0x101;
			public const int MiscGroup = 0x102;
			public const int FileCommandGroup = 0x103;
			public const int ProjectCommandGroup = 0x104;
			public const int MainMenuId = 0x0201;
			public const int GotoSettingsCommandId = 0x0500;
			public const int CompileSassFileCommandId = 0x0501;
			public const int InstallNugetPackageCommandId = 0x0502;
		}
	}
}

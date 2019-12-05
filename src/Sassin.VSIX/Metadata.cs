using System;

namespace Acklann.Sassin
{
	public static class Metadata
	{
		public const string ProductName = "Sassin";
		
		public const string Version = "0.0.20";
		
		public const string Description = "A sass compiler.";

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
			public const int MiscellaneousGroup = 0x102;
			public const int MainCommandGroup = 0x103;
			public const int MainMenu = 0x0201;
			public const int GotoSettingsCommandId = 0x0500;
			public const int CompileSassFileCommandId = 0x0501;
			public const int InstallNugetPackageCommandId = 0x0502;
		}
	}
}

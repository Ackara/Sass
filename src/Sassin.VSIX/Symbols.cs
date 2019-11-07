using System;

namespace Acklann.Sass
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
			public const int MyMenuGroup = 0x1020;
			public const int CompileSassCommandId = 0x0100;
		}
		public struct guidImages
		{
			public const string GuidString = "604adb86-42fb-4488-ae34-e6568b451ede";
			public static readonly Guid Guid = new Guid("604adb86-42fb-4488-ae34-e6568b451ede");
			public const int bmpPic1 = 1;
			public const int bmpPic2 = 2;
			public const int bmpPicSearch = 3;
			public const int bmpPicX = 4;
			public const int bmpPicArrows = 5;
			public const int bmpPicStrikethrough = 6;
		}
	}
}

using System.Collections.Generic;

namespace TexPacker
{
	class Config
	{
		public string IdName = "";

		public List<string> Directories = new List<string>();
		public string Output = "";

		public string Name = "Atlas";
		public string FileFilter = "*";
		public bool IncludeSubDirectories = false;
		public bool AllowRotations = true;

		public int AtlasWidth = 4096;
		public int AtlasHeight = 4096;
	}
}
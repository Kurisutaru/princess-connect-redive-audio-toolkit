using System.Collections.Generic;
using System.IO;

namespace Princess_Connect_ReDive_Audio_Toolkit.Class
{
	public class Variable
	{
		public string dmmDataFolder { get; set; }
		public string bgmFolder { get; set; }
		public string extractFolder { get; set; }
		public string mainDirectory = Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
		public IList<HashedItem> hashedItems { get; set; } = new List<HashedItem>();
		public IList<SqliteItem> SqliteItems { get; set; } = new List<SqliteItem>();
	}
}
using Microsoft.Data.Sqlite;
using Princess_Connect_ReDive_Audio_Toolkit.Class;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading;
using static System.Environment;

namespace Princess_Connect_ReDive_Audio_Toolkit
{
	internal class Program
	{
		private static void Main(string[] args)
		{
			Variable variable = new Variable();
			//Preset get Purikone Folder at LocalLow for DMM
            Guid localLowId = new Guid("A520A1A4-1780-4FF6-BD18-167343C5AF16");
			String localLowFolder = Path.Combine(GetKnownFolderPath(localLowId), "CyGames", "PrincessConnectReDive");
			
            WriteLine(@" ________   ___  ___   ________   ___   ___  __     ________   ________    _______      ");
			WriteLine(@"|\   __  \ |\  \|\  \ |\   __  \ |\  \ |\  \|\  \  |\   __  \ |\   ___  \ |\  ___ \     ");
			WriteLine(@"\ \  \|\  \\ \  \\\  \\ \  \|\  \\ \  \\ \  \/  /|_\ \  \|\  \\ \  \\ \  \\ \   __/|    ");
			WriteLine(@" \ \   ____\\ \  \\\  \\ \   _  _\\ \  \\ \   ___  \\ \  \\\  \\ \  \\ \  \\ \  \_|/__  ");
			WriteLine(@"  \ \  \___| \ \  \\\  \\ \  \\  \|\ \  \\ \  \\ \  \\ \  \\\  \\ \  \\ \  \\ \  \_|\ \ ");
			WriteLine(@"   \ \__\     \ \_______\\ \__\\ _\ \ \__\\ \__\\ \__\\ \_______\\ \__\\ \__\\ \_______\");
			WriteLine(@"    \|__|      \|_______| \|__|\|__| \|__| \|__| \|__| \|_______| \|__| \|__| \|_______|");
			WriteLine("                                                                 ");
			WriteLine("Princess Connect Re:Dive! Audio Toolkit");
			WriteLine("Version 0.0.0.1 Final");
			WriteLine("©Kurisutaru 2020");
			WriteLine("https://www.kurisutaru.net/");
			WriteLine("────────────────────────────────────────────────────────────────────────────────────────────────────");
            WriteLine($"Detected Folder Path : {localLowFolder}");
            WriteLine("If you feel those path were correct, just press enter");
            Write($"Please input DMM Data Folder path : ");
			string tmpPath = ReadLine();

			if(String.IsNullOrEmpty(tmpPath))
			{
				tmpPath = localLowFolder;
            }

			if (!Directory.Exists(tmpPath))
			{
				Exit("Invalid Path . . .");
			}
			variable.dmmDataFolder = tmpPath;
			variable.bgmFolder = Path.Combine(tmpPath, "b");

			if (!File.Exists(Path.Combine(tmpPath, "manifest.db")) || !Directory.Exists(variable.bgmFolder))
			{
				Exit("Manifest data not found or BGM director not exist !");
			}

			#region Processing Step 1. Hashing files

			try
			{
				WriteLine(" - Processing Step 1. Hashing files -");

				IEnumerable<string> files = Directory.EnumerateFiles(variable.bgmFolder, "*", SearchOption.TopDirectoryOnly);

				foreach (string file in files)
				{
					HashedItem hashedItem = new HashedItem()
					{
						Filename = Path.GetFileName(file),
						Hash = CalculateMD5(file)
					};

					WriteLine($"Filename : {hashedItem.Filename} with Hash : {hashedItem.Hash}");

					variable.hashedItems.Add(hashedItem);
				}
			}
			catch (Exception e)
			{
				WriteLine($"Crashing at Step 1 with Exception : {e.Message}. Ask Kuri for more info.");
			}

			#endregion Processing Step 1. Hashing files

			#region Processing Step 2. Getting Database data

			try
			{
				WriteLine("- Processing Step 2. Getting Database data -");

				WriteLine("- Copying database file -");

				if(File.Exists(Path.Combine(variable.dmmDataFolder, "manifest.kuri.db")))
				{
					Thread.Sleep(1);
					File.Delete(Path.Combine(variable.dmmDataFolder, "manifest.kuri.db"));
					Thread.Sleep(1);
				}

				File.Copy(Path.Combine(variable.dmmDataFolder, "manifest.db"), Path.Combine(variable.dmmDataFolder, "manifest.kuri.db"), true);

				string connString = string.Format("Data Source={0};", Path.Combine(variable.dmmDataFolder, "manifest.kuri.db"));

				WriteLine($"Connecting String : {connString}");

				using (SqliteConnection connection = new SqliteConnection(connString))
				{
					connection.Open();

					var selectCommand = connection.CreateCommand();
					selectCommand.CommandText = "SELECT [k] AS [Filename], [v] AS [Hash] FROM [t] WHERE [k] LIKE 'b/%' ORDER BY [k] ASC";
					using (var reader = selectCommand.ExecuteReader())
					{
						while (reader.Read())
						{
							SqliteItem sqliteItem = new SqliteItem()
							{
								DatabaseFilename = reader.GetString(0).Remove(0, 2),
								Hash = reader.GetString(1)
							};

							WriteLine($"Filename : {sqliteItem.DatabaseFilename} with Hash : {sqliteItem.Hash}");

							variable.SqliteItems.Add(sqliteItem);
						}
					}

					connection.Close();
				}
			}
			catch (Exception e)
			{
				WriteLine($"Crashing at Step 2 with Exception : {e.Message}. Ask Kuri for more info.");
			}

			#endregion Processing Step 2. Getting Database data

			#region Processing Step 3. Rename Files

			try
			{
				WriteLine("- Processing Step 3. Rename Files -");

				variable.extractFolder = Path.Combine(variable.mainDirectory, "kuri");

                //Lamba Dropped because I'm fking dumb with complex stuff lmao, see replacement below v
                //IEnumerable<MergedItem> result = variable.SqliteItems
                //	.Select(x => new MergedItem
                //	{
                //		DatabaseFilename = x.DatabaseFilename,
                //		Hash = x.Hash
                //	});

                List<MergedItem> mergedItems = (from sqliteItem in variable.SqliteItems
																							join hashItem in variable.hashedItems
																							on sqliteItem.Hash equals hashItem.Hash
																							select
																							new MergedItem
																							{
																								DatabaseFilename = sqliteItem.DatabaseFilename,
																								Filename = hashItem.Filename,
																								Hash = hashItem.Hash
																							}).ToList();

                if (Directory.Exists(Path.Combine(variable.extractFolder)))
				{
					Directory.Delete(Path.Combine(variable.extractFolder), true);
				}

				Thread.Sleep(1);

				Directory.CreateDirectory(Path.Combine(variable.extractFolder));

                foreach (MergedItem item in mergedItems)
				{
					File.Copy(Path.Combine(variable.bgmFolder, item.Filename), Path.Combine(variable.extractFolder, item.DatabaseFilename), true);
					WriteLine($"Writing {item.Filename} into {item.DatabaseFilename}");
				}
			}
			catch (Exception e)
			{
				WriteLine($"Crashing at Step 3 with Exception : {e.Message}. Ask Kuri for more info.");
			}

			#endregion Processing Step 3. Rename Files

			WriteLine("- Processing Step 4. Completed -");
			WriteLine("- Thanks for using the toolkit ! -");

			ReadLine();
		}

		#region Utilities

		private static string CalculateMD5(string filePath)
		{
			using (var md5 = MD5.Create())
			{
				using (var stream = File.OpenRead(filePath))
				{
					var hash = md5.ComputeHash(stream);
					return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
				}
			}
		}

		public static void Exit(string msg)
		{
			Console.WriteLine($"{msg}. Existing Application . . .");
			Console.Read();
			Environment.Exit(0);
		}

		public static void WriteLine(string msg = "")
		{
			Console.WriteLine(msg);
		}

		public static void Write(string msg)
		{
			Console.Write(msg);
		}

		public static string ReadLine()
		{
			return Console.ReadLine();
		}

        public static string GetKnownFolderPath(Guid knownFolderId)
        {
            IntPtr pszPath = IntPtr.Zero;
            try
            {
                int hr = SHGetKnownFolderPath(knownFolderId, 0, IntPtr.Zero, out pszPath);
                if (hr >= 0)
                    return Marshal.PtrToStringAuto(pszPath);
                throw Marshal.GetExceptionForHR(hr);
            }
            finally
            {
                if (pszPath != IntPtr.Zero)
                    Marshal.FreeCoTaskMem(pszPath);
            }
        }

        [DllImport("shell32.dll")]
        static extern int SHGetKnownFolderPath([MarshalAs(UnmanagedType.LPStruct)] Guid rfid, uint dwFlags, IntPtr hToken, out IntPtr pszPath);

        #endregion Utilities

    }
}
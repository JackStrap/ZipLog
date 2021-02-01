using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using ICSharpCode.SharpZipLib.Zip;

namespace JackStrap.ZipLog
{
	class ZipLog
	{
		public static int cntArcFiles = 0;
		public static StringBuilder sbLog = new StringBuilder();

		static void Main(string[] args)
		{
			// Default value to apply
			string logFileName = "zipLog.log";
			string zipFileName = "zipLogs";
			string zipFileArg = "-w";
			string archiPath = Directory.GetCurrentDirectory();
			string archiExt = ".log";
			int daysToKeep = 30;
			DateTime dtStart = DateTime.Now;

			if (args.Length < 1)
			{
				ShowUsage("Must pass at least 1 argument.", "");
				Environment.Exit(1);
			}

			Console.WriteLine("");
			sbLog.AppendFormat("\t\t---------  {0}  -----------", DateTime.Now.ToString());

			// get passed arguments
			string[] inputParam = GetCommandLineArgs();
			// check passed arguments
			string retMsg = CheckParam(inputParam);

			switch (retMsg)
			{
				case "switch":
					sbLog.AppendFormat("\r\n\r\nERROR: «{0}» isn't a good parameter.", inputParam[0]);
					ShowUsage("ERROR: {} isn't a good parameter", inputParam[0]);

					break;
				case "path":
					sbLog.AppendFormat("\r\n\r\n«{0}» doesn't exist.", inputParam[1]);
					ShowUsage("ERROR: {} doesn't exist", inputParam[1]);
					
					break;
				case "days":
					sbLog.AppendFormat("\r\n\r\nERROR: «{0}» isn't a number.", inputParam[2]);
					ShowUsage("ERROR: {} isn't a number", inputParam[2]);

					break;
				case "ok":
					//default:
					zipFileArg = inputParam[0];

					if (inputParam.Length > 1 && !string.IsNullOrEmpty(inputParam[1]))
					{
						archiPath = inputParam[1];
					}

					if (inputParam.Length > 2 && !string.IsNullOrEmpty(inputParam[2]))
					{ 
						daysToKeep = int.Parse(inputParam[2]); 
					}
					
					if (inputParam.Length > 3 && !string.IsNullOrEmpty(inputParam[3]))
					{
						if (inputParam[3].StartsWith("."))
						{
							archiExt = inputParam[3];
						}
						else
						{
							archiExt = string.Concat(".", inputParam[3]);
						}
					}
					
					if (inputParam.Length > 4 && !string.IsNullOrEmpty(inputParam[4]))
					{
						zipFileName = inputParam[4];
						logFileName = String.Concat(inputParam[4], ".log");
					}

					break;
			}

			if (retMsg.Equals("ok"))
			{
				sbLog.AppendFormat("\r\n\r\nDirectory to archive :\t{0}\r\nDays to keep files :\t{1}\r\nextension to archive :\t{2}\r\n",
					archiPath,
					daysToKeep,
					archiExt);

				try
				{
					// set the name of zip file to create
					zipFileName = CreateFileName(zipFileName, zipFileArg);

					// check if zip exists then Create or Update the zip file
					retMsg = ZipSomeFiles(zipFileName, archiPath, daysToKeep, archiExt);
				}
				catch (Exception ex)
				{
					sbLog.AppendFormat("\r\n\r\nThe process failed: {0}", ex);

				}

				Console.WriteLine("");
				sbLog.AppendFormat("\r\n\r\n\t\t---------  {0}  -----------", DateTime.Now.ToString());
				sbLog.Append(retMsg);
				sbLog.AppendFormat("\r\n\r\nA total of {0} files were archive, in {1} hours\r\n",
					cntArcFiles, DateTime.Now.Subtract(dtStart).ToString());
			}
			else
			{
				sbLog.AppendFormat("\r\n\r\n\t\t---------  {0}  -----------", DateTime.Now.ToString());
			}

			// create the log file
			CreateLogFile(sbLog, logFileName);

		}

		/***************************************************************************/

		/// <summary>
		///	Write to the console how to use the program
		/// </summary>
		/// <param name="errMsg"></param>
		/// <param name="param"></param>
		private static void ShowUsage(string errMsg, string param)
		{
			Console.WriteLine("\r\n-------------------------------------------------");
			Console.WriteLine("{0}", errMsg.Replace("{}", param));

			Console.WriteLine("\r\nUsage: ZipLog {options} \"<pathToArchive>\", <daysToKeep>, <extension>, <zipName>");
			Console.WriteLine("");
			Console.WriteLine("Options:");
			Console.WriteLine("\t-d                      Zip filename daily");
			Console.WriteLine("\t-w                      Zip filename weekly");
			Console.WriteLine("\t-m                      Zip filename monthly");
			Console.WriteLine("");
			Console.WriteLine("other parameters, if not passed.");
			Console.WriteLine("\t<pathToArchive>         CurrentPath");
			Console.WriteLine("\t<daysToKeep>            30 daysToKeep");
			Console.WriteLine("\t<extension>             .log Extension");
			Console.WriteLine("\t<zipName>               Name of zip file to produce");
		}


		/// <summary>
		///	For example, when a program is started with: test.exe "c:\"
		///	args[1] will be c:" while it should be c:\
		/// </summary>
		/// <returns></returns>
		public static string[] GetCommandLineArgs()
		{
			string[] args = Environment.GetCommandLineArgs();
			string retVal = string.Empty;

			//// for dev
			//Console.WriteLine("\r\nArgs.Length: {0}", args.Length);
			//foreach (string retArg in args)
			//{
			//	Console.WriteLine("Args: {0}", retArg);
			//}
			//// for dev

			for (int i = 1; i < args.Length; i++)
			{
				// pass all arguments into a string
				string arg = args[i];
				if (arg.Trim() == ",")
				{
					arg = "empty,";
				}

				// split string with comma has delimiter
				string[] parts = arg.Split(',');

				for (int j = 0; j < parts.Length; j++)
				{
					if (!string.IsNullOrEmpty(parts[j]))
					{
						if (parts[j].EndsWith("\""))
						{
							parts[j] = parts[j].Substring(0, parts[j].Length - 1);
						}

						if (string.IsNullOrEmpty(retVal))
						{
							retVal = parts[j].TrimStart();
						}
						else
						{
							retVal = string.Concat(retVal, ";", parts[j].TrimStart());
						}
					}
				}
			}
			string[] retArgs = retVal.Split(';');
			//// for dev
			//Console.WriteLine("\r\nretArg.Length: {0}", retArgs.Length);
			//foreach (string retArg in retArgs)
			//{
			//	Console.WriteLine("retArg: {0}", retArg);
			//}
			//// for dev
			return retArgs;
		}


		/// <summary>
		///	Check value of paramaters
		/// </summary>
		/// <param name="inputParam"></param>
		/// <returns></returns>
		private static string CheckParam(string[] inputParam)
		{
			// check length of 1st argument
			if (inputParam[0].Length == 2)
			{
				if (inputParam[0][0].ToString().Equals("-"))
				{
					//check if 2nd char is(d, w, m)
					if (!inputParam[0][1].ToString().Equals("d", StringComparison.OrdinalIgnoreCase))
						if (!inputParam[0][1].ToString().Equals("w", StringComparison.OrdinalIgnoreCase))
							if (!inputParam[0][1].ToString().Equals("m", StringComparison.OrdinalIgnoreCase))
								return "switch";
				}
				else
				{
					return "switch";
				}
			}
			else
			{
				return "switch";
			}

			if (inputParam.Length > 1)
			{
				if (inputParam[1] == "empty")
				{
					inputParam[1] = Directory.GetCurrentDirectory();
				}

				if (!Directory.Exists(inputParam[1]))
				{
					return "path";
				}
			}
			
			if (inputParam.Length > 2)
			{
				try { int.Parse(inputParam[2]); }
				catch { return "days"; }
			}
			
			//if (inputParam.Length > 3)
			//{
			//	// check fileExtension
			//}
			
			//if (inputParam.Length > 4)
			//{
			//	// check zip filename
			//}

			return "ok";

		}


		/// <summary>
		///	Set the zip fileName according to the passed argument
		/// </summary>
		/// <param name="fileName"></param>
		/// <param name="fileArg"></param>
		/// <returns></returns>
		private static string CreateFileName(string fileName, string fileArg)
		{

			string strYear, strMonth, strDay, strWeekOfYear;
			int dayOfYear;

			strYear = DateTime.Now.Year.ToString();
			strMonth = DateTime.Now.Month.ToString();
			strDay = DateTime.Now.Day.ToString();
			dayOfYear = (DateTime.Now.DayOfYear / 7) + 1;
			strWeekOfYear = dayOfYear.ToString();

			switch (fileArg)
			{
				case "-d":
					if (strDay.Length < 2)
						strDay = string.Concat("0", strDay);

					if (strMonth.Length < 2)
						strMonth = string.Concat("0", strMonth);

					fileName = string.Concat(
						fileName, "D",
						strYear,
						strMonth,
						strDay,
						".zip");
					break;
				case "-m":
					if (strMonth.Length < 2)
						strMonth = string.Concat("0", strMonth);

					fileName = string.Concat(
						fileName, "M",
						strYear,
						strMonth,
						".zip");
					break;
				case "-w":
					if (strWeekOfYear.Length < 2)
						strWeekOfYear = string.Concat("0", strWeekOfYear);

					fileName = string.Concat(
						fileName, "W",
						strYear,
						strWeekOfYear,
						".zip");
					break;
			}
			return fileName;
		}


		/// <summary>
		/// Check if create or update zip file
		/// </summary>
		/// <param name="zipName"></param>
		/// <param name="backDirName"></param>
		/// <param name="daysToKeep"></param>
		/// <param name="fileExt"></param>
		/// <returns></returns>
		public static string ZipSomeFiles(string zipName, string backDirName, int daysToKeep, string fileExt)
		{
			bool retVal;
			string logEntry;

			if (File.Exists(zipName))
			{
				// Update a existing zip file.
				retVal = UpdateZipFile(zipName, backDirName, daysToKeep, fileExt);
				if (retVal)
					logEntry = string.Format("\r\n\r\nzip file \"{0}\" updated succesfully!", zipName);
				else
					logEntry = string.Format("\r\n\r\nzip file \"{0}\" NOT updated! ERROR!", zipName);

				return logEntry;
			}

			// Create the zip file.
			retVal = CreateZipFile(zipName, backDirName, daysToKeep, fileExt);
			if (retVal)
				logEntry = string.Format("\r\n\r\nzip file \"{0}\" created succesfully!", zipName);
			else
				logEntry = string.Format("\r\n\r\nzip file \"{0}\" NOT created! ERROR!", zipName);

			return logEntry;

		}


		/// <summary>
		/// Create a new zip file
		/// </summary>
		/// <param name="zipName"></param>
		/// <param name="backDirName"></param>
		/// <param name="daysToKeep"></param>
		/// <param name="fileExt"></param>
		/// <returns></returns>
		private static bool CreateZipFile(string zipName, string backDirName, int daysToKeep, string fileExt)
		{
			try
			{
				// Depending on the directory this could be very large and would require more attention
				string[] filenames = Directory.GetFiles(backDirName);

				// 'using' statements guarantee the stream is closed properly which is a big source
				// of problems otherwise.  Its exception safe as well which is great.
				using (ZipOutputStream zipStream = new ZipOutputStream(File.Create(zipName)))
				{
					// 0 - store only to 9 - means best compression
					zipStream.SetLevel(9);

					// To unpacked by built-in extractor, Specify UseZip64.Off, or set the Size.
					zipStream.UseZip64 = UseZip64.Dynamic;

					byte[] buffer = new byte[4096]; // maximum of file size to read 4GB

					foreach (string file in filenames)
					{
						FileInfo archiFI = new FileInfo(file);

						if (archiFI.Extension == fileExt)
						{
							DateTime dtThen = new DateTime(archiFI.LastWriteTime.Ticks);

							if (DateTime.Now.Subtract(dtThen).Days >= daysToKeep)
							{
								// Using GetFileName as the resulting path is not absolute.
								ZipEntry entry = new ZipEntry(Path.GetFileName(file))
								{
									DateTime = dtThen
								};
								zipStream.PutNextEntry(entry);

								using (FileStream fs = File.OpenRead(file))
								{
									int sourceBytes;
									do
									{
										sourceBytes = fs.Read(buffer, 0, buffer.Length);
										zipStream.Write(buffer, 0, sourceBytes);
									} while (sourceBytes > 0);
								}

								archiFI.Delete();

								cntArcFiles += 1;

								Console.Write(".");
								sbLog.AppendFormat("\r\nAdding ==> {0} - {1}", archiFI.Name, DateTime.Now.TimeOfDay);
							}
						}
					}
					// Finish is important without this the created file would be invalid.
					zipStream.Finish();
					// Close is important to wrap things up and unlock the file.
					zipStream.Close();

				} // end Using

				// if cntArcFiles = 0 then delete the zip file cause it' empty.
				if (cntArcFiles == 0)
				{
					FileInfo archiFI = new FileInfo(zipName);
					archiFI.Delete();
				}

				return true;
			}
			catch (Exception ex)
			{
				sbLog.AppendFormat("\r\n\r\nERROR: {0}", ex);
				return false;
			}
		}


		/// <summary>
		/// Update a existing zip file
		/// </summary>
		/// <param name="zipName"></param>
		/// <param name="backDirName"></param>
		/// <param name="daysToKeep"></param>
		/// <param name="fileExt"></param>
		/// <returns></returns>
		private static bool UpdateZipFile(string zipName, string backDirName, int daysToKeep, string fileExt)
		{
			try
			{
				// Depending on the directory this could be very large and would require more attention
				string[] filenames = Directory.GetFiles(backDirName);

				// 'using' statements guarantee the stream is closed properly which is a big source
				// of problems otherwise.  Its exception safe as well which is great.
				using (ZipFile zFile = new ZipFile(zipName))
				{
					// To unpacked by built-in extractor specify UseZip64.Off, or set the Size.
					zFile.UseZip64 = UseZip64.Off;

					//zFile.BeginUpdate();

					foreach (string file in filenames)
					{
						FileInfo archiFI = new FileInfo(file);

						if (archiFI.Extension == fileExt)
						{
							DateTime dtThen = new DateTime(archiFI.LastWriteTime.Ticks);

							if (DateTime.Now.Subtract(dtThen).Days >= daysToKeep)
							{
								// prepare zip for update
								zFile.BeginUpdate();

								// Could also use the now or similar time for the file.
								zFile.EntryFactory = new ZipEntryFactory(dtThen);
								// Use Path.GetFileName as the resulting path is not absolute.
								zFile.Add(@file, Path.GetFileName(file));

								// Commit the add file
								zFile.CommitUpdate();
								// delete file
								archiFI.Delete();

								cntArcFiles += 1;

								Console.Write(".");
								sbLog.AppendFormat("\r\nUpdating ==> {0} - {1}", archiFI.Name, DateTime.Now.TimeOfDay);
							}
						}

					}
					//zFile.CommitUpdate();
					zFile.IsStreamOwner = false;
					zFile.Close(); // Close is important to wrap things up and unlock the file.

				} // end Using

				return true;
			}
			catch (Exception ex)
			{
				sbLog.AppendFormat("\r\n\r\nThe process failed: {0}", ex);
				return false;
			}

		}


		/// <summary>
		/// Create Log File
		/// </summary>
		/// <param name="logToWrite"></param>
		/// <param name="logFileName"></param>
		public static void CreateLogFile(StringBuilder logToWrite, string logFileName)
		{
			try
			{
				using (StreamWriter outfile = new StreamWriter(logFileName))
				{
					outfile.Write(logToWrite.ToString());
				}
			}
			catch (Exception ex)
			{
				WriteEventLog(ex);
			}
		}


		/// <summary>
		/// Write to EventLog
		/// </summary>
		/// <param name="ex"></param>
		public static void WriteEventLog(Exception ex)
		{
			string sSource, sLog, sEvent;

			sSource = Process.GetCurrentProcess().ProcessName;
			sLog = "Application";
			sEvent = ex.ToString();

			if (!EventLog.SourceExists(sSource))
				EventLog.CreateEventSource(sSource, sLog);

			EventLog.WriteEntry(sSource, sEvent, EventLogEntryType.Error);

		}


	}
}

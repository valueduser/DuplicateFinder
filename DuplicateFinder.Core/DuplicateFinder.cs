using System.IO.Abstractions;
using DuplicateFinder.Models;
using System.Collections.Concurrent;
using System.Net;
using File = DuplicateFinder.Models.File;

namespace DuplicateFinder.Core
{
	public class DuplicateFinder
	{
		FileHelpers fileHelpers;

		public DuplicateFinder()
		{
			fileHelpers = new FileHelpers(new FileSystem());
		}

		public void FindDuplicates(Options options)
		{
			ValidateOptions(options);
			//ConcurrentDictionary<string, List<File>> duplicates = dupeFinder.FindDuplicateFiles(job);
			//dupeFinder.ReportResults(duplicates);

			//TODO: Persist
			FindDuplicatesInternal(options);
			
			Console.ReadKey();
		}

		private void FindDuplicatesInternal(Options options)
		{
			foreach (Source source in options.Sources)
			{
				string[] filePaths;
				string authType = "Basic";

				if (source.IsLocalFileSystem)
				{
					filePaths = fileHelpers.WalkFilePaths(source);
					PopulateFileMetaData(source.Name, filePaths, duplicateDictionary);
				}
				else
				{
					System.Uri pathUri = new System.Uri(source.Path);
					NetworkCredential networkCred = new NetworkCredential(source.NetworkShareUser, source.NetworkSharePassword, source.NetworkShareDomain);
					CredentialCache netCache = new CredentialCache();
					netCache.Add(pathUri, authType, networkCred);
					filePaths = fileHelpers.WalkFilePaths(source);
					PopulateFileMetaData(source.Name, filePaths, duplicateDictionary);
					netCache.Remove(pathUri, authType);
				}
			}
		}

		internal ConcurrentDictionary<string, List<File>> PopulateFileMetaData(string sourceName, string[] files, ConcurrentDictionary<string, List<File>> duplicateDictionary)
		{
			Console.WriteLine("Populating metadata for discovered files...");

			bool isInConsole = IsConsoleApplication();

			var pb = isInConsole ? new ProgressBar(PbStyle.DoubleLine, files.Length) : null;
			int lastIncrement = 0;

			if (isInConsole) pb.Refresh(0, "Initializing...");

			int i = 0;
			foreach (string filePath in files)
			{
				// Only update the progress bar occasionally
				if (isInConsole && Math.Floor(i * 100.0 / files.Length) > lastIncrement || i == files.Length)
				{
					string tempFilePath = filePath;
					if (filePath.Contains("{"))
					{
						tempFilePath = tempFilePath.Replace("{", "");
					}
					if (filePath.Contains("}"))
					{
						tempFilePath = tempFilePath.Replace("}", "");
					}

					try
					{
						pb.Refresh(i, tempFilePath);
						lastIncrement = (i * 100 / files.Length);
					}
					catch (Exception e)
					{
						Console.WriteLine(e);
					}
				}

				if (!String.IsNullOrEmpty(filePath))
				{
					using FileUtilContext context = new FileUtilContext();

					long fileSize = _fileSystemHelper.GetFileSize(filePath);
					File tempFile = new File
					{
						Path = filePath,
						Name = _fileSystemHelper.GetFileName(filePath),
						SizeInKiloBytes = fileSize,
						Source = sourceName
					};

					var hashValue = _fileSystemHelper.GetHashedValue(filePath, fileSize, hashLimit);

					var existingHash = context.Hash
						.Where(h => h.Value == hashValue && h.IsPartial == hashLimit > 0)
						.FirstOrDefault();
					Hash tempHash = null;

					if (existingHash != null)
					{
						existingHash.ModifiedOn = DateTime.UtcNow;
						existingHash.HasDuplicate = true;
						tempFile.Hash = existingHash;
					}
					else
					{
						tempHash = new Hash
						{
							//todo: add option to hash only a portion of the file AND / OR check the files table. if the filename && size && path are the same as an entry in the files table, don't bother hashing (optionally) - just use the value from the table
							Value = hashValue,
							IsPartial = hashLimit > 0,
							HasDuplicate = false,
							CreatedOn = DateTime.UtcNow
						};
						tempFile.Hash = tempHash;
					}
					context.File.Add(tempFile);

					context.SaveChanges();

					//Ignore empty directory placeholder
					if (tempFile.Name == "_._")
					{
						continue;
					}

					duplicateDictionary.AddOrUpdate(tempFile.Hash.Value, new List<File>() { tempFile }, (key, value) => { value.Add(tempFile); return value; });
				}
				i++;
			}
			Console.WriteLine("\n...done.");
			return duplicateDictionary;
		}

		public void ValidateOptions(Options options)
		{
			options.Sources.ForEach(delegate (Source source)
			{
				if (!source.IsLocalFileSystem && (String.IsNullOrEmpty(source.NetworkShareUser) || String.IsNullOrEmpty(source.NetworkSharePassword)))
				{
					throw new ArgumentException("Remote Filesystem selected but credentials were missing.");
				}
			});
		}
	}
}
using System.IO.Abstractions;
using DuplicateFinder.Models;
using DuplicateFinder.Data.Data;
using System.Net;
using File = DuplicateFinder.Models.File;
using Spectre.Console;

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
			//TODO: Report results
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
					PopulateFileMetaData(source.Name, filePaths, options.Config.HashSizeLimitInKB);
				}
				else
				{
					System.Uri pathUri = new System.Uri(source.Path);
					NetworkCredential networkCred = new NetworkCredential(source.NetworkShareUser, source.NetworkSharePassword, source.NetworkShareDomain);
					CredentialCache netCache = new CredentialCache();
					netCache.Add(pathUri, authType, networkCred);
					filePaths = fileHelpers.WalkFilePaths(source);
					PopulateFileMetaData(source.Name, filePaths, options.Config.HashSizeLimitInKB);
					netCache.Remove(pathUri, authType);
				}
			}
		}

		internal void PopulateFileMetaData(string sourceName, string[] files, int hashLimit)
		{
			AnsiConsole.Markup("Populating metadata for discovered files...");

			foreach (string filePath in files)
			{
				if (!String.IsNullOrEmpty(filePath))
				{
					using DuplicateFinderContext context = new DuplicateFinderContext();

					string fileName = fileHelpers.GetFileName(filePath);
					if (fileName == "_._")
					{
						//Ignore empty directory placeholder
						continue;
					}

					long fileSize = fileHelpers.GetFileSize(filePath);

					File tempFile = new File
					{
						Path = filePath,
						Name = fileName,
						SizeInKiloBytes = fileSize,
						Source = sourceName
					};

					//TODO: add option to hash only a portion of the file AND / OR check the files table. if the filename && size && path are the same as an entry in the files table, don't bother hashing (optionally) - just use the value from the table
					var hashValue = fileHelpers.GetHashedValue(filePath, fileSize, hashLimit);

					//TODO: Handle situations where both the existing hash and the temp hash are partial / full or mixed
					var existingHash = context.Hash
						.Where(h => h.Value == hashValue && h.IsPartial == hashLimit > 0)
						.FirstOrDefault();

					if (existingHash != null)
					{
						existingHash.HasDuplicate = true;
						existingHash.ModifiedOn = DateTime.UtcNow;
						tempFile.Hash = existingHash;
					}
					else
					{
						Hash tempHash = new Hash
						{
							HasDuplicate = false,
							Value = hashValue,
							IsPartial = hashLimit > 0,
							CreatedOn = DateTime.UtcNow
						};
						tempFile.Hash = tempHash;
					}
					context.File.Add(tempFile);

					context.SaveChanges();
				}
			}
			AnsiConsole.Markup("\n...done.");
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
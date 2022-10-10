using System.IO.Abstractions;
using DuplicateFinder.Models;
using DuplicateFinder.Data.Data;
using System.Net;
using File = DuplicateFinder.Models.File;
using Spectre.Console;
using Options = DuplicateFinder.Models.Options;

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
			AnsiConsole.Status()
			.Spinner(Spinner.Known.Dots)
			.Start("Validating options...", ctx => {
				ValidateOptions(options);
			});
			
			FindDuplicatesInternal(options);
			using DuplicateFinderContext context = new DuplicateFinderContext(options.Config.PersistenceConfig);
			var numberOfDuplicates =
				(from h in context.Hash
				 join f in context.File
				 on h.Id equals f.HashId
				 where h.HasDuplicate == true
				 orderby f.HashId descending
				 select f).Count();

			AnsiConsole.WriteLine($"Found {numberOfDuplicates} duplicate files across all sources.");
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
					PopulateFileMetaData(source.Name, filePaths, options);
				}
				else
				{
					System.Uri pathUri = new System.Uri(source.Path);
					NetworkCredential networkCred = new NetworkCredential(source.NetworkShareUser, source.NetworkSharePassword, source.NetworkShareDomain);
					CredentialCache netCache = new CredentialCache();
					netCache.Add(pathUri, authType, networkCred);
					filePaths = fileHelpers.WalkFilePaths(source);
					PopulateFileMetaData(source.Name, filePaths, options);
					netCache.Remove(pathUri, authType);
				}
			}
		}

		internal void PopulateFileMetaData(string sourceName, string[] files, Options options)
		{
			int hashLimit = options.Config.HashSizeLimitInKB;
			AnsiConsole.Status()
			.Spinner(Spinner.Known.Dots)
			.Start($"Populating metadata for discovered files on {sourceName}...", ctx => {
				foreach (string filePath in files)
				{
					if (!String.IsNullOrEmpty(filePath))
					{
						using DuplicateFinderContext context = new DuplicateFinderContext(options.Config.PersistenceConfig);

						string fileName = fileHelpers.GetFileName(filePath);
						if (fileName == "_._")
						{
							//Ignore empty directory placeholder
							continue;
						}

						// TODO: check if this file path already exists unless Config.ForceRehash is true
						var alreadyFile = from f in context.File
										  where f.Path.Equals(filePath)
										  where f.Source.Equals(sourceName)
										  select f;


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

						var fileExists = context.File
						.Where(f =>
							f.SizeInKiloBytes == tempFile.SizeInKiloBytes &&
							f.Path == tempFile.Path && 
							f.Name == tempFile.Name &&
							f.Hash == tempFile.Hash
						).FirstOrDefault();
						if (fileExists == null)
						{
							context.File.Add(tempFile);
						}

						context.SaveChanges();
					}
				}
			});
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
using System.IO.Abstractions;
using DuplicateFinder.Models;
using DuplicateFinder.Data.Data;
using System.Net;
using File = DuplicateFinder.Models.File;
using Spectre.Console;
using Options = DuplicateFinder.Models.Options;
using SMBLibrary;
using SMBLibrary.Client;
using System.IO;
using System.Net.NetworkInformation;
using EzSmb;
using System.Xml.Linq;
using System.Net.Sockets;

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
			.Start("Validating options...", ctx =>
			{
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
				List<string> filePaths = new List<string>();
				string authType = "Basic";

				if (source.IsLocalFileSystem)
				{
					filePaths = fileHelpers.WalkFilePaths(source);
					PopulateFileMetaData(source.Name, filePaths, options);
				}
				else if (true)
				{
					Stack<Node> nodes = new Stack<Node>();
					IPAddress? serverIp = Dns.GetHostAddresses(source.ServerName).Where(address => address.AddressFamily == AddressFamily.InterNetwork).FirstOrDefault();
					var rootDirectoryNodes = Node.GetNode(@$"{serverIp}\{source.ShareName}", source.NetworkShareUser, source.NetworkSharePassword).GetAwaiter().GetResult()
						.GetList().GetAwaiter().GetResult().ToList(); // :\
					rootDirectoryNodes.ForEach(node => nodes.Push(node));

					while (nodes.Count > 0)
					{
						Node current = nodes.Pop();
						if (current.Type == NodeType.Folder)
						{
							var test = current.GetList().GetAwaiter().GetResult();
							var newNodes = test.ToList<Node>();
							newNodes.ForEach(node =>
							{
								if (node.Type == NodeType.Folder)
								{
									nodes.Push(node);
								}
								else
								{
									AnsiConsole.WriteLine($"it's a file {node.FullPath}");
									filePaths.Add(node.FullPath);
								}
							});
						}
					}
					AnsiConsole.WriteLine(filePaths.Count);

				}
				else if (false)
				{
					// TODO: Constants
					int DIRECTORY_FILE_TYPE = 48; //., .., folder, etc
					int HIDDEN_FILE_TYPE = 34; //.DS_Store
					int FILE_TYPE = 32;
					string[] DIRECTORY_IGNORE_LIST = { ".", ".." };

					SMB2Client client = new SMB2Client();
					IPAddress? serverIp = Dns.GetHostAddresses(source.ServerName).FirstOrDefault();

					bool isConnected = client.Connect(serverIp, SMBTransportType.DirectTCPTransport);
					if (isConnected)
					{
						NTStatus status = client.Login(string.Empty, source.NetworkShareUser, source.NetworkSharePassword);
						if (status == NTStatus.STATUS_SUCCESS)
						{
							ISMBFileStore fileStore = client.TreeConnect(source.ShareName, out status);
							object rootDirectoryHandle;
							FileStatus fileStatus;
							status = fileStore.CreateFile(out rootDirectoryHandle, out fileStatus, String.Empty, AccessMask.GENERIC_READ, SMBLibrary.FileAttributes.Directory, ShareAccess.Read | ShareAccess.Write, CreateDisposition.FILE_OPEN, CreateOptions.FILE_DIRECTORY_FILE, null);
							if (status == NTStatus.STATUS_SUCCESS)
							{
								List<QueryDirectoryFileInformation> fileList;
								status = fileStore.QueryDirectory(out fileList, rootDirectoryHandle, "*", FileInformationClass.FileDirectoryInformation);
								status = fileStore.CloseFile(rootDirectoryHandle);
								foreach (var item in fileList)
								{
									var fileInfo = (item as FileDirectoryInformation);
									AnsiConsole.WriteLine($"FileName:{fileInfo.FileName} Type:{fileInfo.FileAttributes} ({(int)fileInfo.FileAttributes})");
									if ((int)fileInfo.FileAttributes == DIRECTORY_FILE_TYPE && !DIRECTORY_IGNORE_LIST.Contains(fileInfo.FileName))
									{
										AnsiConsole.WriteLine("Pushin to stack");
									}
									else
									{
										AnsiConsole.WriteLine($"{fileInfo.FileName}: {(int)fileInfo.FileAttributes}");
									}
									//if (item.GetType == Type.Directory "Directory")
									//var fileInfo = (item as FileDirectoryInformation);
									//if (fileInfo != null)
									//{
									//	if(fileInfo.FileAttributes )
									//	AnsiConsole.WriteLine($"FileName:{fileInfo.FileName} Type:{fileInfo.FileAttributes}");
									//}
								}
							}
							status = fileStore.Disconnect();
						}

					}


					//             object directoryHandle = null;
					//             var status = client.Login(string.Empty, source.NetworkShareUser, source.NetworkSharePassword);
					//             var fileStore = client.TreeConnect(source.ShareName, out status);
					//             if (isConnected)
					//                 status = fileStore.CreateFile(out directoryHandle, out FileStatus fileStatus, string.Empty, AccessMask.GENERIC_READ, SMBLibrary.FileAttributes.Directory, ShareAccess.Read | ShareAccess.Write, CreateDisposition.FILE_OPEN, CreateOptions.FILE_DIRECTORY_FILE, null);
					//             if (status == NTStatus.STATUS_SUCCESS)
					//             {
					//                 List<QueryDirectoryFileInformation> test2;
					//                 status = fileStore.QueryDirectory(out test2, directoryHandle, "*", FileInformationClass.FileIdFullDirectoryInformation);
					//                 foreach (var item in test2)
					//                 {
					//                     var fileInfo = (item as FileDirectoryInformation);
					//if (fileInfo != null)
					//{
					//                         AnsiConsole.WriteLine($"FileName:{fileInfo.FileName} Type:{fileInfo.FileAttributes}");
					//                     }
					//                 }
					//             }
				}
				else
				{
					// windows only 
					System.Uri pathUri = new System.Uri(source.UncPath);
					NetworkCredential networkCred = new NetworkCredential(source.NetworkShareUser, source.NetworkSharePassword, source.NetworkShareDomain);
					CredentialCache netCache = new CredentialCache();
					netCache.Add(pathUri, authType, networkCred);
					filePaths = fileHelpers.WalkFilePaths(source);
					PopulateFileMetaData(source.Name, filePaths, options);
					netCache.Remove(pathUri, authType);
				}
			}
		}

		internal void PopulateFileMetaData(string sourceName, List<string> files, Options options)
		{
			int hashLimit = options.Config.HashSizeLimitInKB;
			AnsiConsole.Status()
			.Spinner(Spinner.Known.Dots)
			.Start($"Populating metadata for discovered files on {sourceName}...", ctx =>
			{
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


				// TODO: check if non-windows and throw if using UNC (or convert)

				if (String.IsNullOrEmpty(source.UncPath) && String.IsNullOrEmpty(source.ServerName))
				{
					throw new ArgumentException("");
				}
			});
		}
	}
}
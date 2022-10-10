using Microsoft.Extensions.Configuration;

namespace DuplicateFinder.Models
{
	public class Options
	{
		public List<Source> Sources { get; set; }
		public Config Config { get; set; }
		
		public Options()
		{
			Sources = new List<Source>();
			Config = new Config();
			var configuration = new ConfigurationBuilder()
					.SetBasePath(AppContext.BaseDirectory)
					.AddJsonFile("./appsettings.json")
			.Build();

			for (var i = 0; configuration[$"Sources:{i}:SourceName"] != null; i++)
			{
				Source source = new Source()
				{
					Name = configuration[$"Sources:{i}:SourceName"] ?? $"Source Name {i} Not Found",
					NetworkShareUser = configuration[$"Sources:{i}:NetworkShareUser"] ?? "User Not Found",
					NetworkSharePassword = configuration[$"Sources:{i}:NetworkSharePassword"] ?? "Password Not Found",
					NetworkShareDomain = configuration[$"Sources:{i}:NetworkShareDomain"] ?? "Domain Not Found",
					Path = configuration[$"Sources:{i}:Path"] ?? "Path Not Found"
				};
				Boolean.TryParse(configuration[$"Sources:{i}:IsLocalFileSystem"], out bool isLocalFs);
				source.IsLocalFileSystem = isLocalFs;
				Sources.Add(source);
			}

			Config.HashSizeLimitInKB = int.TryParse(configuration["HashSizeLimitInKB"], out int hashLimit) ? hashLimit : 0;
			
			Config.PersistenceConfig = new PersistenceConfig()
			{
				Host = configuration[$"PersistenceConfig:Host"] ?? "DB Host Not Found",
				User = configuration[$"PersistenceConfig:User"] ?? "DB User Not Found",
				Password = configuration[$"PersistenceConfig:Password"] ?? "DB Password Not Found",
				Database = configuration[$"PersistenceConfig:Database"] ?? "DB Database Not Found",
				Port = int.TryParse(configuration[$"PersistenceConfig:Port"], out int port) ? port : 0
			};
		}
	}
}
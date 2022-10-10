namespace DuplicateFinder.Models
{
	public class Config
	{
		public int HashSizeLimitInKB { get; set; }
		public bool ForceRehash { get; set; } = false;
		public PersistenceConfig? PersistenceConfig { get; set; }
	}
}

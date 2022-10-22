namespace DuplicateFinder.Models
{
	public class Source
	{
        public string Name { get; set; }
        public string NetworkShareUser { get; set; }
		public string NetworkSharePassword { get; set; }
		public string NetworkShareDomain { get; set; }
		public string UncPath { get; set; }
		public string ServerName { get; set; }
        public string ShareName { get; set; }
        public bool IsLocalFileSystem { get; set; }
	}
}

using System.ComponentModel.DataAnnotations;

namespace DuplicateFinder.Models
{
	public class File
	{
		[Key]
		public int Id { get; set; }
		public string Name { get; set; }
		public string Path { get; set; }
		public long SizeInKiloBytes { get; set; }
		public string Source { get; set; }
		public int HashId { get; set; }
		public Hash Hash { get; set; }

		public override bool Equals(object obj)
		{
			if (obj == null || GetType() != obj.GetType())
			{
				return false;
			}

			File objresult = (File)obj;

			if (!Name.Equals(objresult.Name) ||
				!Path.Equals(objresult.Path) ||
				!SizeInKiloBytes.Equals(objresult.SizeInKiloBytes)
				//|| !Hash.Equals(objresult.Hash)
				)
			{
				return false;
			}

			return true;
		}

		private bool CompareLists(List<string> list1, List<string> list2)
		{
			for (int i = 0; i < list1.Count; i++)
			{
				if (list1[i] != list2[i])
					return false;
			}
			return true;
		}

		public override int GetHashCode() => base.GetHashCode();
	}
}

/*
CREATE TABLE IF NOT EXISTS file (
    file_id     SERIAL PRIMARY KEY,
    hash_id     INT, 
    CONSTRAINT fk_hash FOREIGN KEY(hash_id) REFERENCES hash(hash_id),
    name        TEXT,
    path        TEXT,
    source      TEXT
);
 */ 
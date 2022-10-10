using System.ComponentModel.DataAnnotations;

namespace DuplicateFinder.Models
{
	public class Hash
	{
		[Key]
		public int Id { get; set; }
		public string Value { get; set; }
		public bool IsPartial { get; set; }
		public bool HasDuplicate { get; set; }
		public DateTime CreatedOn { get; set; }
		public DateTime? ModifiedOn { get; set; }

		public ICollection<File> Files { get; set; }

		public override int GetHashCode() => base.GetHashCode();

		public override bool Equals(object obj)
		{
			if (obj == null || GetType() != obj.GetType())
			{
				return false;
			}

			Hash objresult = (Hash)obj;

			if (!IsPartial.Equals(objresult.IsPartial) ||
				!Value.Equals(objresult.Value))
			{
				return false;
			}

			return true;
		}
	}
}
/*
 * 
 * CREATE TABLE IF NOT EXISTS hash (
    hash_id     SERIAL PRIMARY KEY,
    val         TEXT,
    is_partial  BOOLEAN NOT NULL,
    created_on  TIMESTAMP NOT NULL,
    modified_on TIMESTAMP
);
 */

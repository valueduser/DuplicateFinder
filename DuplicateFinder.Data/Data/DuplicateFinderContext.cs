using Microsoft.EntityFrameworkCore;
using DuplicateFinder.Models;
using File = DuplicateFinder.Models.File;

namespace DuplicateFinder.Data.Data
{
	public class DuplicateFinderContext : DbContext
	{
		private string connectionString;

		public DuplicateFinderContext(PersistenceConfig config)
		{
			connectionString = @$"Host={config.Host};port={config.Port};Username={config.User};Password={config.Password};Database={config.Database}";
		}

		public DbSet<Hash> Hash { get; set; } = null!;
		public DbSet<File> File { get; set; } = null!;

		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) 
			=> optionsBuilder.UseNpgsql(connectionString);
		

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			modelBuilder.Entity<File>()
				.HasOne<Hash>(s => s.Hash)
				.WithMany(g => g.Files)
				.HasForeignKey(s => s.HashId);
		}
	}
}

using IPO.Dictionary.Data.Interfaces;
using IPO.Dictionary.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace IPO.Dictionary.Data
{
    public class DictionarySearchDbContext : DbContext, IDictionarySearchDbContext
    {
        public DictionarySearchDbContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<DictionarySearchRecord> SearchRecords { get; set; } = null!;

        public DbSet<DictionarySearchScreeningData> Dictionaries { get; set; } = null!;
        public DbSet<DictionarySearchScreeningDataSeedingHistory> DictionarySearchScreeningDataSeedingHistories { get; set; } = null!;
    }
}

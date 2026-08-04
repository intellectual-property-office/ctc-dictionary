using IPO.Dictionary.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System.Threading;
using System.Threading.Tasks;

namespace IPO.Dictionary.Data.Interfaces
{
    public interface IDictionarySearchDbContext
    {
        DatabaseFacade Database { get; }
        DbSet<DictionarySearchRecord> SearchRecords { get; set; }
        DbSet<DictionarySearchScreeningData> Dictionaries { get; set; }
        DbSet<DictionarySearchScreeningDataSeedingHistory> DictionarySearchScreeningDataSeedingHistories { get; set; }
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
        int SaveChanges();
    }
}
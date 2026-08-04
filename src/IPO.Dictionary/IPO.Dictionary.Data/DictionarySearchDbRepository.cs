using IPO.Dictionary.Data.Interfaces;
using IPO.Dictionary.Data.Models;
using IPO.Dictionary.Interfaces;
using IPO.Dictionary.Models;
using IPO.Dictionary.Models.DictionarySearch;
using Microsoft.AspNetCore.Mvc.Diagnostics;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace IPO.Dictionary.Data
{
    public class DictionarySearchDbRepository : IDictionarySearchDbRepository
    {
        public IDictionarySearchDbContext Context { get; }
        public DictionarySearchDbRepository(IDictionarySearchDbContext context)
        {
            Context = context;
        }

        public async Task<int> CreateDictionarySearchRecordAsync(string blobName, DictionarySearchFileType dictionarySearchFileType, DictionaryType dictionaryType)
        {
            var record = new DictionarySearchRecord
            {
                FileType = dictionarySearchFileType,
                DictionaryType = dictionaryType,
                Status = Status.Uploaded,
                BlobName = blobName,
                CreatedOn = DateTime.UtcNow,
            };

            this.Context.SearchRecords.Add(record);
            await this.Context.SaveChangesAsync();

            return record.Id;
        }

        public async Task<DictionarySearchData?> GetDictionarySearchDataAsync(int fileId)
        {
            DictionarySearchRecord? record = await this.Context.SearchRecords.FirstOrDefaultAsync(o => o.Id == fileId);

            return record == null ? null : new DictionarySearchData()
            {
                Id = record.Id,
                BlobName = record.BlobName,
                CreatedOn = record.CreatedOn,
                DictionaryType = record.DictionaryType,
                FileType = record.FileType,
                Match = record.Match,
                Status = record.Status
            };
        }

        public async Task UpdateDictionarySearchStatusAsync(int fileId, Status status)
        {
            var record = await this.Context.SearchRecords.FirstOrDefaultAsync(x => x.Id == fileId);
            if (record == null)
                return;

            record.Status = status;
            await this.Context.SaveChangesAsync();
        }

        public async Task<string?> GetDictionaryDataAsync(DictionaryType dictionaryType)
        {
            var result = await this.Context.Dictionaries.SingleOrDefaultAsync(o => o.DictionaryType == dictionaryType);

            if (result == null)
                throw new ArgumentNullException(nameof(dictionaryType), $"The dictionary data for type: {dictionaryType.ToString()} does not exist.");

            return result.DictionaryData;
        }

        public async Task UpdateDictionarySearchProcessResultsAsync(int fileId, ProcessResults processResults)
        {
            var record = await this.Context.SearchRecords.FirstOrDefaultAsync(x => x.Id == fileId);
            if (record == null)
                return;

            record.Match = (processResults.HasMatch ? processResults.Match : null);
            record.Status = Status.Completed;
            await this.Context.SaveChangesAsync();
        }

        public async Task DeleteDictionarySearchRecordAsync(int id)
        {
            var record = await this.Context.SearchRecords.SingleOrDefaultAsync(o => o.Id == id);

            if (record == null)
                return;

            this.Context.SearchRecords.Remove(record);
            await this.Context.SaveChangesAsync();
        }

        public void SeedDictionarySearchData(DirectoryInfo directory)
        {
            var versionFiles = GetDictionarySeedDataSets(directory);
            if (!versionFiles.Any())
                return;

            using var transaction = this.Context.Database.BeginTransaction(System.Data.IsolationLevel.Serializable);

            var latestDbRecords = this.Context.DictionarySearchScreeningDataSeedingHistories
                                               .GroupBy(o => o.Type)
                                               .Select(o => o.FirstOrDefault(x => x.Version == o.Max(m => m.Version)))
                                               .ToList();
            foreach (var versionFile in versionFiles)
            {
                var latestDbRecord = latestDbRecords.FirstOrDefault(x => x!.Type == versionFile.Type);

                if (latestDbRecord == null || latestDbRecord.Version < versionFile.Version)
                {
                    this.Context.DictionarySearchScreeningDataSeedingHistories
                        .Add(new DictionarySearchScreeningDataSeedingHistory()
                        {
                            CreatedOn = DateTime.UtcNow,
                            Version = versionFile.Version,
                            Type = versionFile.Type
                        });
                    this.Context.Dictionaries.RemoveRange(this.Context.Dictionaries
                                                          .Where(o => o.DictionaryType == versionFile.Type));

                    var dictionaryData = File.ReadAllText(versionFile.Path).Trim();

                    this.Context.Dictionaries.Add(new DictionarySearchScreeningData()
                    {
                        Description = $"{versionFile.Type} Dictionary",
                        Name = $"{versionFile.Type} Dictionary",
                        DictionaryType = versionFile.Type,
                        DictionaryData = dictionaryData
                    });
                }
            }
            try
            {
                this.Context.SaveChanges();
                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }

        }

        protected IEnumerable<DictionarySearchDataSeedModel> GetDictionarySeedDataSets(DirectoryInfo dictionaryDataSetsDirectory)
        {
            var fileRegex = new Regex($"^({string.Join('|', Enum.GetNames(typeof(DictionaryType)))})_v(\\d+).(txt|json)$", RegexOptions.IgnoreCase);
            var versionFiles = dictionaryDataSetsDirectory
                               .GetFiles(@"*.*", SearchOption.TopDirectoryOnly)
                               .Where(o => fileRegex.IsMatch(o.Name) && o.Length > 0)
                               .Select(o => new
                               {
                                   version = Int32.Parse(o.Name.Split("_v").Last().Split('.').First())
                                                  ,
                                   type = Enum.Parse<DictionaryType>(o.Name.Split("_v").First(), true)
                                                  ,
                                   isEmpty = (o.Length == 0)
                                                  ,
                                   path = o.FullName
                               })
                               .Where(o => !o.isEmpty)
                               .GroupBy(o => o.type)
                               .Select(o => o.OrderByDescending(o => o.version).FirstOrDefault());

            return versionFiles.Where(o => o != null).Select(o => new DictionarySearchDataSeedModel(o!.version, o.type, o.path));
        }

        public DirectoryInfo GetDictionarySeedDataDirectory(string solutionPath)
        {
            DirectoryInfo dictionaryDataSetsDirectory;
#if DEBUG
            var contentRootPathSegments = solutionPath.Split("\\");
            var solutionName = contentRootPathSegments[(contentRootPathSegments.Length - 2)];
            dictionaryDataSetsDirectory = new DirectoryInfo($"{solutionPath}\\..\\{solutionName}.Data\\DictionaryDataSets");
#else
            dictionaryDataSetsDirectory = new DirectoryInfo($"{solutionPath}\\DictionaryDataSets");
#endif

            return dictionaryDataSetsDirectory.Exists ?
                   dictionaryDataSetsDirectory :
                   throw new DirectoryNotFoundException($"The directory for the dictionary data does not exist. path: {solutionPath}");
        }

    }
}

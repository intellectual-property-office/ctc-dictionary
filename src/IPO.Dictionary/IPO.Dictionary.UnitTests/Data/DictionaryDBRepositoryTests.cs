using AwesomeAssertions;
using IPO.Dictionary.Data;
using IPO.Dictionary.Data.Models;
using IPO.Dictionary.Models;
using IPO.Dictionary.Models.DictionarySearch;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.IO; 
using System.Linq;
using System.Threading.Tasks;

namespace IPO.Dictionary.UnitTests.Data
{
    [TestClass]
    public class DictionaryDBRepositoryTests
    {
        private readonly DictionarySearchDbRepository _dictionarySearchDbRepository;

        public DictionaryDBRepositoryTests()
        {
            this._dictionarySearchDbRepository = new DictionarySearchDbRepository(
                                            new DictionarySearchDbContext(
                                            new DbContextOptionsBuilder<DictionarySearchDbContext>()
                                            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                                            .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                                            .Options
                                            )); 
                                                                                                                                                                                                                                ////to the 'ConfigureWarnings' method in 'DbContext.OnConfiguring' or 'AddDbContext'.
        }

        [TestMethod]
        public void CreateDictionarySearchRecordAsyncReturnsOk()
        {
            // Arrange
            var blobname = "test-blob";
            var dictionarySearchFileType = DictionarySearchFileType.Docx;
            var dictionaryType = DictionaryType.Profanity;

            // Act
            var CreateDictionarySearchRecordTaskResult = _dictionarySearchDbRepository.CreateDictionarySearchRecordAsync(blobname,
                                                        dictionarySearchFileType,
                                                        dictionaryType);
            // Assert
            CreateDictionarySearchRecordTaskResult.IsCompleted.Should().BeTrue();
            this._dictionarySearchDbRepository.Context.SearchRecords.Should().HaveCount(1);
            var newRecord = this._dictionarySearchDbRepository.Context.SearchRecords.FirstOrDefault(o => o.BlobName == blobname);
            newRecord.Should().NotBeNull();
            newRecord!.FileType.Should().Be(dictionarySearchFileType);
            newRecord.DictionaryType.Should().Be(dictionaryType);
        }

        [TestMethod]
        public async Task GetDictionarySearchDataAsyncReturnsNullIfRecordNotFound()
        {
            // Arrange
            var dictionarySearchRecord = GetDictionarySearchRecord();

            this._dictionarySearchDbRepository.Context.SearchRecords.Add(dictionarySearchRecord);
            await this._dictionarySearchDbRepository.Context.SaveChangesAsync();

            // Act
            var dictionarySearchDataResult = await _dictionarySearchDbRepository.GetDictionarySearchDataAsync(2);

            // Assert
            this._dictionarySearchDbRepository.Context.SearchRecords.Should().HaveCount(1);
            dictionarySearchDataResult.Should().BeNull();
        }

        [TestMethod]
        public async Task GetDictionarySearchDataAsyncReturnsExpectedDictionarySearchDataRecord()
        {
            // Arrange
            var dictionarySearchRecord = GetDictionarySearchRecord();

            this._dictionarySearchDbRepository.Context.SearchRecords.Add(dictionarySearchRecord);
            await this._dictionarySearchDbRepository.Context.SaveChangesAsync();

            // Act
            var dictionarySearchDataResult = await _dictionarySearchDbRepository.GetDictionarySearchDataAsync(1);

            // Assert
            this._dictionarySearchDbRepository.Context.SearchRecords.Should().HaveCount(1);
            dictionarySearchDataResult!.BlobName.Should().Be(dictionarySearchRecord.BlobName);
            dictionarySearchDataResult.DictionaryType.Should().Be(DictionaryType.Profanity);
            dictionarySearchDataResult.FileType.Should().Be(DictionarySearchFileType.Pdf);
            dictionarySearchDataResult.Status.Should().Be(Status.Uploaded);
        }

        [DataRow(Status.Uploaded)]
        [DataRow(Status.InProgress)]
        [DataRow(Status.Completed)]
        [DataRow(Status.Failed)]
        [TestMethod]
        public async Task UpdateDictionarySearchStatusAsyncUpdatesDictionarySearchRecordWithExpectedStatus(Status expectedStatus)
        {
            // Arrange
            var dictionarySearchRecord = GetDictionarySearchRecord();

            this._dictionarySearchDbRepository.Context.SearchRecords.Add(dictionarySearchRecord);
            await this._dictionarySearchDbRepository.Context.SaveChangesAsync();

            // Act
            await _dictionarySearchDbRepository.UpdateDictionarySearchStatusAsync(1, expectedStatus);

            // Assert
            this._dictionarySearchDbRepository.Context.SearchRecords.Should().HaveCount(1);
            var result = this._dictionarySearchDbRepository.Context.SearchRecords.FirstOrDefault(o => o.Id == 1);
            result!.Status.Should().Be(expectedStatus);
            result.BlobName.Should().Be(dictionarySearchRecord.BlobName);
        }

        [TestMethod]
        public async Task GetDictionaryDataAsyncReturnsArgumentNullExceptionIfNotFound()
        {
            // Arrange
            this._dictionarySearchDbRepository.Context.Dictionaries.Add(new DictionarySearchScreeningData { DictionaryType = DictionaryType.Profanity});
            await this._dictionarySearchDbRepository.Context.SaveChangesAsync();

            // Act
            var resultAction = async() => await this._dictionarySearchDbRepository.GetDictionaryDataAsync(DictionaryType.Military);

            // Assert
            var exception = await resultAction.Should().ThrowAsync<ArgumentNullException>();
            exception.Subject.First().Message.Should().Be($"The dictionary data for type: {DictionaryType.Military} does not exist. (Parameter 'dictionaryType')");
        }

        [DataRow(DictionaryType.Profanity)]
        [DataRow(DictionaryType.Military)]
        [TestMethod]
        public async Task GetDictionaryDataAsyncReturnsExpectedDictionary(DictionaryType expectedDictionaryType)
        {
            // Arrange
            this._dictionarySearchDbRepository.Context.Dictionaries.Add(new DictionarySearchScreeningData { DictionaryType = DictionaryType.Profanity, DictionaryData = DictionaryType.Profanity.ToString() });
            this._dictionarySearchDbRepository.Context.Dictionaries.Add(new DictionarySearchScreeningData { DictionaryType = DictionaryType.Military, DictionaryData = DictionaryType.Military.ToString() });
            await this._dictionarySearchDbRepository.Context.SaveChangesAsync();

            // Act
            var dictionaryDataResult = await this._dictionarySearchDbRepository.GetDictionaryDataAsync(expectedDictionaryType);

            // Assert
            this._dictionarySearchDbRepository.Context.Dictionaries.Should().HaveCount(2);
            dictionaryDataResult.Should().Be(expectedDictionaryType.ToString());
        }

        [DataRow(true, "matchWord")]
        [DataRow(false, null)]
        [TestMethod]
        public async Task UpdateDictionarySearchProcessResultsAsyncUpdatesDictionarySearchRecordWithExpectedResults(bool hasMatch, string match)
        {
            // Arrange
            var dictionarySearchRecord = GetDictionarySearchRecord();

            this._dictionarySearchDbRepository.Context.SearchRecords.Add(dictionarySearchRecord);
            await this._dictionarySearchDbRepository.Context.SaveChangesAsync();

            var processResults = ProcessResults.CreateSuccesfulProcessResultsModel(hasMatch, match);

            // Act
            await _dictionarySearchDbRepository.UpdateDictionarySearchProcessResultsAsync(1, processResults);

            // Assert
            this._dictionarySearchDbRepository.Context.SearchRecords.Should().HaveCount(1);
            var result = this._dictionarySearchDbRepository.Context.SearchRecords.FirstOrDefault();
            result!.Status.Should().Be(Status.Completed);
            result.Match.Should().Be(match);
        }


        [TestMethod]
        public async Task DeleteDictionarySearchRecordAsyncReturnsWithoutDeleteIfRecordNotFound()
        {
            // Arrange
            var dictionarySearchRecord = GetDictionarySearchRecord();

            this._dictionarySearchDbRepository.Context.SearchRecords.Add(dictionarySearchRecord);
            await this._dictionarySearchDbRepository.Context.SaveChangesAsync();

            // Act
            await this._dictionarySearchDbRepository.DeleteDictionarySearchRecordAsync(2);

            // Assert
            this._dictionarySearchDbRepository.Context.SearchRecords.Should().HaveCount(1);
            var record = this._dictionarySearchDbRepository.Context.SearchRecords.FirstOrDefault(o => o.Id == 2);
            record.Should().BeNull();
        }

        [TestMethod]
        public async Task DeleteDictionarySearchRecordAsyncDeletesRecordSuccessfully()
        {
            // Arrange
            var dictionarySearchRecord = GetDictionarySearchRecord();

            this._dictionarySearchDbRepository.Context.SearchRecords.Add(dictionarySearchRecord);
            await this._dictionarySearchDbRepository.Context.SaveChangesAsync();

            // Act
            await this._dictionarySearchDbRepository.DeleteDictionarySearchRecordAsync(1);

            // Assert
            this._dictionarySearchDbRepository.Context.SearchRecords.Should().BeEmpty();
        }

        private static DictionarySearchRecord GetDictionarySearchRecord()
        {

            return new DictionarySearchRecord()
            {
                Id = 1,
                BlobName = "test-blob",
                CreatedOn = DateTime.UtcNow,
                DictionaryType = DictionaryType.Profanity,
                FileType = DictionarySearchFileType.Pdf,
                Status = Status.Uploaded
            };
        }

        #region Dictionary Data Import Task Tests

        [TestMethod] 
        public async Task SeedDictionarySearchDataWhenNoVersionFileExistReturns()
        {
            // Arrange
            var path = Path.Combine(
                Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)!
                ,"Resources"
                ,"emptyfolder");
 
            var directoryInfo = new DirectoryInfo(path);

            var profanityData = new DictionarySearchScreeningData { DictionaryType = DictionaryType.Profanity
                                                                   ,DictionaryData = DictionaryType.Profanity.ToString() 
                                                                   };
            var militaryData = new DictionarySearchScreeningData { DictionaryType = DictionaryType.Military
                                                                   , DictionaryData = DictionaryType.Military.ToString()
                                                                    };

            this._dictionarySearchDbRepository.Context.Dictionaries.Add(profanityData );
            this._dictionarySearchDbRepository.Context.Dictionaries.Add(militaryData);
            await this._dictionarySearchDbRepository.Context.SaveChangesAsync();

            // Act 
            this._dictionarySearchDbRepository.SeedDictionarySearchData(directoryInfo);


            // Assert 
            this._dictionarySearchDbRepository.Context.DictionarySearchScreeningDataSeedingHistories.Should().BeNullOrEmpty();
            this._dictionarySearchDbRepository.Context.Dictionaries
                .Where(o => o.DictionaryType == DictionaryType.Profanity)
                .Should().OnlyContain((dt) =>dt.Equals(profanityData));
            this._dictionarySearchDbRepository.Context.Dictionaries
                .Where(o => o.DictionaryType == DictionaryType.Military)
                .Should().OnlyContain((dt) => dt.Equals(militaryData));
            this._dictionarySearchDbRepository.Context.SearchRecords.Should().BeNullOrEmpty(); 
        }

        [TestMethod]
        public async Task SeedDictionarySearchDataWhenOlderOrExistingVersionFilesExistThenNoDatabaseChangesHappen()
        {
            // Arrange
            var path = Path.Combine(
                Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)!
                , "Resources" );

            var directoryInfo = new DirectoryInfo(path);

            var profanityData = new DictionarySearchScreeningData
            {
                DictionaryType = DictionaryType.Profanity
                                                                   ,
                DictionaryData = DictionaryType.Profanity.ToString()
            };
            var militaryData = new DictionarySearchScreeningData
            {
                DictionaryType = DictionaryType.Military
                                                                   ,
                DictionaryData = DictionaryType.Military.ToString()
            };

            this._dictionarySearchDbRepository.Context.Dictionaries.Add(profanityData);
            this._dictionarySearchDbRepository.Context.Dictionaries.Add(militaryData);

            var militaryHistoryRecord = new DictionarySearchScreeningDataSeedingHistory() { 
             CreatedOn = DateTime.Now,
              Type = DictionaryType.Military,
               Version = 2
            };

            var profanityHistoryRecord = new DictionarySearchScreeningDataSeedingHistory()
            {
                CreatedOn = DateTime.Now,
                Type = DictionaryType.Profanity,
                Version = 3
            };

            this._dictionarySearchDbRepository.Context
                .DictionarySearchScreeningDataSeedingHistories
                .Add(militaryHistoryRecord);
            this._dictionarySearchDbRepository.Context
                .DictionarySearchScreeningDataSeedingHistories
                .Add(profanityHistoryRecord);

            await this._dictionarySearchDbRepository.Context.SaveChangesAsync();

            // Act 
            this._dictionarySearchDbRepository.SeedDictionarySearchData(directoryInfo);


            // Assert 
            this._dictionarySearchDbRepository.Context.DictionarySearchScreeningDataSeedingHistories
                .Should().NotBeEmpty();

            this._dictionarySearchDbRepository.Context.DictionarySearchScreeningDataSeedingHistories
                .Where(o => o.Type == DictionaryType.Profanity)
                .Should().OnlyContain((dt) => dt.Equals(profanityHistoryRecord));

            this._dictionarySearchDbRepository.Context.DictionarySearchScreeningDataSeedingHistories
                .Where(o => o.Type == DictionaryType.Military)
                .Should().OnlyContain((dt) => dt.Equals(militaryHistoryRecord));

            this._dictionarySearchDbRepository.Context.Dictionaries
                .Where(o => o.DictionaryType == DictionaryType.Profanity)
                .Should().OnlyContain((dt) => dt.Equals(profanityData));

            this._dictionarySearchDbRepository.Context.Dictionaries
                .Where(o => o.DictionaryType == DictionaryType.Military)
                .Should().OnlyContain((dt) => dt.Equals(militaryData));


            this._dictionarySearchDbRepository.Context.SearchRecords.Should().BeNullOrEmpty();
        }

        [TestMethod]
        public async Task SeedDictionarySearchDataWhenNewerVersionFilesExistThenDatabaseChangesHappen()
        {
            // Arrange
            var path = Path.Combine(
                Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)!
                , "Resources");

            var directoryInfo = new DirectoryInfo(path);

            var profanityData = new DictionarySearchScreeningData
            {
                DictionaryType = DictionaryType.Profanity
                                                                   ,
                DictionaryData = DictionaryType.Profanity.ToString()
            };
            var militaryData = new DictionarySearchScreeningData
            {
                DictionaryType = DictionaryType.Military
                                                                   ,
                DictionaryData = DictionaryType.Military.ToString()
            };

            this._dictionarySearchDbRepository.Context.Dictionaries.Add(profanityData);
            this._dictionarySearchDbRepository.Context.Dictionaries.Add(militaryData);

            var militaryHistoryRecord = new DictionarySearchScreeningDataSeedingHistory()
            {
                CreatedOn = DateTime.Now,
                Type = DictionaryType.Military,
                Version = 1
            };

            var profanityHistoryRecord = new DictionarySearchScreeningDataSeedingHistory()
            {
                CreatedOn = DateTime.Now,
                Type = DictionaryType.Profanity,
                Version = 1
            };

            this._dictionarySearchDbRepository.Context
                .DictionarySearchScreeningDataSeedingHistories
                .Add(militaryHistoryRecord);
            this._dictionarySearchDbRepository.Context
                .DictionarySearchScreeningDataSeedingHistories
                .Add(profanityHistoryRecord);

            await this._dictionarySearchDbRepository.Context.SaveChangesAsync();

            var expectedMilitaryVersion = 2;
            var expectedProfanityVersion = 3; 

            // Act 
            this._dictionarySearchDbRepository.SeedDictionarySearchData(directoryInfo);


            // Assert 
            this._dictionarySearchDbRepository.Context.DictionarySearchScreeningDataSeedingHistories
                .Should().NotBeEmpty();

            this._dictionarySearchDbRepository.Context.DictionarySearchScreeningDataSeedingHistories
                .Where(o => o.Type == DictionaryType.Profanity)
                .Should().Contain((dt) => dt.Equals(profanityHistoryRecord));

            this._dictionarySearchDbRepository.Context.DictionarySearchScreeningDataSeedingHistories
                .Where(o => o.Type == DictionaryType.Military)
                .Should().Contain((dt) => dt.Equals(militaryHistoryRecord));

            var latestMilitaryHistoryRecord = this._dictionarySearchDbRepository.Context
                                        .DictionarySearchScreeningDataSeedingHistories
                                        .Where(o => o.Type == DictionaryType.Military)
                                        .OrderByDescending(o => o.Version)
                                        .FirstOrDefault();

            latestMilitaryHistoryRecord.Should().NotBeNull();
            latestMilitaryHistoryRecord!.Id.Should().NotBe(militaryHistoryRecord.Id);
            latestMilitaryHistoryRecord.Version.Should().Be(expectedMilitaryVersion);

            var latestProfanityHistoryRecord = this._dictionarySearchDbRepository.Context
                                        .DictionarySearchScreeningDataSeedingHistories
                                        .Where(o => o.Type == DictionaryType.Profanity)
                                        .OrderByDescending(o => o.Version)
                                        .FirstOrDefault();

            latestProfanityHistoryRecord.Should().NotBeNull();
            latestProfanityHistoryRecord!.Id.Should().NotBe(profanityHistoryRecord.Id);
            latestProfanityHistoryRecord.Version.Should().Be(expectedProfanityVersion);


            var militaryResult = this._dictionarySearchDbRepository.Context.Dictionaries
                .Where(o => o.DictionaryType == DictionaryType.Military).Select(o => o);


            militaryResult.Should().HaveCount(1);
            militaryResult.First().DictionaryData.Should().NotBeNullOrWhiteSpace();
            militaryResult.First().Name.Should().Be($"{DictionaryType.Military} Dictionary");
            militaryResult.First().Description.Should().Be($"{DictionaryType.Military} Dictionary");

            var profanityResult = this._dictionarySearchDbRepository.Context.Dictionaries
                .Where(o => o.DictionaryType == DictionaryType.Profanity).Select(o => o);

            profanityResult.Should().HaveCount(1);
            profanityResult.First().DictionaryData.Should().NotBeNullOrWhiteSpace();
            profanityResult.First().Name.Should().Be($"{DictionaryType.Profanity} Dictionary");
            profanityResult.First().Description.Should().Be($"{DictionaryType.Profanity} Dictionary");
 


            this._dictionarySearchDbRepository.Context.SearchRecords.Should().BeNullOrEmpty();
        } 

        #endregion
    }
}

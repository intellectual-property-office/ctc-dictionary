using AutoFixture;
using AwesomeAssertions;
using IPO.Dictionary.BDDTests.DictionarySearch;
using IPO.Dictionary.BDDTests.DictionarySearch.Data;
using IPO.Dictionary.BDDTests.Helpers;
using IPO.Dictionary.Data;
using IPO.Dictionary.Data.Models;
using IPO.Dictionary.Models.API;
using IPO.Dictionary.Models.DictionarySearch;
using IPO.FileService.BDDTests.DictionarySearch;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Reqnroll;

namespace IPO.Dictionary.BDDTests.Steps
{
    [Binding]
    public class DictionarySearchApiTests
    {
        private readonly ScenarioContext _scenarioContext;
        private readonly TestServer _server;
        private readonly HttpClient _client;
        private readonly DictionarySearchDbContext _dictionarySearchDbContext;
        private readonly Fixture _fixture;

        public DictionarySearchApiTests(ScenarioContext scenarioContext)
        {
            _scenarioContext = scenarioContext;
            _server = TestStartup.GetTestServer();
            _dictionarySearchDbContext = _server.Services.GetService<DictionarySearchDbContext>()!;
            _client = _server.CreateClient();
            _fixture = new Fixture();
        }

        [Given(@"Requesting the following files to be searched")]
        public void GivenRequestingTheFollowingFilesToBeSearched(Table table)
        {
            var requests = table.Rows.Select(o => new DictionarySearchRequestModel(
                                            DocumentBuilder.CreateDocument(
                                                            o["FileName"], 1024)
                                                            , Enum.Parse<DictionaryType>(o["DictionaryType"])));

            _scenarioContext.Add("dictionarySearchRequests", requests);
        }

        [When(@"apiUrl SearchDictionary requested")]
        public async Task WhenApiUrlSearchDictionaryRequested()
        {
            var requests = _scenarioContext.Get<IEnumerable<DictionarySearchRequestModel>>("dictionarySearchRequests");

            var dictionarySearchResults = new List<SearchDictionaryResult>();
            foreach (var request in requests)
            {
                var multipartFormData = new MultipartFormDataContent();

                multipartFormData.Add(new StringContent(request.DictionaryType.ToString()), "dictionaryType");
                multipartFormData.Add(new StreamContent(request.File.OpenReadStream()), "file", request.File.FileName);
                var postResponse = await _client.PostAsync("/SearchDictionary", multipartFormData);
                postResponse.EnsureSuccessStatusCode();
                var postResult = JsonConvert.DeserializeObject<SearchDictionaryResult>(await postResponse.Content.ReadAsStringAsync());
                dictionarySearchResults.Add(postResult!);
            }

            _scenarioContext.Add("dictionarySearchResponseResults", dictionarySearchResults);
        }

        [Then(@"the files to be searched are uploaded succesfully")]
        public void ThenTheFilesToBeSearchedAreUploadedSuccesfully()
        {
            var dictionarySearchResults = _scenarioContext.Get<IEnumerable<SearchDictionaryResult>>("dictionarySearchResponseResults");

            foreach (var result in dictionarySearchResults)
            {
                result.Should().NotBeNull();
                result.ResultsId.Should().BeGreaterThanOrEqualTo(0);
                this._dictionarySearchDbContext.SearchRecords.First(o => o.Id == result.ResultsId).Status.Should().Be(Status.Uploaded);
            }

        }

        [Given(@"Requesting of the results for the file")]
        public async Task GivenRequestingOfTheResultsForTheFile(Table table)
        {
            var createdDictionarySearchRequests = table.CreateSet<SearchDictionaryRequest>();

            await this._dictionarySearchDbContext.SearchRecords.AddRangeAsync(
                                                    createdDictionarySearchRequests.Select(o =>
                                                    _fixture.Build<DictionarySearchRecord>()
                                                    .With(p => p.Status, Status.Completed)
                                                    .With(p => p.Id, o.ResultsId)
                                                    .With(p => p.DictionaryType,  o.DictionaryType)
                                                    .With(p=>p.BlobName, o.FileName)
                                                    .With(p => p.Match, (o.HasMatch ? Guid.NewGuid().ToString() : null))
                                                    .Create())
                                                    );
            await this._dictionarySearchDbContext.SaveChangesAsync();
            _scenarioContext.Add("createdDictionarySearchRequests", createdDictionarySearchRequests);
        }

        [When(@"^apiURL SearchResults\/\{id\} for dictionary search results requested")]
        public async Task WhenApiURLSearchResultsIdForDictionarySearchResultsRequested()
        {
            var createdDictionarySearchRequests = _scenarioContext.Get<IEnumerable<SearchDictionaryRequest>>("createdDictionarySearchRequests");
            var dictionarySearchResults = new List<SearchResult>();
            foreach (var request in createdDictionarySearchRequests)
            {
                var _getResultsUrl = $"/SearchResults/{request.ResultsId}";
                var getResponse = await _client.GetAsync(_getResultsUrl);
                getResponse.EnsureSuccessStatusCode();
                var getResult = JsonConvert.DeserializeObject<SearchResult>(
                    await getResponse.Content.ReadAsStringAsync());

                dictionarySearchResults.Add(getResult!);
            }
            _scenarioContext.Add("requestSearchResults", dictionarySearchResults);
        }


        [Then(@"the dictionary search results are retrieved succesfully")]
        public void ThenTheDictionarySearchResultsAreRetrievedSuccesfully()
        {
            var requestSearchResults = _scenarioContext.Get<List<SearchResult>>("requestSearchResults");
            var requests = _scenarioContext.Get<List<SearchDictionaryRequest>>("createdDictionarySearchRequests");

            foreach (var requestSearchResult in requestSearchResults)
            {
                var request = requests.First(o => o.ResultsId == requestSearchResult.ResultsId);
                requestSearchResult.Should().NotBeNull(); 
                requestSearchResult.Status.Should().Be(Status.Completed);  
                requestSearchResult.Results.Should().NotBeNull();
                requestSearchResult.Results.IsMatch.Should().Be(request.HasMatch);
                if(request.HasMatch)
                {
                    requestSearchResult.Results.Match.Should().NotBeNull();
                }
                else
                {
                    requestSearchResult.Results.Match.Should().BeNull();
                }
                
                this._dictionarySearchDbContext.SearchRecords
                                    .FirstOrDefault(o => o.Id == requestSearchResult.ResultsId)
                                    .Should()
                                    .BeNull();
            }
        }
    }
}

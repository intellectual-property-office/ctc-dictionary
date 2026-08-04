using AwesomeAssertions;
using IPO.Dictionary.Models;
using IPO.Dictionary.Models.API;
using IPO.Dictionary.Models.DictionarySearch;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPO.Dictionary.UnitTests.Models
{
    [TestClass]
    public class DictionarySearchResultSwaggerExamplesTests
    {
        private readonly DictionarySearchResultSwaggerExamples _dictionarySearchResultSwaggerExamples;
        public DictionarySearchResultSwaggerExamplesTests()
        {
            this._dictionarySearchResultSwaggerExamples = new DictionarySearchResultSwaggerExamples();
        }

        [DataRow("When uploaded but the process hasn't started", Status.Uploaded, false, null, null )]
        [DataRow("When completed and there is a match", Status.Completed,  true, true, "coordinates")]
        [DataRow("When completed and there is no match", Status.Completed, true, false, null)]
        [DataRow("When failed", Status.Failed, false, null, null)]
        [DataRow("When in progress", Status.InProgress, false, null, null)]
        [TestMethod]
        public void GetExamplesReturnsCorrectExampleData(string option, Status status, bool hasResults, bool? hasMatch, string match)
        {
            // Arrange 

            // Act
            var results = this._dictionarySearchResultSwaggerExamples.GetExamples();

            // Assert
            var example = results.FirstOrDefault(o=>o.Name.Equals(option));
            example.Should().NotBeNull();
            example!.Value.Status.Should().Be(status);
            example.Value.ResultsId.Should().BeGreaterThanOrEqualTo(1);
            
            if(hasResults)
            {
                example.Value.Results.Should().NotBeNull();
                example.Value.Results.Match.Should().Be(match);
                example.Value.Results.IsMatch.Should().Be(hasMatch!.Value);
            }
            else
            {
                example.Value.Results.Should().BeNull();
            }

        }
    }
}

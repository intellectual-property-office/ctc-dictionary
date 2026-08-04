using IPO.Dictionary.Models.API;
using IPO.Dictionary.Models.DictionarySearch;
using Swashbuckle.AspNetCore.Filters;
using System.Collections.Generic;

namespace IPO.Dictionary.Models
{
    public class DictionarySearchResultSwaggerExamples : IMultipleExamplesProvider<SearchResult>
    {
        public IEnumerable<SwaggerExample<SearchResult>> GetExamples()
        {
            yield return SwaggerExample.Create(
                "When uploaded but the process hasn't started",
                new SearchResult(Status.Uploaded, 1024, null!)
                );
            yield return SwaggerExample.Create(
                "When completed and there is a match",
                new SearchResult(Status.Completed, 1024, new SearchDetails(true, "coordinates")));
            yield return SwaggerExample.Create(
                "When completed and there is no match",
                new SearchResult(Status.Completed, 1024, new SearchDetails(false, null!)));
            yield return SwaggerExample.Create(
                "When in progress",
                new SearchResult(Status.InProgress, 1024, null!)
                );
            yield return SwaggerExample.Create(
                "When failed",
                new SearchResult(Status.Failed, 1024, null!));
        }

    }
}
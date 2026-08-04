using IPO.Dictionary.Models.DictionarySearch;
using Swashbuckle.AspNetCore.Annotations;

namespace IPO.Dictionary.Models.API
{
    [SwaggerSchema(Description = "Contains the dictionary search status and results")]

    public class SearchResult
    {
        public SearchResult(Status status, int resultsId, SearchDetails results)
        {
            Status = status;
            ResultsId = resultsId;
            Results = results;
        }
        [SwaggerSchema(Title = "Status of the search")]
        public Status Status { get; set; }

        [SwaggerSchema(Title = "The results ID")]
        public int ResultsId { get; set; }

        [SwaggerSchema(Title = "Details of the search result")]
        public SearchDetails Results { get; set; }
    }
}

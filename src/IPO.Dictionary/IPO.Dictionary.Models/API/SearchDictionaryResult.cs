using Swashbuckle.AspNetCore.Annotations;

namespace IPO.Dictionary.Models.API
{
    [SwaggerSchema(Description = "Response from a dictionary search request")]
    public class SearchDictionaryResult
    {
        public SearchDictionaryResult(int resultsId)
        {
            ResultsId = resultsId;
        }
        [SwaggerSchema(Title = "The ID of the requested dictionary search")]

        public int ResultsId { get; set; }
    }
}

using IPO.Common.Infrastructure;
using IPO.Dictionary.Interfaces;
using IPO.Dictionary.Models;
using IPO.Dictionary.Models.API;
using IPO.Dictionary.Models.API.Validation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Filters;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Swashbuckle.AspNetCore.Annotations;

namespace IPO.Dictionary.API.Controllers
{
    [Route("/")]
    [ApiController]
    public class DictionarySearchController : ControllerBase
    {
        private readonly IDictionarySearchManagementService _dictionarySearchManagementService;

        public DictionarySearchController(IDictionarySearchManagementService dictionarySearchManagementService)
        {
            this._dictionarySearchManagementService = dictionarySearchManagementService;
        }

        [SwaggerOperation(Summary = "Upload a file to be searched by requested dictionary type.",
                          Description = "**Notes:** \n\n This endpoint lets you upload a file, adds an entry into the SQL database and puts a message on the ServiceBus ready for the WebJob to pickup and process the dictionary search request." +
            "The returned 'resultsId' can subsequently be used in the GET SearchResults request to obtain the results from this asynchronous operation." +
            "\n\nSee the Integration guide for limitations of file size and formats.")]
        [Produces("application/json")]
        [HttpPost]
        [Route("searchDictionary")] 
        [RequestSizeLimit((1181116007))]
        [RequestFormLimits(MultipartBodyLengthLimit = 1181116007)] 
        [ProducesResponseType(StatusCodes.Status202Accepted, Type = typeof(SearchDictionaryResult))]
        [ProducesResponseType(StatusCodes.Status413PayloadTooLarge, Type = typeof(IPOErrorResponse))]
        [ProducesResponseType(StatusCodes.Status415UnsupportedMediaType, Type = typeof(IPOErrorResponse))]
        public async Task<ActionResult<SearchDictionaryResult>> SearchDictionary([FromForm, OnlyOneFormFileIsAllowed] SearchDictionaryModel model)
        {
            var result = await this._dictionarySearchManagementService.SearchDictionaryAsync(model.file!, model.dictionaryType!.Value, model.MaximumFileSize);

            return Accepted(result);
        }

        [SwaggerOperation(Summary = "Returns the dictionary search results for a given ID.",
                          Description = "**Notes:** \n\n This endpoint returns the dictionary search results for the requested Id . The 'id' supplied in the uri is the same as a 'resultsId' returned from the POST SearchDictionary.")]
        [Produces("application/json")]
        [HttpGet]
        [Route("searchResults/{id}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(SearchResult))]
        [SwaggerResponseExample(StatusCodes.Status200OK, typeof(DictionarySearchResultSwaggerExamples))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(IPOErrorResponse))]
        public async Task<ActionResult<SearchResult>> SearchResults([Required, Range(0, int.MaxValue, ErrorMessage = "The id must be a positive integer.")] int id)
        {
            var result = await this._dictionarySearchManagementService.GetDictionaryResultsAsync(id);

            return Ok(result);
        }
    }
}

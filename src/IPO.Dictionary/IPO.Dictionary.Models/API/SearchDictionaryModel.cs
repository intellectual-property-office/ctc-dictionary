using IPO.Dictionary.Models.DictionarySearch;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace IPO.Dictionary.Models.API
{
    public class SearchDictionaryModel
    { 
        [Required(ErrorMessage = "The file is required.")]
        public IFormFile? file { get; set; }
        [Required]
        //Parameter must be in camelCase, otherwise breaks automation negative tests that are case sensitive.
        public DictionaryType? dictionaryType { get; set; }
        [FromHeader(Name = "MaximumFileSize")] 
        public int? MaximumFileSize { get; set; }
    }
}

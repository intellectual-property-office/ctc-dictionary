using System;
using Swashbuckle.AspNetCore.Annotations;

namespace IPO.Dictionary.Models.API
{
    [SwaggerSchema(Description = "Details of the search result")]

    public class SearchDetails
    {
        public SearchDetails(bool isMatch, string match)
        {
            IsMatch = isMatch;
            Match = ((isMatch && string.IsNullOrWhiteSpace(match)) ? throw new ArgumentException("The match cannot be empty or null when there is a match.", nameof(match)) : match);
        }
        [SwaggerSchema(Title = "Match found (true or false)")]
        public bool IsMatch { get; private set; }
        [SwaggerSchema(Title = "Details of a dictionary match")]
        public string Match { get; private set; }
    }
}
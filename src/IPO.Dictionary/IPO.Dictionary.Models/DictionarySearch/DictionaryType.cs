using Swashbuckle.AspNetCore.Annotations;

namespace IPO.Dictionary.Models.DictionarySearch
{
    [SwaggerSchema(Description = "Array of dictionary types")]

    public enum DictionaryType
    {
        Profanity,
        Military
    }
}

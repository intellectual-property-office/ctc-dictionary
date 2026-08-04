using Swashbuckle.AspNetCore.Annotations;

namespace IPO.Dictionary.Models.DictionarySearch
{
    [SwaggerSchema(Description = "List of potential statuses")]
    public enum Status
    {
        Uploaded,
        Completed,
        InProgress,
        Failed
    }
}
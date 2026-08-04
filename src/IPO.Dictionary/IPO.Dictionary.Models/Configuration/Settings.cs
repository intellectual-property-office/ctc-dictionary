using System.ComponentModel.DataAnnotations;

namespace IPO.Dictionary.Models.Configuration
{
    public class Settings
    {
        public int MaximumOperationTime { get; set; }
        [Required]
        public ValidationSettings? ValidationSettings { get; set; }
    }
}

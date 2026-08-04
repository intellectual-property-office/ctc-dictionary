using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace IPO.Dictionary.Models.Configuration
{
    public class ValidationSettings
    {
        [Required]
        public IEnumerable<string>? AcceptedFileExtensions { get; set; }
        [Required]
        public IEnumerable<string>? AcceptedFileMimeTypes { get; set; }
        [Required, Range(1, long.MaxValue)]
        public long SizeLimit { get; set; }
        [Required]
        public string? PdfLibraryLicenseKey { get; set; }
        [Required]
        public string? WordLibraryLicenseKey { get; set; }
    }
}
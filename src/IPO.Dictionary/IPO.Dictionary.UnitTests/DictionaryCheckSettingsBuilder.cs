using IPO.Dictionary.Models.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPO.Dictionary.UnitTests
{
    public static class DictionaryCheckSettingsBuilder
    {
        public static Settings Build(int maximumOperationTime = 300, int sizeLimit = 2024)
        {
            return new Settings()
            {
                MaximumOperationTime = 1200,
                ValidationSettings = new ValidationSettings()
                {
                    AcceptedFileExtensions = new string[] { ".ODT", ".DOCX", ".PDF" },
                    AcceptedFileMimeTypes = new string[] { "application/vnd.oasis.opendocument.text".ToUpperInvariant(), "application/vnd.openxmlformats-officedocument.wordprocessingml.document".ToUpperInvariant(), "application/pdf".ToUpperInvariant() },
                    SizeLimit = sizeLimit
                }

            };
        }
    }
}

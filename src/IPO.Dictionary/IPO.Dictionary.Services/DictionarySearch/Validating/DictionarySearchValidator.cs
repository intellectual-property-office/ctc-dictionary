using Aspose.Words;
using Aspose.Words.Loading;
using IPO.Common.Infrastructure;
using IPO.Dictionary.Interfaces;
using IPO.Dictionary.Models.Configuration;
using IPO.Dictionary.Models.DictionarySearch;
using Microsoft.Extensions.Logging;
using Spire.Pdf;
using System;
using System.IO;
using System.Linq;

namespace IPO.Dictionary.Models
{
    public class DictionarySearchValidator : IDictionarySearchValidator
    {
        private readonly Settings _settings;
        private readonly ILogger<DictionarySearchValidator> _logger;

        public DictionarySearchValidator(Settings settings, ILogger<DictionarySearchValidator> logger)
        {
            this._settings = settings;
            this._logger = logger;
        }

        public DictionarySearchValidationResult Validate(Stream file, string fileName, string contentType, int? optionalFileSizeHeader)
        {
            var maximumFileSize = GetMaximumFileSize(this._settings.ValidationSettings!.SizeLimit, optionalFileSizeHeader);
            if (file.Length > maximumFileSize)
                return DictionarySearchValidationResult.CreatePayloadTooLargeValidationResult(maximumFileSize);
            if (!this._settings.ValidationSettings.AcceptedFileExtensions!.Contains(Path.GetExtension(fileName).ToUpperInvariant()))
                return DictionarySearchValidationResult.CreateUnsupportedFileTypesValidationResult(this._settings.ValidationSettings.AcceptedFileExtensions!);
            if (!this._settings.ValidationSettings.AcceptedFileMimeTypes!.Contains(contentType.ToUpperInvariant()))
                return DictionarySearchValidationResult.CreateUnsupportedMediaTypesValidationResult(this._settings.ValidationSettings.AcceptedFileMimeTypes!);
            return DictionarySearchValidationResult.CreateSuccessValidationResult();
        }

        public ErrorType? @ValidateFormat(Stream file, string fileName) => GetDocumentType(fileName) switch
        {
            DictionarySearchFileType.Docx => ValidateWordDocument(file, LoadFormat.Docx),
            DictionarySearchFileType.Odt => ValidateWordDocument(file, LoadFormat.Odt),
            DictionarySearchFileType.Pdf => ValidatePdfDocument(file),
            DictionarySearchFileType.NotSupported => throw new NotImplementedException(),
            _ => throw new NotImplementedException()
        };

        private ErrorType? ValidatePdfDocument(Stream data)
        {
            try
            { 
                var document = new PdfDocument();
                document.LoadFromStream(data);

                return (!HasValidVersion(document) ? ErrorType.InvalidPDFVersion :  null);
            }
            catch (Exception ex)
            { 
                if (ex.Message.Equals("can not open an encrypted document. The password is invalid.", StringComparison.InvariantCultureIgnoreCase))
                {
                    return ErrorType.FileEncrypted;
                }
                _logger.LogError(ex, "The PDF document cannot be loaded.");
                return ErrorType.FileCannotBeLoaded;
            }
        }
        public static bool HasValidVersion(PdfDocument document)
        {
            return document.FileInfo.Version switch
            {
                PdfVersion.Version1_0 or PdfVersion.Version1_1 or PdfVersion.Version1_2 or PdfVersion.Version1_3 => false,
                _ => true,
            };
        }
        private ErrorType? ValidateWordDocument(Stream data, LoadFormat loadFormat)
        { 
            try
            {
                _ = FileFormatUtil.DetectFileFormat(data);

                _ = new Document(data, loadOptions: new LoadOptions() { LoadFormat = loadFormat });
                return null;
            }
            catch (IncorrectPasswordException)
            {
                return ErrorType.FileEncrypted;
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "The Word document cannot be loaded.");
                return ErrorType.FileCannotBeLoaded;
            }
        }

        public static DictionarySearchFileType GetDocumentType(string fileName)
        {
            Enum.TryParse<DictionarySearchFileType>(Path.GetExtension(fileName).Replace(".", ""), true, out DictionarySearchFileType type);

            return type;
        }

        public static long GetMaximumFileSize(long systemMaximumFileSize, int? optionalFileSizeHeader)
        {
            if (!optionalFileSizeHeader.HasValue)
                return systemMaximumFileSize;

            var optionalFileSizeInMB = (optionalFileSizeHeader.Value * 1024 * 1024);

            if (optionalFileSizeInMB <= 0 || optionalFileSizeInMB > systemMaximumFileSize)
                return systemMaximumFileSize;

            return optionalFileSizeInMB;
        }

        public static StatusCodeException GetStatusCodeException<T>(int code, string errorMessage, string errorCode)
        {
            var error = Error.Create<T>(errorCode);
            error.Description += $" {errorMessage}";
            return new StatusCodeException(error, errorMessage, null, code);
        }

        public string @GetErrorMessage(ErrorType errorType) => errorType switch
        {
            ErrorType.FileEncrypted => $"Encrypted files are not supported.",
            ErrorType.InvalidPDFVersion => $"File must be a pdf with version 1.4 or higher.",
            ErrorType.FileCannotBeLoaded => $"The file cannot be loaded.",
            _ => throw new NotImplementedException()
        };

    }
}
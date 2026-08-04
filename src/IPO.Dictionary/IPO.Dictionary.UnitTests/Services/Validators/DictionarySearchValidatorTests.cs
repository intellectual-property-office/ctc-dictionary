using AwesomeAssertions;
using IPO.Dictionary.Models;
using IPO.Dictionary.Models.Configuration;
using IPO.Dictionary.Models.DictionarySearch;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Spire.Pdf;
using System.IO;


namespace IPO.Dictionary.UnitTests.Services.Validators
{
    [TestClass]
    public class DictionarySearchValidatorTests
    {
        private readonly DictionarySearchValidator _validator;
        private readonly Settings _settings;
        private readonly Mock<ILogger<DictionarySearchValidator>> _mockLogger;

        public DictionarySearchValidatorTests()
        {
            _mockLogger = new Mock<ILogger<DictionarySearchValidator>>(); 
            _settings = DictionaryCheckSettingsBuilder.Build();
            _validator = new DictionarySearchValidator(_settings, _mockLogger.Object);
        }

        [TestMethod]
        public void ValidateWhenFileLengthGreaterThanMaximumFileSizeReturnsPayloadTooLargeValidationResult()
        {
            // Arrange
            var file = DocumentBuilder.CreateDocument("test.docx", (int)(_settings.ValidationSettings!.SizeLimit + 1));

            // Act
            var result = _validator.Validate(file.OpenReadStream(), file.FileName, file.ContentType, null);

            // Assert
            var validationResult = result.As<DictionarySearchValidationResult>();
            validationResult.Code.Should().Be(413);
            validationResult.Error.Should().Be($"File size larger than {(_settings.ValidationSettings.SizeLimit / 1024f / 1024f).ToString("#0")} MB.");
        }

        [TestMethod]
        public void ValidateWhenFileExtensionIsInvalidReturnsUnsupportedFileTypesValidationResult()
        {
            // Arrange
            var file = DocumentBuilder.CreateDocument("test.png", (int)(_settings.ValidationSettings!.SizeLimit ));

            // Act
            var result = _validator.Validate(file.OpenReadStream(), file.FileName, file.ContentType, null);

            // Assert
            var validationResult = result.As<DictionarySearchValidationResult>();
            validationResult.Code.Should().Be(415);
            validationResult.Error.Should().Be($"Unsupported file type, supported media types: {string.Join(", ", _settings.ValidationSettings.AcceptedFileExtensions!)}.");
        }

        [TestMethod]
        public void ValidateWhenFileMimeTypeIsInvalidReturnsUnsupportedMediaTypesValidationResult()
        {
            // Arrange
            var file = DocumentBuilder.CreateDocument("test.docx", (int)(_settings.ValidationSettings!.SizeLimit));

            // Act
            var result = _validator.Validate(file.OpenReadStream(), file.FileName, "audio/mpeg3", null);

            // Assert
            var validationResult = result.As<DictionarySearchValidationResult>();
            validationResult.Code.Should().Be(415);
            validationResult.Error.Should().Be($"Unsupported media type, supported media types: {string.Join(", ", _settings.ValidationSettings.AcceptedFileMimeTypes!)}.");
        }

        [TestMethod]
        public void ValidateReturnsSuccessValidationResult()
        {
            // Arrange
            var file = DocumentBuilder.CreateDocument("test.docx", (int)(_settings.ValidationSettings!.SizeLimit));

            // Act
            var result = _validator.Validate(file.OpenReadStream(), file.FileName, file.ContentType, null);

            // Assert
            var validationResult = result.As<DictionarySearchValidationResult>();
            validationResult.Code.Should().Be(200);
        }

        [DataRow(1024,null,1024)]
        [DataRow(1024, -1, 1024)]
        [DataRow(1024, 0, 1024)]
        [DataRow(1024, 1, 1024)]
        [DataRow((1024 * 1024 * 2), 1, (int)(1024 * 1024))]
        [TestMethod]
        public void GetMaximumFileSizeReturnsExpectedResult(long maximumFileSize, int? maximumOptionalFileSizeInMb, long expectedResult)
        { 
            // Arrange

            // Act 
            var result = DictionarySearchValidator.GetMaximumFileSize(maximumFileSize, maximumOptionalFileSizeInMb);

            // Assert
            result.Should().Be(expectedResult);
        }
         
        [DataRow(ErrorType.FileEncrypted, "Encrypted files are not supported.")]
        [DataRow(ErrorType.InvalidPDFVersion, "File must be a pdf with version 1.4 or higher.")]
        [DataRow(ErrorType.FileCannotBeLoaded, "The file cannot be loaded.")]
        [TestMethod]
        public void GetErrorMessageReturnsCorrectMessage(ErrorType errorType, string expectedResult)
        {
            // Arrange 

            // Act
            var errorMessage = _validator.GetErrorMessage(errorType);

            // Assert
            errorMessage.Should().Be(expectedResult);
        } 

        [TestMethod]
        public void ValidateFormatWhenPdfReturnsNoValidationErrors()
        {
            // Arrange
            var file = DocumentBuilder.CreatePdf();

            //Act
            var results = _validator.ValidateFormat(file, "test.pdf");

            //Assert
            results.Should().BeNull();
        }

        [TestMethod]
        public void ValidateFormatWhenPdfReturnsFileCannotBeLoadedError()
        {
            // Arrange
            var file = (Stream)null!;

            //Act
            var results = _validator.ValidateFormat(file, "test.pdf");

            //Assert
            results.Should().NotBeNull();
            results!.Value.Should().Be(ErrorType.FileCannotBeLoaded);
        }

        [TestMethod]
        public void ValidateFormatWhenPdfReturnsFileEncryptedError()
        {
            // Arrange
            var file = DocumentBuilder.CreatePdfWithEncryption();

            //Act
            var results = _validator.ValidateFormat(file,"test.pdf");

            //Assert
            results.Should().NotBeNull();
            results!.Value.Should().Be(ErrorType.FileEncrypted);
        } 

        [DataRow(PdfVersion.Version1_0, false)]
        [DataRow(PdfVersion.Version1_1, false)]
        [DataRow(PdfVersion.Version1_2, false)]
        [DataRow(PdfVersion.Version1_3, false)]
        [DataRow(PdfVersion.Version1_4, true)]
        [DataRow(PdfVersion.Version1_5, true)]
        [DataRow(PdfVersion.Version1_6, true)]
        [DataRow(PdfVersion.Version1_7, true)]
        [TestMethod]
        public void ValidateFormatWhenPdfReturnsInvalidPdfVersionError(PdfVersion pdfVersion, bool isValid)
        {
            // Arrange
            var file = DocumentBuilder.CreatePdf(version: pdfVersion);

            //Act
            var results = _validator.ValidateFormat(file, "test.pdf");

            //Assert
            if(isValid)
            {
                results.Should().BeNull();
            }
            else
            {

                results.Should().NotBeNull();
                results!.Value.Should().Be(ErrorType.InvalidPDFVersion);
            }  
        }

        [DataRow("test.docx")]
        [DataRow("test.odt")]
        [TestMethod]
        public void ValidateformatReturnsNoValidationErrors(string fileName)
        {
            // Arrange
            var file = DocumentBuilder.CreateDocx(); 

            //Act
            var results = _validator.ValidateFormat(file, fileName);

            //Assert
            results.Should().BeNull();
        }

        [DataRow("test.docx")]
        [DataRow("test.odt")]
        [TestMethod]
        public void ValidateFormatReturnsFileCannotBeLoadedError(string fileName)
        {
            // Arrange
            var file = (Stream)null!;

            //Act
            var results = _validator.ValidateFormat(file, fileName);

            //Assert
            results.Should().NotBeNull();
            results!.Value.Should().Be(ErrorType.FileCannotBeLoaded);
        }

        [DataRow("test.docx")]
        [DataRow("test.odt")]
        [TestMethod]
        public void ValidateFormatReturnsFileEncryptedError(string fileName)
        {
            // Arrange
            var file = DocumentBuilder.CreateDocxWithEncryption();

            //Act
            var results = _validator.ValidateFormat(file, fileName);

            //Assert
            results.Should().NotBeNull();
            results!.Value.Should().Be(ErrorType.FileEncrypted); 
        }
    }
}

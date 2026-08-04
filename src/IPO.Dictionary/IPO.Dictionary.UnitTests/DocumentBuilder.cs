using Aspose.Words;
using Aspose.Words.Loading;
using Aspose.Words.Saving;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using Moq;
using Spire.Pdf;
using Spire.Pdf.Attachments;
using Spire.Pdf.Graphics;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

namespace IPO.Dictionary.UnitTests
{
    public static class DocumentBuilder
    {
        public static IFormFile CreateDocument(string name, int length)
        {
            var mockedFile = new Mock<IFormFile>();
            var rnd = new Random();
            char[] contentArray = new char[length];
            int currentCharIndex = 0;
            while (currentCharIndex < length)
            {
                char randomChar = (char)rnd.Next('a', 'z');
                contentArray[currentCharIndex] = randomChar;
                currentCharIndex++;
            }
            var ms = new MemoryStream();
            var writer = new StreamWriter(ms);
            writer.Write(contentArray);
            writer.Flush();
            ms.Position = 0;
            mockedFile.Setup(o => o.OpenReadStream()).Returns(ms);
            mockedFile.Setup(o => o.FileName).Returns(name);
            mockedFile.Setup(o => o.Length).Returns(ms.Length);
            mockedFile.Setup(o => o.ContentType).Returns(GetContentType(name));
            return mockedFile.Object;
        }

        public static string GetContentType(string fileName)
        {
            if (Path.GetExtension(fileName) == ".odt")
            {
                return "application/vnd.oasis.opendocument.text";
            }

            if (!new FileExtensionContentTypeProvider().TryGetContentType(fileName, out string? contentType))
            {
                contentType = "application/octet-stream";
            }

            return contentType;
        }

        public static Stream CreateDocx(Dictionary<int, string>? pages = null, int numberOfPages =1, string? match = null)
        {
            var stream = new MemoryStream();
            var document = new Document(stream, new LoadOptions { LoadFormat = LoadFormat.Docx });

            var builder = new Aspose.Words.DocumentBuilder(document); 

            int index = 0;
            var matchAdded = false;
            while (index < (pages != null ? pages.Count : numberOfPages))
            {
                var pageText = "This is a page.";
                if (!matchAdded && match != null)
                {
                    pageText += " " + match;
                    matchAdded = true;
                }
                builder.Write((pages != null ? pages[index] : pageText));
                builder.InsertBreak(BreakType.PageBreak);
                index++;
            }

            var saveOptions = new OoxmlSaveOptions(SaveFormat.Docx);
            saveOptions.Compliance = OoxmlCompliance.Iso29500_2008_Transitional; 

            document.Save(stream, saveOptions);
            return stream;
        }

        public static Stream CreateOdt(Dictionary<int, string>? pages = null, int numberOfPages = 1, string? match = null)
        {
            var stream = new MemoryStream();
            var document = new Document(stream, new LoadOptions { LoadFormat = LoadFormat.Odt });

            var builder = new Aspose.Words.DocumentBuilder(document); 

            int index = 0;
            var matchAdded = false;
            while (index < (pages != null ? pages.Count : numberOfPages))
            {
                var pageText = "This is a page.";
                if (!matchAdded && match != null)
                {
                    pageText += " " + match;
                    matchAdded = true;
                }
                builder.Write((pages != null ? pages[index] : pageText));
                builder.InsertBreak(BreakType.PageBreak);
                index++;
            }

            var saveOptions = new OoxmlSaveOptions(SaveFormat.Docx);
            saveOptions.Compliance = OoxmlCompliance.Iso29500_2008_Transitional;

              
            document.Save(stream, saveOptions);
            return stream;
        }

        public static Stream CreatePdf(Dictionary<int, string>? pages = null, int numberOfPages = 1, string? match = null, PdfVersion version = PdfVersion.Version1_4)
        {
            var stream = new MemoryStream();
            var document = new PdfDocument();
            document.FileInfo.Version = version;

            int index = 0;
            var matchAdded = false;
            while (index < (pages != null ? pages.Count : numberOfPages))
            {
                var pageText = "This is a page.";
                if (!matchAdded && match != null)
                {
                    pageText += " " + match;
                    matchAdded = true;
                }
                var page = document.Pages.Add(new SizeF() { Height = 842, Width = 595 });
                page.Canvas.DrawTemplate(page.CreateTemplate(), new PointF(0, 0));
                page.Canvas.DrawString((pages != null ? pages[index] : pageText)
                                        , new PdfFont(PdfFontFamily.Helvetica
                                        , 12),
                                                        new PdfSolidBrush(Color.Black), 10, 10);

                index++;
            } 

            document.SaveToStream(stream, FileFormat.PDF);
            return stream;
        }

        public static Stream CreateDocxWithEncryption()
        {
            var stream = new MemoryStream();
            var document = new Document(stream ,new LoadOptions() { LoadFormat = LoadFormat.Docx });

            var saveOptions = new OoxmlSaveOptions(SaveFormat.Docx);
            saveOptions.Compliance = OoxmlCompliance.Iso29500_2008_Transitional;

            saveOptions.Password = "test-password";

            document.Save(stream, saveOptions);

            return stream;
        }


        public static Stream CreateOdtWithEncryption()
        {
            var stream = new MemoryStream();
            var document = new Document(stream, new LoadOptions() { LoadFormat = LoadFormat.Odt }); 

            var saveOptions = new OoxmlSaveOptions(SaveFormat.Docx);
            saveOptions.Compliance = OoxmlCompliance.Iso29500_2008_Transitional;

            saveOptions.Password = "test-password";

            document.Save(stream, saveOptions);

            return stream;
        }
         
        public static Stream CreatePdfWithEncryption()
        {
            var stream = new MemoryStream();
            var document = new PdfDocument();

			var securityPolicy = new PdfPasswordSecurityPolicy("test-password", "owner-password");
			document.Encrypt(securityPolicy);

			document.SaveToStream(stream, FileFormat.PDF);

            return stream;
        }
    }
}

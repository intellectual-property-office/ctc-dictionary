using Microsoft.AspNetCore.Http;
using Moq;
using System;
using System.IO;

namespace IPO.FileService.BDDTests.DictionarySearch
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
            mockedFile.Setup(o => o.ContentType).Returns(Path.GetExtension(name));
            return mockedFile.Object;
        }
    }
}

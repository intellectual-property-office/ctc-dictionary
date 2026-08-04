using System;
using System.IO;

namespace IPO.Dictionary.Models.DictionarySearch
{
    public class StorageFile
    {
        public StorageFile(long contentLength, string contentType, Stream data)
        {
            ContentLength = contentLength;
            ContentType = contentType ?? throw new ArgumentNullException(nameof(contentType));
            Data = data ?? throw new ArgumentNullException(nameof(data));
        }

        public long ContentLength { get; private set; }
        public string ContentType { get; private set; }

        public Stream Data { get; private set; }
    }
}

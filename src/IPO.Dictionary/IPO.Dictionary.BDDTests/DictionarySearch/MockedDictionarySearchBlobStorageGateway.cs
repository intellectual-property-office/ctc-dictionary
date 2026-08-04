using IPO.Dictionary.Interfaces;
using IPO.Dictionary.Models.DictionarySearch;
using System;
using System.IO;
using System.Threading.Tasks;

namespace IPO.Dictionary.BDDTests.DictionarySearch
{
    public class MockedDictionarySearchBlobStorageGateway : IDictionarySearchBlobStorageGateway
    {
        public async Task<bool> DeleteBlobAsync(string blobName)
        {
            return await Task.FromResult(true);
        }

        public async Task<StorageFile> GetUploadedBlobAsync(string blobName)
        {
            return await Task.FromResult(
                                   new StorageFile(1024
                                               , Guid.NewGuid().ToString()
                                               , new MemoryStream(Guid.NewGuid().ToByteArray()
                                               )
                                               )
                                       );
        }

        public async Task<string> UploadFileAsync(Stream fileData)
        {
            return await Task.FromResult(Guid.NewGuid().ToString());
        }
    }
}
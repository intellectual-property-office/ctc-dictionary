using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using IPO.Dictionary.Interfaces;
using IPO.Dictionary.Models.DictionarySearch;
using System;
using System.IO;
using System.Threading.Tasks;

namespace IPO.Dictionary.Gateways
{
    public class DictionarySearchBlobStorageGateway : IDictionarySearchBlobStorageGateway
    {
        private readonly BlobContainerClient _blobContainerClient;

        public DictionarySearchBlobStorageGateway(BlobContainerClient blobContainerClient)
        {
            this._blobContainerClient = blobContainerClient;
        }

        public async Task<string> UploadFileAsync(Stream fileData)
        {
            var blobName = Guid.NewGuid().ToString();
            fileData.Seek(0, SeekOrigin.Begin);
            _ = await _blobContainerClient.UploadBlobAsync(blobName, fileData);

            return blobName;
        }

        public async Task<StorageFile> GetUploadedBlobAsync(string blobName)
        {
            var blobClient = _blobContainerClient.GetBlobClient(blobName);
            var blobPropertiesResponse = await blobClient.GetPropertiesAsync();
            var blobProperties = blobPropertiesResponse.Value;
            var blobStream = new MemoryStream();
            _ = await blobClient.DownloadToAsync(blobStream);
            blobStream.Seek(0, SeekOrigin.Begin);
            return new StorageFile(blobProperties.ContentLength, blobProperties.ContentType, blobStream);
        }

        public async Task<bool> DeleteBlobAsync(string blobName)
        {
            var response = await _blobContainerClient.DeleteBlobIfExistsAsync(blobName, DeleteSnapshotsOption.IncludeSnapshots);
            return response.Value;
        }
    }
}

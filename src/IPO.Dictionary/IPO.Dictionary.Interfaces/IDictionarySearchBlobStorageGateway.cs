using IPO.Dictionary.Models.DictionarySearch;

namespace IPO.Dictionary.Interfaces
{
    public interface IDictionarySearchBlobStorageGateway
    {
        Task<bool> DeleteBlobAsync(string blobName);
        Task<StorageFile> GetUploadedBlobAsync(string blobName);
        Task<string> UploadFileAsync(Stream fileData);
    }
}
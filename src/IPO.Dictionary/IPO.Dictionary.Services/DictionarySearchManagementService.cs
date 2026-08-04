using IPO.Dictionary.Interfaces;
using IPO.Dictionary.Models;
using IPO.Dictionary.Models.API;
using IPO.Dictionary.Models.DictionarySearch; 
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;


namespace IPO.Dictionary.Services
{ 

    public class DictionarySearchManagementService : IDictionarySearchManagementService
    {
        private readonly IDictionarySearchBlobStorageGateway _dictionarySearchBlobStorageGateway;
        private readonly IDictionarySearchTopicGateway _dictionarySearchTopicGateway;
        private readonly IDictionarySearchProcessor _dictionarySearchProcessingService;
        private readonly IDictionarySearchValidator _dictionarySearchValidationService;
        private readonly IDictionarySearchDbRepository _dictionarySearchRepositoryService;
        private readonly ILogger<DictionarySearchManagementService> _logger;


        public DictionarySearchManagementService(IDictionarySearchBlobStorageGateway dictionarySearchBlobStorageGateway,
                                                IDictionarySearchTopicGateway dictionarySearchTopicGateway,
                                                IDictionarySearchProcessor dictionarySearchProcessingService,
                                                IDictionarySearchValidator dictionarySearchValidationService,
                                                IDictionarySearchDbRepository dictionarySearchRepositoryService,
                                                ILogger<DictionarySearchManagementService> logger)
        {
            this._dictionarySearchBlobStorageGateway = dictionarySearchBlobStorageGateway;
            this._dictionarySearchTopicGateway = dictionarySearchTopicGateway;
            this._dictionarySearchProcessingService = dictionarySearchProcessingService;
            this._dictionarySearchValidationService = dictionarySearchValidationService;
            this._dictionarySearchRepositoryService = dictionarySearchRepositoryService;
            this._logger = logger;
        } 

        public async Task<SearchDictionaryResult> SearchDictionaryAsync(IFormFile file, DictionaryType dictionaryType, int? maximumFileSizeHeader)
        { 
            var fileData = file.OpenReadStream();
            var validationResult = this._dictionarySearchValidationService.Validate(fileData,
                                                                                    file.FileName,
                                                                                    file.ContentType,
                                                                                    maximumFileSizeHeader);
            if (validationResult.Code != 200)
                throw DictionarySearchValidator.GetStatusCodeException<DictionarySearchManagementService>(validationResult.Code, validationResult.Error!, "E007");

            var formatValidationResult = this._dictionarySearchValidationService.ValidateFormat(fileData, file.FileName);

            if(formatValidationResult != null)
                throw DictionarySearchValidator.GetStatusCodeException<DictionarySearchManagementService>(422, _dictionarySearchValidationService.GetErrorMessage(formatValidationResult.Value), "E007");

            var blobName = await _dictionarySearchBlobStorageGateway.UploadFileAsync(fileData);
             
            int documentId = await _dictionarySearchRepositoryService.CreateDictionarySearchRecordAsync(blobName,
                                                                                                        DictionarySearchValidator.GetDocumentType(file.FileName),
                                                                                                        dictionaryType);


            await _dictionarySearchTopicGateway.SendMessageToSearchAsync(documentId);

            await fileData.DisposeAsync();

            return new SearchDictionaryResult(documentId);
        }

        public async Task<SearchResult> GetDictionaryResultsAsync(int id)
        {
            var dictionarySearchRecord = await _dictionarySearchRepositoryService.GetDictionarySearchDataAsync(id);

            if (dictionarySearchRecord == null)
                throw StatusCodeExceptionFactory.CreateFileNotExistsStatusCodeException<DictionarySearchManagementService>("E007", id.ToString());

            var processResult = new SearchResult(dictionarySearchRecord.Status
                                                , dictionarySearchRecord.Id
                                                , (dictionarySearchRecord.Status == Status.Completed
                                                ? new SearchDetails((dictionarySearchRecord.Match != null), dictionarySearchRecord.Match!)
                                                : null!));

            if (dictionarySearchRecord.Status == Status.Completed)
            {
                await _dictionarySearchRepositoryService.DeleteDictionarySearchRecordAsync(id);
            }

            return processResult;
        } 

        public async Task ProcessDictionarySearchMessageAsync(DictionarySearchBusMessage searchMessage)
        { 
            var dictionarySearchRecord = await _dictionarySearchRepositoryService.GetDictionarySearchDataAsync(searchMessage.FileId);

            if (dictionarySearchRecord == null)
            {
                _logger.LogError("The dictionary search record with id: {searchMessage.FileId} does not exist.", searchMessage.FileId);
                return;
            }

            if(dictionarySearchRecord.Status == Status.Completed)
            {
                _logger.LogError("The dictionary search record with id: {searchMessage.FileId} is already completed.", searchMessage.FileId);
                return;
            }

            await _dictionarySearchRepositoryService.UpdateDictionarySearchStatusAsync(dictionarySearchRecord.Id, Status.InProgress);

            var processModel = await GetProcessModelForPermanentDocumentAsync(dictionarySearchRecord);

            var processResults = _dictionarySearchProcessingService.SearchDocument(processModel);

            if(processResults.HasError)
            {
                await this._dictionarySearchRepositoryService.UpdateDictionarySearchStatusAsync(dictionarySearchRecord.Id, Status.Failed);
                _logger.LogError("The file for the dictionary search record with id: {searchMessage.FileId} cannot be loaded. ErrorType: {processResults.ErrorType}", searchMessage.FileId, processResults.ErrorType);
                return;
            }

            await _dictionarySearchRepositoryService.UpdateDictionarySearchProcessResultsAsync(dictionarySearchRecord.Id, processResults);


            try
            {
                await _dictionarySearchBlobStorageGateway.DeleteBlobAsync(dictionarySearchRecord.BlobName!);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "The deletion of file:{dictionarySearchRecord.BlobName} failed.", dictionarySearchRecord.BlobName);
            }
        }


        protected async Task<DictionarySearchProcessModel> GetProcessModelForPermanentDocumentAsync(DictionarySearchData dictionarySearchRecord)
        {
            var blob = await _dictionarySearchBlobStorageGateway.GetUploadedBlobAsync(dictionarySearchRecord.BlobName!);
            var dictionaryData = await _dictionarySearchRepositoryService.GetDictionaryDataAsync(dictionarySearchRecord.DictionaryType);
            return new DictionarySearchProcessModel(dictionarySearchRecord.FileType,
                                                    blob.Data,
                                                    this._dictionarySearchProcessingService.GetDictionaryValues(dictionaryData!));
        }
    }
}

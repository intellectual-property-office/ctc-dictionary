namespace IPO.Dictionary.Interfaces
{
    public interface IDictionarySearchTopicGateway
    {
        Task SendMessageToSearchAsync(int fileId);
    }

}
namespace PriceForge.Application.Images;

public interface IImageStorage
{
    Task SaveAsync(string key, Stream inputStream, string contentType, CancellationToken cancellationToken);
    Task DeleteAsync(string key, CancellationToken cancellationToken);
    string GetDownloadUrl(string key);
    Task<List<string>> ListKeysAsync(DateTime threshold, CancellationToken stoppingToken);
    Task<(int deleteCount, int deleteErrorCount)> DeleteExpiredKeysAsync(List<string> keys, CancellationToken stoppingToken);
}

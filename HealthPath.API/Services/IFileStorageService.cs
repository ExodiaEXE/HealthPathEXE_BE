using System.IO;
using System.Threading.Tasks;

namespace HealthPath.API.Services;

public interface IFileStorageService
{
    Task<string> UploadAsync(Stream fileStream, string fileName, string contentType, string folder);
    Task DeleteAsync(string fileUrlOrKey);
    Task<string> GeneratePresignedUploadUrlAsync(string fileKey, string contentType, int expiresInMinutes = 15);
}

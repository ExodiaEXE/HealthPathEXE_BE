using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace HealthPath.API.Services;

public interface IFileStorageService
{
    Task<string> UploadAsync(Stream fileStream, string fileName, string contentType, string folder);
    Task DeleteAsync(string fileUrlOrKey);
    Task<string> GeneratePresignedUploadUrlAsync(string fileKey, string contentType, int expiresInMinutes = 15);
    Task<string> GeneratePresignedDownloadUrlAsync(string fileKey, int expiresInMinutes = 60);
    Task<string> GetPlaybackUrlAsync(string fileUrlOrKey, int expiresInMinutes = 60);
    Task<List<StoredFileInfo>> ListFilesAsync(string folderPrefix);
}

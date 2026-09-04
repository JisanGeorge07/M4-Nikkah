using Domain;
using Microsoft.AspNetCore.Http;

namespace Application.Interfaces.Persistence;

public interface IFileService
{
    Task<string> SaveFile(IFormFile file, string folderPath, CancellationToken cancellationToken = default);
    Task<bool> DeleteFile(string filePath);
    Task DeleteExistFile(string filePath);
    Task DeleteSelectedFiles(Images image, string basePath, bool deleteImage1, bool deleteImage2, bool deleteImage3, bool deleteImage4, bool deleteImage5);
    Task SaveAllFiles<TEntity, TModel>(TEntity entity, TModel model, string filePath);
    Task DeleteAllFiles<TEntity>(TEntity entity);
    Task<string> UploadFile(IFormFile file, string folderPath);
}
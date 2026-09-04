using Application.Interfaces.Persistence;
using Domain;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace Persistence.Services;

public class FileService : IFileService
{
    private readonly IWebHostEnvironment _hostingEnvironment;

    public FileService(IWebHostEnvironment hostingEnvironment)
    {
        _hostingEnvironment = hostingEnvironment;
    }

    public async Task<string> SaveFile(IFormFile file, string folderPath, CancellationToken cancellationToken = default)
    {
        var ext = Path.GetExtension(file.FileName).ToLower();
        var fileNameWithOutExtension = Guid.NewGuid();
        var fileNameWithExtension = fileNameWithOutExtension + ext;

        bool applyWatermark = folderPath.Contains("Registration", StringComparison.OrdinalIgnoreCase) &&
                              (ext == ".jpg" || ext == ".jpeg" || ext == ".png" || ext == ".webp" || ext == ".bmp");

        if (applyWatermark)
        {
            fileNameWithExtension = fileNameWithOutExtension + "_wm" + ext;
        }

        var filePath = Path.Combine(folderPath, fileNameWithExtension);
        filePath = filePath.Replace("\\", "/");
        var uploadPath = Path.Combine(_hostingEnvironment.WebRootPath, folderPath);
        if (!Directory.Exists(uploadPath)) Directory.CreateDirectory(uploadPath);

        var fullOutputPath = Path.Combine(_hostingEnvironment.WebRootPath, filePath);

        if (applyWatermark)
        {
            try
            {
                var watermarkPath = Path.Combine(_hostingEnvironment.WebRootPath, "assets/images/card-watermark.png");

                if (File.Exists(watermarkPath))
                {
                    using (var imageStream = file.OpenReadStream())
                    using (var image = await Image.LoadAsync(imageStream, cancellationToken))
                    using (var watermark = await Image.LoadAsync(watermarkPath, cancellationToken))
                    {
                        // Resize watermark relative to the image size (20% of main image width)
                        int watermarkWidth = (int)(image.Width * 0.20);
                        if (watermarkWidth < 20) watermarkWidth = 20;

                        int watermarkHeight = (int)(watermark.Height * ((double)watermarkWidth / watermark.Width));
                        watermark.Mutate(x => x.Resize(watermarkWidth, watermarkHeight));

                        // Position: left 2.6% of width, top 9.2% of height (matches CSS: left 8px, top 35px on a 300px card)
                        int xPos = (int)(image.Width * 0.026);
                        int yPos = (int)(image.Height * 0.092);

                        // Draw the watermark on the image with 0.85 opacity (matching CSS opacity)
                        image.Mutate(x => x.DrawImage(watermark, new Point(xPos, yPos), 0.85f));

                        // Save the watermarked image to the destination
                        await image.SaveAsync(fullOutputPath, cancellationToken);
                    }
                    return filePath;
                }
            }
            catch (Exception ex)
            {
                // Fallback to standard save if image processing fails (e.g., corrupted file or format mismatch)
            }
        }

        // Standard save (fallback or for non-profile assets)
        await using (var stream = new FileStream(fullOutputPath, FileMode.Create))
        {
            await file.CopyToAsync(stream, cancellationToken);
        }

        return filePath;
    }

    public Task<bool> DeleteFile(string filePath)
    {
        var filepath = Path.Combine(_hostingEnvironment.WebRootPath, filePath);
        if (!File.Exists(filepath)) return Task.FromResult(false);

        File.Delete(filepath);
        return Task.FromResult(true);
    }

    public async Task SaveAllFiles<TEntity, TModel>(TEntity entity, TModel model, string filePath)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));
        if (model == null) throw new ArgumentNullException(nameof(model));

        var properties = entity.GetType()
            .GetProperties()
            .Select(property => property.Name)
            .Where(x => x.EndsWith("Path"))
            .Select(x => x[..^4])
            .ToList();

        foreach (var property in properties)
        {
            var entityFilePathProperty = entity.GetType().GetProperty(property + "Path");
            var modelFilePathProperty = model.GetType().GetProperty(property + "Path");
            var modelFileProperty = model.GetType().GetProperty(property);

            if (entityFilePathProperty == null || modelFilePathProperty == null || modelFileProperty == null
                || entityFilePathProperty.PropertyType != typeof(string)
                || modelFilePathProperty.PropertyType != typeof(string)
                || modelFileProperty.PropertyType != typeof(IFormFile)) continue;

            var entityFilePath = entityFilePathProperty.GetValue(entity) as string;
            var modelFilePath = modelFilePathProperty.GetValue(model) as string;
            var modelFile = modelFileProperty.GetValue(model) as IFormFile;


            if ((modelFile != null || string.IsNullOrWhiteSpace(modelFilePath))
                && !string.IsNullOrWhiteSpace(entityFilePath))
            {
                await DeleteFile(entityFilePath);
                modelFilePathProperty.SetValue(model, null);
                entityFilePathProperty.SetValue(entity, null);
            }

            if (modelFile == null) continue;

            var path = await SaveFile(modelFile, filePath);
            modelFilePathProperty.SetValue(model, path);
            entityFilePathProperty.SetValue(entity, path);
        }
    }

    public async Task DeleteAllFiles<TEntity>(TEntity entity)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));

        var properties = entity.GetType()
            .GetProperties()
            .Select(property => property.Name)
            .Where(x => x.EndsWith("Path"))
            .Select(x => x[..^4])
            .ToList();

        foreach (var property in properties)
        {
            var entityFilePathProperty = entity.GetType().GetProperty(property + "Path");
            if (entityFilePathProperty == null || entityFilePathProperty.PropertyType != typeof(string)) continue;

            if (entityFilePathProperty.GetValue(entity) is string entityFilePath) await DeleteFile(entityFilePath);
        }
    }
    public async Task<string> UploadFile(IFormFile file, string folderPath)
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException("File is empty");

        string uploadPath = Path.Combine(_hostingEnvironment.WebRootPath, folderPath);
        if (!Directory.Exists(uploadPath))
        {
            Directory.CreateDirectory(uploadPath);
        }

        string uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
        string filePath = Path.Combine(uploadPath, uniqueFileName);

        using (var fileStream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(fileStream);
        }

        return Path.Combine(folderPath, uniqueFileName);
    }
    public async Task DeleteExistFile(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
            return;

        string fullPath = Path.Combine(_hostingEnvironment.WebRootPath, filePath);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }
    }
    public async Task DeleteSelectedFiles(Images image, string basePath, bool deleteImage1, bool deleteImage2, bool deleteImage3, bool deleteImage4, bool deleteImage5)
    {
        //// Method to normalize and correct the file path
        //string NormalizePath(string path)
        //{
        //    if (string.IsNullOrEmpty(path))
        //    {
        //        return string.Empty;
        //    }

        //    // Ensure consistent directory separators
        //    path = path.Replace("\\", "/");

        //    // Ensure that the path is relative and does not start with basePath
        //    if (path.Contains("wwwroot"))
        //    {
        //        path = path.Replace("wwwroot/", "");
        //    }

        //    return path.TrimStart('/');
        //}

        async Task DeleteFile(string imagePath, bool deleteImage)
        {
            if (deleteImage && !string.IsNullOrEmpty(imagePath))
            {
                string fullPath = Path.Combine(basePath, imagePath);
                fullPath = Path.GetFullPath(fullPath);


                /*                if (!fileExists)
                                {
                                    // Try with double-check using fully normalized path
                                    fullPath = Path.GetFullPath(fullPath);
                                    fileExists = File.Exists(fullPath);
                                }*/
                if (File.Exists(fullPath))
                {
                    try
                    {
                        File.Delete(fullPath); // Attempt to delete the file
                    }
                    catch (Exception ex)
                    {
                        // Log any exceptions that occur during file deletion
                        Console.WriteLine($"Error deleting file at {fullPath}: {ex.Message}");
                    }
                }
                else
                {
                    // Log if the file does not exist
                    Console.WriteLine($"File not found at path: {fullPath}");
                }
            }
        }
        await DeleteFile(image.Image1Path, deleteImage1);
        await DeleteFile(image.Image2Path, deleteImage2);
        await DeleteFile(image.Image3Path, deleteImage3);
        await DeleteFile(image.Image4Path, deleteImage4);
        await DeleteFile(image.Image5Path, deleteImage5);
    }

}
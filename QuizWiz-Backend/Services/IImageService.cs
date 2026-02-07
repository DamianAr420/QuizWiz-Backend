using CloudinaryDotNet.Actions;
public interface IImageService
{
    Task<ImageUploadResult> UploadImageAsync(IFormFile file);
    Task<DeletionResult> DeleteImageAsync(string publicId);
}
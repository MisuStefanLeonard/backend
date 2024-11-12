namespace E_Commerce_BackEnd.Services.uBucketService;

public interface IBucketService
{
    public Task UploadImageToBucket(IFormFileCollection? image, string? imageName, string bucketFolder);
    public Task UploadImageToBucket(string? imageName, string bucketFolder);
    public Task DeleteImageFromBucket(string imageName, string bucketFolder);
    public Task<Stream?> DownloadFile(string s3Key);
    public Task<string?> GeneratePresignedUrl(string imagePath, string bucketDir);
}
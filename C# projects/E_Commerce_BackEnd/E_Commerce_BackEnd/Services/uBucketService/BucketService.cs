using E_Commerce_BackEnd.Services.Helpers.AWS_Secret.AWSBucket_CRUD;

namespace E_Commerce_BackEnd.Services.uBucketService;

public class BucketService : IBucketService
{
    private readonly IBucketAcces _bucketAcces;

    public BucketService(IBucketAcces bucketAcces)
    {
        _bucketAcces = bucketAcces;
        
    }

    public async Task UploadImageToBucket(IFormFileCollection? image, string? imageName, string bucketFolder)
    {
        if (image == null || string.IsNullOrEmpty(imageName)) return;

        using var imageStream = new MemoryStream();
        await image[0].CopyToAsync(imageStream);
        imageStream.Position = 0;

        var addImageToBucketResponse = await _bucketAcces.AddOrUpdateToBucket(imageStream, bucketFolder, imageName);
        if (addImageToBucketResponse == -1)
        {
            throw new Exception("An error occurred when adding to bucket");
        }
    }
    
    public async Task UploadImageToBucket(string? imageName, string bucketFolder)
    {
        if (imageName == null || string.IsNullOrEmpty(imageName)) return;
        var imagesDirPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.Desktop)}/imagini_import/imagini_update";
        var machineFullImagePath = Path.Join(imagesDirPath,imageName);

        if (!File.Exists(machineFullImagePath))
        {
            throw new FileNotFoundException("File not found");
        }
        
        using var imageStream = new MemoryStream();
        await using (var fileStream = new FileStream(machineFullImagePath, FileMode.Open, FileAccess.Read))
        {
            await fileStream.CopyToAsync(imageStream);
        }
        imageStream.Position = 0;

        var addImageToBucketResponse = await _bucketAcces.AddOrUpdateToBucket(imageStream, bucketFolder, imageName);
        if (addImageToBucketResponse == -1)
        {
            throw new Exception("An error occurred when adding to bucket");
        }
    }

    public async Task DeleteImageFromBucket(string imageName, string bucketFolder)
    {
        var deleteImageResponse = await _bucketAcces.DeleteFromBucket($"images/{bucketFolder}/{imageName}");
        if (deleteImageResponse == -1)
        {
            throw new Exception("An error occurred when deleting from bucket");
        }
    }

    public async Task<Stream?> DownloadFile(string s3Key)
    {
        var getFileStream = await _bucketAcces.DownloadFile(s3Key);
        return getFileStream;

    }

    public async Task<string?> GeneratePresignedUrl(string imagePath, string bucketDir)
    {
        var url =  await _bucketAcces.GenerateUrl(imagePath, bucketDir);
        return url ?? null;
    }
}
namespace E_Commerce_BackEnd.Services.Helpers.AWS_Secret.AWSBucket_CRUD;

public interface IBucketAcces
{
    public Task<int> AddOrUpdateToBucket(string imagePath,string? subDirectory , string fileNameInS3);
    public Task<int> AddOrUpdateToBucket(MemoryStream imageStream,string? subDirectory , string fileNameInS3);
    public Task<int> DeleteFromBucket(string keyName);
    public Task<string?> GenerateUrl(string imagePath, string bucketDir);
    public Task<Stream?> DownloadFile(string s3Key);
    public Task<List<string>?> ListDirsFromBuckets();
}
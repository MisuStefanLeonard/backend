using System.Net;
using System.Runtime.InteropServices;
using Amazon;
using Amazon.S3.Model;

namespace E_Commerce_BackEnd.Services.Helpers.AWS_Secret.AWSBucket_CRUD;

using Amazon.S3;


public class BucketAccess : IBucketAcces
{
    private readonly string _bucketName = "texxbucket";
    private static readonly string DirectoryPrefix = "images/";
    private static readonly IAmazonS3 AmazonS3Client = new AmazonS3Client(RegionEndpoint.EUCentral1);
    private readonly ILogger<BucketAccess> _logger;

    public BucketAccess(ILogger<BucketAccess> logger)
    {
        _logger = logger;
    }


    private async Task<bool> DirectoryExists(string key)
    {
        try
        {
            var dirPathInBucket = key ;
            var listObjectRequest = new ListObjectsV2Request
            {
                BucketName = _bucketName,
                Prefix = dirPathInBucket
            };

            var response = await AmazonS3Client.ListObjectsV2Async(listObjectRequest);

            if (response.HttpStatusCode == HttpStatusCode.OK)
            {
                _logger.LogInformation($"Directory in {_bucketName} with name {dirPathInBucket} exists");
                return true;
            }
            else
            {
                _logger.LogInformation($"{response.HttpStatusCode}");
                return false;
            }
            
        }
        catch (AmazonS3Exception e)
        {
            _logger.LogInformation(e.Message);
            throw;
        }
    } 
    
    /// <summary>
    /// File upload to S3 Bucket
    /// </summary>
    /// <param name="imagePath">The imagePath where the AWSSDK will search</param>
    /// <param name="subDirectory">OPTIONAL! If there is a subdirectory where the file will be uploaded</param>
    /// <param name="fileNameInS3">The key of the file in the bucket</param>
    /// <returns></returns>
    public async Task<int> AddOrUpdateToBucket(string imagePath,string? subDirectory , string fileNameInS3)
    {
        
        try
        {
            string key;

            if (string.IsNullOrEmpty(subDirectory))
            {
                key = DirectoryPrefix + fileNameInS3;
            }
            else
            {
                key = DirectoryPrefix + subDirectory + "/" + fileNameInS3;

            }
            
            // key = fileNameInS3
            // bucketname : texxbucket/images/....(the dir where the file will be inserted);
            
            var request = new PutObjectRequest
            {
                BucketName = _bucketName,
                Key = key,
                FilePath = imagePath
            };
            _logger.LogInformation($"path -> {imagePath} ");
            
            var response = await AmazonS3Client.PutObjectAsync(request);
            if (response.HttpStatusCode == HttpStatusCode.OK)
            {
                _logger.LogInformation($"Successfully uploaded {fileNameInS3} to {_bucketName}.");
                return 1;
            }

            _logger.LogError($"Could not upload {fileNameInS3} to {_bucketName}.");
            return -1;
        }
        catch (AmazonS3Exception ex)
        {
            _logger.LogError($"Error when adding the item from {_bucketName}");
            return -1;
        }
    }

    public async Task<int> AddOrUpdateToBucket(MemoryStream imageStream, string? subDirectory, string fileNameInS3)
    {
        try
        {
            string key;
            if (string.IsNullOrEmpty(subDirectory))
            {
                key = DirectoryPrefix + fileNameInS3;
            }
            else
            {
                key = DirectoryPrefix + subDirectory + "/" + fileNameInS3;

            }
            var request = new PutObjectRequest
            {
                BucketName = _bucketName,
                Key = key,
                InputStream = imageStream,
                ContentType = "image/jpeg"
            };
            
            var response = await AmazonS3Client.PutObjectAsync(request);
            if (response.HttpStatusCode == HttpStatusCode.OK)
            {
                _logger.LogInformation($"Successfully uploaded {fileNameInS3} to {_bucketName}.");
                return 1;
            }

            _logger.LogError($"Could not upload {fileNameInS3} to {_bucketName}.");
            return -1;
        }
        catch (AmazonS3Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }


    public async Task<int> DeleteFromBucket(string keyName)
    {
        try
        {
            var keyNameExists = await DirectoryExists(keyName);

            if (keyNameExists)
            {
                await AmazonS3Client.DeleteObjectAsync(_bucketName, keyName);
                _logger.LogInformation($"Succesfully deleted item with keyname {keyName}");
                return 1;
            }
            
            _logger.LogInformation($"Directory in {_bucketName} with key name {keyName} does not exist");
            return 0;
        }
        catch (AmazonS3Exception e)
        {
            _logger.LogError($"Error when deleting item with Key {keyName} from {_bucketName}");
            _logger.LogInformation(e.Message);
            throw;
        }
       
    }

    public async Task<string?> GenerateUrl(string imagePath, string bucketDir)
    {
        try
        {
            var key = DirectoryPrefix + bucketDir + "/" + imagePath;
            var responseIfExists = await DirectoryExists(key);

            if (responseIfExists)
            {
                var request = new GetPreSignedUrlRequest
                {
                    BucketName = _bucketName,
                    Key = key,
                    Expires = DateTime.UtcNow.AddHours(12)
                };

                var imgUrl = await AmazonS3Client.GetPreSignedURLAsync(request);
                return imgUrl;
            }
            else
            {
                throw new AmazonS3Exception("The path of the image in the bucket is not correct");
            }
           
        }
        catch (AmazonS3Exception e)
        {
          _logger.LogInformation($"Error when trying to generate preSignedUrl in {_bucketName} with file {imagePath}");
          return null;
        }
    }

    public async Task<int> ListItemsFromBuckets()
    {
        try
        {
            var request = new ListObjectsV2Request
            {
                BucketName = _bucketName,
                Prefix = DirectoryPrefix
            };
            var response = await AmazonS3Client.ListObjectsV2Async(request);
            foreach (S3Object entry in response.S3Objects)
            {
                Console.WriteLine($"Key: {entry.Key}, Size: {entry.Size}");
            }
        }
        catch (AmazonS3Exception e)
        {
            _logger.LogError($"Error when listing the items from {_bucketName}");
            _logger.LogInformation(e.Message);
            return -1;
        }

        return 1;
    }

    
}
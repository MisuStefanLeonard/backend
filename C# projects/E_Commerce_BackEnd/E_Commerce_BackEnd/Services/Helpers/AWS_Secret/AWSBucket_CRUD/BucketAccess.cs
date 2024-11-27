using SkiaSharp;
using System.Net;
using Amazon;
using Amazon.S3.Model;
using Microsoft.Extensions.Caching.Memory;


namespace E_Commerce_BackEnd.Services.Helpers.AWS_Secret.AWSBucket_CRUD;

using Amazon.S3;


public class BucketAccess : IBucketAcces
{
    private const string BucketName = "texxbucket";
    private const string DirectoryPrefix = "images/";
    private static readonly AmazonS3Client AmazonS3Client = new (RegionEndpoint.EUCentral1);
    // private readonly IMemoryCache _cache;
    private readonly ILogger<BucketAccess> _logger;
    private readonly string? _cloudFontDomain;
    

    public BucketAccess(ILogger<BucketAccess> logger, IConfiguration configuration)
    {
        _logger = logger;
        // _cache = cache;
        _cloudFontDomain = configuration["CloudFont:Id"];
    }

    // private byte[] ResizeImage(MemoryStream imageStream, int maxWidth , int maxHeight)
    // {
    //     SKBitmap image = SKBitmap.Decode(imageStream);
    //
    //     var ratioX = (double)maxWidth / image.Width;
    //     var ratioY = (double)maxHeight / image.Height;
    //     var ratio = Math.Min(ratioX, ratioY);
    //
    //     var newWidth = (int)(image.Width * ratio);
    //     var newHeight = (int)(image.Height * ratio);
    //
    //     var info = new SKImageInfo(newWidth, newHeight);
    //     image = image.Resize(info, SKFilterQuality.High);
    //
    //     using var ms = new MemoryStream();
    //     image.Encode(ms, SKEncodedImageFormat.Jpeg, 100);
    //     return ms.ToArray();
    //     
    // }

    private async Task<bool> DirectoryExists(string key)
    {
        try
        {
            var listObjectRequest = new ListObjectsV2Request
            {
                BucketName = BucketName,
                Prefix = key
            };

            var response = await AmazonS3Client.ListObjectsV2Async(listObjectRequest);

            if (response.HttpStatusCode == HttpStatusCode.OK)
            {
                _logger.LogInformation($"Directory in {BucketName} with name {key} exists");
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

    private async Task<bool> CreateDirInBucket(string name)
    {
        try
        {
            var createDirRequest = new PutObjectRequest
            {
                BucketName = BucketName,
                Key = name,
                ContentBody = string.Empty
            };

            var response = await AmazonS3Client.PutObjectAsync(createDirRequest);
            return response.HttpStatusCode == HttpStatusCode.OK;
        }
        catch (AmazonS3Exception e)
        {
            _logger.LogInformation(e.Message);
            return false;
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
        // subDirectory is the main folder after images:
        try
        {
            var directoryExists = await DirectoryExists($"images/{subDirectory}/");
            if (!directoryExists && subDirectory != null)
            {
                var responseFromCreatingFolder = await CreateDirInBucket(subDirectory);
                if (responseFromCreatingFolder)
                {
                    _logger.LogInformation($"Created file with name : {subDirectory}");
                }
                else
                {
                    throw new AmazonS3Exception($"Nu s-a putut crea fisierul cu numele {subDirectory}");
                }
            }
            string key;

            if (string.IsNullOrEmpty(subDirectory))
            {
                key = DirectoryPrefix + fileNameInS3;
            }
            else
            {
                key = DirectoryPrefix + subDirectory + "/" + fileNameInS3;

            }

            using var imageStream = new MemoryStream(); 
            await File.OpenRead(imagePath).CopyToAsync(imageStream);

            // var imageResized = ResizeImage(imageStream, 900, 400);
            // using var newResizedImage = new MemoryStream(imageResized);
            
            
            var request = new PutObjectRequest
            {
                BucketName = BucketName,
                Key = key,
                InputStream = imageStream,
                ContentType = "image/*"
            };
            
            var response = await AmazonS3Client.PutObjectAsync(request);
            if (response.HttpStatusCode == HttpStatusCode.OK)
            {
                _logger.LogInformation($"Successfully uploaded {fileNameInS3} to {BucketName}.");
                return 1;
            }

            _logger.LogError($"Could not upload {fileNameInS3} to {BucketName}.");
            return -1;
        }
        catch (AmazonS3Exception ex)
        {
            _logger.LogError($"Error when adding the item from {BucketName}");
            Console.WriteLine(ex.Message);
            return -1;
        }
    }

    public async Task<int> AddOrUpdateToBucket(MemoryStream imageStream, string? subDirectory, string fileNameInS3)
    {
        try
        {
            var directoryExists = await DirectoryExists($"images/{subDirectory}/");
            if (!directoryExists && subDirectory != null)
            {
                var responseFromCreatingFolder = await CreateDirInBucket(subDirectory);
                if (responseFromCreatingFolder)
                {
                    _logger.LogInformation($"Created file with name : {subDirectory}");
                }
                else
                {
                    throw new AmazonS3Exception($"Nu s-a putut crea fisierul cu numele {subDirectory}");
                }
            }
            
            string key;
            if (string.IsNullOrEmpty(subDirectory))
            {
                key = DirectoryPrefix + fileNameInS3;
            }
            else
            {
                key = DirectoryPrefix + subDirectory + "/" + fileNameInS3;

            }

            // var imageResized = ResizeImage(imageStream, 900, 400);
            // using var resizedImageStream = new MemoryStream(imageResized);
            
            var request = new PutObjectRequest
            {
                BucketName = BucketName,
                Key = key,
                InputStream = imageStream,
                ContentType = "image/*"
            };
            
            var response = await AmazonS3Client.PutObjectAsync(request);
            if (response.HttpStatusCode == HttpStatusCode.OK)
            {
                _logger.LogInformation($"Successfully uploaded {fileNameInS3} to {BucketName}.");
                return 1;
            }

            _logger.LogError($"Could not upload {fileNameInS3} to {BucketName}.");
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
                await AmazonS3Client.DeleteObjectAsync(BucketName, keyName);
                _logger.LogInformation($"Succesfully deleted item with keyname {keyName}");
                return 1;
            }
            
            _logger.LogInformation($"Directory in {BucketName} with key name {keyName} does not exist");
            return 0;
        }
        catch (AmazonS3Exception e)
        {
            _logger.LogError($"Error when deleting item with Key {keyName} from {BucketName}");
            _logger.LogInformation(e.Message);
            throw;
        }
       
    }

    // public async Task<string?> GenerateUrl(string imagePath, string bucketDir)
    // {
    //     try
    //     {
    //         var key = DirectoryPrefix + bucketDir + "/" + imagePath;
    //         var cacheKey = $"presignedUrl_{key}";
    //         
    //         if (_cache.TryGetValue(cacheKey, out string? cachedUrl))
    //         {
    //             return cachedUrl;
    //         }
    //         var responseIfExists = await DirectoryExists(key);
    //
    //         if (responseIfExists)
    //         {
    //             var request = new GetPreSignedUrlRequest
    //             {
    //                 BucketName = BucketName,
    //                 Key = key,
    //                 Expires = DateTime.UtcNow.AddHours(12)
    //             };
    //
    //             var imgUrl = await AmazonS3Client.GetPreSignedURLAsync(request);
    //             
    //             _cache.Set(cacheKey, imgUrl, TimeSpan.FromHours(12));
    //             return imgUrl;
    //         }
    //         else
    //         {
    //             throw new AmazonS3Exception("The path of the image in the bucket is not correct");
    //         }
    //        
    //     }
    //     catch (AmazonS3Exception e)
    //     {
    //       _logger.LogInformation($"Error when trying to generate preSignedUrl in {BucketName} with file {imagePath}");
    //       Console.WriteLine(e.Message);
    //       return null;
    //     }
    // }
    
    public async Task<string?> GenerateUrl(string imagePath, string bucketDir)
    {
        try
        {
            // Construct the CloudFront URL directly
            await Task.Delay(1);
            var cloudFrontUrl = $"{_cloudFontDomain}/images/{bucketDir}/{imagePath}";
            return cloudFrontUrl;
        }
        catch (AmazonS3Exception e)
        {
            _logger.LogInformation($"Error when trying to generate CloudFront URL in {BucketName} with file {imagePath}");
            Console.WriteLine(e.Message);
            return null;
        }
    }


    public async Task<Stream?> DownloadFile(string s3Key)
    {
        try
        {
           
            var getFile = new GetObjectRequest
            {
                BucketName = BucketName,
                Key = s3Key
            };
            var response = await AmazonS3Client.GetObjectAsync(getFile);
            return response.ResponseStream;
            
        }
        catch (AmazonS3Exception e)
        {
            Console.WriteLine(e);
            return null;
        }
    }

    public async Task<List<string>?> ListDirsFromBuckets()
    {
        try
        {
            var listOfDirs = new List<string>();
            var request = new ListObjectsV2Request
            {
                BucketName = BucketName,
                Prefix = DirectoryPrefix,
                Delimiter = "/"
            };
            var response = await AmazonS3Client.ListObjectsV2Async(request);
            var directoriesParsed = response.CommonPrefixes
                .Select(dir =>
                {
                    var firstStep = dir.Remove(0, 7); // removing images/
                    var secondStep = firstStep.TrimEnd('/'); // removing end /
                    return secondStep;
                });
            listOfDirs.AddRange(directoriesParsed);

            return listOfDirs;
        }
        catch (AmazonS3Exception e)
        {
            _logger.LogError($"Error when listing the items from {BucketName}");
            _logger.LogInformation(e.Message);
            return null;
        }
    }

    
}
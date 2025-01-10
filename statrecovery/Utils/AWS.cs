using Amazon.S3;
using Amazon.S3.Model;
using statrecovery.Models;

namespace statrecovery.Utils
{
    public static class AWS
    {
        private static readonly string AwsGenericErrorMessage = @"Error {0} AWS! {1}";

        public static async Task<bool> DownloadFileAsync(IAmazonS3 client, string objectName, string filePath, CancellationToken ct)
        {
            var req = new GetObjectRequest
            {
                BucketName = Settings.AwsBucketName,
                Key = objectName,
            };

            try
            {
                using var resp = await client.GetObjectAsync(req, ct);
                await resp.WriteResponseStreamToFileAsync(filePath, false, ct);
                return resp.HttpStatusCode == System.Net.HttpStatusCode.OK;
            }
            catch (AmazonS3Exception ex)
            {
                if (ex.ErrorCode == "NoSuchKey")
                {
                    return false;
                }
                throw new InvalidOperationException(string.Format(AwsGenericErrorMessage, "downloading from", $"Bucket: {Settings.AwsBucketName}, Object: {objectName}, Error Message: {ex.Message}"));
            }
        }

        public static async Task<bool> UploadFileAsync(IAmazonS3 client, string objectName, string filePath, CancellationToken ct)
        {
            var req = new PutObjectRequest
            {
                BucketName = Settings.AwsBucketName,
                Key = objectName,
                FilePath = filePath,
            };

            try
            {
                var resp = await client.PutObjectAsync(req, ct);
                return resp.HttpStatusCode == System.Net.HttpStatusCode.OK;
            }
            catch (AmazonS3Exception ex)
            {
                throw new InvalidOperationException(string.Format(AwsGenericErrorMessage, "uploading to", $"Bucket: {Settings.AwsBucketName}, Object: {objectName}, Error Message: {ex.Message}"));
            }
        }

        public static async Task<bool> UploadStreamAsync(IAmazonS3 client, string objectName, Stream stream, CancellationToken ct)
        {
            var req = new PutObjectRequest
            {
                BucketName = Settings.AwsBucketName,
                Key = objectName,
                InputStream = stream,
            };

            try
            {
                var resp = await client.PutObjectAsync(req, ct);
                return resp.HttpStatusCode == System.Net.HttpStatusCode.OK;
            }
            catch (AmazonS3Exception ex)
            {
                throw new InvalidOperationException(string.Format(AwsGenericErrorMessage, "uploading stream to", $"Bucket: {Settings.AwsBucketName}, Object: {objectName}, Error Message: {ex.Message}"));
            }
        }

        public static async Task<bool> DeleteObjectAsync(IAmazonS3 client, string objectName, CancellationToken ct)
        {
            var req = new DeleteObjectRequest
            {
                BucketName = Settings.AwsBucketName,
                Key = objectName,
            };

            try
            {
                var resp = await client.DeleteObjectAsync(req, ct);
                return resp.HttpStatusCode == System.Net.HttpStatusCode.OK;
            }
            catch (AmazonS3Exception ex)
            {
                throw new InvalidOperationException(string.Format(AwsGenericErrorMessage, "deleting from", $"Bucket: {Settings.AwsBucketName}, Object: {objectName}, Error Message: {ex.Message}"));
            }
        }

        public static async Task<AwsObject[]> ListObjectsAsync(IAmazonS3 client, string objectPrefix, CancellationToken ct)
        {
            var req = new ListObjectsV2Request
            {
                BucketName = Settings.AwsBucketName,
                MaxKeys = Settings.PaginationSize,
                Prefix = string.IsNullOrWhiteSpace(objectPrefix) ? null : objectPrefix,
            };

            var ret = new List<AwsObject>();

            do
            {
                var resp = await client.ListObjectsV2Async(req, ct);

                if (resp.HttpStatusCode == System.Net.HttpStatusCode.OK)
                {
                    ret.AddRange(resp.S3Objects.Select(obj => new AwsObject(obj.Key, obj.Size, obj.LastModified)));

                    if (resp.IsTruncated)
                    {
                        req.StartAfter = resp.S3Objects.LastOrDefault()?.Key;
                    }
                    else
                    {
                        req = null;
                    }
                }
            }
            while (req != null);

            return [.. ret];
        }

        public static AmazonS3Client GetClient() =>
            new(Settings.AwsAccessKeyID, Settings.AwsAccessSecret, Amazon.RegionEndpoint.GetBySystemName(Settings.AwsBucketRegion));
    }
}
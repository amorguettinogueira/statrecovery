using Amazon.S3;
using Amazon.S3.Model;

namespace statrecovery.test
{
    public static class Utils
    {
        public static void UploadFile(IAmazonS3 client, string bucketName)
        {
            var filePath = @"C:\Users\MI-DRI\Documents\Github\convertiss-wireframe.png";
            var keyName = Path.GetFileName(filePath);

            var request = new PutObjectRequest
            {
                //BucketName = bucketName,
                BucketName = bucketName,
                Key = $"subdir/{keyName}",
                FilePath = filePath
            };

            var response = client.PutObjectAsync(request).GetAwaiter().GetResult();
            if (response.HttpStatusCode == System.Net.HttpStatusCode.OK)
            {
                Console.WriteLine($"Successfully uploaded {keyName} to {bucketName}.");
            }
            else
            {
                Console.WriteLine($"Could not upload {keyName} to {bucketName}.");
            }
        }
    }
}
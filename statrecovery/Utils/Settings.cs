using Microsoft.Extensions.Configuration;

namespace statrecovery.Utils
{
    public static class Settings
    {
        /* these settings are not configurable at runtime */
        public static readonly string ConfigurationFile = "appsettings.json";
        public static readonly string UserSecretsKey = "41ffb1cf-3d2e-4b8f-9f00-4f2cc1db0b46";
        public static readonly string MetadataFileName = "metadata.db";

        /* these are seetings the user must/can set for runtime */
        public static int PaginationSize { get; set; }
        public static int PoNumberPaddingLength { get; set; }
        public static bool SensitiveFileNaming { get; set; }
        public static int ParallelTaskCount { get; set; }
        public static string TemporaryFilePath { get; set; } = string.Empty;
        public static string ZipFilePrefix { get; set; } = string.Empty;
        public static string AwsAccessKeyID { get; set; } = string.Empty;
        public static string AwsAccessSecret { get; set; } = string.Empty;
        public static string AwsBucketRegion { get; set; } = string.Empty;
        public static string AwsBucketName { get; set; } = string.Empty;

        public static void LoadConfiguration(IConfigurationRoot configuration)
        {
            PaginationSize = configuration.GetValue(nameof(PaginationSize), 100);
            PoNumberPaddingLength = configuration.GetValue(nameof(PoNumberPaddingLength), 10);
            SensitiveFileNaming = configuration.GetValue(nameof(SensitiveFileNaming), false);
            ParallelTaskCount = configuration.GetValue(nameof(ParallelTaskCount), 8);
            TemporaryFilePath = configuration.GetValue(nameof(TemporaryFilePath), ".");
            ZipFilePrefix = configuration.GetValue(nameof(ZipFilePrefix), string.Empty);

            AwsAccessKeyID = configuration.GetValue(nameof(AwsAccessKeyID), string.Empty);
            AwsAccessSecret = configuration.GetValue(nameof(AwsAccessSecret), string.Empty);
            AwsBucketRegion = configuration.GetValue(nameof(AwsBucketRegion), string.Empty);
            AwsBucketName = configuration.GetValue(nameof(AwsBucketName), string.Empty);
        }

        private static List<string> AddIfEmpty(this List<string> list, string configContent, string configName)
        {
            if (string.IsNullOrWhiteSpace(configContent))
            {
                list.Add(configName);
            }
            return list;
        }

        public static void ValidateConfiguration()
        {
            var invalidItems = (new List<string>())
                .AddIfEmpty(AwsAccessKeyID, nameof(AwsAccessKeyID))
                .AddIfEmpty(AwsAccessSecret, nameof(AwsAccessSecret))
                .AddIfEmpty(AwsBucketRegion, nameof(AwsBucketRegion))
                .AddIfEmpty(AwsBucketName, nameof(AwsBucketName));

            if (invalidItems.Count > 0)
            {
                throw new InvalidOperationException(
                    $"\nThese settings need to be defined before execution: \n\t{string.Join("\n\t", invalidItems)}." +
                    $"\n\nYou can set them using a secrets.json file (recommended), passing them as command line arguments or add them to '{ConfigurationFile}' file (unsafe).\t");
            }
        }
    }
}
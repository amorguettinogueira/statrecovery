using Amazon.S3;
using Amazon.Util.Internal;
using statrecovery.Models;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace statrecovery.Utils
{
    public static class Process
    {
        private static readonly Regex csvRegex = new("\\.[Cc][Ss][Vv]$");
        private static readonly Regex pdfRegex = new("\\.[Pp][Dd][Ff]$");
        private static readonly Regex zipRegex = new("\\.[Zz][Ii][Pp]$");

        public static async Task<Database> LoadMetadataAsync()
        {
            var localMetadataDb = Path.Combine(".", Settings.MetadataFileName);

            using var client = AWS.GetClient();

            if (await AWS.DownloadFileAsync(client, Settings.MetadataFileName, localMetadataDb, CancellationToken.None))
            {
                return DatabaseManager.LoadFromFile(localMetadataDb);
            }

            return new Database();
        }

        public static async Task SaveMetadataAsync(Database db)
        {
            var localMetadataDb = Path.Combine(".", Settings.MetadataFileName);

            DatabaseManager.SaveToFile(db, localMetadataDb);

            using var client = AWS.GetClient();

            if (!await AWS.UploadFileAsync(client, Settings.MetadataFileName, localMetadataDb, CancellationToken.None))
            {
                throw new ExternalException($"Unexpected error saving metadata file to S3! Bucket: {Settings.AwsBucketName}, Object: {Settings.MetadataFileName}");
            }
        }

        private static bool FileAlreadyExists(string localFile, AwsObject cloudFile)
        {
            //this could be a lot better and trustworthy if ListObjectVersions access
            //was open in AWS it would be possible to replace it by a hash calculation
            //instead of trusting just in size and date
            var local = new FileInfo(localFile);
            return local.Exists &&
                local.Length == cloudFile.Size &&
                local.LastWriteTime == cloudFile.Date;
        }

        private static async Task DownloadZipFileAsync(Database db, IAmazonS3 client,
            AwsObject zipFile, ConcurrentBag<Task> tasks, SemaphoreSlim semaphore, CancellationToken ct)
        {
            var localZipFile = Path.Combine(Settings.TemporaryFilePath, zipFile.Name);
            var localTempFolder = Path.Combine(Settings.TemporaryFilePath, Path.GetFileNameWithoutExtension(Path.GetTempFileName()));

            if (FileAlreadyExists(localZipFile, zipFile) ||
                await AWS.DownloadFileAsync(client, zipFile.Name, localZipFile, ct))
            {
                File.SetLastWriteTime(localZipFile, zipFile.Date);
                using var zip = ZipFile.OpenRead(localZipFile);
                zip.ExtractToDirectory(localTempFolder);

                var extractFiles = Directory.GetFiles(localTempFolder)
                    .Select(fileName =>
                        (type: csvRegex.IsMatch(fileName)
                                ? 'c' : (pdfRegex.IsMatch(fileName) ? 'p' : 'x')
                        , fileName))
                    .Where(obj => obj.type != 'x')
                    .GroupBy(obj => obj.type)
                    .ToDictionary(group => group.Key, group => group.Select(obj => obj.fileName).ToArray());

                if (extractFiles.TryGetValue('p', out var filesToUpload) && filesToUpload != null && filesToUpload.Length > 0)
                {
                    var csvIndex = LoadCsvs(extractFiles.TryGetValue('c', out var values) ? values : [], ct);
                    foreach (var file in filesToUpload)
                    {
                        if (ct.IsCancellationRequested)
                        {
                            break;
                        }
                        var csvKey = csvIndex.Keys.FirstOrDefault(pdf => pdf.EndsWith(Path.GetFileName(file), Settings.SensitiveFileNaming
                            ? StringComparison.OrdinalIgnoreCase
                            : StringComparison.Ordinal), string.Empty);
                        var poNumber = string.IsNullOrWhiteSpace(csvKey) ? string.Empty : csvIndex[csvKey];
                        tasks.Add(Task.Run(async () =>
                        {
                            await semaphore.WaitAsync();
                            try
                            {
                                await UploadPdfFileAsync(db, zipFile.Name, client, file, poNumber, tasks, ct);
                            }
                            finally
                            {
                                semaphore.Release();
                            }
                        }, ct));
                    }
                }
            }
        }

        private static readonly string CsvFieldSeparator = "~";

        private static (int fileIndex, int poIndex) GetIndexes(string line)
        {
            int poIndex = -1, fnIndex = -1;

            var header = line.Split(CsvFieldSeparator);

            for (int i = 0; i < header?.Length && (poIndex == -1 || fnIndex == -1); i++)
            {
                if (header[i].Equals("PO Number", StringComparison.OrdinalIgnoreCase))
                {
                    poIndex = i;
                }
                else if (header[i].Equals("Attachment List", StringComparison.OrdinalIgnoreCase))
                {
                    fnIndex = i;
                }
            }

            return (fnIndex, poIndex);
        }

        private static string GetPdfFileName(string filePath)
        {
            filePath = Path.AltDirectorySeparatorChar == '\\'
                ? Path.GetFileName(filePath.Replace('/', '\\'))
                : filePath;
            return Path.GetFileName(filePath);
        }

        private static bool TryParseCsvLines(this string[]? lines, [NotNullWhen(true)] out Dictionary<string, string>? dict)
        {
            if ((lines?.Length ?? 0) > 0)
            {
                (int fnIndex, int poIndex) = GetIndexes(lines?[0] ?? string.Empty);

                if (poIndex != -1 && fnIndex > -1)
                {
                    dict = lines?[1..]
                        .Select(line =>
                        {
                            var fields = line.Split(CsvFieldSeparator);
                            if (fields?.Length > 0 && fnIndex < fields.Length)
                            {
                                var files = fields[fnIndex].Split(",");
                                if (files?.Length > 0)
                                {
                                    return files.Select(file =>
                                        KeyValuePair.Create(GetPdfFileName(file),
                                        poIndex < fields.Length ? fields[poIndex] : string.Empty));
                                }
                            }
                            return [KeyValuePair.Create(string.Empty, string.Empty)];
                        })
                        .SelectMany(list => list, (list, item) => item)
                        .Where(obj => !string.IsNullOrEmpty(obj.Key))
                        //Attention! There were duplicates in the files (one pdf showing in
                        //more than a line). This was resolved moving those cases to
                        //the lowest PO number in case they have different values
                        .GroupBy(g => g.Key)
                        .ToDictionary(group => group.Key, group => group.Min(kv => kv.Value) ?? string.Empty)
                        ?? [];

                    return true;
                }
            }
            dict = null;
            return false;
        }

        public static Dictionary<string, string> LoadCsvs(string[] csvFiles, CancellationToken ct)
        {
            var dict = new Dictionary<string, string>();

            foreach (var csvFile in csvFiles)
            {
                if (ct.IsCancellationRequested)
                {
                    break;
                }

                if (File.ReadAllLines(csvFile).TryParseCsvLines(out var itens))
                {
                    foreach (var (file, poNumber) in itens)
                    {
                        if (ct.IsCancellationRequested)
                        {
                            break;
                        }
                        dict.Add(file, poNumber);
                    }
                }
            }

            return dict;
        }

        private static async Task UploadPdfFileAsync(Database db, string zipName, IAmazonS3 client,
            string file, string poNumber, ConcurrentBag<Task> tasks, CancellationToken ct)
        {
            var pdf = new ExtractedPdf(Path.GetFileName(file), File.GetCreationTime(file), poNumber);
            var objectName = $"by-po/{pdf.PoNumber}/{pdf.Name}";
            if (await AWS.UploadFileAsync(client, objectName, file, ct))
            {
                db.AddPdf(zipName, pdf);
            }
        }

        public static async Task ProcessZipFilesAsync(Database db)
        {
            var ct = new CancellationTokenSource().Token;
            var semaphore = new SemaphoreSlim(Settings.ParallelTaskCount);
            using var client = AWS.GetClient();

            var zipsToProcess = (await AWS.ListObjectsAsync(client, Settings.ZipFilePrefix, ct))
                .Where(zipFile => zipRegex.IsMatch(zipFile.Name) &&
                    !db.Keys.Any(key => key.Equals(zipFile.Name, Settings.SensitiveFileNaming
                    ? StringComparison.OrdinalIgnoreCase
                    : StringComparison.Ordinal)));

            var tasks = new ConcurrentBag<Task>();

            foreach (var zipName in zipsToProcess)
            {
                tasks.Add(
                    Task.Run(async () =>
                    {
                        await semaphore.WaitAsync();
                        try
                        {
                            await DownloadZipFileAsync(db, client, zipName, tasks, semaphore, ct);
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    }, ct));
            }

            while (!ct.IsCancellationRequested && tasks.Any(task => (int)task.Status < (int)TaskStatus.RanToCompletion))
            {
                await Task.WhenAll(tasks);
            }

            if (tasks.Any(task => task.IsFaulted))
            {
                Console.WriteLine($"Error occurred during the execution...");
                foreach (var task in tasks.Where(task => task.IsFaulted))
                {
                    Console.WriteLine($"\t Task {task.Id}: {task.Exception?.Message ?? "No error could be retrieved for this task"}");
                }
            }

            if (ct.IsCancellationRequested)
            {
                Console.WriteLine($"*** Execution was cancelled ***");
            }
        }

        //this method was not supposed to exist, but I upload all pdfs in the wrong place in the first run by accident
        public static async Task DeletePdfs()
        {
            using var client = AWS.GetClient();

            var objectsToDelete = await AWS.ListObjectsAsync(client, "by-po/", CancellationToken.None);

            await Parallel.ForEachAsync(objectsToDelete, async (objToDelete, ct) =>
            {
                await AWS.DeleteObjectAsync(client, objToDelete.Name, CancellationToken.None);
            });
        }
    }
}
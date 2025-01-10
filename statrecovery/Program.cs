using Microsoft.Extensions.Configuration;
using statrecovery.Models;
using statrecovery.Utils;

var Configuration = new ConfigurationBuilder()
  .AddJsonFile(Settings.ConfigurationFile, optional: true, reloadOnChange: true)
  .AddCommandLine(args)
  .AddUserSecrets(Settings.UserSecretsKey)
  .Build();

Settings.LoadConfiguration(Configuration);

try
{
    Settings.ValidateConfiguration(); //abort in case settings are not okay

    //await Process.DeletePdfs();

    Database db = await Process.LoadMetadataAsync();
    try
    {
        await Process.ProcessZipFilesAsync(db);
    }
    finally
    {
        await Process.SaveMetadataAsync(db);
    }
}
catch (Exception e)
{
    //this could go to sentry or graylog
    Console.WriteLine(e.Message);
}
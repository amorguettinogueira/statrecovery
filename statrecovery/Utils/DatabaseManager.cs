using statrecovery.Models;

namespace statrecovery.Utils
{
    public static class DatabaseManager
    {
        public static Database LoadFromFile(string fileName)
        {
            var lines = File.ReadAllLines(fileName);

            if (lines != null && lines.Length > 0)
            {
                var values = lines.Select(line =>
                    {
                        var pieces = line.Split('\t');
                        if (pieces.Length == 2)
                        {
                            var zipName = pieces[0];
                            var pdfs = pieces[1].Split('\\').Select(text => text.TryParsePdf(out var pdf) ? pdf : null).ToList();
                            if (!pdfs.Any(item => item == null))
                            {
                                return (key: zipName, values: pdfs);
                            }
                        }
                        //force failure in case something is off
                        return (null!, null!);
                    });

                if (values != null &&
                    !values.Any(obj => obj.key == null || obj.values == null || obj.values.Count == 0))
                {
                    //was able to parse without error
                    var db = new Database();
                    foreach (var item in values)
                    {
                        db.TryAdd(item.key, item.values!);
                    }
                    return db;
                }
            }

            //if reached this point something in the file
            //is out of place because parsing failed
            return new();
        }

        public static void SaveToFile(Database db, string fileName) =>
            File.WriteAllLines(fileName,
                db.Select(entry => $"{entry.Key}\t{string.Join('\\', entry.Value.Select(pdf => pdf.ToString()))}"));
    }
}
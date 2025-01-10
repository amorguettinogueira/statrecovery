using System.Collections.Concurrent;

namespace statrecovery.Models
{
    public class Database : ConcurrentDictionary<string, List<ExtractedPdf>>
    {
        public Database AddPdf(string zipFile, ExtractedPdf pdf)
        {
            if (!TryGetValue(zipFile, out var pdfList))
            {
                pdfList = [];
                this[zipFile] = pdfList;
            }

            pdfList.Add(pdf);

            return this;
        }
    }
}
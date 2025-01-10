using statrecovery.Models;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace statrecovery.Utils
{
    public static class Extensions
    {
        public static bool TryParsePdf(this string text, [NotNullWhen(true)] out ExtractedPdf? pdf)
        {
            //$"{Date:u}|{Name}|{PoNumber}";
            var pieces = text.Split('|');
            if (pieces.Length == 3 && DateTime.TryParseExact(pieces[0], "u", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var date))
            {
                pdf = new(pieces[1], date, pieces[2]);
                return true;
            }
            pdf = null;
            return false;
        }
    }
}
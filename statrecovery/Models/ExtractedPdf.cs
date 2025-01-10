using statrecovery.Utils;

namespace statrecovery.Models
{
    public class ExtractedPdf(string name, DateTime date, string poNumber)
    {
        public string Name { get; set; } = name;
        public DateTime Date { get; set; } = date;
        public string PoNumber { get; set; } = poNumber.PadLeft(Settings.PoNumberPaddingLength, '0');

        public override bool Equals(object? obj) =>
            obj is ExtractedPdf pdf &&
            pdf.Name == Name &&
            pdf.Date == Date &&
            pdf.PoNumber == PoNumber;

        public override string? ToString() =>
            $"{Date:u}|{Name}|{PoNumber}";

        public override int GetHashCode() =>
            ToString()?.GetHashCode() ?? 0;
    }
}
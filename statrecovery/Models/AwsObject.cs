namespace statrecovery.Models
{
    public class AwsObject(string name, long size, DateTime date)
    {
        public string Name { get; set; } = name;
        public long Size { get; set; } = size;
        public DateTime Date { get; set; } = date;
    }
}
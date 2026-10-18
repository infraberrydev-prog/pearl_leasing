namespace pearl_leasing.Models
{
    public class Building
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Code { get; set; }

        public ICollection<StallUnit> StallUnits { get; set; } = new List<StallUnit>();
    }
}

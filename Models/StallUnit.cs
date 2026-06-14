namespace pearl_leasing.Models
{
    public class StallUnit
    {
        public int Id { get; set; }
        public string UnitNumber { get; set; } = string.Empty;
        public bool IsOccupied { get; set; } = false;
        public int BuildingId { get; set; }
        public Building? Building { get; set; }
    }
}

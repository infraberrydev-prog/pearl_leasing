namespace pearl_leasing.Models
{
    public class StallUnit
    {
        public int Id { get; set; }
        public string UnitNumber { get; set; } = string.Empty; // e.g., "Stall A-1"
        public bool IsOccupied { get; set; } = false;

        // Foreign Key at Navigation para sa Building
        public int BuildingId { get; set; }
        public Building? Building { get; set; }
    }
}

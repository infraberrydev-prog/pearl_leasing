namespace pearl_leasing.DTO
{
    public class CreateBulkStallsRequestDto
    {
        public int BuildingId { get; set; }
        public string Prefix { get; set; } = "A";
        public int StartNumber { get; set; } = 1;
        public int EndNumber { get; set; } = 21;
    }
}

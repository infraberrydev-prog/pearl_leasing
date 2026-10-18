namespace pearl_leasing.DTO
{
    public class SavingSuccessfulResponse
    {
        public string message { get; set; } = string.Empty;
        public List<int> savedIds { get; set; } = new();
    }
}

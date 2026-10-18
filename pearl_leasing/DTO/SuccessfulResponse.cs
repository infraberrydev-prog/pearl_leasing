namespace pearl_leasing.DTO
{
    public class SuccessfulResponse
    {
        public string message { get; set; } = string.Empty;
    }

    public class SuccessfulResponse<T> : SuccessfulResponse
    {
        public T? data { get; set; }

        public SuccessfulResponse() { }

        public SuccessfulResponse(string message, T? data = default)
        {
            this.message = message;
            this.data = data;
        }
    }
}

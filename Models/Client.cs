namespace LashAccountingSystem.Models
{
    public class Client
    {
        public int ClientId { get; set; }
        public string ClientSurname { get; set; } = string.Empty;
        public string ClientName { get; set; } = string.Empty;
        public string? ClientPatronymic { get; set; }
        public string ClientPhoneNumber { get; set; } = string.Empty;
        public string? ClientEmail { get; set; }
        public string? ClientHealthFeatures { get; set; }
    }
}

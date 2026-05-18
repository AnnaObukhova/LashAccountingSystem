namespace LashAccountingSystem.Models
{
    public class ServiceMaterial
    {
        public int Id { get; set; }
        public int TempId { get; set; }
        public int ServiceId { get; set; }
        public int MaterialId { get; set; }
        public string MaterialName { get; set; }
        public decimal QuantityRequired { get; set; }
    }
}
namespace LashAccountingSystem.Models
{
    public class ServiceMaterial
    {
        public int Id { get; set; }
        public int TempId { get; set; }  // для временного хранения в окне добавления
        public int ServiceId { get; set; }
        public int MaterialId { get; set; }
        public string MaterialName { get; set; }
        public decimal QuantityRequired { get; set; }
    }
}
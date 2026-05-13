namespace LashAccountingSystem.Models
{
    public class Material
    {
        public int MaterialId { get; set; }
        public string MaterialName { get; set; }
        public string MaterialManufacturer { get; set; }
        public decimal MaterialPrice { get; set; }
        public string MaterialContraindications { get; set; }
        public decimal MaterialStock { get; set; }
    }
}
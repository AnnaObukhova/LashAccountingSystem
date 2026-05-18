using System;

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
        public DateTime? LastIncomingDate { get; set; }
        public string LastSupplier { get; set; }
        public decimal? LastIncomingQuantity { get; set; }
        public decimal? LastIncomingPrice { get; set; }
    }
}
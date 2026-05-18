namespace LashAccountingSystem.Models
{
    public class MaterialReportItem
    {
        public int MaterialId { get; set; }
        public string MaterialName { get; set; }
        public string Manufacturer { get; set; }
        public decimal TotalQuantity { get; set; }
        public decimal Price { get; set; }
        public decimal TotalCost { get; set; }
        public int AppointmentCount { get; set; }
        public DateTime? LastIncomingDate { get; set; }
        public string LastSupplier { get; set; }
        public decimal? LastIncomingPrice { get; set; }
        public decimal CurrentStock { get; set; }
    }
}
using System;

namespace LashAccountingSystem.Models
{
    public class PriceHistory
    {
        public int PriceHistoryId { get; set; }
        public int ServiceId { get; set; }
        public string ServiceName { get; set; }
        public decimal Price { get; set; }
        public DateTime ApplicationDate { get; set; }
    }
}
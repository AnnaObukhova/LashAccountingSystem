using System;

namespace LashAccountingSystem.Models
{
    public class Appointment
    {
        public int AppointmentId { get; set; }
        public int ClientId { get; set; }
        public string ClientName { get; set; } = string.Empty;
        public int MasterId { get; set; }
        public string MasterName { get; set; } = string.Empty;
        public int PriceHistoryId { get; set; }
        public string ServiceName { get; set; } = string.Empty;
        public decimal ServicePrice { get; set; }
        public DateTime AppointmentDate { get; set; }
        public TimeSpan AppointmentTime { get; set; }
        public string AppointmentStatus { get; set; } = string.Empty;
        public bool PaymentStatus { get; set; }
        public string? PaymentMethod { get; set; }
        public string? Notes { get; set; }
        public TimeSpan EndTime { get; set; }
        public int DisplayOrder { get; set; }
    }
}
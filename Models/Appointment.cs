using System;

namespace LashAccountingSystem.Models
{
    public class Appointment
    {
        public int AppointmentId { get; set; }
        public int ClientId { get; set; }
        public string ClientName { get; set; } = string.Empty;     // ← исправлено
        public int MasterId { get; set; }
        public string MasterName { get; set; } = string.Empty;     // ← исправлено
        public int PriceHistoryId { get; set; }
        public string ServiceName { get; set; } = string.Empty;    // ← исправлено
        public decimal ServicePrice { get; set; }
        public DateTime AppointmentDate { get; set; }
        public TimeSpan AppointmentTime { get; set; }
        public string AppointmentStatus { get; set; } = string.Empty; // ← исправлено
        public bool PaymentStatus { get; set; }
        public string? PaymentMethod { get; set; }   // может быть null
        public string? Notes { get; set; }           // может быть null
        public TimeSpan EndTime { get; set; }
    }
}
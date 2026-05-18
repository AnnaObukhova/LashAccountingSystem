using System;

namespace LashAccountingSystem.Models
{
    public class MaterialConsumption
    {
        public int ConsumptionId { get; set; }
        public int AppointmentId { get; set; }
        public string ClientName { get; set; }
        public string ServiceName { get; set; }
        public DateTime AppointmentDate { get; set; }
        public TimeSpan AppointmentTime { get; set; }
        public int MaterialId { get; set; }
        public string MaterialName { get; set; }
        public decimal MaterialConsumptionAmount { get; set; }
        public string DateTimeDisplay => $"{AppointmentDate:dd.MM.yyyy} {AppointmentTime:hh\\:mm}";
    }
}
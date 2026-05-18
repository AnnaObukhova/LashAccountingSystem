namespace LashAccountingSystem.Models
{
    public class Master
    {
        public int MasterId { get; set; }
        public string MasterSurname { get; set; }
        public string MasterName { get; set; }
        public string MasterPatronymic { get; set; }
        public string MasterPhoneNumber { get; set; }
        public string MasterSpecialization { get; set; }
        public string MasterContractNumber { get; set; }
        public string FullName => $"{MasterSurname} {MasterName}";
    }
}
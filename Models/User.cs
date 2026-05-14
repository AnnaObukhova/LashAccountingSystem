//using System;

//namespace LashAccountingSystem.Models
//{
//    public class User
//    {
//        public int UserId { get; set; }
//        public string Login { get; set; }
//        public string PasswordHash { get; set; }
//        public string Salt { get; set; }
//        public DateTime RegistrationDate { get; set; }
//        public bool IsActive { get; set; }
//    }
//}
namespace LashAccountingSystem.Models
{
    public class User
    {
        public int UserId { get; set; }
        public string Login { get; set; }
        public bool IsActive { get; set; }
    }
}
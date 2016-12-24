namespace App.Models
{
    public class User
    {
        public int id { get; set; }
        public string login { get; set; }
        public string password_hash { get; set; }
        public string salt { get; set; }
    }
}

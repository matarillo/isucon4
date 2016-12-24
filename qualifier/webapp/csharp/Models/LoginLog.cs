using System;

namespace App.Models
{
    public class LoginLog
    {
        public long id { get; set; }
        public DateTime created_at { get; set; }
        public int user_id { get; set; }
        public string login { get; set; }
        public string ip { get; set; }
        public byte succeeded { get; set; }
    }
}

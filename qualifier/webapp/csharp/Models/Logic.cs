using App.Infrastructure;
using Dapper;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace App.Models
{
    public class Logic
    {
        private Threshold _threshold;
        private IDbFactory _dbFactory;
        private IHttpContextAccessor _httpContextAccessor;

        public Logic(Threshold threshold, IDbFactory dbFactory, IHttpContextAccessor httpContextAccessor)
        {
            _threshold = threshold;
            _dbFactory = dbFactory;
            _httpContextAccessor = httpContextAccessor;
        }

        public static string CalculatePasswordHash(string password, string salt)
        {
            var input = Encoding.UTF8.GetBytes(password + ":" + salt);
            using (var algorithm = SHA256.Create())
            {
                var hash = algorithm.ComputeHash(input);
                var output = string.Join("", hash.Select(x => x.ToString("x2")));
                return output;
            }
        }

        public void LoginLog(bool succeeded, string login, int? user_id = null)
        {
            // Console.WriteLine("login_log: " + succeeded + ", " + login + "," + user_id);
            using (var conn = _dbFactory.CreateDbConnection())
            {
                conn.Open();
                conn.Execute("INSERT INTO login_log (`created_at`, `user_id`, `login`, `ip`, `succeeded`) VALUES (NOW(),@user_id,@login,@ip,@succeeded)",
                    new { user_id = user_id, login = login, ip = GetRemoteAddress(), succeeded = succeeded ? (byte)1 : (byte)0 });
            }
        }

        public bool UserLocked(User user)
        {
            if (user == null)
            {
                return false;
            }
            using (var conn = _dbFactory.CreateDbConnection())
            {
                conn.Open();
                var failures = conn.ExecuteScalar<int>("SELECT COUNT(1) AS failures FROM login_log WHERE user_id = @user_id AND id > IFNULL((select id from login_log where user_id = @user_id AND succeeded = 1 ORDER BY id DESC LIMIT 1), 0);",
                    new { user_id = user.id });
                return _threshold.UserLockThreshold <= failures;
            }
        }
        public bool IpBannded()
        {
            using (var conn = _dbFactory.CreateDbConnection())
            {
                conn.Open();
                var failures = conn.ExecuteScalar<int>("SELECT COUNT(1) AS failures FROM login_log WHERE ip = @ip AND id > IFNULL((select id from login_log where ip = @ip AND succeeded = 1 ORDER BY id DESC LIMIT 1), 0)",
                    new { ip = GetRemoteAddress() });
                return _threshold.IpBanThreshold <= failures;
            }
        }

        public (User user, string err) AttemptLogin(string login, string password)
        {
            User user;
            using (var conn = _dbFactory.CreateDbConnection())
            {
                conn.Open();
                user = conn.Query<User>("SELECT * FROM users WHERE login=@login",
                    new { login = login }).FirstOrDefault();
            }
            if (IpBannded())
            {
                LoginLog(false, login, user?.id);
                return (null, "banned");
            }
            if (UserLocked(user))
            {
                LoginLog(false, login, user?.id);
                return (null, "locked");
            }
            if (user != null && CalculatePasswordHash(password, user.salt) == user.password_hash)
            {
                LoginLog(true, login, user?.id);
                return (user, null);
            }
            else if (user != null)
            {
                LoginLog(false, login, user?.id);
                return (null, "wrong_password");
            }
            else
            {
                LoginLog(false, login, user?.id);
                return (null, "wrong_login");
            }
        }

        public User CurrentUser()
        {
            var userId = _httpContextAccessor.HttpContext.Session.GetInt32("user_id");
            if (userId == null)
            {
                return null;
            }
            using (var conn = _dbFactory.CreateDbConnection())
            {
                conn.Open();
                var user = conn.Query<User>("SELECT * FROM users WHERE id=@id",
                    new { id = userId.Value }).FirstOrDefault();
                return user;
            }
        }

        public LoginLog LastLogin()
        {
            var user = CurrentUser();
            if (user == null)
            {
                return null;
            }
            using (var conn = _dbFactory.CreateDbConnection())
            {
                conn.Open();
                var logs = conn.Query<LoginLog>("SELECT * FROM login_log WHERE succeeded = 1 AND user_id = @id ORDER BY id DESC LIMIT 2",
                    new { id = user.id });
                return logs.LastOrDefault();
            }
        }

        public IList<string> BannedIps()
        {
            var threshold = _threshold.IpBanThreshold;
            using (var conn = _dbFactory.CreateDbConnection())
            {
                conn.Open();
                var not_succeeded = conn.Query("SELECT ip FROM (SELECT ip, MAX(succeeded) as max_succeeded, COUNT(1) as cnt FROM login_log GROUP BY ip) AS t0 WHERE t0.max_succeeded = 0 AND t0.cnt >= @threshold",
                    new { threshold = threshold });
                var ips = not_succeeded.Select(x => (string)x.ip).ToList();
                var last_succeeds = conn.Query("SELECT ip, MAX(id) AS last_login_id FROM login_log WHERE succeeded = 1 GROUP by ip");
                foreach (var row in last_succeeds)
                {
                    var count = conn.ExecuteScalar<int>("SELECT COUNT(1) AS cnt FROM login_log WHERE ip = @ip AND @last_login_id < id",
                        new { ip = (string)row.ip, last_login_id = (long)row.last_login_id });
                    if (threshold <= count)
                    {
                        ips.Add((string)row.ip);
                    }
                }
                return ips;
            }
        }
        public IList<string> LockedUsers()
        {
            var threshold = _threshold.UserLockThreshold;
            using (var conn = _dbFactory.CreateDbConnection())
            {
                conn.Open();
                var not_succeeded = conn.Query("SELECT user_id, login FROM (SELECT user_id, login, MAX(succeeded) as max_succeeded, COUNT(1) as cnt FROM login_log GROUP BY user_id, login) AS t0 WHERE t0.user_id IS NOT NULL AND t0.max_succeeded = 0 AND t0.cnt >= @threshold",
                    new { threshold = threshold });
                var logins = not_succeeded.Select(x => (string)x.login).ToList();
                var last_succeeds = conn.Query("SELECT user_id, login, MAX(id) AS last_login_id FROM login_log WHERE user_id IS NOT NULL AND succeeded = 1 GROUP BY user_id, login");
                foreach (var row in last_succeeds)
                {
                    var count = conn.ExecuteScalar<int>("SELECT COUNT(1) AS cnt FROM login_log WHERE user_id = @user_id AND @last_login_id < id",
                        new { user_id = (int)row.user_id, last_login_id = (long)row.last_login_id });
                    if (threshold <= count)
                    {
                        logins.Add((string)row.login);
                    }
                }
                return logins;
            }
        }
        private string GetRemoteAddress()
        {
            var ctx = _httpContextAccessor.HttpContext;
            var xff = ctx.Request.Headers["X-Forwarded-For"];
            if (xff.Count > 0)
            {
                return xff[0];
            }
            return ctx.Connection.RemoteIpAddress.ToString();
        }
    }
}

using App.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace App.Controllers
{
    public class HomeController : Controller
    {
        private Logic _logic;

        public HomeController(Logic logic)
        {
            _logic = logic;
        }

        [HttpGet]
        [Route("")]
        public IActionResult Index()
        {
            var message = HttpContext.Session.GetString("message");
            if (message != null)
            {
                HttpContext.Session.Remove("message");
            }
            ViewData["message"] = message;
            return View();
        }

        [HttpPost]
        [Route("login")]
        public IActionResult Login(string login, string password)
        {
            (var user, var err) = _logic.AttemptLogin(login, password);
            if (user != null)
            {
                HttpContext.Session.SetInt32("user_id", user.id);
                return RedirectToAction("MyPage");
            }
            else
            {
                // Console.WriteLine("err = " + err);
                if (err == "locked")
                {
                    HttpContext.Session.SetString("message", "This account is locked.");
                }
                else if (err == "banned")
                {
                    HttpContext.Session.SetString("message", "You're banned.");
                }
                else
                {
                    HttpContext.Session.SetString("message", "Wrong username or password");
                }
                return RedirectToAction("Index");
            }
        }

        [HttpGet]
        [Route("mypage")]
        public IActionResult MyPage()
        {
            var user = _logic.CurrentUser();
            if (user != null)
            {
                ViewData["user"] = user;
                ViewData["last_login"] = _logic.LastLogin();
                return View();
            }
            else
            {
                HttpContext.Session.SetString("message", "You must be logged in");
                return RedirectToAction("Index");
            }
        }

        [HttpGet]
        public IActionResult Report()
        {
            return Json(new { banned_ips = _logic.BannedIps(), locked_users = _logic.LockedUsers() });
        }
    }
}

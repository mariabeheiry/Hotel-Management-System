using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hotel_Management_System.Controllers
{
    public class DashboardController : Controller
    {
        [Authorize(Roles = "Admin")]
        public IActionResult AdminHome()
        {
            return View();
        }

        [Authorize(Roles = "Guest")]
        public IActionResult GuestHome()
        {
            return View();
        }
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hotel_Management_System.Models
{
    [Authorize(Roles = "Guest")]
    public class GuestController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}

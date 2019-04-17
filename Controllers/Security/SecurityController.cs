using Microsoft.AspNetCore.Mvc;

namespace DevOps.Controllers.Security
{
    public class SecurityController : Controller
    {
        public IActionResult Logout()
        {
            return StatusCode(401, "你已经登出了");
        }
    }
}

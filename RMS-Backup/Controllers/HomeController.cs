using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using RMS.Data;
using RMS.Models;

namespace RMS.Controllers
{
    public class HomeController : Controller
    {
        private readonly OracleDb _db;

        public HomeController(OracleDb db)
        {
            _db = db;
        }

        public IActionResult Index()
        {
            ViewBag.TotalCustomers = _db.Scalar("SELECT COUNT(*) FROM CUSTOMER_3NF");
            ViewBag.TotalMovies = _db.Scalar("SELECT COUNT(*) FROM MOVIE_3NF");
            ViewBag.TotalTheatres = _db.Scalar("SELECT COUNT(*) FROM THEATRE_3NF");
            ViewBag.TotalHalls = _db.Scalar("SELECT COUNT(*) FROM HALL_3NF");
            ViewBag.TotalShows = _db.Scalar("SELECT COUNT(*) FROM SHOW_3NF");
            ViewBag.TotalTickets = _db.Scalar("SELECT COUNT(*) FROM TICKET_3NF");
            ViewBag.TotalPayments = _db.Scalar("SELECT COUNT(*) FROM PAYMENT_3NF");
            ViewBag.PaidPayments = _db.Scalar("SELECT COUNT(*) FROM PAYMENT_3NF WHERE PAYMENT_STATUS='Paid'");

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
using Microsoft.AspNetCore.Mvc;
using Oracle.ManagedDataAccess.Client;
using RMS.Data;

namespace RMS.Controllers
{
    public class ShowController : Controller
    {
        private readonly OracleDb _db;

        public ShowController(OracleDb db)
        {
            _db = db;
        }

        public IActionResult Index()
        {
            ViewBag.Movies = _db.Query("SELECT MOVIE_ID, MOVIE_TITLE FROM MOVIE_3NF ORDER BY MOVIE_ID");

            ViewBag.Halls = _db.Query(@"
                SELECT HALL_ID, HALL_NAME
                FROM HALL_3NF
                ORDER BY HALL_ID
            ");

            var dt = _db.Query(@"
                SELECT s.SHOW_ID, s.MOVIE_ID, m.MOVIE_TITLE, s.HALL_ID, h.HALL_NAME,
                       s.SHOW_DATE, s.SHOW_TIME, s.BASE_TICKET_PRICE
                FROM SHOW_3NF s
                JOIN MOVIE_3NF m ON m.MOVIE_ID = s.MOVIE_ID
                JOIN HALL_3NF h ON h.HALL_ID = s.HALL_ID
                ORDER BY s.SHOW_ID
            ");
            return View(dt);
        }

        [HttpPost]
        public IActionResult Create(string showId, string movieId, string hallId, DateTime showDate, string showTime, decimal baseTicketPrice)
        {
            _db.Execute(@"
                INSERT INTO SHOW_3NF (SHOW_ID, MOVIE_ID, HALL_ID, SHOW_DATE, SHOW_TIME, BASE_TICKET_PRICE)
                VALUES (:SHOW_ID, :MOVIE_ID, :HALL_ID, :SHOW_DATE, :SHOW_TIME, :BASE_TICKET_PRICE)
            ",
            new OracleParameter("SHOW_ID", showId),
            new OracleParameter("MOVIE_ID", movieId),
            new OracleParameter("HALL_ID", hallId),
            new OracleParameter("SHOW_DATE", showDate),
            new OracleParameter("SHOW_TIME", showTime),
            new OracleParameter("BASE_TICKET_PRICE", baseTicketPrice));

            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Update(string showId, string movieId, string hallId, DateTime showDate, string showTime, decimal baseTicketPrice)
        {
            _db.Execute(@"
                UPDATE SHOW_3NF
                SET MOVIE_ID=:MOVIE_ID,
                    HALL_ID=:HALL_ID,
                    SHOW_DATE=:SHOW_DATE,
                    SHOW_TIME=:SHOW_TIME,
                    BASE_TICKET_PRICE=:BASE_TICKET_PRICE
                WHERE SHOW_ID=:SHOW_ID
            ",
            new OracleParameter("MOVIE_ID", movieId),
            new OracleParameter("HALL_ID", hallId),
            new OracleParameter("SHOW_DATE", showDate),
            new OracleParameter("SHOW_TIME", showTime),
            new OracleParameter("BASE_TICKET_PRICE", baseTicketPrice),
            new OracleParameter("SHOW_ID", showId));

            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Delete(string showId)
        {
            _db.Execute("DELETE FROM SHOW_3NF WHERE SHOW_ID=:SHOW_ID",
                new OracleParameter("SHOW_ID", showId));

            return RedirectToAction("Index");
        }
    }
}
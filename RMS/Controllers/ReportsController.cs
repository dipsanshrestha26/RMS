using Microsoft.AspNetCore.Mvc;
using Oracle.ManagedDataAccess.Client;
using RMS.Data;

namespace RMS.Controllers
{
    public class ReportsController : Controller
    {
        private readonly OracleDb _db;

        public ReportsController(OracleDb db)
        {
            _db = db;
        }

        private void LoadDropdowns()
        {
            ViewBag.Customers = _db.Query("SELECT CUSTOMER_ID, CUSTOMER_NAME FROM CUSTOMER_3NF ORDER BY CUSTOMER_ID");

            ViewBag.Halls = _db.Query(@"
                SELECT h.HALL_ID, h.HALL_NAME, t.THEATRE_NAME
                FROM HALL_3NF h
                JOIN THEATRE_3NF t ON t.THEATRE_ID = h.THEATRE_ID
                ORDER BY h.HALL_ID
            ");

            ViewBag.Movies = _db.Query("SELECT MOVIE_ID, MOVIE_TITLE FROM MOVIE_3NF ORDER BY MOVIE_ID");
        }

        public IActionResult Index()
        {
            LoadDropdowns();
            return View();
        }

        [HttpPost]
        public IActionResult CustomerTickets(string customerId)
        {
            LoadDropdowns();

            var dt = _db.Query(@"
                SELECT c.CUSTOMER_ID, c.CUSTOMER_NAME, c.CUSTOMER_ADDRESS, c.CUSTOMER_PHONE,
                       t.TICKET_ID, t.SEAT_NUMBER, t.BOOKING_DATETIME, t.TICKET_STATUS,
                       s.SHOW_DATE, s.SHOW_TIME, m.MOVIE_TITLE
                FROM CUSTOMER_3NF c
                JOIN TICKET_3NF t ON t.CUSTOMER_ID = c.CUSTOMER_ID
                JOIN SHOW_3NF s ON s.SHOW_ID = t.SHOW_ID
                JOIN MOVIE_3NF m ON m.MOVIE_ID = s.MOVIE_ID
                WHERE c.CUSTOMER_ID = :CUSTOMER_ID
                  AND t.BOOKING_DATETIME >= ADD_MONTHS(SYSDATE, -6)
                ORDER BY t.BOOKING_DATETIME DESC
            ",
            new OracleParameter("CUSTOMER_ID", customerId));

            ViewBag.Report1 = dt;
            return View("Index");
        }

        [HttpPost]
        public IActionResult HallShows(string hallId)
        {
            LoadDropdowns();

            var dt = _db.Query(@"
                SELECT t.THEATRE_NAME, t.THEATRE_CITY, h.HALL_NAME, h.SEATING_CAPACITY,
                       m.MOVIE_TITLE, s.SHOW_DATE, s.SHOW_TIME, s.BASE_TICKET_PRICE
                FROM HALL_3NF h
                JOIN THEATRE_3NF t ON t.THEATRE_ID = h.THEATRE_ID
                JOIN SHOW_3NF s ON s.HALL_ID = h.HALL_ID
                JOIN MOVIE_3NF m ON m.MOVIE_ID = s.MOVIE_ID
                WHERE h.HALL_ID = :HALL_ID
                ORDER BY s.SHOW_DATE, s.SHOW_TIME
            ",
            new OracleParameter("HALL_ID", hallId));

            ViewBag.Report2 = dt;
            return View("Index");
        }

        [HttpPost]
        public IActionResult Top3Occupancy(string movieId)
        {
            LoadDropdowns();

            var dt = _db.Query(@"
                SELECT *
                FROM (
                    SELECT t.THEATRE_NAME,
                           h.HALL_NAME,
                           h.SEATING_CAPACITY,
                           COUNT(CASE WHEN p.PAYMENT_STATUS = 'Paid' THEN tk.TICKET_ID END) AS PAID_TICKETS,
                           ROUND((COUNT(CASE WHEN p.PAYMENT_STATUS = 'Paid' THEN tk.TICKET_ID END) / NULLIF(h.SEATING_CAPACITY,0)) * 100, 2) AS OCCUPANCY_PERCENT
                    FROM SHOW_3NF s
                    JOIN MOVIE_3NF m ON m.MOVIE_ID = s.MOVIE_ID
                    JOIN HALL_3NF h ON h.HALL_ID = s.HALL_ID
                    JOIN THEATRE_3NF t ON t.THEATRE_ID = h.THEATRE_ID
                    LEFT JOIN TICKET_3NF tk ON tk.SHOW_ID = s.SHOW_ID
                    LEFT JOIN PAYMENT_3NF p ON p.TICKET_ID = tk.TICKET_ID
                    WHERE m.MOVIE_ID = :MOVIE_ID
                    GROUP BY t.THEATRE_NAME, h.HALL_NAME, h.SEATING_CAPACITY
                    ORDER BY OCCUPANCY_PERCENT DESC
                )
                WHERE ROWNUM <= 3
            ",
            new OracleParameter("MOVIE_ID", movieId));

            ViewBag.Report3 = dt;
            return View("Index");
        }
    }
}
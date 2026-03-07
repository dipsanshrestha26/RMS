using Microsoft.AspNetCore.Mvc;
using Oracle.ManagedDataAccess.Client;
using RMS.Data;

namespace RMS.Controllers
{
    public class HallController : Controller
    {
        private readonly OracleDb _db;

        public HallController(OracleDb db)
        {
            _db = db;
        }

        public IActionResult Index()
        {
            ViewBag.Theatres = _db.Query(@"
                SELECT THEATRE_ID, THEATRE_NAME
                FROM THEATRE_3NF
                ORDER BY THEATRE_ID
            ");

            var dt = _db.Query(@"
                SELECT h.HALL_ID, h.THEATRE_ID, h.HALL_NAME, h.SEATING_CAPACITY, t.THEATRE_NAME
                FROM HALL_3NF h
                JOIN THEATRE_3NF t ON t.THEATRE_ID = h.THEATRE_ID
                ORDER BY h.HALL_ID
            ");
            return View(dt);
        }

        [HttpPost]
        public IActionResult Create(string hallId, string theatreId, string hallName, int seatingCapacity)
        {
            _db.Execute(@"
                INSERT INTO HALL_3NF (HALL_ID, THEATRE_ID, HALL_NAME, SEATING_CAPACITY)
                VALUES (:HALL_ID, :THEATRE_ID, :HALL_NAME, :SEATING_CAPACITY)
            ",
            new OracleParameter("HALL_ID", hallId),
            new OracleParameter("THEATRE_ID", theatreId),
            new OracleParameter("HALL_NAME", hallName),
            new OracleParameter("SEATING_CAPACITY", seatingCapacity));

            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Update(string hallId, string theatreId, string hallName, int seatingCapacity)
        {
            _db.Execute(@"
                UPDATE HALL_3NF
                SET THEATRE_ID=:THEATRE_ID,
                    HALL_NAME=:HALL_NAME,
                    SEATING_CAPACITY=:SEATING_CAPACITY
                WHERE HALL_ID=:HALL_ID
            ",
            new OracleParameter("THEATRE_ID", theatreId),
            new OracleParameter("HALL_NAME", hallName),
            new OracleParameter("SEATING_CAPACITY", seatingCapacity),
            new OracleParameter("HALL_ID", hallId));

            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Delete(string hallId)
        {
            _db.Execute("DELETE FROM HALL_3NF WHERE HALL_ID=:HALL_ID",
                new OracleParameter("HALL_ID", hallId));

            return RedirectToAction("Index");
        }
    }
}
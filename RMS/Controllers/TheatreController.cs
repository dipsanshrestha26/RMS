using Microsoft.AspNetCore.Mvc;
using Oracle.ManagedDataAccess.Client;
using RMS.Data;

namespace RMS.Controllers
{
    public class TheatreController : Controller
    {
        private readonly OracleDb _db;

        public TheatreController(OracleDb db)
        {
            _db = db;
        }

        public IActionResult Index()
        {
            var dt = _db.Query(@"
                SELECT THEATRE_ID, THEATRE_NAME, THEATRE_CITY
                FROM THEATRE_3NF
                ORDER BY THEATRE_ID
            ");
            return View(dt);
        }

        [HttpPost]
        public IActionResult Create(string theatreId, string theatreName, string theatreCity)
        {
            _db.Execute(@"
                INSERT INTO THEATRE_3NF (THEATRE_ID, THEATRE_NAME, THEATRE_CITY)
                VALUES (:THEATRE_ID, :THEATRE_NAME, :THEATRE_CITY)
            ",
            new OracleParameter("THEATRE_ID", theatreId),
            new OracleParameter("THEATRE_NAME", theatreName),
            new OracleParameter("THEATRE_CITY", theatreCity));

            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Update(string theatreId, string theatreName, string theatreCity)
        {
            _db.Execute(@"
                UPDATE THEATRE_3NF
                SET THEATRE_NAME=:THEATRE_NAME,
                    THEATRE_CITY=:THEATRE_CITY
                WHERE THEATRE_ID=:THEATRE_ID
            ",
            new OracleParameter("THEATRE_NAME", theatreName),
            new OracleParameter("THEATRE_CITY", theatreCity),
            new OracleParameter("THEATRE_ID", theatreId));

            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Delete(string theatreId)
        {
            _db.Execute("DELETE FROM THEATRE_3NF WHERE THEATRE_ID=:THEATRE_ID",
                new OracleParameter("THEATRE_ID", theatreId));

            return RedirectToAction("Index");
        }
    }
}
using Microsoft.AspNetCore.Mvc;
using Oracle.ManagedDataAccess.Client;
using RMS.Data;
using System;

namespace RMS.Controllers
{
    public class ShowController : Controller
    {
        private readonly OracleDb _db;

        public ShowController(OracleDb db)
        {
            _db = db;
        }

        private bool ShowExists(string hallId, DateTime showDate, string showTime, string excludeId = null)
        {
            string sql = @"
                SELECT COUNT(*)
                FROM SHOW_3NF
                WHERE HALL_ID = :HALL_ID
                  AND TRUNC(SHOW_DATE) = TRUNC(:SHOW_DATE)
                  AND TRIM(SHOW_TIME) = TRIM(:SHOW_TIME)";

            if (!string.IsNullOrEmpty(excludeId))
            {
                sql += " AND SHOW_ID <> :EXCLUDE_ID";

                var dt2 = _db.Query(sql,
                    new OracleParameter("HALL_ID", hallId),
                    new OracleParameter("SHOW_DATE", showDate),
                    new OracleParameter("SHOW_TIME", showTime),
                    new OracleParameter("EXCLUDE_ID", excludeId));

                return Convert.ToInt32(dt2.Rows[0][0]) > 0;
            }

            var dt = _db.Query(sql,
                new OracleParameter("HALL_ID", hallId),
                new OracleParameter("SHOW_DATE", showDate),
                new OracleParameter("SHOW_TIME", showTime));

            return Convert.ToInt32(dt.Rows[0][0]) > 0;
        }

        public IActionResult Index()
        {
            ViewBag.Movies = _db.Query(@"
                SELECT MOVIE_ID, MOVIE_TITLE
                FROM MOVIE_3NF
                ORDER BY MOVIE_ID
            ");

            ViewBag.Halls = _db.Query(@"
                SELECT h.HALL_ID, h.HALL_NAME, t.THEATRE_NAME
                FROM HALL_3NF h
                JOIN THEATRE_3NF t ON t.THEATRE_ID = h.THEATRE_ID
                ORDER BY h.HALL_ID
            ");

            var dt = _db.Query(@"
                SELECT s.SHOW_ID,
                       s.MOVIE_ID,
                       m.MOVIE_TITLE,
                       s.HALL_ID,
                       h.HALL_NAME,
                       t.THEATRE_NAME,
                       s.SHOW_DATE,
                       s.SHOW_TIME,
                       s.BASE_TICKET_PRICE
                FROM SHOW_3NF s
                JOIN MOVIE_3NF m ON m.MOVIE_ID = s.MOVIE_ID
                JOIN HALL_3NF h ON h.HALL_ID = s.HALL_ID
                JOIN THEATRE_3NF t ON t.THEATRE_ID = h.THEATRE_ID
                ORDER BY s.SHOW_ID
            ");

            return View(dt);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(string showId, string movieId, string hallId, DateTime showDate, string showTime, decimal baseTicketPrice)
        {
            try
            {
                if (ShowExists(hallId, showDate, showTime))
                {
                    TempData["ErrorMessage"] = "Another show already exists in the same hall at the same date and time.";
                    return RedirectToAction("Index");
                }

                _db.Execute(@"
                    INSERT INTO SHOW_3NF
                    (SHOW_ID, MOVIE_ID, HALL_ID, SHOW_DATE, SHOW_TIME, BASE_TICKET_PRICE)
                    VALUES
                    (:SHOW_ID, :MOVIE_ID, :HALL_ID, :SHOW_DATE, :SHOW_TIME, :BASE_TICKET_PRICE)
                ",
                new OracleParameter("SHOW_ID", showId),
                new OracleParameter("MOVIE_ID", movieId),
                new OracleParameter("HALL_ID", hallId),
                new OracleParameter("SHOW_DATE", showDate),
                new OracleParameter("SHOW_TIME", showTime),
                new OracleParameter("BASE_TICKET_PRICE", baseTicketPrice));

                TempData["SuccessMessage"] = "Show added successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Failed to add show. " + ex.Message;
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Update(string showId, string movieId, string hallId, DateTime showDate, string showTime, decimal baseTicketPrice)
        {
            try
            {
                if (ShowExists(hallId, showDate, showTime, showId))
                {
                    TempData["ErrorMessage"] = "Another show already exists in the same hall at the same date and time.";
                    return RedirectToAction("Index");
                }

                _db.Execute(@"
                    UPDATE SHOW_3NF
                    SET MOVIE_ID = :MOVIE_ID,
                        HALL_ID = :HALL_ID,
                        SHOW_DATE = :SHOW_DATE,
                        SHOW_TIME = :SHOW_TIME,
                        BASE_TICKET_PRICE = :BASE_TICKET_PRICE
                    WHERE SHOW_ID = :SHOW_ID
                ",
                new OracleParameter("MOVIE_ID", movieId),
                new OracleParameter("HALL_ID", hallId),
                new OracleParameter("SHOW_DATE", showDate),
                new OracleParameter("SHOW_TIME", showTime),
                new OracleParameter("BASE_TICKET_PRICE", baseTicketPrice),
                new OracleParameter("SHOW_ID", showId));

                TempData["SuccessMessage"] = "Show updated successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Failed to update show. " + ex.Message;
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(string showId)
        {
            try
            {
                _db.Execute("DELETE FROM SHOW_3NF WHERE SHOW_ID = :SHOW_ID",
                    new OracleParameter("SHOW_ID", showId));

                TempData["SuccessMessage"] = "Show deleted successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Failed to delete show. " + ex.Message;
            }

            return RedirectToAction("Index");
        }
    }
}
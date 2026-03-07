using Microsoft.AspNetCore.Mvc;
using Oracle.ManagedDataAccess.Client;
using RMS.Data;
using System;

namespace RMS.Controllers
{
    public class HallController : Controller
    {
        private readonly OracleDb _db;

        public HallController(OracleDb db)
        {
            _db = db;
        }

        private bool HallExists(string theatreId, string hallName, string excludeId = null)
        {
            string sql = @"
                SELECT COUNT(*)
                FROM HALL_3NF
                WHERE THEATRE_ID = :THEATRE_ID
                  AND UPPER(TRIM(HALL_NAME)) = UPPER(TRIM(:HALL_NAME))";

            if (!string.IsNullOrEmpty(excludeId))
            {
                sql += " AND HALL_ID <> :EXCLUDE_ID";
                var dt2 = _db.Query(sql,
                    new OracleParameter("THEATRE_ID", theatreId),
                    new OracleParameter("HALL_NAME", hallName),
                    new OracleParameter("EXCLUDE_ID", excludeId));
                return Convert.ToInt32(dt2.Rows[0][0]) > 0;
            }

            var dt = _db.Query(sql,
                new OracleParameter("THEATRE_ID", theatreId),
                new OracleParameter("HALL_NAME", hallName));

            return Convert.ToInt32(dt.Rows[0][0]) > 0;
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
        [ValidateAntiForgeryToken]
        public IActionResult Create(string hallId, string theatreId, string hallName, int seatingCapacity)
        {
            try
            {
                if (HallExists(theatreId, hallName))
                {
                    TempData["ErrorMessage"] = "Duplicate hall name in same theatre is not allowed.";
                    return RedirectToAction("Index");
                }

                _db.Execute(@"
                    INSERT INTO HALL_3NF (HALL_ID, THEATRE_ID, HALL_NAME, SEATING_CAPACITY)
                    VALUES (:HALL_ID, :THEATRE_ID, :HALL_NAME, :SEATING_CAPACITY)
                ",
                new OracleParameter("HALL_ID", hallId),
                new OracleParameter("THEATRE_ID", theatreId),
                new OracleParameter("HALL_NAME", hallName),
                new OracleParameter("SEATING_CAPACITY", seatingCapacity));

                TempData["SuccessMessage"] = "Hall added successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Failed to add hall. " + ex.Message;
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Update(string hallId, string theatreId, string hallName, int seatingCapacity)
        {
            try
            {
                if (HallExists(theatreId, hallName, hallId))
                {
                    TempData["ErrorMessage"] = "Another hall with same name already exists in this theatre.";
                    return RedirectToAction("Index");
                }

                _db.Execute(@"
                    UPDATE HALL_3NF
                    SET THEATRE_ID = :THEATRE_ID,
                        HALL_NAME = :HALL_NAME,
                        SEATING_CAPACITY = :SEATING_CAPACITY
                    WHERE HALL_ID = :HALL_ID
                ",
                new OracleParameter("THEATRE_ID", theatreId),
                new OracleParameter("HALL_NAME", hallName),
                new OracleParameter("SEATING_CAPACITY", seatingCapacity),
                new OracleParameter("HALL_ID", hallId));

                TempData["SuccessMessage"] = "Hall updated successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Failed to update hall. " + ex.Message;
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(string hallId)
        {
            try
            {
                _db.Execute("DELETE FROM HALL_3NF WHERE HALL_ID = :HALL_ID",
                    new OracleParameter("HALL_ID", hallId));

                TempData["SuccessMessage"] = "Hall deleted successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Failed to delete hall. " + ex.Message;
            }

            return RedirectToAction("Index");
        }
    }
}
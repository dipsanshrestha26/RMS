using Microsoft.AspNetCore.Mvc;
using Oracle.ManagedDataAccess.Client;
using RMS.Data;
using System;

namespace RMS.Controllers
{
    public class TheatreController : Controller
    {
        private readonly OracleDb _db;

        public TheatreController(OracleDb db)
        {
            _db = db;
        }

        private bool TheatreExists(string theatreName, string theatreCity, string excludeId = null)
        {
            string sql = @"
                SELECT COUNT(*)
                FROM THEATRE_3NF
                WHERE UPPER(TRIM(THEATRE_NAME)) = UPPER(TRIM(:THEATRE_NAME))
                  AND UPPER(TRIM(THEATRE_CITY)) = UPPER(TRIM(:THEATRE_CITY))";

            if (!string.IsNullOrEmpty(excludeId))
            {
                sql += " AND THEATRE_ID <> :EXCLUDE_ID";
                var dt2 = _db.Query(sql,
                    new OracleParameter("THEATRE_NAME", theatreName),
                    new OracleParameter("THEATRE_CITY", theatreCity),
                    new OracleParameter("EXCLUDE_ID", excludeId));
                return Convert.ToInt32(dt2.Rows[0][0]) > 0;
            }

            var dt = _db.Query(sql,
                new OracleParameter("THEATRE_NAME", theatreName),
                new OracleParameter("THEATRE_CITY", theatreCity));

            return Convert.ToInt32(dt.Rows[0][0]) > 0;
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
        [ValidateAntiForgeryToken]
        public IActionResult Create(string theatreId, string theatreName, string theatreCity)
        {
            try
            {
                if (TheatreExists(theatreName, theatreCity))
                {
                    TempData["ErrorMessage"] = "Duplicate theatre is not allowed.";
                    return RedirectToAction("Index");
                }

                _db.Execute(@"
                    INSERT INTO THEATRE_3NF (THEATRE_ID, THEATRE_NAME, THEATRE_CITY)
                    VALUES (:THEATRE_ID, :THEATRE_NAME, :THEATRE_CITY)
                ",
                new OracleParameter("THEATRE_ID", theatreId),
                new OracleParameter("THEATRE_NAME", theatreName),
                new OracleParameter("THEATRE_CITY", theatreCity));

                TempData["SuccessMessage"] = "Theatre added successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Failed to add theatre. " + ex.Message;
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Update(string theatreId, string theatreName, string theatreCity)
        {
            try
            {
                if (TheatreExists(theatreName, theatreCity, theatreId))
                {
                    TempData["ErrorMessage"] = "Another theatre with same name and city already exists.";
                    return RedirectToAction("Index");
                }

                _db.Execute(@"
                    UPDATE THEATRE_3NF
                    SET THEATRE_NAME = :THEATRE_NAME,
                        THEATRE_CITY = :THEATRE_CITY
                    WHERE THEATRE_ID = :THEATRE_ID
                ",
                new OracleParameter("THEATRE_NAME", theatreName),
                new OracleParameter("THEATRE_CITY", theatreCity),
                new OracleParameter("THEATRE_ID", theatreId));

                TempData["SuccessMessage"] = "Theatre updated successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Failed to update theatre. " + ex.Message;
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(string theatreId)
        {
            try
            {
                _db.Execute("DELETE FROM THEATRE_3NF WHERE THEATRE_ID = :THEATRE_ID",
                    new OracleParameter("THEATRE_ID", theatreId));

                TempData["SuccessMessage"] = "Theatre deleted successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Failed to delete theatre. " + ex.Message;
            }

            return RedirectToAction("Index");
        }
    }
}
using Microsoft.AspNetCore.Mvc;
using Oracle.ManagedDataAccess.Client;
using RMS.Data;
using System;

namespace RMS.Controllers
{
    public class MovieController : Controller
    {
        private readonly OracleDb _db;

        public MovieController(OracleDb db)
        {
            _db = db;
        }

        private bool MovieExists(string movieTitle, string movieLanguage, DateTime releaseDate, string excludeId = null)
        {
            string sql = @"
                SELECT COUNT(*)
                FROM MOVIE_3NF
                WHERE UPPER(TRIM(MOVIE_TITLE)) = UPPER(TRIM(:MOVIE_TITLE))
                  AND UPPER(TRIM(MOVIE_LANGUAGE)) = UPPER(TRIM(:MOVIE_LANGUAGE))
                  AND TRUNC(RELEASE_DATE) = TRUNC(:RELEASE_DATE)";

            if (!string.IsNullOrEmpty(excludeId))
            {
                sql += " AND MOVIE_ID <> :EXCLUDE_ID";
                var dt2 = _db.Query(sql,
                    new OracleParameter("MOVIE_TITLE", movieTitle),
                    new OracleParameter("MOVIE_LANGUAGE", movieLanguage),
                    new OracleParameter("RELEASE_DATE", releaseDate),
                    new OracleParameter("EXCLUDE_ID", excludeId));
                return Convert.ToInt32(dt2.Rows[0][0]) > 0;
            }

            var dt = _db.Query(sql,
                new OracleParameter("MOVIE_TITLE", movieTitle),
                new OracleParameter("MOVIE_LANGUAGE", movieLanguage),
                new OracleParameter("RELEASE_DATE", releaseDate));

            return Convert.ToInt32(dt.Rows[0][0]) > 0;
        }

        public IActionResult Index()
        {
            var dt = _db.Query(@"
                SELECT MOVIE_ID, MOVIE_TITLE, MOVIE_GENRE, MOVIE_LANGUAGE, MOVIE_DURATION, RELEASE_DATE
                FROM MOVIE_3NF
                ORDER BY MOVIE_ID
            ");
            return View(dt);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(string movieId, string movieTitle, string movieGenre, string movieLanguage, string movieDuration, DateTime releaseDate)
        {
            try
            {
                if (MovieExists(movieTitle, movieLanguage, releaseDate))
                {
                    TempData["ErrorMessage"] = "Duplicate movie is not allowed.";
                    return RedirectToAction("Index");
                }

                _db.Execute(@"
                    INSERT INTO MOVIE_3NF (MOVIE_ID, MOVIE_TITLE, MOVIE_GENRE, MOVIE_LANGUAGE, MOVIE_DURATION, RELEASE_DATE)
                    VALUES (:MOVIE_ID, :MOVIE_TITLE, :MOVIE_GENRE, :MOVIE_LANGUAGE, :MOVIE_DURATION, :RELEASE_DATE)
                ",
                new OracleParameter("MOVIE_ID", movieId),
                new OracleParameter("MOVIE_TITLE", movieTitle),
                new OracleParameter("MOVIE_GENRE", movieGenre),
                new OracleParameter("MOVIE_LANGUAGE", movieLanguage),
                new OracleParameter("MOVIE_DURATION", movieDuration),
                new OracleParameter("RELEASE_DATE", releaseDate));

                TempData["SuccessMessage"] = "Movie added successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Failed to add movie. " + ex.Message;
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Update(string movieId, string movieTitle, string movieGenre, string movieLanguage, string movieDuration, DateTime releaseDate)
        {
            try
            {
                if (MovieExists(movieTitle, movieLanguage, releaseDate, movieId))
                {
                    TempData["ErrorMessage"] = "Another movie with same title, language, and release date already exists.";
                    return RedirectToAction("Index");
                }

                _db.Execute(@"
                    UPDATE MOVIE_3NF
                    SET MOVIE_TITLE = :MOVIE_TITLE,
                        MOVIE_GENRE = :MOVIE_GENRE,
                        MOVIE_LANGUAGE = :MOVIE_LANGUAGE,
                        MOVIE_DURATION = :MOVIE_DURATION,
                        RELEASE_DATE = :RELEASE_DATE
                    WHERE MOVIE_ID = :MOVIE_ID
                ",
                new OracleParameter("MOVIE_TITLE", movieTitle),
                new OracleParameter("MOVIE_GENRE", movieGenre),
                new OracleParameter("MOVIE_LANGUAGE", movieLanguage),
                new OracleParameter("MOVIE_DURATION", movieDuration),
                new OracleParameter("RELEASE_DATE", releaseDate),
                new OracleParameter("MOVIE_ID", movieId));

                TempData["SuccessMessage"] = "Movie updated successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Failed to update movie. " + ex.Message;
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(string movieId)
        {
            try
            {
                _db.Execute("DELETE FROM MOVIE_3NF WHERE MOVIE_ID = :MOVIE_ID",
                    new OracleParameter("MOVIE_ID", movieId));

                TempData["SuccessMessage"] = "Movie deleted successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Failed to delete movie. " + ex.Message;
            }

            return RedirectToAction("Index");
        }
    }
}
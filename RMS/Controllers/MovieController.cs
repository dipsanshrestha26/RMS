using Microsoft.AspNetCore.Mvc;
using Oracle.ManagedDataAccess.Client;
using RMS.Data;

namespace RMS.Controllers
{
    public class MovieController : Controller
    {
        private readonly OracleDb _db;

        public MovieController(OracleDb db)
        {
            _db = db;
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
        public IActionResult Create(string movieId, string movieTitle, string movieGenre, string movieLanguage, string movieDuration, DateTime releaseDate)
        {
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

            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Update(string movieId, string movieTitle, string movieGenre, string movieLanguage, string movieDuration, DateTime releaseDate)
        {
            _db.Execute(@"
                UPDATE MOVIE_3NF
                SET MOVIE_TITLE=:MOVIE_TITLE,
                    MOVIE_GENRE=:MOVIE_GENRE,
                    MOVIE_LANGUAGE=:MOVIE_LANGUAGE,
                    MOVIE_DURATION=:MOVIE_DURATION,
                    RELEASE_DATE=:RELEASE_DATE
                WHERE MOVIE_ID=:MOVIE_ID
            ",
            new OracleParameter("MOVIE_TITLE", movieTitle),
            new OracleParameter("MOVIE_GENRE", movieGenre),
            new OracleParameter("MOVIE_LANGUAGE", movieLanguage),
            new OracleParameter("MOVIE_DURATION", movieDuration),
            new OracleParameter("RELEASE_DATE", releaseDate),
            new OracleParameter("MOVIE_ID", movieId));

            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Delete(string movieId)
        {
            _db.Execute("DELETE FROM MOVIE_3NF WHERE MOVIE_ID=:MOVIE_ID",
                new OracleParameter("MOVIE_ID", movieId));

            return RedirectToAction("Index");
        }
    }
}
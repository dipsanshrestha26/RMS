using Microsoft.AspNetCore.Mvc;
using Oracle.ManagedDataAccess.Client;
using RMS.Data;
using System.Data;

namespace RMS.Controllers
{
    public class TicketController : Controller
    {
        private readonly OracleDb _db;

        public TicketController(OracleDb db)
        {
            _db = db;
        }

        private void LoadDropdowns(string? selectedShowId = null)
        {
            ViewBag.Customers = _db.Query("SELECT CUSTOMER_ID, CUSTOMER_NAME FROM CUSTOMER_3NF ORDER BY CUSTOMER_ID");

            ViewBag.Shows = _db.Query(@"
                SELECT s.SHOW_ID, m.MOVIE_TITLE
                FROM SHOW_3NF s
                JOIN MOVIE_3NF m ON m.MOVIE_ID = s.MOVIE_ID
                ORDER BY s.SHOW_ID
            ");

            ViewBag.Seats = new List<string>();

            if (!string.IsNullOrEmpty(selectedShowId))
            {
                ViewBag.Seats = GetAvailableSeats(selectedShowId);
                ViewBag.SelectedShowId = selectedShowId;
            }
        }

        private List<string> GetAvailableSeats(string showId)
        {
            var seats = new List<string>();

            var hallIdObj = _db.Scalar("SELECT HALL_ID FROM SHOW_3NF WHERE SHOW_ID = :SHOW_ID",
                new OracleParameter("SHOW_ID", showId));

            if (hallIdObj == null)
                return seats;

            string hallId = hallIdObj.ToString()!;

            var capacityObj = _db.Scalar("SELECT SEATING_CAPACITY FROM HALL_3NF WHERE HALL_ID = :HALL_ID",
                new OracleParameter("HALL_ID", hallId));

            if (capacityObj == null)
                return seats;

            int capacity = Convert.ToInt32(capacityObj);

            var bookedDt = _db.Query(@"
                SELECT SEAT_NUMBER
                FROM TICKET_3NF
                WHERE SHOW_ID = :SHOW_ID
            ", new OracleParameter("SHOW_ID", showId));

            var bookedSeats = new HashSet<string>(
                bookedDt.Rows.Cast<DataRow>()
                    .Select(r => r["SEAT_NUMBER"].ToString()!.ToUpper())
            );

            int seatsPerRow = 10;
            int rows = (int)Math.Ceiling(capacity / (double)seatsPerRow);

            for (int r = 0; r < rows; r++)
            {
                char rowLetter = (char)('A' + r);

                for (int s = 1; s <= seatsPerRow; s++)
                {
                    int seatNumber = (r * seatsPerRow) + s;
                    if (seatNumber > capacity) break;

                    string seatCode = $"{rowLetter}{s}";
                    if (!bookedSeats.Contains(seatCode))
                    {
                        seats.Add(seatCode);
                    }
                }
            }

            return seats;
        }

        public IActionResult Index(string? showId)
        {
            LoadDropdowns(showId);

            var dt = _db.Query(@"
                SELECT t.TICKET_ID, t.SHOW_ID, t.CUSTOMER_ID, c.CUSTOMER_NAME,
                       t.SEAT_NUMBER, t.BOOKING_DATETIME, t.TICKET_STATUS
                FROM TICKET_3NF t
                JOIN CUSTOMER_3NF c ON c.CUSTOMER_ID = t.CUSTOMER_ID
                ORDER BY t.TICKET_ID
            ");

            return View(dt);
        }

        [HttpPost]
        public IActionResult Create(string ticketId, string showId, string customerId, string seatNumber, DateTime bookingDatetime, string ticketStatus)
        {
            try
            {
                var existing = Convert.ToInt32(_db.Scalar(@"
                    SELECT COUNT(*)
                    FROM TICKET_3NF
                    WHERE SHOW_ID = :SHOW_ID
                      AND UPPER(SEAT_NUMBER) = UPPER(:SEAT_NUMBER)
                ",
                new OracleParameter("SHOW_ID", showId),
                new OracleParameter("SEAT_NUMBER", seatNumber)));

                if (existing > 0)
                {
                    TempData["Error"] = $"Seat {seatNumber} is already booked for show {showId}. Please choose another seat.";
                    return RedirectToAction("Index", new { showId });
                }

                _db.Execute(@"
                    INSERT INTO TICKET_3NF (TICKET_ID, SHOW_ID, CUSTOMER_ID, SEAT_NUMBER, BOOKING_DATETIME, TICKET_STATUS)
                    VALUES (:TICKET_ID, :SHOW_ID, :CUSTOMER_ID, :SEAT_NUMBER, :BOOKING_DATETIME, :TICKET_STATUS)
                ",
                new OracleParameter("TICKET_ID", ticketId),
                new OracleParameter("SHOW_ID", showId),
                new OracleParameter("CUSTOMER_ID", customerId),
                new OracleParameter("SEAT_NUMBER", seatNumber),
                new OracleParameter("BOOKING_DATETIME", bookingDatetime),
                new OracleParameter("TICKET_STATUS", ticketStatus));

                TempData["Success"] = "Ticket added successfully.";
            }
            catch (OracleException ex)
            {
                TempData["Error"] = $"Database error: {ex.Message}";
            }

            return RedirectToAction("Index", new { showId });
        }

        [HttpPost]
        public IActionResult Delete(string ticketId)
        {
            try
            {
                _db.Execute("DELETE FROM TICKET_3NF WHERE TICKET_ID=:TICKET_ID",
                    new OracleParameter("TICKET_ID", ticketId));

                TempData["Success"] = "Ticket deleted successfully.";
            }
            catch (OracleException ex) when (ex.Number == 2292)
            {
                TempData["Error"] = "Cannot delete this ticket because a payment is linked to it. Delete the related payment first.";
            }
            catch (OracleException ex)
            {
                TempData["Error"] = $"Database error: {ex.Message}";
            }

            return RedirectToAction("Index");
        }
    }
}
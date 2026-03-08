using Microsoft.AspNetCore.Mvc;
using Oracle.ManagedDataAccess.Client;
using RMS.Data;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

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
            ViewBag.Customers = _db.Query(@"
                SELECT CUSTOMER_ID, CUSTOMER_NAME
                FROM CUSTOMER_3NF
                ORDER BY CUSTOMER_ID
            ");

            ViewBag.Shows = _db.Query(@"
                SELECT s.SHOW_ID,
                       m.MOVIE_TITLE,
                       h.HALL_NAME,
                       t.THEATRE_NAME,
                       s.SHOW_DATE,
                       s.SHOW_TIME
                FROM SHOW_3NF s
                JOIN MOVIE_3NF m ON m.MOVIE_ID = s.MOVIE_ID
                JOIN HALL_3NF h ON h.HALL_ID = s.HALL_ID
                JOIN THEATRE_3NF t ON t.THEATRE_ID = h.THEATRE_ID
                ORDER BY s.SHOW_DATE, s.SHOW_TIME, s.SHOW_ID
            ");

            ViewBag.Seats = new List<string>();
            ViewBag.SelectedShowId = selectedShowId ?? "";

            if (!string.IsNullOrEmpty(selectedShowId))
            {
                ViewBag.Seats = GetAvailableSeats(selectedShowId);
            }
        }

        private List<string> GetAvailableSeats(string showId)
        {
            var seats = new List<string>();

            var hallIdObj = _db.Scalar(@"
                SELECT HALL_ID
                FROM SHOW_3NF
                WHERE SHOW_ID = :SHOW_ID
            ", new OracleParameter("SHOW_ID", showId));

            if (hallIdObj == null)
                return seats;

            string hallId = hallIdObj.ToString()!;

            var capacityObj = _db.Scalar(@"
                SELECT SEATING_CAPACITY
                FROM HALL_3NF
                WHERE HALL_ID = :HALL_ID
            ", new OracleParameter("HALL_ID", hallId));

            if (capacityObj == null)
                return seats;

            int capacity = Convert.ToInt32(capacityObj);

            var bookedDt = _db.Query(@"
                SELECT SEAT_NUMBER
                FROM TICKET_3NF
                WHERE SHOW_ID = :SHOW_ID
                  AND UPPER(TICKET_STATUS) <> 'CANCELLED'
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
                    int actualSeatNumber = (r * seatsPerRow) + s;
                    if (actualSeatNumber > capacity) break;

                    string seatCode = $"{rowLetter}{s}";
                    if (!bookedSeats.Contains(seatCode))
                    {
                        seats.Add(seatCode);
                    }
                }
            }

            return seats;
        }

        private bool TicketIdExists(string ticketId)
        {
            var count = _db.Scalar(@"
                SELECT COUNT(*)
                FROM TICKET_3NF
                WHERE TICKET_ID = :TICKET_ID
            ", new OracleParameter("TICKET_ID", ticketId));

            return Convert.ToInt32(count) > 0;
        }

        private DateTime? GetShowDate(string showId)
        {
            var obj = _db.Scalar(@"
                SELECT SHOW_DATE
                FROM SHOW_3NF
                WHERE SHOW_ID = :SHOW_ID
            ", new OracleParameter("SHOW_ID", showId));

            if (obj == null || obj == DBNull.Value)
                return null;

            return Convert.ToDateTime(obj);
        }

        private DateTime? GetShowDateTime(string showId)
        {
            var dt = _db.Query(@"
                SELECT SHOW_DATE, SHOW_TIME
                FROM SHOW_3NF
                WHERE SHOW_ID = :SHOW_ID
            ", new OracleParameter("SHOW_ID", showId));

            if (dt.Rows.Count == 0)
                return null;

            DateTime showDate = Convert.ToDateTime(dt.Rows[0]["SHOW_DATE"]);
            string showTime = dt.Rows[0]["SHOW_TIME"].ToString() ?? "00:00";

            TimeSpan timePart;
            if (!TimeSpan.TryParse(showTime, out timePart))
            {
                if (!TimeSpan.TryParse(showTime + ":00", out timePart))
                {
                    timePart = TimeSpan.Zero;
                }
            }

            return showDate.Date.Add(timePart);
        }

        public IActionResult Index(string? showId)
        {
            LoadDropdowns(showId);

            var dt = _db.Query(@"
                SELECT t.TICKET_ID,
                       t.SHOW_ID,
                       c.CUSTOMER_ID,
                       c.CUSTOMER_NAME,
                       t.SEAT_NUMBER,
                       t.BOOKING_DATETIME,
                       t.TICKET_STATUS,
                       m.MOVIE_TITLE,
                       h.HALL_NAME,
                       th.THEATRE_NAME,
                       s.SHOW_DATE,
                       s.SHOW_TIME
                FROM TICKET_3NF t
                JOIN CUSTOMER_3NF c ON c.CUSTOMER_ID = t.CUSTOMER_ID
                JOIN SHOW_3NF s ON s.SHOW_ID = t.SHOW_ID
                JOIN MOVIE_3NF m ON m.MOVIE_ID = s.MOVIE_ID
                JOIN HALL_3NF h ON h.HALL_ID = s.HALL_ID
                JOIN THEATRE_3NF th ON th.THEATRE_ID = h.THEATRE_ID
                ORDER BY t.TICKET_ID
            ");

            return View(dt);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(string ticketId, string showId, string customerId, string seatNumber, DateTime bookingDatetime, string ticketStatus)
        {
            try
            {
                if (TicketIdExists(ticketId))
                {
                    TempData["ErrorMessage"] = "Ticket ID already exists.";
                    return RedirectToAction("Index", new { showId });
                }

                DateTime? showDate = GetShowDate(showId);
                if (showDate == null)
                {
                    TempData["ErrorMessage"] = "Selected show was not found.";
                    return RedirectToAction("Index", new { showId });
                }

                DateTime? showDateTime = GetShowDateTime(showId);
                if (showDateTime == null)
                {
                    TempData["ErrorMessage"] = "Selected show date or time is invalid.";
                    return RedirectToAction("Index", new { showId });
                }

                if (bookingDatetime.Date < showDate.Value.Date)
                {
                    TempData["ErrorMessage"] = $"Ticket booking date cannot be before the show date ({showDate.Value:dd/MM/yyyy}).";
                    return RedirectToAction("Index", new { showId });
                }

                if (bookingDatetime > showDateTime.Value)
                {
                    TempData["ErrorMessage"] = "Ticket booking date and time cannot be after the show date and time.";
                    return RedirectToAction("Index", new { showId });
                }

                var existing = Convert.ToInt32(_db.Scalar(@"
                    SELECT COUNT(*)
                    FROM TICKET_3NF
                    WHERE SHOW_ID = :SHOW_ID
                      AND UPPER(SEAT_NUMBER) = UPPER(:SEAT_NUMBER)
                      AND UPPER(TICKET_STATUS) <> 'CANCELLED'
                ",
                new OracleParameter("SHOW_ID", showId),
                new OracleParameter("SEAT_NUMBER", seatNumber)));

                if (existing > 0)
                {
                    TempData["ErrorMessage"] = $"Seat {seatNumber} is already booked for this show.";
                    return RedirectToAction("Index", new { showId });
                }

                _db.Execute(@"
                    INSERT INTO TICKET_3NF
                    (TICKET_ID, SHOW_ID, CUSTOMER_ID, SEAT_NUMBER, BOOKING_DATETIME, TICKET_STATUS)
                    VALUES
                    (:TICKET_ID, :SHOW_ID, :CUSTOMER_ID, :SEAT_NUMBER, :BOOKING_DATETIME, :TICKET_STATUS)
                ",
                new OracleParameter("TICKET_ID", ticketId),
                new OracleParameter("SHOW_ID", showId),
                new OracleParameter("CUSTOMER_ID", customerId),
                new OracleParameter("SEAT_NUMBER", seatNumber),
                new OracleParameter("BOOKING_DATETIME", bookingDatetime),
                new OracleParameter("TICKET_STATUS", ticketStatus));

                TempData["SuccessMessage"] = "Ticket added successfully.";
            }
            catch (OracleException ex)
            {
                TempData["ErrorMessage"] = "Database error: " + ex.Message;
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error: " + ex.Message;
            }

            return RedirectToAction("Index", new { showId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(string ticketId)
        {
            try
            {
                _db.Execute(@"
                    DELETE FROM TICKET_3NF
                    WHERE TICKET_ID = :TICKET_ID
                ", new OracleParameter("TICKET_ID", ticketId));

                TempData["SuccessMessage"] = "Ticket deleted successfully.";
            }
            catch (OracleException ex) when (ex.Number == 2292)
            {
                TempData["ErrorMessage"] = "Cannot delete this ticket because a payment is linked to it. Delete the related payment first.";
            }
            catch (OracleException ex)
            {
                TempData["ErrorMessage"] = "Database error: " + ex.Message;
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error: " + ex.Message;
            }

            return RedirectToAction("Index");
        }
    }
}
using Microsoft.AspNetCore.Mvc;
using Oracle.ManagedDataAccess.Client;
using RMS.Data;
using System;

namespace RMS.Controllers
{
    public class PaymentController : Controller
    {
        private readonly OracleDb _db;

        public PaymentController(OracleDb db)
        {
            _db = db;
        }

        private bool PaymentIdExists(string paymentId)
        {
            var count = _db.Scalar(@"
                SELECT COUNT(*)
                FROM PAYMENT_3NF
                WHERE PAYMENT_ID = :PAYMENT_ID
            ", new OracleParameter("PAYMENT_ID", paymentId));

            return Convert.ToInt32(count) > 0;
        }

        private void LoadTickets()
        {
            ViewBag.Tickets = _db.Query(@"
                SELECT t.TICKET_ID,
                       t.SHOW_ID,
                       t.SEAT_NUMBER,
                       c.CUSTOMER_NAME,
                       m.MOVIE_TITLE,
                       s.BASE_TICKET_PRICE
                FROM TICKET_3NF t
                JOIN CUSTOMER_3NF c ON c.CUSTOMER_ID = t.CUSTOMER_ID
                JOIN SHOW_3NF s ON s.SHOW_ID = t.SHOW_ID
                JOIN MOVIE_3NF m ON m.MOVIE_ID = s.MOVIE_ID
                ORDER BY t.TICKET_ID
            ");
        }

        private decimal GetExpectedAmount(string ticketId)
        {
            var amountObj = _db.Scalar(@"
                SELECT s.BASE_TICKET_PRICE
                FROM TICKET_3NF t
                JOIN SHOW_3NF s ON s.SHOW_ID = t.SHOW_ID
                WHERE t.TICKET_ID = :TICKET_ID
            ", new OracleParameter("TICKET_ID", ticketId));

            if (amountObj == null)
                return 0;

            return Convert.ToDecimal(amountObj);
        }

        private void UpdateTicketStatusBasedOnPayment(string ticketId, string paymentStatus)
        {
            if (paymentStatus.Equals("Paid", StringComparison.OrdinalIgnoreCase))
            {
                _db.Execute(@"
                    UPDATE TICKET_3NF
                    SET TICKET_STATUS = 'Booked'
                    WHERE TICKET_ID = :TICKET_ID
                ", new OracleParameter("TICKET_ID", ticketId));
            }
            else if (paymentStatus.Equals("Pending", StringComparison.OrdinalIgnoreCase))
            {
                _db.Execute(@"
                    UPDATE TICKET_3NF
                    SET TICKET_STATUS = 'Pending'
                    WHERE TICKET_ID = :TICKET_ID
                ", new OracleParameter("TICKET_ID", ticketId));
            }
            else if (paymentStatus.Equals("Failed", StringComparison.OrdinalIgnoreCase))
            {
                _db.Execute(@"
                    UPDATE TICKET_3NF
                    SET TICKET_STATUS = 'Pending'
                    WHERE TICKET_ID = :TICKET_ID
                ", new OracleParameter("TICKET_ID", ticketId));
            }
        }

        public IActionResult Index()
        {
            LoadTickets();

            var dt = _db.Query(@"
                SELECT p.PAYMENT_ID,
                       p.TICKET_ID,
                       p.PAYMENT_MODE,
                       p.PAYMENT_DATE,
                       p.AMOUNT_PAID,
                       p.PAYMENT_STATUS
                FROM PAYMENT_3NF p
                ORDER BY p.PAYMENT_ID
            ");

            return View(dt);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(string paymentId, string ticketId, string paymentMode, DateTime paymentDate, decimal amountPaid, string paymentStatus)
        {
            try
            {
                if (PaymentIdExists(paymentId))
                {
                    TempData["ErrorMessage"] = "Payment ID already exists.";
                    return RedirectToAction("Index");
                }

                decimal expectedAmount = GetExpectedAmount(ticketId);

                if (expectedAmount <= 0)
                {
                    TempData["ErrorMessage"] = "Invalid ticket selected.";
                    return RedirectToAction("Index");
                }

                if (amountPaid != expectedAmount)
                {
                    TempData["ErrorMessage"] = $"Amount paid must be equal to the ticket price. Expected amount is {expectedAmount}.";
                    return RedirectToAction("Index");
                }

                _db.Execute(@"
                    INSERT INTO PAYMENT_3NF
                    (PAYMENT_ID, TICKET_ID, PAYMENT_MODE, PAYMENT_DATE, AMOUNT_PAID, PAYMENT_STATUS)
                    VALUES
                    (:PAYMENT_ID, :TICKET_ID, :PAYMENT_MODE, :PAYMENT_DATE, :AMOUNT_PAID, :PAYMENT_STATUS)
                ",
                new OracleParameter("PAYMENT_ID", paymentId),
                new OracleParameter("TICKET_ID", ticketId),
                new OracleParameter("PAYMENT_MODE", paymentMode),
                new OracleParameter("PAYMENT_DATE", paymentDate),
                new OracleParameter("AMOUNT_PAID", amountPaid),
                new OracleParameter("PAYMENT_STATUS", paymentStatus));

                UpdateTicketStatusBasedOnPayment(ticketId, paymentStatus);

                TempData["SuccessMessage"] = "Payment added successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Failed to add payment. " + ex.Message;
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Update(string paymentId, string ticketId, string paymentMode, DateTime paymentDate, decimal amountPaid, string paymentStatus)
        {
            try
            {
                decimal expectedAmount = GetExpectedAmount(ticketId);

                if (expectedAmount <= 0)
                {
                    TempData["ErrorMessage"] = "Invalid ticket selected.";
                    return RedirectToAction("Index");
                }

                if (amountPaid != expectedAmount)
                {
                    TempData["ErrorMessage"] = $"Amount paid must be equal to the ticket price. Expected amount is {expectedAmount}.";
                    return RedirectToAction("Index");
                }

                _db.Execute(@"
                    UPDATE PAYMENT_3NF
                    SET TICKET_ID = :TICKET_ID,
                        PAYMENT_MODE = :PAYMENT_MODE,
                        PAYMENT_DATE = :PAYMENT_DATE,
                        AMOUNT_PAID = :AMOUNT_PAID,
                        PAYMENT_STATUS = :PAYMENT_STATUS
                    WHERE PAYMENT_ID = :PAYMENT_ID
                ",
                new OracleParameter("TICKET_ID", ticketId),
                new OracleParameter("PAYMENT_MODE", paymentMode),
                new OracleParameter("PAYMENT_DATE", paymentDate),
                new OracleParameter("AMOUNT_PAID", amountPaid),
                new OracleParameter("PAYMENT_STATUS", paymentStatus),
                new OracleParameter("PAYMENT_ID", paymentId));

                UpdateTicketStatusBasedOnPayment(ticketId, paymentStatus);

                TempData["SuccessMessage"] = "Payment updated successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Failed to update payment. " + ex.Message;
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(string paymentId)
        {
            try
            {
                _db.Execute(@"
                    DELETE FROM PAYMENT_3NF
                    WHERE PAYMENT_ID = :PAYMENT_ID
                ", new OracleParameter("PAYMENT_ID", paymentId));

                TempData["SuccessMessage"] = "Payment deleted successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Failed to delete payment. " + ex.Message;
            }

            return RedirectToAction("Index");
        }
    }
}
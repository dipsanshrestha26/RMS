using Microsoft.AspNetCore.Mvc;
using Oracle.ManagedDataAccess.Client;
using RMS.Data;

namespace RMS.Controllers
{
    public class PaymentController : Controller
    {
        private readonly OracleDb _db;

        public PaymentController(OracleDb db)
        {
            _db = db;
        }

        public IActionResult Index()
        {
            ViewBag.Tickets = _db.Query("SELECT TICKET_ID FROM TICKET_3NF ORDER BY TICKET_ID");

            var dt = _db.Query(@"
                SELECT PAYMENT_ID, TICKET_ID, PAYMENT_MODE, PAYMENT_DATE, AMOUNT_PAID, PAYMENT_STATUS
                FROM PAYMENT_3NF
                ORDER BY PAYMENT_ID
            ");
            return View(dt);
        }

        [HttpPost]
        public IActionResult Create(string paymentId, string ticketId, string paymentMode, DateTime paymentDate, decimal amountPaid, string paymentStatus)
        {
            _db.Execute(@"
                INSERT INTO PAYMENT_3NF (PAYMENT_ID, TICKET_ID, PAYMENT_MODE, PAYMENT_DATE, AMOUNT_PAID, PAYMENT_STATUS)
                VALUES (:PAYMENT_ID, :TICKET_ID, :PAYMENT_MODE, :PAYMENT_DATE, :AMOUNT_PAID, :PAYMENT_STATUS)
            ",
            new OracleParameter("PAYMENT_ID", paymentId),
            new OracleParameter("TICKET_ID", ticketId),
            new OracleParameter("PAYMENT_MODE", paymentMode),
            new OracleParameter("PAYMENT_DATE", paymentDate),
            new OracleParameter("AMOUNT_PAID", amountPaid),
            new OracleParameter("PAYMENT_STATUS", paymentStatus));

            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Update(string paymentId, string ticketId, string paymentMode, DateTime paymentDate, decimal amountPaid, string paymentStatus)
        {
            _db.Execute(@"
                UPDATE PAYMENT_3NF
                SET TICKET_ID=:TICKET_ID,
                    PAYMENT_MODE=:PAYMENT_MODE,
                    PAYMENT_DATE=:PAYMENT_DATE,
                    AMOUNT_PAID=:AMOUNT_PAID,
                    PAYMENT_STATUS=:PAYMENT_STATUS
                WHERE PAYMENT_ID=:PAYMENT_ID
            ",
            new OracleParameter("TICKET_ID", ticketId),
            new OracleParameter("PAYMENT_MODE", paymentMode),
            new OracleParameter("PAYMENT_DATE", paymentDate),
            new OracleParameter("AMOUNT_PAID", amountPaid),
            new OracleParameter("PAYMENT_STATUS", paymentStatus),
            new OracleParameter("PAYMENT_ID", paymentId));

            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Delete(string paymentId)
        {
            _db.Execute("DELETE FROM PAYMENT_3NF WHERE PAYMENT_ID=:PAYMENT_ID",
                new OracleParameter("PAYMENT_ID", paymentId));

            return RedirectToAction("Index");
        }
    }
}
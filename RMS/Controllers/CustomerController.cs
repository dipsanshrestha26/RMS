using Microsoft.AspNetCore.Mvc;
using Oracle.ManagedDataAccess.Client;
using RMS.Data;
using System;

namespace RMS.Controllers
{
    public class CustomerController : Controller
    {
        private readonly OracleDb _db;

        public CustomerController(OracleDb db)
        {
            _db = db;
        }

        public IActionResult Index()
        {
            var dt = _db.Query(@"
                SELECT CUSTOMER_ID, CUSTOMER_NAME, CUSTOMER_ADDRESS, CUSTOMER_PHONE
                FROM CUSTOMER_3NF
                ORDER BY CUSTOMER_ID
            ");

            return View(dt);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(string customerId, string customerName, string customerAddress, string customerPhone)
        {
            try
            {
                _db.Execute(@"
                    INSERT INTO CUSTOMER_3NF (CUSTOMER_ID, CUSTOMER_NAME, CUSTOMER_ADDRESS, CUSTOMER_PHONE)
                    VALUES (:CUSTOMER_ID, :CUSTOMER_NAME, :CUSTOMER_ADDRESS, :CUSTOMER_PHONE)
                ",
                new OracleParameter("CUSTOMER_ID", customerId),
                new OracleParameter("CUSTOMER_NAME", customerName),
                new OracleParameter("CUSTOMER_ADDRESS", customerAddress),
                new OracleParameter("CUSTOMER_PHONE", customerPhone));

                TempData["SuccessMessage"] = "Customer added successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Failed to add customer. " + ex.Message;
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Update(string customerId, string customerName, string customerAddress, string customerPhone)
        {
            try
            {
                _db.Execute(@"
                    UPDATE CUSTOMER_3NF
                    SET CUSTOMER_NAME = :CUSTOMER_NAME,
                        CUSTOMER_ADDRESS = :CUSTOMER_ADDRESS,
                        CUSTOMER_PHONE = :CUSTOMER_PHONE
                    WHERE CUSTOMER_ID = :CUSTOMER_ID
                ",
                new OracleParameter("CUSTOMER_NAME", customerName),
                new OracleParameter("CUSTOMER_ADDRESS", customerAddress),
                new OracleParameter("CUSTOMER_PHONE", customerPhone),
                new OracleParameter("CUSTOMER_ID", customerId));

                TempData["SuccessMessage"] = "Customer updated successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Failed to update customer. " + ex.Message;
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(string customerId)
        {
            try
            {
                _db.Execute(@"
                    DELETE FROM CUSTOMER_3NF
                    WHERE CUSTOMER_ID = :CUSTOMER_ID
                ",
                new OracleParameter("CUSTOMER_ID", customerId));

                TempData["SuccessMessage"] = "Customer deleted successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Failed to delete customer. " + ex.Message;
            }

            return RedirectToAction("Index");
        }
    }
}
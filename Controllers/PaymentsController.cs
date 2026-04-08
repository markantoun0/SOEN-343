﻿﻿﻿using Microsoft.AspNetCore.Mvc;
using SUMMS.Api.Data;
using SUMMS.Api.Domain.Models;
using System;
using System.Threading.Tasks;

namespace SUMMS.Api.Controllers
{
    [ApiController]
    [Route("api/payments")]
    public class PaymentsController : ControllerBase
    {
        private readonly AppDbContext _db;

        public PaymentsController(AppDbContext db)
        {
            _db = db;
        }

        public class PaymentRequest
        {
            public int UserId { get; set; }
            public decimal Amount { get; set; }
            public string PaymentMethod { get; set; } = "CreditCard";
            public string? ReservationType { get; set; }
            public DateTime ReservationStartDate { get; set; }
            public DateTime ReservationEndDate { get; set; }

            public string? CardholderName { get; set; }
            public string? CardNumber { get; set; }
            public string? Expiry { get; set; }
            public string? Cvc { get; set; }
        }

        [HttpPost("process")]
        public async Task<IActionResult> ProcessPayment([FromBody] PaymentRequest req)
        {
            if (req.Amount <= 0)
                return BadRequest("Invalid amount");

            var normalizedCardNumber = (req.CardNumber ?? string.Empty).Replace(" ", string.Empty).Replace("-", string.Empty);
            var normalizedExpiry = (req.Expiry ?? string.Empty).Trim();

            string? last4 = null;
            if (!string.IsNullOrWhiteSpace(normalizedCardNumber) && normalizedCardNumber.Length >= 4)
            {
                last4 = normalizedCardNumber.Substring(normalizedCardNumber.Length - 4);
            }

            string? expMonth = null;
            string? expYear = null;
            if (!string.IsNullOrWhiteSpace(normalizedExpiry) && normalizedExpiry.Contains('/'))
            {
                var parts = normalizedExpiry.Split('/');
                if (parts.Length == 2)
                {
                    expMonth = parts[0].Trim();
                    expYear = parts[1].Trim();
                }
            }

            var payment = new Payment
            {
                UserId = req.UserId,
                Amount = req.Amount,
                PaymentMethod = req.PaymentMethod,
                Status = "Paid",
                CreatedAt = DateTime.UtcNow,
                ReservationType = req.ReservationType,
                ReservationStartDate = req.ReservationStartDate,
                ReservationEndDate = req.ReservationEndDate,
                CardholderName = req.CardholderName,
                CardLast4 = last4,
                ExpiryMonth = expMonth,
                ExpiryYear = expYear
            };

            _db.Payments.Add(payment);
            await _db.SaveChangesAsync();

            return Ok(new { success = true, paymentId = payment.Id });
        }
    }
}
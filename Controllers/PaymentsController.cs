﻿﻿using Microsoft.AspNetCore.Mvc;
using SUMMS.Api.Data;
using SUMMS.Api.Domain.Models;
using System;
using System.Globalization;
using System.Text.RegularExpressions;
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

            if (!TryValidatePaymentDetails(req, out var validationError, out var normalizedCardNumber, out var normalizedExpiry))
                return BadRequest(new { success = false, message = validationError });

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

        private static bool TryValidatePaymentDetails(
            PaymentRequest request,
            out string validationError,
            out string normalizedCardNumber,
            out string normalizedExpiry)
        {
            validationError = string.Empty;
            normalizedCardNumber = (request.CardNumber ?? string.Empty).Replace(" ", string.Empty).Replace("-", string.Empty);
            normalizedExpiry = (request.Expiry ?? string.Empty).Trim();
            var cardholderName = (request.CardholderName ?? string.Empty).Trim();
            var cvc = (request.Cvc ?? string.Empty).Trim();

            if (!Regex.IsMatch(cardholderName, "^(?=.{2,50}$)[A-Za-z]+(?:[ '-][A-Za-z]+)*$"))
            {
                validationError = "Invalid cardholder name.";
                return false;
            }

            if (!Regex.IsMatch(normalizedCardNumber, "^\\d{12,19}$") || !IsLuhnValid(normalizedCardNumber))
            {
                validationError = "Invalid card number.";
                return false;
            }

            var expiryMatch = Regex.Match(normalizedExpiry, "^(0[1-9]|1[0-2])\\/(\\d{2})$");
            if (!expiryMatch.Success)
            {
                validationError = "Invalid expiry. Use MM/YY.";
                return false;
            }

            var month = int.Parse(expiryMatch.Groups[1].Value, CultureInfo.InvariantCulture);
            var year = 2000 + int.Parse(expiryMatch.Groups[2].Value, CultureInfo.InvariantCulture);
            var now = DateTime.UtcNow;
            if (year < now.Year || (year == now.Year && month < now.Month))
            {
                validationError = "Card expiry date cannot be in the past.";
                return false;
            }

            normalizedExpiry = $"{month:D2}/{year % 100:D2}";

            if (!Regex.IsMatch(cvc, "^\\d{3,4}$"))
            {
                validationError = "Invalid CVC.";
                return false;
            }

            return true;
        }

        private static bool IsLuhnValid(string cardNumber)
        {
            var sum = 0;
            var shouldDouble = false;

            for (var i = cardNumber.Length - 1; i >= 0; i--)
            {
                if (!char.IsDigit(cardNumber[i]))
                    return false;

                var digit = cardNumber[i] - '0';

                if (shouldDouble)
                {
                    digit *= 2;
                    if (digit > 9)
                        digit -= 9;
                }

                sum += digit;
                shouldDouble = !shouldDouble;
            }

            return sum % 10 == 0;
        }
    }
}

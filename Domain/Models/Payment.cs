﻿using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace SUMMS.Api.Domain.Models
{
    public class Payment
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int? ReservationId { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = "CreditCard";
        public string Status { get; set; } = "Pending";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? ReservationType { get; set; }
        public DateTime? ReservationStartDate { get; set; }
        public DateTime? ReservationEndDate { get; set; }

        public string? CardholderName { get; set; }
        public string? CardLast4 { get; set; }
        public string? ExpiryMonth { get; set; }
        public string? ExpiryYear { get; set; }
        public string? CardNumber { get; set; }
        public string? ExpiryDate { get; set; }
        public DateTime? PaymentDate { get; set; }
    }
}

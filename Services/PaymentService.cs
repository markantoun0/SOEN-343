using Microsoft.EntityFrameworkCore;
using SUMMS.Api.Data;
using SUMMS.Api.Domain.Models;
using SUMMS.Api.DTOs;
using SUMMS.Api.Services.Interfaces;

namespace SUMMS.Api.Services;

public class PaymentService : IPaymentService
{
    private readonly AppDbContext _db;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(AppDbContext db, ILogger<PaymentService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<bool> ProcessPaymentAsync(int reservationId, string cardNumber, string expiryDate, string cvv)
    {
        _logger.LogInformation("Processing payment for reservation {ReservationId}", reservationId);

        var reservation = await _db.Reservations.FindAsync(reservationId);
        if (reservation == null)
        {
            _logger.LogWarning("Reservation {ReservationId} not found", reservationId);
            return false;
        }

        var payment = new Payment
        {
            ReservationId = reservationId,
            UserId = reservation.UserId ?? 0,
            Amount = 0m,
            PaymentDate = DateTime.UtcNow,
            CardLast4 = MaskCardNumber(cardNumber),
            Status = "Completed"
        };

        _db.Payments.Add(payment);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Payment processed for reservation {ReservationId}", reservationId);
        return true;
    }

    public async Task<bool> PaymentExistsAsync(int reservationId)
    {
        return await _db.Payments.AnyAsync(p => p.ReservationId == reservationId);
    }

    private static string MaskCardNumber(string cardNumber)
    {
        var cleaned = cardNumber.Replace(" ", "").Replace("-", "");
        if (cleaned.Length < 4)
            return "****";
        return $"****{cleaned.Substring(cleaned.Length - 4)}";
    }
}




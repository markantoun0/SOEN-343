using SUMMS.Api.DTOs;

namespace SUMMS.Api.Services.Interfaces;

public interface IPaymentService
{
    Task<bool> ProcessPaymentAsync(int reservationId, string cardNumber, string expiryDate, string cvv);
    Task<bool> PaymentExistsAsync(int reservationId);
}



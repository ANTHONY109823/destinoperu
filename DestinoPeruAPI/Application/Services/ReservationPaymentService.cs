using DestinoPeruAPI.Application.DTOs;
using DestinoPeruAPI.Application.Interfaces;
using DestinoPeruAPI.Domain.Entities;

namespace DestinoPeruAPI.Application.Services;

public class ReservationService(IReservationRepository reservationRepository, ITourRepository tourRepository)
{
    private const decimal CommissionRate = 0.10m;

    public async Task<IEnumerable<ReservationDto>> GetByUserAsync(int userId) => (await reservationRepository.GetByUserAsync(userId)).Select(MapToDto);
    public async Task<IEnumerable<ReservationDto>> GetByAgencyAsync(int agencyId) => (await reservationRepository.GetByAgencyAsync(agencyId)).Select(MapToDto);

    public async Task<ApiResponse<ReservationDto>> CreateAsync(CreateReservationRequest request, int userId)
    {
        var tour = await tourRepository.GetWithAgencyAsync(request.TourId);
        if (tour == null || !tour.IsActive) return new ApiResponse<ReservationDto>(false, "Tour no disponible.", null);
        var totalReserved = await reservationRepository.GetTotalReservedAsync(request.TourId);
        if (totalReserved + request.Quantity > tour.Capacity) return new ApiResponse<ReservationDto>(false, $"Solo quedan {tour.Capacity - totalReserved} lugares.", null);
        var total = tour.Price * request.Quantity;
        var reservation = new Reservation { UserId = userId, TourId = request.TourId, Quantity = request.Quantity, Total = total, Commission = total * CommissionRate, Status = "Pending" };
        await reservationRepository.AddAsync(reservation);
        var created = await reservationRepository.GetWithDetailsAsync(reservation.Id);
        return new ApiResponse<ReservationDto>(true, "Reserva creada.", MapToDto(created!));
    }

    public async Task<ApiResponse<ReservationDto>> CancelAsync(int id, int userId)
    {
        var r = await reservationRepository.GetWithDetailsAsync(id);
        if (r == null) return new ApiResponse<ReservationDto>(false, "Reserva no encontrada.", null);
        if (r.UserId != userId) return new ApiResponse<ReservationDto>(false, "Sin permiso.", null);
        if (r.Status == "Paid") return new ApiResponse<ReservationDto>(false, "No se puede cancelar una reserva pagada.", null);
        r.Status = "Cancelled"; await reservationRepository.UpdateAsync(r);
        return new ApiResponse<ReservationDto>(true, "Reserva cancelada.", MapToDto(r));
    }

    private static ReservationDto MapToDto(Reservation r) => new(r.Id, r.UserId, r.User?.Name ?? "", r.TourId, r.Tour?.Title ?? "", r.Tour?.Location ?? "", r.Tour?.Date ?? DateTime.UtcNow, r.Quantity, r.Total, r.Commission, r.Status, r.CreatedAt);
}

public class PaymentService(IPaymentRepository paymentRepository, IReservationRepository reservationRepository)
{
    public async Task<ApiResponse<PaymentDto>> ProcessAsync(CreatePaymentRequest request, int userId)
    {
        var reservation = await reservationRepository.GetWithDetailsAsync(request.ReservationId);
        if (reservation == null) return new ApiResponse<PaymentDto>(false, "Reserva no encontrada.", null);
        if (reservation.UserId != userId) return new ApiResponse<PaymentDto>(false, "Sin permiso.", null);
        if (reservation.Status != "Pending") return new ApiResponse<PaymentDto>(false, "La reserva no está pendiente.", null);
        var existing = await paymentRepository.GetByReservationAsync(request.ReservationId);
        if (existing != null) return new ApiResponse<PaymentDto>(false, "Esta reserva ya tiene un pago.", null);
        var payment = new Payment { ReservationId = request.ReservationId, Amount = reservation.Total, Method = request.Method, Status = "Completed", TransactionId = $"SIM-{Guid.NewGuid():N}"[..20] };
        await paymentRepository.AddAsync(payment);
        reservation.Status = "Paid"; await reservationRepository.UpdateAsync(reservation);
        return new ApiResponse<PaymentDto>(true, "Pago procesado.", new PaymentDto(payment.Id, payment.ReservationId, payment.Amount, payment.Method, payment.Status, payment.TransactionId, payment.CreatedAt));
    }
}

using DestinoPeruAPI.Application.DTOs;
using DestinoPeruAPI.Application.Interfaces;
using DestinoPeruAPI.Domain.Entities;

namespace DestinoPeruAPI.Application.Services;

public class ReservationService(
    IReservationRepository reservationRepository,
    ITourRepository tourRepository,
    ITourQueryRepository tourQuery)
{
    private const decimal CommissionRate = 0.10m;

    public async Task<IEnumerable<ReservationDto>> GetByUserAsync(int userId) =>
        (await reservationRepository.GetByUserAsync(userId)).Select(MapToDto);

    public async Task<IEnumerable<ReservationDto>> GetByPartnerAsync(int partnerId) =>
        (await reservationRepository.GetByPartnerAsync(partnerId)).Select(MapToDto);

    public async Task<ApiResponse<ReservationDto>> CreateAsync(CreateReservationRequest request, int userId)
    {
        var tour = await tourQuery.GetByIdAsync(request.TourId);
        if (tour == null || !tour.IsActive) return new ApiResponse<ReservationDto>(false, "Tour no disponible.", null);
        if (tour.AvailableCapacity < request.Quantity)
            return new ApiResponse<ReservationDto>(false, $"Solo quedan {tour.AvailableCapacity} lugares.", null);

        if (!await tourRepository.TryReserveCapacityAsync(request.TourId, request.Quantity))
            return new ApiResponse<ReservationDto>(false, "Cupos agotados. Intenta de nuevo.", null);

        var total = tour.Price * request.Quantity;
        var points = (int)Math.Floor(total * 0.01m);
        var reservation = new Reservation
        {
            UserId = userId,
            TourId = request.TourId,
            Quantity = request.Quantity,
            Total = total,
            Commission = total * CommissionRate,
            LoyaltyPointsEarned = points,
            Status = "Pending"
        };
        await reservationRepository.AddAsync(reservation);

        if (request.Passengers?.Count > 0)
        {
            await reservationRepository.AddPassengersAsync(request.Passengers.Select(p => new PassengerManifest
            {
                ReservationId = reservation.Id,
                FullName = p.FullName,
                Dni = p.Dni,
                PickupPoint = p.PickupPoint
            }));
        }

        var created = await reservationRepository.GetWithDetailsAsync(reservation.Id);
        return new ApiResponse<ReservationDto>(true, "Reserva creada.", MapToDto(created!));
    }

    public async Task<ApiResponse<ReservationDto>> CancelAsync(int id, int userId)
    {
        var r = await reservationRepository.GetWithDetailsAsync(id);
        if (r == null) return new ApiResponse<ReservationDto>(false, "Reserva no encontrada.", null);
        if (r.UserId != userId) return new ApiResponse<ReservationDto>(false, "Sin permiso.", null);
        if (r.Status == "Paid") return new ApiResponse<ReservationDto>(false, "No se puede cancelar una reserva pagada.", null);
        r.Status = "Cancelled";
        await reservationRepository.UpdateAsync(r);
        var tour = await tourRepository.GetByIdAsync(r.TourId);
        if (tour != null)
        {
            tour.AvailableCapacity += r.Quantity;
            await tourRepository.UpdateAsync(tour);
        }
        return new ApiResponse<ReservationDto>(true, "Reserva cancelada.", MapToDto(r));
    }

    private static ReservationDto MapToDto(Reservation r) => new(
        r.Id, r.UserId, r.User?.Name ?? "", r.TourId, r.Tour?.Title ?? "", r.Tour?.Slug ?? "",
        r.Tour?.Location ?? "", r.Tour?.Date ?? DateTime.UtcNow, r.Quantity, r.Total, r.Commission,
        r.LoyaltyPointsEarned, r.Status, r.CreatedAt);
}

public class PaymentService(
    IPaymentRepository paymentRepository,
    IReservationRepository reservationRepository,
    ILoyaltyRepository loyaltyRepository)
{
    public async Task<ApiResponse<PaymentDto>> ProcessAsync(CreatePaymentRequest request, int userId)
    {
        var reservation = await reservationRepository.GetWithDetailsAsync(request.ReservationId);
        if (reservation == null) return new ApiResponse<PaymentDto>(false, "Reserva no encontrada.", null);
        if (reservation.UserId != userId) return new ApiResponse<PaymentDto>(false, "Sin permiso.", null);
        if (reservation.Status != "Pending") return new ApiResponse<PaymentDto>(false, "La reserva no está pendiente.", null);

        var existing = await paymentRepository.GetByReservationAsync(request.ReservationId);
        if (existing != null) return new ApiResponse<PaymentDto>(false, "Esta reserva ya tiene un pago.", null);

        var isDigitalWallet = request.Method is "Yape" or "Plin";
        var payment = new Payment
        {
            ReservationId = request.ReservationId,
            Amount = reservation.Total,
            Method = request.Method,
            Status = isDigitalWallet ? "AwaitingVoucher" : "Completed",
            TransactionId = isDigitalWallet ? null : $"SIM-{Guid.NewGuid():N}"[..20],
            QrReference = request.QrReference,
            VoucherStatus = isDigitalWallet ? "Pending" : "None"
        };
        await paymentRepository.AddAsync(payment);

        if (!isDigitalWallet)
        {
            reservation.Status = "Paid";
            await reservationRepository.UpdateAsync(reservation);
            await loyaltyRepository.AddPointsAsync(reservation.UserId, reservation.LoyaltyPointsEarned);
        }

        return new ApiResponse<PaymentDto>(true,
            isDigitalWallet ? "Sube tu voucher para validar el pago." : "Pago procesado.",
            MapToDto(payment));
    }

    public async Task<ApiResponse<PaymentDto>> SubmitVoucherAsync(SubmitVoucherRequest request, int userId)
    {
        var payment = await paymentRepository.GetByIdAsync(request.PaymentId);
        if (payment == null) return new ApiResponse<PaymentDto>(false, "Pago no encontrado.", null);
        var reservation = await reservationRepository.GetWithDetailsAsync(payment.ReservationId);
        if (reservation?.UserId != userId) return new ApiResponse<PaymentDto>(false, "Sin permiso.", null);

        payment.VoucherUrl = request.VoucherUrl;
        payment.VoucherStatus = "Pending";
        payment.Status = "AwaitingValidation";
        await paymentRepository.UpdateAsync(payment);
        return new ApiResponse<PaymentDto>(true, "Voucher enviado. Espera validación del partner.", MapToDto(payment));
    }

    private static PaymentDto MapToDto(Payment p) => new(
        p.Id, p.ReservationId, p.Amount, p.Method, p.Status, p.TransactionId,
        p.VoucherUrl, p.VoucherStatus, p.QrReference, p.CreatedAt);
}

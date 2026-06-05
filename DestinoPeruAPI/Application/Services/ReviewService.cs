using DestinoPeruAPI.Application.DTOs;
using DestinoPeruAPI.Domain.Entities;
using DestinoPeruAPI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DestinoPeruAPI.Application.Services;

public class ReviewService(AppDbContext db)
{
    private static readonly string[] EligibleStatuses = ["Paid", "Confirmed", "Completed"];

    public async Task<TourReviewsDto> GetByTourAsync(int tourId, int? userId)
    {
        var reviews = await db.Reviews
            .Where(r => r.TourId == tourId)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new ReviewDto(r.Id, r.TourId, r.UserName, r.Rating, r.Comment, r.CreatedAt))
            .ToListAsync();

        var count = reviews.Count;
        var average = count > 0 ? Math.Round(reviews.Average(r => r.Rating), 1) : 0;

        var alreadyReviewed = false;
        var canReview = false;
        if (userId is > 0)
        {
            alreadyReviewed = await db.Reviews.AnyAsync(r => r.TourId == tourId && r.UserId == userId);
            canReview = !alreadyReviewed && await HasEligibleReservationAsync(tourId, userId.Value);
        }

        return new TourReviewsDto(average, count, canReview, alreadyReviewed, reviews);
    }

    public async Task<ApiResponse<ReviewDto>> CreateAsync(CreateReviewRequest request, int userId, string fallbackName)
    {
        if (request.Rating is < 1 or > 5)
            return new ApiResponse<ReviewDto>(false, "La calificación debe estar entre 1 y 5 estrellas.", null);

        var tour = await db.Tours.FirstOrDefaultAsync(t => t.Id == request.TourId);
        if (tour is null) return new ApiResponse<ReviewDto>(false, "Tour no encontrado.", null);

        if (await db.Reviews.AnyAsync(r => r.TourId == request.TourId && r.UserId == userId))
            return new ApiResponse<ReviewDto>(false, "Ya dejaste una reseña para este tour.", null);

        if (!await HasEligibleReservationAsync(request.TourId, userId))
            return new ApiResponse<ReviewDto>(false, "Solo puedes reseñar tours que ya reservaste y pagaste.", null);

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        var name = string.IsNullOrWhiteSpace(user?.Name) ? fallbackName : user!.Name;

        var review = new Review
        {
            TourId = request.TourId,
            PartnerId = tour.PartnerId,
            UserId = userId,
            UserName = name,
            Rating = request.Rating,
            Comment = (request.Comment ?? string.Empty).Trim(),
            CreatedAt = DateTime.UtcNow
        };
        db.Reviews.Add(review);
        await db.SaveChangesAsync();

        return new ApiResponse<ReviewDto>(true, "¡Gracias por tu reseña!",
            new ReviewDto(review.Id, review.TourId, review.UserName, review.Rating, review.Comment, review.CreatedAt));
    }

    private Task<bool> HasEligibleReservationAsync(int tourId, int userId) =>
        db.Reservations.AnyAsync(r =>
            r.TourId == tourId && r.UserId == userId && EligibleStatuses.Contains(r.Status));
}

using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using QuanLyDatHang.Data;
using QuanLyDatHang.DTOs;
using QuanLyDatHang.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace QuanLyDatHang.Services
{
    public interface IReviewService
    {
        Task<ReviewDto> CreateReviewAsync(ReviewCreateDto reviewDto, Guid customerId);
        Task<ReviewDto> UpdateReviewAsync(Guid reviewId, ReviewUpdateDto reviewDto, Guid customerId);
        Task<bool> DeleteReviewAsync(Guid reviewId, Guid customerId);
        Task<ReviewDto> GetReviewByIdAsync(Guid reviewId);
        Task<List<ReviewDto>> GetReviewsByStoreAsync(Guid storeId, int page = 1, int pageSize = 10);
        Task<List<ReviewDto>> GetReviewsByCustomerAsync(Guid customerId, int page = 1, int pageSize = 10);
        Task<ReviewStatisticsDto> GetReviewStatisticsAsync(Guid storeId);
        Task<bool> AddResponseAsync(Guid reviewId, string response, Guid storeOwnerId);
        Task<List<ReviewDto>> GetReviewsByMenuAsync(Guid menuId);
    }

    public class ReviewService : IReviewService
    {
        private readonly ApplicationDbContext _context;

        public ReviewService(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<ReviewDto> CreateReviewAsync(ReviewCreateDto reviewDto, Guid customerId)
        {
            var existingReview = await _context.Reviews
                .FirstOrDefaultAsync(r => r.OrderId == reviewDto.OrderId);

            if (existingReview != null)
                throw new InvalidOperationException("Đơn hàng này đã được đánh giá");

            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.Id == reviewDto.OrderId && o.CustomerId == customerId)
                ?? throw new InvalidOperationException("Không tìm thấy đơn hàng");

            if (order.Status != OrderStatus.Completed)
                throw new InvalidOperationException("Chỉ có thể đánh giá đơn hàng đã hoàn thành");

            var review = new Review
            { 
                Id = Guid.NewGuid(),
                OrderId = reviewDto.OrderId,
                StoreId = reviewDto.StoreId,
                CustomerId = customerId,
                Rating = reviewDto.Rating,
                Comment = reviewDto.Comment,
                MenuId = reviewDto.MenuId,
                ImageUrls = JsonSerializer.Serialize(reviewDto.ImageUrls ?? new List<string>()),
                CreatedAt = DateTime.UtcNow,
            };

            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            return await GetReviewByIdAsync(review.Id);
        }

        public async Task<ReviewDto> UpdateReviewAsync(Guid reviewId, ReviewUpdateDto reviewDto, Guid customerId)
        {
            var review = await _context.Reviews
                .Include(r => r.Order)
                .FirstOrDefaultAsync(r => r.Id == reviewId && r.CustomerId == customerId)
                ?? throw new InvalidOperationException("Không tìm thấy đánh giá hoặc không có quyền");

            if (review.Order.Status != OrderStatus.Completed)
                throw new InvalidOperationException("Chỉ có thể cập nhật đánh giá cho đơn hàng đã hoàn thành");

            review.Comment = reviewDto.Comment ?? review.Comment;
            if (reviewDto.Rating.HasValue) review.Rating = reviewDto.Rating.Value;
            if (reviewDto.MenuId.HasValue) review.MenuId = reviewDto.MenuId;
            review.ImageUrls = JsonSerializer.Serialize(reviewDto.ImageUrls ?? JsonSerializer.Deserialize<List<string>>(review.ImageUrls) ?? new List<string>());
            review.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return await GetReviewByIdAsync(reviewId);
        }

        public async Task<bool> DeleteReviewAsync(Guid reviewId, Guid customerId)
        {
            var review = await _context.Reviews
                .FirstOrDefaultAsync(r => r.Id == reviewId && r.CustomerId == customerId);
            if (review == null) return false;

            _context.Reviews.Remove(review);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<ReviewDto> GetReviewByIdAsync(Guid reviewId)
        {
            var review = await _context.Reviews
                .Include(r => r.Store)
                .Include(r => r.Customer)
                .Include(r => r.Order)
                .FirstOrDefaultAsync(r => r.Id == reviewId);

            if (review == null) return null;

            return MapToReviewDto(review);
        }

        public async Task<List<ReviewDto>> GetReviewsByStoreAsync(Guid storeId, int page = 1, int pageSize = 10)
        {
            var reviews = await _context.Reviews
                .Where(r => r.StoreId == storeId && r.IsApproved)
                .OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Include(r => r.Store)
                .Include(r => r.Customer)
                .ToListAsync();

            return reviews.Select(MapToReviewDto).ToList();
        }

        public async Task<List<ReviewDto>> GetReviewsByCustomerAsync(Guid customerId, int page = 1, int pageSize = 10)
        {
            var reviews = await _context.Reviews
                .Where(r => r.CustomerId == customerId && r.IsApproved)
                .OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Include(r => r.Store)
                .Include(r => r.Customer)
                .ToListAsync();

            return reviews.Select(MapToReviewDto).ToList();
        }

        public async Task<ReviewStatisticsDto> GetReviewStatisticsAsync(Guid storeId)
        {
            var reviews = await _context.Reviews
                .Where(r => r.StoreId == storeId && r.IsApproved)
                .ToListAsync();

            var statistics = new ReviewStatisticsDto
            {
                AverageRating = reviews.Any() ? Math.Round(reviews.Average(r => r.Rating), 2) : 0,
                TotalReviews = reviews.Count,
                RatingDistribution = Enumerable.Range(1, 5).ToDictionary(i => i, i => reviews.Count(r => (int)r.Rating == i))
            };

            return statistics;
        }
        // phan hoi binh luan cua nguoi mua
        public async Task<bool> AddResponseAsync(Guid reviewId, string response, Guid storeOwnerId)
        {
            var review = await _context.Reviews
                .Include(r => r.Store)
                .FirstOrDefaultAsync(r => r.Id == reviewId)
                ?? throw new InvalidOperationException("Không tìm thấy đánh giá");

            if (review.Store.SellerId != storeOwnerId)
                throw new InvalidOperationException("Không có quyền phản hồi");

            review.Response = response;
            review.ResponseDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<List<ReviewDto>> GetReviewsByMenuAsync(Guid menuId)
        {
            var reviews = await _context.Reviews
                .Where(r => r.MenuId == menuId && r.IsApproved)
                .OrderByDescending(r => r.CreatedAt)
                .Include(r => r.Customer)
                .Include(r => r.Store)
                .ToListAsync();
            return reviews.Select(MapToReviewDto).ToList();
        }

        private ReviewDto MapToReviewDto(Review review)
        {
            return new ReviewDto
            {
                Id = review.Id,
                StoreId = review.StoreId,
                MenuId = review.MenuId,
                StoreName = review.Store.Name,
                CustomerId = review.CustomerId,
                CustomerName = review.Customer.FullName, // Giả định không dùng IsAnonymous
                Rating = review.Rating,
                Comment = review.Comment,
                ImageUrls = JsonSerializer.Deserialize<List<string>>(review.ImageUrls) ?? new List<string>(),
                Response = review.Response,
                CreatedAt = review.CreatedAt
            };

        }
        
    }
}
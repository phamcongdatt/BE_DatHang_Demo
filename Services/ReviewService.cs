using Microsoft.EntityFrameworkCore;
using QuanLyDatHang.Data;
using QuanLyDatHang.DTOs;
using QuanLyDatHang.Models;
using System.Security.Claims;
using System.Text.Json;

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
        Task<ReviewResponseDto> CreateReviewResponseAsync(Guid reviewId, ReviewResponseDto responseDto, Guid storeOwnerId);
        Task<ReviewResponseDto> UpdateReviewResponseAsync(Guid reviewId, ReviewResponseDto responseDto, Guid storeOwnerId);
        Task<bool> ReportReviewAsync(Guid reviewId, ReviewReportDto reportDto, Guid reporterId);
        Task<bool> HideReviewAsync(Guid reviewId, string reason, Guid adminId);
        Task<bool> ApproveReviewAsync(Guid reviewId, Guid adminId);
        Task<bool> RejectReviewAsync(Guid reviewId, string reason, Guid adminId);
        Task<List<ReviewDto>> GetPendingReviewsAsync(int page = 1, int pageSize = 10);
        Task<List<ReviewReport>> GetReviewReportsAsync(int page = 1, int pageSize = 10);
        Task<bool> ResolveReportAsync(Guid reportId, ReportStatus status, Guid adminId);
    }

    public class ReviewService : IReviewService
    {
        private readonly ApplicationDbContext _context;

        public ReviewService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ReviewDto> CreateReviewAsync(ReviewCreateDto reviewDto, Guid customerId)
        {
            // Kiểm tra xem đơn hàng đã được đánh giá chưa
            var existingReview = await _context.Reviews
                .FirstOrDefaultAsync(r => r.OrderId == reviewDto.OrderId);

            if (existingReview != null)
            {
                throw new InvalidOperationException("Đơn hàng này đã được đánh giá");
            }

            // Kiểm tra xem đơn hàng có thuộc về khách hàng không
            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.Id == reviewDto.OrderId && o.CustomerId == customerId);

            if (order == null)
            {
                throw new InvalidOperationException("Không tìm thấy đơn hàng hoặc không có quyền đánh giá");
            }

            if (order.Status != OrderStatus.Completed)
            {
                throw new InvalidOperationException("Chỉ có thể đánh giá đơn hàng đã hoàn thành");
            }

            var review = new Review
            {
                Id = Guid.NewGuid(),
                OrderId = reviewDto.OrderId,
                StoreId = reviewDto.StoreId,
                CustomerId = customerId,
                Rating = reviewDto.Rating,
                Comment = reviewDto.Comment,
                TasteRating = reviewDto.TasteRating,
                ServiceRating = reviewDto.ServiceRating,
                PriceRating = reviewDto.PriceRating,
                QualityRating = reviewDto.QualityRating,
                ImageUrls = JsonSerializer.Serialize(reviewDto.ImageUrls),
                IsAnonymous = reviewDto.IsAnonymous,
                Status = ReviewStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            _context.Reviews.Add(review);

            // Thêm hình ảnh nếu có
            if (reviewDto.ImageUrls?.Any() == true)
            {
                foreach (var imageUrl in reviewDto.ImageUrls)
                {
                    var reviewImage = new ReviewImage
                    {
                        Id = Guid.NewGuid(),
                        ReviewId = review.Id,
                        ImageUrl = imageUrl,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.ReviewImages.Add(reviewImage);
                }
            }

            await _context.SaveChangesAsync();

            return await GetReviewByIdAsync(review.Id);
        }

        public async Task<ReviewDto> UpdateReviewAsync(Guid reviewId, ReviewUpdateDto reviewDto, Guid customerId)
        {
            var review = await _context.Reviews
                .Include(r => r.ReviewImages)
                .FirstOrDefaultAsync(r => r.Id == reviewId && r.CustomerId == customerId);

            if (review == null)
            {
                throw new InvalidOperationException("Không tìm thấy đánh giá hoặc không có quyền chỉnh sửa");
            }

            if (review.Status != ReviewStatus.Pending && review.Status != ReviewStatus.Approved)
            {
                throw new InvalidOperationException("Không thể chỉnh sửa đánh giá đã bị từ chối hoặc ẩn");
            }

            // Cập nhật thông tin
            if (!string.IsNullOrEmpty(reviewDto.Comment))
                review.Comment = reviewDto.Comment;

            if (reviewDto.Rating.HasValue)
                review.Rating = reviewDto.Rating.Value;

            if (reviewDto.TasteRating.HasValue)
                review.TasteRating = reviewDto.TasteRating.Value;

            if (reviewDto.ServiceRating.HasValue)
                review.ServiceRating = reviewDto.ServiceRating.Value;

            if (reviewDto.PriceRating.HasValue)
                review.PriceRating = reviewDto.PriceRating.Value;

            if (reviewDto.QualityRating.HasValue)
                review.QualityRating = reviewDto.QualityRating.Value;

            if (reviewDto.ImageUrls?.Any() == true)
            {
                review.ImageUrls = JsonSerializer.Serialize(reviewDto.ImageUrls);

                // Xóa hình ảnh cũ
                _context.ReviewImages.RemoveRange(review.ReviewImages);

                // Thêm hình ảnh mới
                foreach (var imageUrl in reviewDto.ImageUrls)
                {
                    var reviewImage = new ReviewImage
                    {
                        Id = Guid.NewGuid(),
                        ReviewId = review.Id,
                        ImageUrl = imageUrl,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.ReviewImages.Add(reviewImage);
                }
            }

            review.UpdatedAt = DateTime.UtcNow;
            review.Status = ReviewStatus.Pending; // Reset về pending để admin duyệt lại

            await _context.SaveChangesAsync();

            return await GetReviewByIdAsync(review.Id);
        }

        public async Task<bool> DeleteReviewAsync(Guid reviewId, Guid customerId)
        {
            var review = await _context.Reviews
                .FirstOrDefaultAsync(r => r.Id == reviewId && r.CustomerId == customerId);

            if (review == null)
            {
                return false;
            }

            _context.Reviews.Remove(review);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<ReviewDto> GetReviewByIdAsync(Guid reviewId)
        {
            var review = await _context.Reviews
                .Include(r => r.Customer)
                .Include(r => r.Store)
                .Include(r => r.ReviewImages)
                .Include(r => r.ReviewResponse)
                .Include(r => r.ReviewReports)
                .FirstOrDefaultAsync(r => r.Id == reviewId);

            if (review == null)
                return null;

            return MapToReviewDto(review);
        }

        public async Task<List<ReviewDto>> GetReviewsByStoreAsync(Guid storeId, int page = 1, int pageSize = 10)
        {
            var reviews = await _context.Reviews
                .Include(r => r.Customer)
                .Include(r => r.Store)
                .Include(r => r.ReviewImages)
                .Include(r => r.ReviewResponse)
                .Include(r => r.ReviewReports)
                .Where(r => r.StoreId == storeId && r.Status == ReviewStatus.Approved && !r.IsHidden)
                .OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return reviews.Select(MapToReviewDto).ToList();
        }

        public async Task<List<ReviewDto>> GetReviewsByCustomerAsync(Guid customerId, int page = 1, int pageSize = 10)
        {
            var reviews = await _context.Reviews
                .Include(r => r.Customer)
                .Include(r => r.Store)
                .Include(r => r.ReviewImages)
                .Include(r => r.ReviewResponse)
                .Include(r => r.ReviewReports)
                .Where(r => r.CustomerId == customerId)
                .OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return reviews.Select(MapToReviewDto).ToList();
        }

        public async Task<ReviewStatisticsDto> GetReviewStatisticsAsync(Guid storeId)
        {
            var reviews = await _context.Reviews
                .Where(r => r.StoreId == storeId && r.Status == ReviewStatus.Approved && !r.IsHidden)
                .ToListAsync();

            if (!reviews.Any())
            {
                return new ReviewStatisticsDto();
            }

            var stats = new ReviewStatisticsDto
            {
                AverageRating = Math.Round(reviews.Average(r => r.Rating), 2),
                AverageTasteRating = Math.Round(reviews.Average(r => r.TasteRating), 2),
                AverageServiceRating = Math.Round(reviews.Average(r => r.ServiceRating), 2),
                AveragePriceRating = Math.Round(reviews.Average(r => r.PriceRating), 2),
                AverageQualityRating = Math.Round(reviews.Average(r => r.QualityRating), 2),
                TotalReviews = reviews.Count,
                PendingReviews = await _context.Reviews.CountAsync(r => r.StoreId == storeId && r.Status == ReviewStatus.Pending),
                ApprovedReviews = reviews.Count,
                HiddenReviews = await _context.Reviews.CountAsync(r => r.StoreId == storeId && r.IsHidden)
            };

            // Phân bố rating
            for (int i = 1; i <= 5; i++)
            {
                stats.RatingDistribution[i] = reviews.Count(r => (int)r.Rating == i);
            }

            return stats;
        }

        public async Task<ReviewResponseDto> CreateReviewResponseAsync(Guid reviewId, ReviewResponseDto responseDto, Guid storeOwnerId)
        {
            var review = await _context.Reviews
                .Include(r => r.Store)
                .FirstOrDefaultAsync(r => r.Id == reviewId);

            if (review == null)
            {
                throw new InvalidOperationException("Không tìm thấy đánh giá");
            }

            if (review.Store.SellerId != storeOwnerId)
            {
                throw new InvalidOperationException("Không có quyền phản hồi đánh giá này");
            }

            // Kiểm tra xem đã có phản hồi chưa
            var existingResponse = await _context.ReviewResponses
                .FirstOrDefaultAsync(rr => rr.ReviewId == reviewId);

            if (existingResponse != null)
            {
                throw new InvalidOperationException("Đánh giá này đã có phản hồi");
            }

            var reviewResponse = new ReviewResponse
            {
                Id = Guid.NewGuid(),
                ReviewId = reviewId,
                StoreOwnerId = storeOwnerId,
                Response = responseDto.Response,
                CreatedAt = DateTime.UtcNow
            };

            _context.ReviewResponses.Add(reviewResponse);
            await _context.SaveChangesAsync();

            return responseDto;
        }

        public async Task<ReviewResponseDto> UpdateReviewResponseAsync(Guid reviewId, ReviewResponseDto responseDto, Guid storeOwnerId)
        {
            var reviewResponse = await _context.ReviewResponses
                .Include(rr => rr.Review)
                .ThenInclude(r => r.Store)
                .FirstOrDefaultAsync(rr => rr.ReviewId == reviewId && rr.StoreOwnerId == storeOwnerId);

            if (reviewResponse == null)
            {
                throw new InvalidOperationException("Không tìm thấy phản hồi hoặc không có quyền chỉnh sửa");
            }

            reviewResponse.Response = responseDto.Response;
            reviewResponse.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return responseDto;
        }

        public async Task<bool> ReportReviewAsync(Guid reviewId, ReviewReportDto reportDto, Guid reporterId)
        {
            var review = await _context.Reviews
                .FirstOrDefaultAsync(r => r.Id == reviewId);

            if (review == null)
            {
                return false;
            }

            // Kiểm tra xem đã báo cáo chưa
            var existingReport = await _context.ReviewReports
                .FirstOrDefaultAsync(rr => rr.ReviewId == reviewId && rr.ReporterId == reporterId);

            if (existingReport != null)
            {
                throw new InvalidOperationException("Bạn đã báo cáo đánh giá này rồi");
            }

            var report = new ReviewReport
            {
                Id = Guid.NewGuid(),
                ReviewId = reviewId,
                ReporterId = reporterId,
                Reason = reportDto.Reason,
                Description = reportDto.Description,
                Status = ReportStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            _context.ReviewReports.Add(report);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> HideReviewAsync(Guid reviewId, string reason, Guid adminId)
        {
            var review = await _context.Reviews
                .FirstOrDefaultAsync(r => r.Id == reviewId);

            if (review == null)
            {
                return false;
            }

            review.IsHidden = true;
            review.HideReason = reason;
            review.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> ApproveReviewAsync(Guid reviewId, Guid adminId)
        {
            var review = await _context.Reviews
                .FirstOrDefaultAsync(r => r.Id == reviewId);

            if (review == null)
            {
                return false;
            }

            review.Status = ReviewStatus.Approved;
            review.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> RejectReviewAsync(Guid reviewId, string reason, Guid adminId)
        {
            var review = await _context.Reviews
                .FirstOrDefaultAsync(r => r.Id == reviewId);

            if (review == null)
            {
                return false;
            }

            review.Status = ReviewStatus.Rejected;
            review.HideReason = reason;
            review.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<List<ReviewDto>> GetPendingReviewsAsync(int page = 1, int pageSize = 10)
        {
            var reviews = await _context.Reviews
                .Include(r => r.Customer)
                .Include(r => r.Store)
                .Include(r => r.ReviewImages)
                .Where(r => r.Status == ReviewStatus.Pending)
                .OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return reviews.Select(MapToReviewDto).ToList();
        }

        public async Task<List<ReviewReport>> GetReviewReportsAsync(int page = 1, int pageSize = 10)
        {
            return await _context.ReviewReports
                .Include(rr => rr.Review)
                .Include(rr => rr.Reporter)
                .Include(rr => rr.ResolvedByUser)
                .OrderByDescending(rr => rr.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<bool> ResolveReportAsync(Guid reportId, ReportStatus status, Guid adminId)
        {
            var report = await _context.ReviewReports
                .FirstOrDefaultAsync(rr => rr.Id == reportId);

            if (report == null)
            {
                return false;
            }

            report.Status = status;
            report.ResolvedAt = DateTime.UtcNow;
            report.ResolvedBy = adminId;

            await _context.SaveChangesAsync();

            return true;
        }

        private ReviewDto MapToReviewDto(Review review)
        {
            var imageUrls = !string.IsNullOrEmpty(review.ImageUrls) 
                ? JsonSerializer.Deserialize<List<string>>(review.ImageUrls) 
                : new List<string>();

            return new ReviewDto
            {
                Id = review.Id,
                OrderId = review.OrderId,
                StoreId = review.StoreId,
                StoreName = review.Store?.Name,
                CustomerId = review.CustomerId,
                CustomerName = review.IsAnonymous ? "Khách hàng ẩn danh" : review.Customer?.FullName,
                CustomerAvatar = null,
                Rating = review.Rating,
                TasteRating = review.TasteRating,
                ServiceRating = review.ServiceRating,
                PriceRating = review.PriceRating,
                QualityRating = review.QualityRating,
                Comment = review.Comment,
                ImageUrls = imageUrls,
                Status = review.Status,
                IsAnonymous = review.IsAnonymous,
                IsHidden = review.IsHidden,
                HideReason = review.HideReason,
                CreatedAt = review.CreatedAt,
                UpdatedAt = review.UpdatedAt,
                Response = review.ReviewResponse != null ? new ReviewResponseDto
                {
                    Response = review.ReviewResponse.Response
                } : null,
                ReportCount = review.ReviewReports?.Count ?? 0,
                CanReport = true, // Logic này có thể phức tạp hơn
                CanRespond = review.ReviewResponse == null
            };
        }
    }
} 
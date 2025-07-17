using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyDatHang.Data;
using QuanLyDatHang.Models;
using QuanLyDatHang.DTOs;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace QuanLyDatHang.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoryController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public CategoryController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Lấy danh sách tất cả danh mục (cho người dùng chọn)
        /// </summary>
        [HttpGet("GetAll")]
        public async Task<IActionResult> GetCategories()
        {
            var categories = await _context.Categories
                .Where(c => c.KichHoat)
                .OrderBy(c => c.Ten)
                .Select(c => new
                {
                    c.Id,
                    c.Ten,
                    c.MoTa,
                    c.MacDinh
                })
                .ToListAsync();

            return Ok(new
            {
                success = true,
                message = "Lấy danh sách danh mục thành công",
                data = categories
            });
        }

        /// <summary>
        /// Lấy danh sách danh mục mặc định (cho form tạo cửa hàng/món ăn)
        /// </summary>
        [HttpGet("GetDefault")]
        public async Task<IActionResult> GetDefaultCategories()
        {
            var defaultCategories = await _context.Categories
                .Where(c => c.KichHoat && c.MacDinh)
                .OrderBy(c => c.Ten)
                .Select(c => new
                {
                    c.Id,
                    c.Ten,
                    c.MoTa
                })
                .ToListAsync();

            return Ok(new
            {
                success = true,
                message = "Lấy danh mục mặc định thành công",
                data = defaultCategories
            });
        }

        /// <summary>
        /// Lấy thông tin danh mục theo ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetCategory(Guid id)
        {
            var category = await _context.Categories
                .Where(c => c.Id == id && c.KichHoat)
                .Select(c => new
                {
                    c.Id,
                    c.Ten,
                    c.MoTa,
                    c.MacDinh
                })
                .FirstOrDefaultAsync();

            if (category == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Không tìm thấy danh mục"
                });
            }

            return Ok(new
            {
                success = true,
                message = "Lấy thông tin danh mục thành công",
                data = category
            });
        }

       

        /// <summary>
        /// Đề xuất tạo danh mục mới (người dùng thường)
        /// </summary>
        [HttpPost("suggest")]
        [Authorize]
        public async Task<IActionResult> SuggestCategory([FromBody] CategorySuggestionDto suggestion)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Dữ liệu không hợp lệ",
                    errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)
                });
            }

            // Kiểm tra xem danh mục đã tồn tại chưa
            var existingCategory = await _context.Categories
                .FirstOrDefaultAsync(c => c.Ten.ToLower() == suggestion.Ten.ToLower());

            if (existingCategory != null)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Danh mục này đã tồn tại"
                });
            }

            // Tạo đề xuất danh mục (không kích hoạt ngay)
            var categorySuggestion = new Category
            {
                Id = Guid.NewGuid(),
                Ten = suggestion.Ten,
                MoTa = suggestion.MoTa,
                KichHoat = false, // Chưa kích hoạt, cần admin duyệt
                MacDinh = false
            };

            _context.Categories.Add(categorySuggestion);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "Đề xuất danh mục đã được gửi. Admin sẽ xem xét và phê duyệt.",
                data = new
                {
                    categorySuggestion.Id,
                    categorySuggestion.Ten,
                    categorySuggestion.MoTa,
                    status = "Chờ duyệt"
                }
            });
        }

        /// <summary>
        /// Đề xuất cập nhật danh mục (người dùng thường)
        /// </summary>
        [HttpPost("suggest-update/{id}")]
        [Authorize]
        public async Task<IActionResult> SuggestCategoryUpdate(Guid id, [FromBody] CategoryUpdateDto categoryDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Dữ liệu không hợp lệ",
                    errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)
                });
            }

            var originalCategory = await _context.Categories.FindAsync(id);
            if (originalCategory == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Không tìm thấy danh mục gốc"
                });
            }

            // Tạo bản sao để đề xuất cập nhật
            var updateSuggestion = new Category
            {
                Id = Guid.NewGuid(),
                Ten = categoryDto.Ten,
                MoTa = categoryDto.MoTa,
                KichHoat = false, // Chưa kích hoạt
                MacDinh = categoryDto.MacDinh
            };

            _context.Categories.Add(updateSuggestion);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "Đề xuất cập nhật danh mục đã được gửi. Admin sẽ xem xét và phê duyệt.",
                data = new
                {
                    originalId = id,
                    originalName = originalCategory.Ten,
                    suggestionId = updateSuggestion.Id,
                    updateSuggestion.Ten,
                    updateSuggestion.MoTa,
                    status = "Chờ duyệt"
                }
            });
        }  
    }
}

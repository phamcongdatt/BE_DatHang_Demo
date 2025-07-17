using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuanLyDatHang.DTOs;
using QuanLyDatHang.Services;
using System.Security.Claims;

namespace QuanLyDatHang.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class WishlistController : ControllerBase
    {
        private readonly IWishlistService _wishlistService;

        public WishlistController(IWishlistService wishlistService)
        {
            _wishlistService = wishlistService;
        }

        /// <summary>
        /// Lấy danh sách yêu thích
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetWishlist()
        {
            try
            {
                var customerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                var wishlist = await _wishlistService.GetWishlistAsync(customerId);

                return Ok(new
                {
                    success = true,
                    data = wishlist,
                    message = "Lấy danh sách yêu thích thành công"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Có lỗi xảy ra khi lấy danh sách "
                });
            }
        }

        /// <summary>
        /// Thêm món ăn vào danh sách yêu thích
        /// </summary>
        [HttpPost("add")]
        public async Task<IActionResult> AddToWishlist([FromBody] AddToWishlistDto dto)
        {
            try
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

                var customerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                var wishlistItem = await _wishlistService.AddToWishlistAsync(customerId, dto);

                return Ok(new
                {
                    success = true,
                    data = wishlistItem,
                    message = "Đã thêm món ăn vào danh sách yêu thích"
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Có lỗi xảy ra khi thêm vào danh sách yêu thích"
                });
            }
        }

        /// <summary>
        /// Xóa món ăn khỏi danh sách yêu thích
        /// </summary>
        [HttpDelete("remove/{menuId}")]
        public async Task<IActionResult> RemoveFromWishlist(Guid menuId)
        {
            try
            {
                var customerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                var result = await _wishlistService.RemoveFromWishlistAsync(customerId, menuId);

                if (result)
                {
                    return Ok(new
                    {
                        success = true,
                        message = "Đã xóa món ăn khỏi danh sách yêu thích"
                    });
                }
                else
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Không tìm thấy món ăn trong danh sách yêu thích"
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Có lỗi xảy ra khi xóa khỏi danh sách yêu thích"
                });
            }
        }

        /// <summary>
        /// Kiểm tra món ăn có trong danh sách yêu thích không
        /// </summary>
        [HttpGet("check/{menuId}")]
        public async Task<IActionResult> CheckWishlistStatus(Guid menuId)
        {
            try
            {
                var customerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                var isInWishlist = await _wishlistService.IsInWishlistAsync(customerId, menuId);

                return Ok(new
                {
                    success = true,
                    data = new { isInWishlist },
                    message = "Kiểm tra trạng thái yêu thích thành công"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Có lỗi xảy ra khi kiểm tra trạng thái yêu thích"
                });
            }
        }

        /// <summary>
        /// chuyen doi  trạng thái yêu thích (thêm/xóa)
        /// </summary>
        [HttpPost("toggle/{menuId}")]
        public async Task<IActionResult> ToggleWishlist(Guid menuId)
        {
            try
            {
                var customerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                var isInWishlist = await _wishlistService.IsInWishlistAsync(customerId, menuId);

                if (isInWishlist)
                {
                    // Xóa khỏi wishlist
                    var removed = await _wishlistService.RemoveFromWishlistAsync(customerId, menuId);
                    return Ok(new
                    {
                        success = true,
                        data = new { isInWishlist = false },
                        message = "Đã xóa khỏi danh sách yêu thích"
                    });
                }
                else
                {
                    // Thêm vào wishlist
                    var addDto = new AddToWishlistDto { MenuId = menuId };
                    var addedItem = await _wishlistService.AddToWishlistAsync(customerId, addDto);
                    return Ok(new
                    {
                        success = true,
                        data = new { isInWishlist = true, item = addedItem },
                        message = "Đã thêm vào danh sách yêu thích"
                    });
                }
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Có lỗi xảy ra khi thay đổi trạng thái yêu thích"
                });
            }
        }
    }
} 
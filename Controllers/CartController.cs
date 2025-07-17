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
    public class CartController : ControllerBase
    {
        private readonly ICartService _cartService;
        private readonly IWishlistService _wishlistService;

        public CartController(ICartService cartService, IWishlistService wishlistService)
        {
            _cartService = cartService;
            _wishlistService = wishlistService;
        }

        /// <summary>
        /// Lấy thông tin giỏ hàng
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetCart()
        {
            try
            {
                var customerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                var cart = await _cartService.GetCartAsync(customerId);

                return Ok(new
                {
                    success = true,
                    data = cart,
                    message = "Lấy thông tin giỏ hàng thành công"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Có lỗi xảy ra khi lấy thông tin giỏ hàng"
                });
            }
        }

        /// <summary>
        /// Thêm món ăn vào giỏ hàng
        /// </summary>
        [HttpPost("add-item")]
        public async Task<IActionResult> AddToCart([FromBody] AddToCartDto dto)
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
                var cartItem = await _cartService.AddToCartAsync(customerId, dto);

                return Ok(new
                {
                    success = true,
                    data = cartItem,
                    message = "Đã thêm món ăn vào giỏ hàng thành công"
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
                    message = "Có lỗi xảy ra khi thêm món ăn vào giỏ hàng"
                });
            }
        }

        /// <summary>
        /// Cập nhật số lượng món ăn trong giỏ hàng
        /// </summary>
        [HttpPut("update-quantity/{itemId}")]
        public async Task<IActionResult> UpdateCartItem(Guid itemId, [FromBody] UpdateCartItemDto dto)
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
                var cartItem = await _cartService.UpdateCartItemAsync(customerId, itemId, dto);

                return Ok(new
                {
                    success = true,
                    data = cartItem,
                    message = "Cập nhật giỏ hàng thành công"
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
                    message = "Có lỗi xảy ra khi cập nhật giỏ hàng"
                });
            }
        }

        /// <summary>
        /// Xóa món ăn khỏi giỏ hàng
        /// </summary>
        [HttpDelete("remove-item/{itemId}")]
        public async Task<IActionResult> RemoveFromCart(Guid itemId)
        {
            try
            {
                var customerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                var result = await _cartService.RemoveFromCartAsync(customerId, itemId);

                if (result)
                {
                    return Ok(new
                    {
                        success = true,
                        message = "Đã xóa món ăn khỏi giỏ hàng"
                    });
                }
                else
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Không tìm thấy món ăn trong giỏ hàng"
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Có lỗi xảy ra khi xóa món ăn khỏi giỏ hàng"
                });
            }
        }

        /// <summary>
        /// Xóa toàn bộ giỏ hàng
        /// </summary>
        [HttpDelete("clear")]
        public async Task<IActionResult> ClearCart()
        {
            try
            {
                var customerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                var result = await _cartService.ClearCartAsync(customerId);

                if (result)
                {
                    return Ok(new
                    {
                        success = true,
                        message = "Đã xóa toàn bộ giỏ hàng"
                    });
                }
                else
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Giỏ hàng trống"
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Có lỗi xảy ra khi xóa giỏ hàng"
                });
            }
        }

        /// <summary>
        /// Lấy tổng quan giỏ hàng (số lượng, tổng tiền)
        /// </summary>
        [HttpGet("summary")]
        public async Task<IActionResult> GetCartSummary()
        {
            try
            {
                var customerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                var cartSummary = await _cartService.GetCartSummaryAsync(customerId);

                return Ok(new
                {
                    success = true,
                    data = cartSummary,
                    message = "Lấy tổng quan giỏ hàng thành công"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Có lỗi xảy ra khi lấy tổng quan giỏ hàng"
                });
            }
        }
    }
} 
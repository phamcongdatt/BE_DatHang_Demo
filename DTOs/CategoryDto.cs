using System.ComponentModel.DataAnnotations;

namespace QuanLyDatHang.DTOs
{
    public class CategorySuggestionDto
    {
        [Required]
        [StringLength(100, ErrorMessage = "Tên danh mục không được quá 100 ký tự")]
        public string Ten { get; set; }

        [StringLength(255, ErrorMessage = "Mô tả không được quá 255 ký tự")]
        public string MoTa { get; set; }
    }

    public class CategoryCreateDto
    {
        [Required]
        [StringLength(100, ErrorMessage = "Tên danh mục không được quá 100 ký tự")]
        public string Ten { get; set; }

        [StringLength(255, ErrorMessage = "Mô tả không được quá 255 ký tự")]
        public string MoTa { get; set; }

        public bool MacDinh { get; set; } = false;
    }

    public class CategoryUpdateDto
    {
        [Required]
        [StringLength(100, ErrorMessage = "Tên danh mục không được quá 100 ký tự")]
        public string Ten { get; set; }

        [StringLength(255, ErrorMessage = "Mô tả không được quá 255 ký tự")]
        public string MoTa { get; set; }

        public bool MacDinh { get; set; }
    }
    public class CategoryUpdateRequestDto
    {
        [Required(ErrorMessage = "Mã danh mục là bắt buộc")]
        public Guid CategoryId { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "Tên mới không được quá 100 ký tự")]
        public string TenMoi { get; set; }

        [StringLength(255, ErrorMessage = "Mô tả mới không được quá 255 ký tự")]
        public string MoTaMoi { get; set; }

        public bool MacDinhMoi { get; set; } = false;
    }

}
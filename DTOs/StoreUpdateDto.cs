using System.ComponentModel.DataAnnotations;

namespace QuanLyDatHang.DTOs
{
    public class StoreUpdateDto
    {
        [StringLength(100)]
        public string Name { get; set; }

        [StringLength(255)]
        public string Address { get; set; }

        public string Description { get; set; }

        public decimal? Latitude { get; set; }

        public decimal? Longitude { get; set; }
    }
} 
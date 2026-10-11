using System;

namespace BanHangChuNgoc.Models
{
    public class BaoHang
    {
        public int MaBao { get; set; }
        public string TenBao { get; set; } = string.Empty;
        public decimal TienNhap { get; set; }
        public DateTime NgayNhap { get; set; } = DateTime.Now;
        public DateTime? NgayKetThuc { get; set; }
        public decimal TienBanThua { get; set; }
        public string TrangThai { get; set; } = "Đang bán"; // "Đang bán", "Đã kết thúc"
        public string? GhiChu { get; set; }

        // Các thuộc tính phụ trợ hiển thị
        public string NgayNhapFormatted => NgayNhap.ToString("dd/MM/yyyy HH:mm");
        public string NgayKetThucFormatted => NgayKetThuc.HasValue ? NgayKetThuc.Value.ToString("dd/MM/yyyy HH:mm") : "---";
        public bool IsDangBan => TrangThai == "Đang bán";
    }
}

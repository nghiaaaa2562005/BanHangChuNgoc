using System;

namespace BanHangChuNgoc.Models
{
    public class ThongKeBaoHang
    {
        public int MaBao { get; set; }
        public string TenBao { get; set; } = string.Empty;
        public decimal TienNhap { get; set; } // Đơn vị: k (nghìn đồng)
        public DateTime NgayNhap { get; set; }
        public DateTime? NgayKetThuc { get; set; }
        public decimal TienBanThua { get; set; } // Đơn vị: k (nghìn đồng)
        public string TrangThai { get; set; } = string.Empty;
        public string? GhiChu { get; set; }

        public int TongSoAoBanLe { get; set; }
        public decimal TongTienBanLe { get; set; }
        public decimal TongThu => TongTienBanLe + TienBanThua;
        public decimal LoiNhuan => TongThu - TienNhap; // Dương: Lãi hơn vốn, Âm: Chưa đủ hòa vốn

        public double TyLeLoiNhuan => TienNhap > 0 
            ? Math.Round((double)(LoiNhuan / TienNhap) * 100.0, 1) 
            : 0;

        public bool IsLoi => LoiNhuan >= 0;
        public string TienNhapFormatted => TienNhap.ToString("N0") + " k";
        public string TongTienBanLeFormatted => TongTienBanLe.ToString("N0") + " k";
        public string TienBanThuaFormatted => TienBanThua.ToString("N0") + " k";
        public string TongThuFormatted => TongThu.ToString("N0") + " k";
        public string LoiNhuanFormatted => (LoiNhuan >= 0 ? "+" : "") + LoiNhuan.ToString("N0") + " k";
        public string TyLeLoiNhuanFormatted => (TyLeLoiNhuan >= 0 ? "+" : "") + TyLeLoiNhuan.ToString("F1") + "%";
        public string NgayNhapFormatted => NgayNhap.ToString("dd/MM/yyyy");
        public string NgayKetThucFormatted => NgayKetThuc.HasValue ? NgayKetThuc.Value.ToString("dd/MM/yyyy") : "Đang bán";
    }
}

using System;

namespace BanHangChuNgoc.Models
{
    public class BanLe
    {
        public int MaBanLe { get; set; }
        public int MaBao { get; set; }
        public DateTime NgayBan { get; set; } = DateTime.Now;
        public int SoLuong { get; set; } = 1;
        public decimal DonGia { get; set; } // Tính theo nghìn đ (k), VD: 30k lưu 30
        public decimal ThanhTien => SoLuong * DonGia;
        public string? GhiChu { get; set; }

        public string NgayBanFormatted => NgayBan.ToString("dd/MM/yyyy HH:mm");
        public string DonGiaFormatted => DonGia.ToString("N0") + " k";
        public string ThanhTienFormatted => ThanhTien.ToString("N0") + " k";
    }
}

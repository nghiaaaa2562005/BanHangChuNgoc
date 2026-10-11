using System;
using System.IO;
using System.Text.Json;

namespace BanHangChuNgoc.Services;

public class TaiKhoanModel
{
    public string TaiKhoan { get; set; } = "NgocMai";
    public string MatKhau { get; set; } = "NgocMai";
}

public class AuthService
{
    private static readonly string FilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "taikhoan.json");

    public static TaiKhoanModel LayThongTinTaiKhoan()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                string json = File.ReadAllText(FilePath);
                var model = JsonSerializer.Deserialize<TaiKhoanModel>(json);
                if (model != null && !string.IsNullOrWhiteSpace(model.TaiKhoan))
                {
                    return model;
                }
            }

            // Nếu file chưa có hoặc rỗng, tạo file mặc định
            var defaultAcc = new TaiKhoanModel { TaiKhoan = "NgocMai", MatKhau = "NgocMai" };
            LuuTaiKhoan(defaultAcc);
            return defaultAcc;
        }
        catch
        {
            return new TaiKhoanModel { TaiKhoan = "NgocMai", MatKhau = "NgocMai" };
        }
    }

    public static void LuuTaiKhoan(TaiKhoanModel acc)
    {
        try
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(acc, options);
            File.WriteAllText(FilePath, json);
        }
        catch
        {
            // Bỏ qua lỗi ghi file nếu có vấn đề quyền hạn
        }
    }

    public static bool KiemTraDangNhap(string? taiKhoan, string? matKhau)
    {
        if (string.IsNullOrWhiteSpace(taiKhoan) || string.IsNullOrWhiteSpace(matKhau))
        {
            return false;
        }

        var config = LayThongTinTaiKhoan();
        return string.Equals(taiKhoan.Trim(), config.TaiKhoan.Trim(), StringComparison.OrdinalIgnoreCase)
               && string.Equals(matKhau.Trim(), config.MatKhau.Trim());
    }
}

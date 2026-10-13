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
    public static string GetFilePath()
    {
        string baseFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "taikhoan.json");
        if (File.Exists(baseFile))
        {
            return baseFile;
        }

        string projectPath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "taikhoan.json"));
        if (File.Exists(projectPath))
        {
            return projectPath;
        }

        return baseFile;
    }

    public static TaiKhoanModel LayThongTinTaiKhoan()
    {
        try
        {
            string path = GetFilePath();
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
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
            string path = GetFilePath();
            File.WriteAllText(path, json);

            string baseFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "taikhoan.json");
            if (!string.Equals(path, baseFile, StringComparison.OrdinalIgnoreCase))
            {
                File.WriteAllText(baseFile, json);
            }
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

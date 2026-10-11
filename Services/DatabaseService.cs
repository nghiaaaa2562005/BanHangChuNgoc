using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using BanHangChuNgoc.Models;

namespace BanHangChuNgoc.Services
{
    public class DatabaseService
    {
        public static string ConnectionString { get; set; } = 
            @"Server=localhost;Database=QuanLyBanHangChuNgoc;Trusted_Connection=True;TrustServerCertificate=True;Connect Timeout=5;";

        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                using var conn = new SqlConnection(ConnectionString);
                await conn.OpenAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task EnsureDatabaseCreatedAsync()
        {
            try
            {
                using var conn = new SqlConnection(ConnectionString);
                await conn.OpenAsync();

                string initQuery = @"
                IF OBJECT_ID('BaoHang', 'U') IS NULL
                BEGIN
                    CREATE TABLE BaoHang (
                        MaBao INT IDENTITY(1,1) PRIMARY KEY,
                        TenBao NVARCHAR(150) NOT NULL,
                        TienNhap DECIMAL(18,2) NOT NULL DEFAULT 0,
                        NgayNhap DATETIME NOT NULL DEFAULT GETDATE(),
                        NgayKetThuc DATETIME NULL,
                        TienBanThua DECIMAL(18,2) NOT NULL DEFAULT 0,
                        TrangThai NVARCHAR(50) NOT NULL DEFAULT N'Đang bán',
                        GhiChu NVARCHAR(500) NULL
                    );
                END

                IF OBJECT_ID('BanLe', 'U') IS NULL
                BEGIN
                    CREATE TABLE BanLe (
                        MaBanLe INT IDENTITY(1,1) PRIMARY KEY,
                        MaBao INT NOT NULL,
                        NgayBan DATETIME NOT NULL DEFAULT GETDATE(),
                        SoLuong INT NOT NULL DEFAULT 1 CHECK (SoLuong > 0),
                        DonGia DECIMAL(18,2) NOT NULL DEFAULT 0,
                        ThanhTien AS (SoLuong * DonGia),
                        GhiChu NVARCHAR(255) NULL,
                        CONSTRAINT FK_BanLe_BaoHang FOREIGN KEY (MaBao) REFERENCES BaoHang(MaBao) ON DELETE CASCADE
                    );
                END
                ";

                using var cmd = new SqlCommand(initQuery, conn);
                await cmd.ExecuteNonQueryAsync();
            }
            catch
            {
            }
        }

        // ==================== QUẢN LÝ BAO HÀNG ====================

        public async Task<List<BaoHang>> GetDanhSachBaoHangAsync(string? filterTrangThai = null)
        {
            var list = new List<BaoHang>();
            using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();

            string query = "SELECT MaBao, TenBao, TienNhap, NgayNhap, NgayKetThuc, TienBanThua, TrangThai, GhiChu FROM BaoHang ";
            if (!string.IsNullOrEmpty(filterTrangThai) && filterTrangThai != "Tất cả")
            {
                query += "WHERE TrangThai = @TrangThai ";
            }
            query += "ORDER BY NgayNhap DESC;";

            using var cmd = new SqlCommand(query, conn);
            if (!string.IsNullOrEmpty(filterTrangThai) && filterTrangThai != "Tất cả")
            {
                cmd.Parameters.AddWithValue("@TrangThai", filterTrangThai);
            }

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new BaoHang
                {
                    MaBao = reader.GetInt32(0),
                    TenBao = reader.GetString(1),
                    TienNhap = reader.GetDecimal(2),
                    NgayNhap = reader.GetDateTime(3),
                    NgayKetThuc = reader.IsDBNull(4) ? null : reader.GetDateTime(4),
                    TienBanThua = reader.GetDecimal(5),
                    TrangThai = reader.GetString(6),
                    GhiChu = reader.IsDBNull(7) ? null : reader.GetString(7)
                });
            }
            return list;
        }

        public async Task<int> ThemBaoHangAsync(BaoHang bao)
        {
            using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();

            string query = @"
            INSERT INTO BaoHang (TenBao, TienNhap, NgayNhap, TrangThai, GhiChu)
            VALUES (@TenBao, @TienNhap, @NgayNhap, N'Đang bán', @GhiChu);
            SELECT SCOPE_IDENTITY();";

            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@TenBao", bao.TenBao);
            cmd.Parameters.AddWithValue("@TienNhap", bao.TienNhap);
            cmd.Parameters.AddWithValue("@NgayNhap", bao.NgayNhap);
            cmd.Parameters.AddWithValue("@GhiChu", (object?)bao.GhiChu ?? DBNull.Value);

            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        public async Task ChotBaoHangAsync(int maBao, decimal tienBanThua, DateTime ngayKetThuc)
        {
            using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();

            string query = @"
            UPDATE BaoHang 
            SET TienBanThua = @TienBanThua, 
                NgayKetThuc = @NgayKetThuc, 
                TrangThai = N'Đã kết thúc'
            WHERE MaBao = @MaBao;";

            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@MaBao", maBao);
            cmd.Parameters.AddWithValue("@TienBanThua", tienBanThua);
            cmd.Parameters.AddWithValue("@NgayKetThuc", ngayKetThuc);

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task XoaBaoHangAsync(int maBao)
        {
            using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();

            string query = "DELETE FROM BaoHang WHERE MaBao = @MaBao;";
            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@MaBao", maBao);

            await cmd.ExecuteNonQueryAsync();
        }

        // ==================== BÁN LẺ ====================

        public async Task<List<BanLe>> GetDanhSachBanLeAsync(int maBao)
        {
            var list = new List<BanLe>();
            using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();

            string query = @"
            SELECT MaBanLe, MaBao, NgayBan, SoLuong, DonGia, GhiChu 
            FROM BanLe 
            WHERE MaBao = @MaBao 
            ORDER BY NgayBan DESC;";

            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@MaBao", maBao);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new BanLe
                {
                    MaBanLe = reader.GetInt32(0),
                    MaBao = reader.GetInt32(1),
                    NgayBan = reader.GetDateTime(2),
                    SoLuong = reader.GetInt32(3),
                    DonGia = reader.GetDecimal(4),
                    GhiChu = reader.IsDBNull(5) ? null : reader.GetString(5)
                });
            }
            return list;
        }

        public async Task<int> ThemBanLeAsync(BanLe banLe)
        {
            using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();

            string query = @"
            INSERT INTO BanLe (MaBao, NgayBan, SoLuong, DonGia, GhiChu)
            VALUES (@MaBao, @NgayBan, @SoLuong, @DonGia, @GhiChu);
            SELECT SCOPE_IDENTITY();";

            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@MaBao", banLe.MaBao);
            cmd.Parameters.AddWithValue("@NgayBan", banLe.NgayBan);
            cmd.Parameters.AddWithValue("@SoLuong", banLe.SoLuong);
            cmd.Parameters.AddWithValue("@DonGia", banLe.DonGia);
            cmd.Parameters.AddWithValue("@GhiChu", (object?)banLe.GhiChu ?? DBNull.Value);

            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        public async Task XoaBanLeAsync(int maBanLe)
        {
            using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();

            string query = "DELETE FROM BanLe WHERE MaBanLe = @MaBanLe;";
            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@MaBanLe", maBanLe);

            await cmd.ExecuteNonQueryAsync();
        }

        // ==================== BÁO CÁO THỐNG KÊ ====================

        public async Task<List<ThongKeBaoHang>> GetThongKeTatCaBaoAsync(int? thang = null, int? nam = null)
        {
            var list = new List<ThongKeBaoHang>();
            using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();

            string query = @"
            SELECT 
                b.MaBao,
                b.TenBao,
                b.TienNhap,
                b.NgayNhap,
                b.NgayKetThuc,
                b.TienBanThua,
                b.TrangThai,
                b.GhiChu,
                ISNULL(SUM(bl.SoLuong), 0) AS TongSoAoBanLe,
                ISNULL(SUM(bl.ThanhTien), 0) AS TongTienBanLe
            FROM BaoHang b
            LEFT JOIN BanLe bl ON b.MaBao = bl.MaBao
            WHERE (@Thang IS NULL OR MONTH(b.NgayNhap) = @Thang)
              AND (@Nam IS NULL OR YEAR(b.NgayNhap) = @Nam)
            GROUP BY 
                b.MaBao, b.TenBao, b.TienNhap, b.NgayNhap, b.NgayKetThuc, b.TienBanThua, b.TrangThai, b.GhiChu
            ORDER BY b.NgayNhap DESC;";

            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@Thang", (object?)thang ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Nam", (object?)nam ?? DBNull.Value);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new ThongKeBaoHang
                {
                    MaBao = reader.GetInt32(0),
                    TenBao = reader.GetString(1),
                    TienNhap = reader.GetDecimal(2),
                    NgayNhap = reader.GetDateTime(3),
                    NgayKetThuc = reader.IsDBNull(4) ? null : reader.GetDateTime(4),
                    TienBanThua = reader.GetDecimal(5),
                    TrangThai = reader.GetString(6),
                    GhiChu = reader.IsDBNull(7) ? null : reader.GetString(7),
                    TongSoAoBanLe = reader.GetInt32(8),
                    TongTienBanLe = reader.GetDecimal(9)
                });
            }
            return list;
        }
    }
}

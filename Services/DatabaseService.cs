using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.Data.SqlClient;
using BanHangChuNgoc.Models;

namespace BanHangChuNgoc.Services
{
    public class DatabaseService
    {
        public static string DbPath
        {
            get
            {
                try
                {
                    string projectDir = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", ".."));
                    if (Directory.Exists(projectDir) && File.Exists(Path.Combine(projectDir, "BanHangChuNgoc.csproj")))
                    {
                        return Path.Combine(projectDir, "BanHangChuNgoc.db");
                    }
                }
                catch
                {
                }
                return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "BanHangChuNgoc.db");
            }
        }

        public static string ConnectionString => $"Data Source={DbPath};Foreign Keys=True;";

        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                using var conn = new SqliteConnection(ConnectionString);
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
                using var conn = new SqliteConnection(ConnectionString);
                await conn.OpenAsync();

                string initQuery = @"
                PRAGMA foreign_keys = ON;

                CREATE TABLE IF NOT EXISTS BaoHang (
                    MaBao INTEGER PRIMARY KEY AUTOINCREMENT,
                    TenBao TEXT NOT NULL,
                    TienNhap NUMERIC NOT NULL DEFAULT 0,
                    NgayNhap TEXT NOT NULL,
                    NgayKetThuc TEXT NULL,
                    TienBanThua NUMERIC NOT NULL DEFAULT 0,
                    TrangThai TEXT NOT NULL DEFAULT 'Đang bán',
                    GhiChu TEXT NULL
                );

                CREATE TABLE IF NOT EXISTS BanLe (
                    MaBanLe INTEGER PRIMARY KEY AUTOINCREMENT,
                    MaBao INTEGER NOT NULL,
                    NgayBan TEXT NOT NULL,
                    SoLuong INTEGER NOT NULL DEFAULT 1 CHECK (SoLuong > 0),
                    DonGia NUMERIC NOT NULL DEFAULT 0,
                    ThanhTien NUMERIC GENERATED ALWAYS AS (SoLuong * DonGia) STORED,
                    GhiChu TEXT NULL,
                    CONSTRAINT FK_BanLe_BaoHang FOREIGN KEY (MaBao) REFERENCES BaoHang(MaBao) ON DELETE CASCADE
                );
                ";

                using (var cmd = new SqliteCommand(initQuery, conn))
                {
                    await cmd.ExecuteNonQueryAsync();
                }

                // Kiểm tra xem bảng có dữ liệu chưa
                using var countCmd = new SqliteCommand("SELECT COUNT(*) FROM BaoHang;", conn);
                long count = Convert.ToInt64(await countCmd.ExecuteScalarAsync());

                if (count == 0)
                {
                    await TryMigrateFromSqlServerAsync(conn);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"EnsureDatabaseCreatedAsync error: {ex.Message}");
            }
        }

        private async Task TryMigrateFromSqlServerAsync(SqliteConnection sqliteConn)
        {
            try
            {
                string sqlServerConnStr = @"Server=localhost;Database=QuanLyBanHangChuNgoc;Trusted_Connection=True;TrustServerCertificate=True;Connect Timeout=3;";
                using var sqlConn = new SqlConnection(sqlServerConnStr);
                await sqlConn.OpenAsync();

                var baoList = new List<BaoHang>();
                using (var cmdBao = new SqlCommand("SELECT MaBao, TenBao, TienNhap, NgayNhap, NgayKetThuc, TienBanThua, TrangThai, GhiChu FROM BaoHang ORDER BY MaBao ASC;", sqlConn))
                using (var reader = await cmdBao.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        baoList.Add(new BaoHang
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
                }

                var banLeList = new List<BanLe>();
                using (var cmdBanLe = new SqlCommand("SELECT MaBanLe, MaBao, NgayBan, SoLuong, DonGia, GhiChu FROM BanLe ORDER BY MaBanLe ASC;", sqlConn))
                using (var reader = await cmdBanLe.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        banLeList.Add(new BanLe
                        {
                            MaBanLe = reader.GetInt32(0),
                            MaBao = reader.GetInt32(1),
                            NgayBan = reader.GetDateTime(2),
                            SoLuong = reader.GetInt32(3),
                            DonGia = reader.GetDecimal(4),
                            GhiChu = reader.IsDBNull(5) ? null : reader.GetString(5)
                        });
                    }
                }

                if (baoList.Count > 0)
                {
                    using var trans = sqliteConn.BeginTransaction();

                    foreach (var bao in baoList)
                    {
                        string insertBao = @"
                        INSERT INTO BaoHang (MaBao, TenBao, TienNhap, NgayNhap, NgayKetThuc, TienBanThua, TrangThai, GhiChu)
                        VALUES (@MaBao, @TenBao, @TienNhap, @NgayNhap, @NgayKetThuc, @TienBanThua, @TrangThai, @GhiChu);";

                        using var cmd = new SqliteCommand(insertBao, sqliteConn, trans);
                        cmd.Parameters.AddWithValue("@MaBao", bao.MaBao);
                        cmd.Parameters.AddWithValue("@TenBao", bao.TenBao);
                        cmd.Parameters.AddWithValue("@TienNhap", bao.TienNhap);
                        cmd.Parameters.AddWithValue("@NgayNhap", bao.NgayNhap.ToString("yyyy-MM-dd HH:mm:ss"));
                        cmd.Parameters.AddWithValue("@NgayKetThuc", bao.NgayKetThuc.HasValue ? bao.NgayKetThuc.Value.ToString("yyyy-MM-dd HH:mm:ss") : (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@TienBanThua", bao.TienBanThua);
                        cmd.Parameters.AddWithValue("@TrangThai", bao.TrangThai);
                        cmd.Parameters.AddWithValue("@GhiChu", (object?)bao.GhiChu ?? DBNull.Value);
                        await cmd.ExecuteNonQueryAsync();
                    }

                    foreach (var bl in banLeList)
                    {
                        string insertBl = @"
                        INSERT INTO BanLe (MaBanLe, MaBao, NgayBan, SoLuong, DonGia, GhiChu)
                        VALUES (@MaBanLe, @MaBao, @NgayBan, @SoLuong, @DonGia, @GhiChu);";

                        using var cmd = new SqliteCommand(insertBl, sqliteConn, trans);
                        cmd.Parameters.AddWithValue("@MaBanLe", bl.MaBanLe);
                        cmd.Parameters.AddWithValue("@MaBao", bl.MaBao);
                        cmd.Parameters.AddWithValue("@NgayBan", bl.NgayBan.ToString("yyyy-MM-dd HH:mm:ss"));
                        cmd.Parameters.AddWithValue("@SoLuong", bl.SoLuong);
                        cmd.Parameters.AddWithValue("@DonGia", bl.DonGia);
                        cmd.Parameters.AddWithValue("@GhiChu", (object?)bl.GhiChu ?? DBNull.Value);
                        await cmd.ExecuteNonQueryAsync();
                    }

                    await trans.CommitAsync();
                }
            }
            catch
            {
                // Bỏ qua nếu không kết nối được SQL Server
            }
        }

        // ==================== QUẢN LÝ BAO HÀNG ====================

        public async Task<List<BaoHang>> GetDanhSachBaoHangAsync(string? filterTrangThai = null)
        {
            var list = new List<BaoHang>();
            using var conn = new SqliteConnection(ConnectionString);
            await conn.OpenAsync();

            string query = "SELECT MaBao, TenBao, TienNhap, NgayNhap, NgayKetThuc, TienBanThua, TrangThai, GhiChu FROM BaoHang ";
            if (!string.IsNullOrEmpty(filterTrangThai) && filterTrangThai != "Tất cả")
            {
                query += "WHERE TrangThai = @TrangThai ";
            }
            query += "ORDER BY NgayNhap DESC;";

            using var cmd = new SqliteCommand(query, conn);
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
                    NgayNhap = ParseDateTime(reader.GetString(3)),
                    NgayKetThuc = reader.IsDBNull(4) ? null : ParseDateTime(reader.GetString(4)),
                    TienBanThua = reader.GetDecimal(5),
                    TrangThai = reader.GetString(6),
                    GhiChu = reader.IsDBNull(7) ? null : reader.GetString(7)
                });
            }
            return list;
        }

        public async Task<int> ThemBaoHangAsync(BaoHang bao)
        {
            using var conn = new SqliteConnection(ConnectionString);
            await conn.OpenAsync();

            string query = @"
            INSERT INTO BaoHang (TenBao, TienNhap, NgayNhap, TrangThai, GhiChu)
            VALUES (@TenBao, @TienNhap, @NgayNhap, 'Đang bán', @GhiChu);
            SELECT last_insert_rowid();";

            using var cmd = new SqliteCommand(query, conn);
            cmd.Parameters.AddWithValue("@TenBao", bao.TenBao);
            cmd.Parameters.AddWithValue("@TienNhap", bao.TienNhap);
            cmd.Parameters.AddWithValue("@NgayNhap", bao.NgayNhap.ToString("yyyy-MM-dd HH:mm:ss"));
            cmd.Parameters.AddWithValue("@GhiChu", (object?)bao.GhiChu ?? DBNull.Value);

            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        public async Task ChotBaoHangAsync(int maBao, decimal tienBanThua, DateTime ngayKetThuc)
        {
            using var conn = new SqliteConnection(ConnectionString);
            await conn.OpenAsync();

            string query = @"
            UPDATE BaoHang 
            SET TienBanThua = @TienBanThua, 
                NgayKetThuc = @NgayKetThuc, 
                TrangThai = 'Đã kết thúc'
            WHERE MaBao = @MaBao;";

            using var cmd = new SqliteCommand(query, conn);
            cmd.Parameters.AddWithValue("@MaBao", maBao);
            cmd.Parameters.AddWithValue("@TienBanThua", tienBanThua);
            cmd.Parameters.AddWithValue("@NgayKetThuc", ngayKetThuc.ToString("yyyy-MM-dd HH:mm:ss"));

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task XoaBaoHangAsync(int maBao)
        {
            using var conn = new SqliteConnection(ConnectionString);
            await conn.OpenAsync();

            using var cmdEn = new SqliteCommand("PRAGMA foreign_keys = ON;", conn);
            await cmdEn.ExecuteNonQueryAsync();

            string query = @"
            DELETE FROM BanLe WHERE MaBao = @MaBao;
            DELETE FROM BaoHang WHERE MaBao = @MaBao;";

            using var cmd = new SqliteCommand(query, conn);
            cmd.Parameters.AddWithValue("@MaBao", maBao);

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task XoaDanhSachBaoHangAsync(IEnumerable<int> maBaoList)
        {
            var list = maBaoList.Distinct().ToList();
            if (list.Count == 0) return;

            using var conn = new SqliteConnection(ConnectionString);
            await conn.OpenAsync();

            using var trans = conn.BeginTransaction();
            try
            {
                var paramNames = list.Select((id, index) => $"@p{index}").ToList();
                string inClause = string.Join(", ", paramNames);

                string query = $@"
                DELETE FROM BanLe WHERE MaBao IN ({inClause});
                DELETE FROM BaoHang WHERE MaBao IN ({inClause});";

                using var cmd = new SqliteCommand(query, conn, trans);
                for (int i = 0; i < list.Count; i++)
                {
                    cmd.Parameters.AddWithValue($"@p{i}", list[i]);
                }

                await cmd.ExecuteNonQueryAsync();
                await trans.CommitAsync();
            }
            catch
            {
                await trans.RollbackAsync();
                throw;
            }
        }

        // ==================== BÁN LẺ ====================

        public async Task<List<BanLe>> GetDanhSachBanLeAsync(int maBao)
        {
            var list = new List<BanLe>();
            using var conn = new SqliteConnection(ConnectionString);
            await conn.OpenAsync();

            string query = @"
            SELECT MaBanLe, MaBao, NgayBan, SoLuong, DonGia, GhiChu 
            FROM BanLe 
            WHERE MaBao = @MaBao 
            ORDER BY NgayBan DESC;";

            using var cmd = new SqliteCommand(query, conn);
            cmd.Parameters.AddWithValue("@MaBao", maBao);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new BanLe
                {
                    MaBanLe = reader.GetInt32(0),
                    MaBao = reader.GetInt32(1),
                    NgayBan = ParseDateTime(reader.GetString(2)),
                    SoLuong = reader.GetInt32(3),
                    DonGia = reader.GetDecimal(4),
                    GhiChu = reader.IsDBNull(5) ? null : reader.GetString(5)
                });
            }
            return list;
        }

        public async Task<int> ThemBanLeAsync(BanLe banLe)
        {
            using var conn = new SqliteConnection(ConnectionString);
            await conn.OpenAsync();

            string query = @"
            INSERT INTO BanLe (MaBao, NgayBan, SoLuong, DonGia, GhiChu)
            VALUES (@MaBao, @NgayBan, @SoLuong, @DonGia, @GhiChu);
            SELECT last_insert_rowid();";

            using var cmd = new SqliteCommand(query, conn);
            cmd.Parameters.AddWithValue("@MaBao", banLe.MaBao);
            cmd.Parameters.AddWithValue("@NgayBan", banLe.NgayBan.ToString("yyyy-MM-dd HH:mm:ss"));
            cmd.Parameters.AddWithValue("@SoLuong", banLe.SoLuong);
            cmd.Parameters.AddWithValue("@DonGia", banLe.DonGia);
            cmd.Parameters.AddWithValue("@GhiChu", (object?)banLe.GhiChu ?? DBNull.Value);

            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        public async Task XoaBanLeAsync(int maBanLe)
        {
            using var conn = new SqliteConnection(ConnectionString);
            await conn.OpenAsync();

            string query = "DELETE FROM BanLe WHERE MaBanLe = @MaBanLe;";
            using var cmd = new SqliteCommand(query, conn);
            cmd.Parameters.AddWithValue("@MaBanLe", maBanLe);

            await cmd.ExecuteNonQueryAsync();
        }

        // ==================== BÁO CÁO THỐNG KÊ ====================

        public async Task<List<ThongKeBaoHang>> GetThongKeTatCaBaoAsync(int? tuThang = null, int? denThang = null, int? nam = null)
        {
            var list = new List<ThongKeBaoHang>();
            using var conn = new SqliteConnection(ConnectionString);
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
                COALESCE(SUM(bl.SoLuong), 0) AS TongSoAoBanLe,
                COALESCE(SUM(bl.ThanhTien), 0) AS TongTienBanLe
            FROM BaoHang b
            LEFT JOIN BanLe bl ON b.MaBao = bl.MaBao
            WHERE (@TuThang IS NULL OR CAST(strftime('%m', b.NgayNhap) AS INTEGER) >= @TuThang)
              AND (@DenThang IS NULL OR CAST(strftime('%m', b.NgayNhap) AS INTEGER) <= @DenThang)
              AND (@Nam IS NULL OR CAST(strftime('%Y', b.NgayNhap) AS INTEGER) = @Nam)
            GROUP BY 
                b.MaBao, b.TenBao, b.TienNhap, b.NgayNhap, b.NgayKetThuc, b.TienBanThua, b.TrangThai, b.GhiChu
            ORDER BY b.NgayNhap DESC;";

            using var cmd = new SqliteCommand(query, conn);
            cmd.Parameters.AddWithValue("@TuThang", (object?)tuThang ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@DenThang", (object?)denThang ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Nam", (object?)nam ?? DBNull.Value);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new ThongKeBaoHang
                {
                    MaBao = reader.GetInt32(0),
                    TenBao = reader.GetString(1),
                    TienNhap = reader.GetDecimal(2),
                    NgayNhap = ParseDateTime(reader.GetString(3)),
                    NgayKetThuc = reader.IsDBNull(4) ? null : ParseDateTime(reader.GetString(4)),
                    TienBanThua = reader.GetDecimal(5),
                    TrangThai = reader.GetString(6),
                    GhiChu = reader.IsDBNull(7) ? null : reader.GetString(7),
                    TongSoAoBanLe = reader.GetInt32(8),
                    TongTienBanLe = reader.GetDecimal(9)
                });
            }
            return list;
        }

        private static DateTime ParseDateTime(string value)
        {
            if (DateTime.TryParse(value, out var dt))
            {
                return dt;
            }
            return DateTime.Now;
        }
    }
}

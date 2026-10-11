-- ===================================================================
-- CƠ SỞ DỮ LIỆU: QUẢN LÝ BÁN HÀNG QUẦN ÁO THEO BAO (BanHangChuNgoc)
-- Hệ quản trị CSDL: Microsoft SQL Server (T-SQL)
-- Quy chuẩn tiền tệ: Rút gọn trừ 3 số 0 (Đơn vị tính: nghìn đồng / k)
-- Ví dụ: 1.000.000đ lưu 1000 | 30.000đ lưu 30 | 4.500.000đ lưu 4500
-- ===================================================================

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

-- Tạo CSDL nếu chưa có
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'QuanLyBanHangChuNgoc')
BEGIN
    CREATE DATABASE QuanLyBanHangChuNgoc;
END
GO

USE QuanLyBanHangChuNgoc;
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

-- Xóa bảng cũ nếu tồn tại
IF OBJECT_ID('v_ThongKeBaoHang', 'V') IS NOT NULL DROP VIEW v_ThongKeBaoHang;
IF OBJECT_ID('BanLe', 'U') IS NOT NULL DROP TABLE BanLe;
IF OBJECT_ID('BaoHang', 'U') IS NOT NULL DROP TABLE BaoHang;
GO

-- ===================================================================
-- 1. BẢNG BAO HÀNG (Quản lý các kiện / bao quần áo nhập về)
-- ===================================================================
CREATE TABLE BaoHang (
    MaBao INT IDENTITY(1,1) PRIMARY KEY,
    TenBao NVARCHAR(150) NOT NULL,              -- Tên/Loại hàng (VD: Bao áo phông Mỹ, Bao len Nhật...)
    TienNhap DECIMAL(18,2) NOT NULL DEFAULT 0,  -- Tiền vốn mua bao (đơn vị: k)
    NgayNhap DATETIME NOT NULL DEFAULT GETDATE(),-- Ngày nhập bao về
    
    NgayKetThuc DATETIME NULL,                  -- Ngày bán bao thừa đi và chốt sổ bao đó
    TienBanThua DECIMAL(18,2) NOT NULL DEFAULT 0,-- Tiền bán thanh lý bao thừa (đơn vị: k)
    TrangThai NVARCHAR(50) NOT NULL DEFAULT N'Đang bán', -- 'Đang bán', 'Đã kết thúc'
    
    GhiChu NVARCHAR(500) NULL                   -- Ghi chú thêm
);
GO

-- ===================================================================
-- 2. BẢNG BÁN LẺ (Ghi nhận từng lần bán áo lẻ từ bao hàng)
-- ===================================================================
CREATE TABLE BanLe (
    MaBanLe INT IDENTITY(1,1) PRIMARY KEY,
    MaBao INT NOT NULL,                         -- Áo này lấy từ bao nào
    NgayBan DATETIME NOT NULL DEFAULT GETDATE(), -- Thời điểm bán
    SoLuong INT NOT NULL DEFAULT 1 CHECK (SoLuong > 0), -- Số lượng áo bán
    DonGia DECIMAL(18,2) NOT NULL DEFAULT 0,    -- Giá bán cái áo (đơn vị: k, VD: 30, 50, 80)
    ThanhTien AS (SoLuong * DonGia),            -- Tổng tiền thu lần bán (đơn vị: k)
    GhiChu NVARCHAR(255) NULL,                  -- Ghi chú nếu có
    
    CONSTRAINT FK_BanLe_BaoHang FOREIGN KEY (MaBao) REFERENCES BaoHang(MaBao) ON DELETE CASCADE
);
GO

-- ===================================================================
-- 3. VIEW TỔNG HỢP: THỐNG KÊ DOANH THU & LÃI HƠN VỐN (ĐƠN VỊ: k)
-- ===================================================================
CREATE VIEW v_ThongKeBaoHang AS
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
    ISNULL(SUM(bl.ThanhTien), 0) AS TongTienBanLe,
    -- Tổng thu = Tiền bán lẻ + Tiền bán bao thừa
    (ISNULL(SUM(bl.ThanhTien), 0) + b.TienBanThua) AS TongThu,
    -- Lợi nhuận (Lãi hơn vốn) = Tổng thu - Tiền vốn nhập
    ((ISNULL(SUM(bl.ThanhTien), 0) + b.TienBanThua) - b.TienNhap) AS LoiNhuan,
    -- Tỷ lệ % Lãi hơn vốn
    CASE 
        WHEN b.TienNhap > 0 THEN 
            ROUND((((ISNULL(SUM(bl.ThanhTien), 0) + b.TienBanThua) - b.TienNhap) / b.TienNhap) * 100.0, 1)
        ELSE 0 
    END AS TyLeLoiNhuan
FROM BaoHang b
LEFT JOIN BanLe bl ON b.MaBao = bl.MaBao
GROUP BY 
    b.MaBao, b.TenBao, b.TienNhap, b.NgayNhap, b.NgayKetThuc, b.TienBanThua, b.TrangThai, b.GhiChu;
GO


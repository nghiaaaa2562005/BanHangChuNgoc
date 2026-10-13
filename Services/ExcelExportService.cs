using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClosedXML.Excel;
using BanHangChuNgoc.Models;

namespace BanHangChuNgoc.Services
{
    public class ExcelExportService
    {
        public void ExportThongKeDoanhThu(
            string filePath,
            IEnumerable<ThongKeBaoHang> danhSach,
            string thoiGianLoc,
            decimal tongVon,
            decimal tongBanLe,
            decimal tongBanThua,
            decimal tongDoanhThu,
            decimal tongLoiNhuan,
            int tongSoAo)
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Thống kê doanh thu");

            // Hiển thị lưới ô
            ws.ShowGridLines = true;

            // 1. TIÊU ĐỀ BÁO CÁO
            ws.Range("A2:L2").Merge();
            var titleCell = ws.Cell("A2");
            titleCell.Value = "BÁO CÁO THỐNG KÊ DOANH THU & DÒNG TIỀN";
            titleCell.Style.Font.Bold = true;
            titleCell.Style.Font.FontSize = 16;
            titleCell.Style.Font.FontColor = XLColor.FromHtml("#1E3A8A");
            titleCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            titleCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            ws.Row(2).Height = 28;

            ws.Range("A3:L3").Merge();
            var subTitleCell = ws.Cell("A3");
            subTitleCell.Value = $"Bộ lọc: {thoiGianLoc}   |   Ngày xuất: {DateTime.Now:dd/MM/yyyy HH:mm}";
            subTitleCell.Style.Font.Italic = true;
            subTitleCell.Style.Font.FontSize = 10;
            subTitleCell.Style.Font.FontColor = XLColor.FromHtml("#64748B");
            subTitleCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Row(3).Height = 18;

            // 2. KHỐI TỔNG HỢP KPI (Rows 5-6)
            string[] kpiHeaders = { "TỔNG VỐN BỎ RA", "TỔNG THU BÁN LẺ", "TỔNG THU BAO THỪA", "TỔNG DOANH THU", "TỔNG LÃI RÒNG", "TỔNG ÁO ĐÃ BÁN" };
            object[] kpiValues = { tongVon, tongBanLe, tongBanThua, tongDoanhThu, tongLoiNhuan, tongSoAo };
            string[] kpiColors = { "#DC2626", "#059669", "#D97706", "#2563EB", tongLoiNhuan >= 0 ? "#16A34A" : "#DC2626", "#4F46E5" };

            for (int i = 0; i < 6; i++)
            {
                int startCol = 1 + i * 2;
                int endCol = startCol + 1;

                // Header cell
                var headerRange = ws.Range(5, startCol, 5, endCol);
                headerRange.Merge();
                headerRange.Value = kpiHeaders[i];
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Font.FontSize = 9;
                headerRange.Style.Font.FontColor = XLColor.FromHtml("#475569");
                headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#F1F5F9");
                headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                headerRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                // Value cell
                var valRange = ws.Range(6, startCol, 6, endCol);
                valRange.Merge();
                if (i == 5)
                {
                    valRange.Value = $"{kpiValues[i]} cái";
                }
                else
                {
                    decimal val = (decimal)kpiValues[i];
                    valRange.Value = (i == 4 && val > 0 ? "+" : "") + val.ToString("N0") + " k";
                }
                valRange.Style.Font.Bold = true;
                valRange.Style.Font.FontSize = 12;
                valRange.Style.Font.FontColor = XLColor.FromHtml(kpiColors[i]);
                valRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#F8FAFC");
                valRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                valRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                // Border cho từng khối KPI
                var blockRange = ws.Range(5, startCol, 6, endCol);
                blockRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                blockRange.Style.Border.OutsideBorderColor = XLColor.FromHtml("#CBD5E1");
            }
            ws.Row(5).Height = 20;
            ws.Row(6).Height = 24;

            // 3. BẢNG CHI TIẾT
            int headerRow = 8;
            string[] tableHeaders = new[]
            {
                "Mã bao", "Tên bao hàng", "Ngày nhập", "Ngày chốt", 
                "Tiền vốn (k)", "Số áo bán", "Thu bán lẻ (k)", "Thu bao thừa (k)", 
                "Tổng thu (k)", "Lãi hơn vốn (k)", "% Lãi/Lỗ", "Trạng thái", "Ghi chú"
            };

            for (int col = 1; col <= tableHeaders.Length; col++)
            {
                var cell = ws.Cell(headerRow, col);
                cell.Value = tableHeaders[col - 1];
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontSize = 10;
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1E3A8A");
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                cell.Style.Border.OutsideBorderColor = XLColor.FromHtml("#0F172A");
            }
            ws.Row(headerRow).Height = 26;

            int currentRow = headerRow + 1;
            int stt = 1;

            foreach (var item in danhSach)
            {
                ws.Cell(currentRow, 1).Value = item.MaBao;
                ws.Cell(currentRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                ws.Cell(currentRow, 2).Value = item.TenBao;
                ws.Cell(currentRow, 2).Style.Font.Bold = true;

                ws.Cell(currentRow, 3).Value = item.NgayNhap.ToString("dd/MM/yyyy");
                ws.Cell(currentRow, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                ws.Cell(currentRow, 4).Value = item.NgayKetThuc.HasValue ? item.NgayKetThuc.Value.ToString("dd/MM/yyyy") : "Đang bán";
                ws.Cell(currentRow, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                ws.Cell(currentRow, 5).Value = item.TienNhap;
                ws.Cell(currentRow, 5).Style.NumberFormat.Format = "#,##0";

                ws.Cell(currentRow, 6).Value = item.TongSoAoBanLe;
                ws.Cell(currentRow, 6).Style.NumberFormat.Format = "#,##0";
                ws.Cell(currentRow, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                ws.Cell(currentRow, 7).Value = item.TongTienBanLe;
                ws.Cell(currentRow, 7).Style.NumberFormat.Format = "#,##0";

                ws.Cell(currentRow, 8).Value = item.TienBanThua;
                ws.Cell(currentRow, 8).Style.NumberFormat.Format = "#,##0";

                ws.Cell(currentRow, 9).Value = item.TongThu;
                ws.Cell(currentRow, 9).Style.NumberFormat.Format = "#,##0";

                var cellLoiNhuan = ws.Cell(currentRow, 10);
                cellLoiNhuan.Value = item.LoiNhuan;
                cellLoiNhuan.Style.NumberFormat.Format = "+#,##0;-#,##0;0";
                cellLoiNhuan.Style.Font.Bold = true;
                cellLoiNhuan.Style.Font.FontColor = item.LoiNhuan >= 0 ? XLColor.FromHtml("#16A34A") : XLColor.FromHtml("#DC2626");

                var cellTyLe = ws.Cell(currentRow, 11);
                cellTyLe.Value = (item.TyLeLoiNhuan / 100.0);
                cellTyLe.Style.NumberFormat.Format = "+0.0%;-0.0%;0.0%";
                cellTyLe.Style.Font.Bold = true;
                cellTyLe.Style.Font.FontColor = item.LoiNhuan >= 0 ? XLColor.FromHtml("#16A34A") : XLColor.FromHtml("#DC2626");
                cellTyLe.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                ws.Cell(currentRow, 12).Value = item.TrangThai;
                ws.Cell(currentRow, 12).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                ws.Cell(currentRow, 13).Value = item.GhiChu ?? string.Empty;

                // Màu xen kẽ dòng chẵn lẻ
                if (stt % 2 == 0)
                {
                    ws.Range(currentRow, 1, currentRow, 13).Style.Fill.BackgroundColor = XLColor.FromHtml("#F8FAFC");
                }

                // Kẻ khung từng ô
                for (int col = 1; col <= 13; col++)
                {
                    ws.Cell(currentRow, col).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    ws.Cell(currentRow, col).Style.Border.OutsideBorderColor = XLColor.FromHtml("#E2E8F0");
                }

                ws.Row(currentRow).Height = 22;
                currentRow++;
                stt++;
            }

            // DÒNG TỔNG CỘNG CUỐI BẢNG
            var totalRange = ws.Range(currentRow, 1, currentRow, 4);
            totalRange.Merge();
            totalRange.Value = "TỔNG CỘNG";
            totalRange.Style.Font.Bold = true;
            totalRange.Style.Font.FontSize = 10;
            totalRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Cell(currentRow, 5).Value = tongVon;
            ws.Cell(currentRow, 5).Style.NumberFormat.Format = "#,##0";
            ws.Cell(currentRow, 5).Style.Font.Bold = true;
            ws.Cell(currentRow, 5).Style.Font.FontColor = XLColor.FromHtml("#DC2626");

            ws.Cell(currentRow, 6).Value = tongSoAo;
            ws.Cell(currentRow, 6).Style.NumberFormat.Format = "#,##0";
            ws.Cell(currentRow, 6).Style.Font.Bold = true;
            ws.Cell(currentRow, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

            ws.Cell(currentRow, 7).Value = tongBanLe;
            ws.Cell(currentRow, 7).Style.NumberFormat.Format = "#,##0";
            ws.Cell(currentRow, 7).Style.Font.Bold = true;
            ws.Cell(currentRow, 7).Style.Font.FontColor = XLColor.FromHtml("#059669");

            ws.Cell(currentRow, 8).Value = tongBanThua;
            ws.Cell(currentRow, 8).Style.NumberFormat.Format = "#,##0";
            ws.Cell(currentRow, 8).Style.Font.Bold = true;
            ws.Cell(currentRow, 8).Style.Font.FontColor = XLColor.FromHtml("#D97706");

            ws.Cell(currentRow, 9).Value = tongDoanhThu;
            ws.Cell(currentRow, 9).Style.NumberFormat.Format = "#,##0";
            ws.Cell(currentRow, 9).Style.Font.Bold = true;

            var cellTongLai = ws.Cell(currentRow, 10);
            cellTongLai.Value = tongLoiNhuan;
            cellTongLai.Style.NumberFormat.Format = "+#,##0;-#,##0;0";
            cellTongLai.Style.Font.Bold = true;
            cellTongLai.Style.Font.FontColor = tongLoiNhuan >= 0 ? XLColor.FromHtml("#16A34A") : XLColor.FromHtml("#DC2626");

            var cellTongTyLe = ws.Cell(currentRow, 11);
            double tyLeChung = tongVon > 0 ? (double)(tongLoiNhuan / tongVon) : 0;
            cellTongTyLe.Value = tyLeChung;
            cellTongTyLe.Style.NumberFormat.Format = "+0.0%;-0.0%;0.0%";
            cellTongTyLe.Style.Font.Bold = true;
            cellTongTyLe.Style.Font.FontColor = tongLoiNhuan >= 0 ? XLColor.FromHtml("#16A34A") : XLColor.FromHtml("#DC2626");
            cellTongTyLe.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

            ws.Cell(currentRow, 12).Value = "";
            ws.Cell(currentRow, 13).Value = "";

            var sumRowRange = ws.Range(currentRow, 1, currentRow, 13);
            sumRowRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#FEF3C7"); // Màu vàng kem nổi bật
            for (int col = 1; col <= 13; col++)
            {
                ws.Cell(currentRow, col).Style.Border.TopBorder = XLBorderStyleValues.Thin;
                ws.Cell(currentRow, col).Style.Border.BottomBorder = XLBorderStyleValues.Double;
                ws.Cell(currentRow, col).Style.Border.TopBorderColor = XLColor.FromHtml("#92400E");
                ws.Cell(currentRow, col).Style.Border.BottomBorderColor = XLColor.FromHtml("#92400E");
            }
            ws.Row(currentRow).Height = 25;

            // 4. CĂN CHỈNH ĐỘ RỘNG CỘT TỰ ĐỘNG
            ws.Columns().AdjustToContents();
            // Đảm bảo không cột nào quá bé
            for (int col = 1; col <= 13; col++)
            {
                if (ws.Column(col).Width < 12)
                {
                    ws.Column(col).Width = 12;
                }
            }

            workbook.SaveAs(filePath);
        }
    }
}

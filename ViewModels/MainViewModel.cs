using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BanHangChuNgoc.Models;
using BanHangChuNgoc.Services;
using BanHangChuNgoc;

namespace BanHangChuNgoc.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly DatabaseService _dbService;
        private readonly ExcelExportService _excelExportService;

        public MainViewModel()
        {
            _dbService = new DatabaseService();
            _excelExportService = new ExcelExportService();

            // Khởi tạo danh sách Tháng
            DanhSachThang.Add("Tất cả");
            for (int m = 1; m <= 12; m++)
            {
                DanhSachThang.Add($"Tháng {m}");
            }
            SelectedTuThang = "Tất cả";
            SelectedDenThang = "Tất cả";

            // Khởi tạo danh sách Năm
            DanhSachNam.Add("Tất cả");
            int currentYear = DateTime.Now.Year;
            for (int y = 2024; y <= Math.Max(currentYear + 2, 2030); y++)
            {
                DanhSachNam.Add(y.ToString());
            }
            SelectedNam = "Tất cả";

            _ = KhoiTaoAsync();
        }

        private async Task KhoiTaoAsync()
        {
            await _dbService.EnsureDatabaseCreatedAsync();
            await TaiDanhSachBaoHangAsync();
            await TaiBaoCaoThongKeAsync();
        }

        // ==========================================
        // DANH SÁCH & BAO HÀNG ĐANG CHỌN
        // ==========================================

        [ObservableProperty]
        private ObservableCollection<BaoHang> _danhSachBaoHang = new();

        [ObservableProperty]
        private BaoHang? _selectedBaoHang;

        async partial void OnSelectedBaoHangChanged(BaoHang? value)
        {
            if (value != null)
            {
                await TaiDanhSachBanLeAsync(value.MaBao);
                CapNhatThongTinTienDoBao(value);
            }
            else
            {
                DanhSachBanLe.Clear();
                ResetThongTinTienDo();
            }
        }

        [ObservableProperty]
        private ObservableCollection<BanLe> _danhSachBanLe = new();

        // ==========================================
        // ĐIỂM HÒA VỐN & LÃI HƠN BAO NHIÊU
        // (Tất cả tiền tệ đều theo đơn vị: k / nghìn đồng)
        // ==========================================

        [ObservableProperty]
        private int _currentTongAoBanLe;

        [ObservableProperty]
        private decimal _currentTongTienBanLe;

        // Chỉ số hòa vốn
        [ObservableProperty]
        private double _currentHoaVonPhanTram; // Ví dụ: 68.5% hoặc 124.0%

        [ObservableProperty]
        private double _currentHoaVonProgressBarValue; // 0 - 100

        [ObservableProperty]
        private bool _currentDaHoaVon;

        [ObservableProperty]
        private decimal _currentConThieuDeHoaVon; // Đơn vị: k

        // Chỉ số LÃI HƠN BAO NHIÊU khi đã qua điểm hòa vốn
        [ObservableProperty]
        private decimal _currentTienLaiHonVon; // Đơn vị: k

        [ObservableProperty]
        private double _currentTyLeLaiHonVon; // Tỷ lệ % lãi hơn vốn

        [ObservableProperty]
        private string _currentThongBaoTrangThaiHoaVon = string.Empty;

        [ObservableProperty]
        private string _currentChiTietLaiLo = string.Empty;

        private void CapNhatThongTinTienDoBao(BaoHang bao)
        {
            CurrentTongAoBanLe = DanhSachBanLe.Sum(x => x.SoLuong);
            CurrentTongTienBanLe = DanhSachBanLe.Sum(x => x.ThanhTien);
            
            decimal tongThuHienTai = CurrentTongTienBanLe + bao.TienBanThua;
            decimal chenhLech = tongThuHienTai - bao.TienNhap;

            // Tính % tiến độ thu hồi vốn
            if (bao.TienNhap > 0)
            {
                CurrentHoaVonPhanTram = Math.Round((double)(tongThuHienTai / bao.TienNhap) * 100.0, 1);
            }
            else
            {
                CurrentHoaVonPhanTram = 100.0;
            }

            // Thanh Progress Bar (0 đến 100)
            CurrentHoaVonProgressBarValue = Math.Min(100.0, Math.Max(0.0, CurrentHoaVonPhanTram));

            if (chenhLech >= 0)
            {
                // ĐÃ HÒA VỐN VÀ ĐANG LÃI
                CurrentDaHoaVon = true;
                CurrentConThieuDeHoaVon = 0;
                CurrentTienLaiHonVon = chenhLech;
                CurrentTyLeLaiHonVon = bao.TienNhap > 0 ? Math.Round((double)(chenhLech / bao.TienNhap) * 100.0, 1) : 0;

                CurrentThongBaoTrangThaiHoaVon = $"🎉 ĐÃ HÒA VỐN! (Đạt {CurrentHoaVonPhanTram}% vốn)";
                CurrentChiTietLaiLo = $"LÃI HƠN TIỀN VỐN: +{CurrentTienLaiHonVon:N0} k (+{CurrentTyLeLaiHonVon:F1}%)";
            }
            else
            {
                // CHƯA HÒA VỐN
                CurrentDaHoaVon = false;
                CurrentConThieuDeHoaVon = Math.Abs(chenhLech);
                CurrentTienLaiHonVon = 0;
                CurrentTyLeLaiHonVon = 0;

                CurrentThongBaoTrangThaiHoaVon = $"⏳ CHƯA HÒA VỐN (Đã thu {CurrentHoaVonPhanTram}% vốn)";
                CurrentChiTietLaiLo = $"CÒN THIẾU {CurrentConThieuDeHoaVon:N0} k NỮA LÀ HÒA VỐN";
            }
        }

        private void ResetThongTinTienDo()
        {
            CurrentTongAoBanLe = 0;
            CurrentTongTienBanLe = 0;
            CurrentHoaVonPhanTram = 0;
            CurrentHoaVonProgressBarValue = 0;
            CurrentDaHoaVon = false;
            CurrentConThieuDeHoaVon = 0;
            CurrentTienLaiHonVon = 0;
            CurrentTyLeLaiHonVon = 0;
            CurrentThongBaoTrangThaiHoaVon = "Chưa có bao hàng nào được chọn";
            CurrentChiTietLaiLo = "Vui lòng nhập bao mới ở cột bên trái để bắt đầu";
        }

        // ==========================================
        // FORM: THÊM BAO HÀNG MỚI
        // ==========================================

        [ObservableProperty]
        private string _newTenBao = string.Empty;

        [ObservableProperty]
        private string _newTienNhapText = string.Empty;

        [ObservableProperty]
        private DateTime _newNgayNhap = DateTime.Now;

        [ObservableProperty]
        private string? _newGhiChu;

        [RelayCommand]
        private async Task ThemBaoHangAsync()
        {
            if (string.IsNullOrWhiteSpace(NewTenBao))
            {
                MessageBox.Show("Vui lòng nhập tên/loại hàng cho bao mới!", "Thiếu thông tin", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!decimal.TryParse(NewTienNhapText.Replace(",", "").Replace(".", "").Trim(), out decimal tienNhap) || tienNhap < 0)
            {
                MessageBox.Show("Vui lòng nhập số tiền vốn mua bao hợp lệ (VD: 4500 thay vì 4500000)!", "Giá tiền không đúng", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var bao = new BaoHang
            {
                TenBao = NewTenBao.Trim(),
                TienNhap = tienNhap,
                NgayNhap = NewNgayNhap,
                GhiChu = NewGhiChu?.Trim(),
                TrangThai = "Đang bán"
            };

            try
            {
                await _dbService.ThemBaoHangAsync(bao);
                MessageBox.Show($"Đã nhập thành công bao: {bao.TenBao} (Vốn: {bao.TienNhap:N0} k)", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);

                // Reset form
                NewTenBao = string.Empty;
                NewTienNhapText = string.Empty;
                NewGhiChu = string.Empty;
                NewNgayNhap = DateTime.Now;

                await TaiDanhSachBaoHangAsync();
                await TaiBaoCaoThongKeAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi lưu bao hàng: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ==========================================
        // FORM: BÁN LẺ ÁO TỪ BAO
        // ==========================================

        [ObservableProperty]
        private string _quickDonGiaText = string.Empty;

        [ObservableProperty]
        private int _quickSoLuong = 1;

        [ObservableProperty]
        private string? _quickGhiChu;

        [RelayCommand]
        private async Task ThemBanLeAsync()
        {
            if (SelectedBaoHang == null)
            {
                MessageBox.Show("Vui lòng chọn một bao hàng để bán!", "Chưa chọn bao", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!decimal.TryParse(QuickDonGiaText.Replace(",", "").Replace(".", "").Trim(), out decimal donGia) || donGia <= 0)
            {
                MessageBox.Show("Vui lòng nhập đơn giá chiếc áo (VD: 30 cho 30 nghìn, 50 cho 50 nghìn)!", "Giá không hợp lệ", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (QuickSoLuong <= 0)
            {
                QuickSoLuong = 1;
            }

            var banLe = new BanLe
            {
                MaBao = SelectedBaoHang.MaBao,
                NgayBan = DateTime.Now,
                SoLuong = QuickSoLuong,
                DonGia = donGia,
                GhiChu = QuickGhiChu?.Trim()
            };

            try
            {
                await _dbService.ThemBanLeAsync(banLe);
                
                // Reset form bán lẻ
                QuickDonGiaText = string.Empty;
                QuickSoLuong = 1;
                QuickGhiChu = string.Empty;

                await TaiDanhSachBanLeAsync(SelectedBaoHang.MaBao);
                CapNhatThongTinTienDoBao(SelectedBaoHang);
                await TaiBaoCaoThongKeAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi ghi nhận bán lẻ: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private async Task XoaBanLeAsync(BanLe banLe)
        {
            if (banLe == null || SelectedBaoHang == null) return;

            string thongTin = $"Lượt bán {banLe.SoLuong} cái - Đơn giá: {banLe.DonGia:N0} k ({banLe.NgayBanFormatted}) của bao '{SelectedBaoHang.TenBao}'";
            if (!XacNhanXoaWindow.XacNhan(thongTin))
            {
                return;
            }

            try
            {
                await _dbService.XoaBanLeAsync(banLe.MaBanLe);
                await TaiDanhSachBanLeAsync(SelectedBaoHang.MaBao);
                CapNhatThongTinTienDoBao(SelectedBaoHang);
                await TaiBaoCaoThongKeAsync();
                MessageBox.Show("Đã xóa lượt bán lẻ thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi xóa: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ==========================================
        // FORM: CHỐT SỔ & BÁN BAO THỪA
        // ==========================================

        [ObservableProperty]
        private string _chotTienBanThuaText = string.Empty;

        [ObservableProperty]
        private DateTime _chotNgayKetThuc = DateTime.Now;

        [RelayCommand]
        private async Task ChotBaoHangAsync()
        {
            if (SelectedBaoHang == null)
            {
                MessageBox.Show("Vui lòng chọn bao hàng cần chốt!", "Chưa chọn bao", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!decimal.TryParse(ChotTienBanThuaText.Replace(",", "").Replace(".", "").Trim(), out decimal tienBanThua) || tienBanThua < 0)
            {
                MessageBox.Show("Vui lòng nhập tiền bán bao thừa (VD: 800 cho 800 nghìn)!", "Giá không hợp lệ", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            decimal tongThu = CurrentTongTienBanLe + tienBanThua;
            decimal loiNhuan = tongThu - SelectedBaoHang.TienNhap;

            string chiTietLoi = loiNhuan >= 0 
                ? $"LÃI HƠN VỐN: +{loiNhuan:N0} k" 
                : $"CHƯA ĐỦ VỐN: {loiNhuan:N0} k";

            string thongBao = $"Xác nhận chốt bao: '{SelectedBaoHang.TenBao}'\n" +
                              $"- Vốn nhập: {SelectedBaoHang.TienNhap:N0} k\n" +
                              $"- Thu bán lẻ: {CurrentTongTienBanLe:N0} k ({CurrentTongAoBanLe} cái)\n" +
                              $"- Thu bán bao thừa: {tienBanThua:N0} k\n" +
                              $"- TỔNG THU: {tongThu:N0} k\n" +
                              $"- {chiTietLoi}\n\n" +
                              "Bạn có chắc chắn muốn chốt và kết thúc bao hàng này?";

            var confirm = MessageBox.Show(thongBao, "Xác nhận chốt sổ bao", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm == MessageBoxResult.Yes)
            {
                try
                {
                    await _dbService.ChotBaoHangAsync(SelectedBaoHang.MaBao, tienBanThua, ChotNgayKetThuc);
                    MessageBox.Show("Đã chốt sổ và chuyển bao hàng sang trạng thái 'Đã kết thúc'!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);

                    ChotTienBanThuaText = string.Empty;
                    await TaiDanhSachBaoHangAsync();
                    await TaiBaoCaoThongKeAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi chốt bao: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        [RelayCommand]
        private async Task XoaBaoHangAsync(object? param)
        {
            int maBao = 0;
            string tenBao = string.Empty;

            if (param is BaoHang bao)
            {
                maBao = bao.MaBao;
                tenBao = bao.TenBao;
            }
            else if (param is ThongKeBaoHang tk)
            {
                maBao = tk.MaBao;
                tenBao = tk.TenBao;
            }

            if (maBao == 0) return;

            string thongTin = $"Bao hàng '{tenBao}' (Mã #{maBao}) và toàn bộ dữ liệu bán lẻ liên quan";
            if (!XacNhanXoaWindow.XacNhan(thongTin))
            {
                return;
            }

            try
            {
                await _dbService.XoaBaoHangAsync(maBao);
                await TaiDanhSachBaoHangAsync();
                await TaiBaoCaoThongKeAsync();
                MessageBox.Show($"Đã xóa bao hàng '{tenBao}' thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi xóa: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ==========================================
        // BÁO CÁO THỐNG KÊ & DÒNG TIỀN
        // ==========================================

        [ObservableProperty]
        private ObservableCollection<ThongKeBaoHang> _danhSachThongKe = new();

        [ObservableProperty]
        private decimal _tongBaoCaoTienNhap;

        [ObservableProperty]
        private decimal _tongBaoCaoTienBanLe;

        [ObservableProperty]
        private decimal _tongBaoCaoTienBanThua;

        [ObservableProperty]
        private decimal _tongBaoCaoDoanhThu;

        [ObservableProperty]
        private decimal _tongBaoCaoLoiNhuan;

        [ObservableProperty]
        private int _tongBaoCaoAoBanLe;

        [ObservableProperty]
        private ObservableCollection<string> _danhSachThang = new();

        [ObservableProperty]
        private string _selectedTuThang = "Tất cả";

        [ObservableProperty]
        private string _selectedDenThang = "Tất cả";

        [ObservableProperty]
        private ObservableCollection<string> _danhSachNam = new();

        [ObservableProperty]
        private string _selectedNam = "Tất cả";

        [RelayCommand]
        private async Task LocBaoCaoAsync()
        {
            await TaiBaoCaoThongKeAsync();
        }

        [RelayCommand]
        private async Task XoaBoLocBaoCaoAsync()
        {
            SelectedTuThang = "Tất cả";
            SelectedDenThang = "Tất cả";
            SelectedNam = "Tất cả";
            await TaiBaoCaoThongKeAsync();
        }

        [RelayCommand]
        private void XuatExcel()
        {
            if (DanhSachThongKe == null || DanhSachThongKe.Count == 0)
            {
                MessageBox.Show("Hiện không có dữ liệu báo cáo nào để xuất file Excel!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                Title = "Xuất báo cáo thống kê doanh thu",
                Filter = "Tệp Excel (*.xlsx)|*.xlsx",
                FileName = "thống kê vè  doanh thu.xlsx"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    string thoiGianLoc = "";
                    if (SelectedTuThang == "Tất cả" && SelectedDenThang == "Tất cả")
                    {
                        thoiGianLoc = "Cả năm";
                    }
                    else if (SelectedTuThang == SelectedDenThang)
                    {
                        thoiGianLoc = SelectedTuThang;
                    }
                    else
                    {
                        thoiGianLoc = $"Từ {SelectedTuThang} đến {SelectedDenThang}";
                    }

                    if (SelectedNam != "Tất cả")
                    {
                        thoiGianLoc += $" - Năm {SelectedNam}";
                    }
                    else
                    {
                        thoiGianLoc += " - Tất cả các năm";
                    }

                    _excelExportService.ExportThongKeDoanhThu(
                        saveFileDialog.FileName,
                        DanhSachThongKe,
                        thoiGianLoc,
                        TongBaoCaoTienNhap,
                        TongBaoCaoTienBanLe,
                        TongBaoCaoTienBanThua,
                        TongBaoCaoDoanhThu,
                        TongBaoCaoLoiNhuan,
                        TongBaoCaoAoBanLe
                    );

                    var result = MessageBox.Show(
                        $"Xuất file Excel thành công!\n\nĐường dẫn: {saveFileDialog.FileName}\n\nBạn có muốn mở file ngay không?",
                        "Thành công",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Information);

                    if (result == MessageBoxResult.Yes)
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = saveFileDialog.FileName,
                            UseShellExecute = true
                        });
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi khi xuất file Excel: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        [RelayCommand]
        private async Task XoaDuLieuDaLocAsync()
        {
            if (DanhSachThongKe == null || DanhSachThongKe.Count == 0)
            {
                MessageBox.Show("Hiện không có dữ liệu báo cáo nào trong danh sách đã lọc để xóa!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            string thoiGianLoc = "";
            if (SelectedTuThang == "Tất cả" && SelectedDenThang == "Tất cả")
            {
                thoiGianLoc = "Cả năm";
            }
            else if (SelectedTuThang == SelectedDenThang)
            {
                thoiGianLoc = SelectedTuThang;
            }
            else
            {
                thoiGianLoc = $"Từ {SelectedTuThang} đến {SelectedDenThang}";
            }

            if (SelectedNam != "Tất cả")
            {
                thoiGianLoc += $" - Năm {SelectedNam}";
            }
            else
            {
                thoiGianLoc += " - Tất cả các năm";
            }

            string thongTinXoa = $"{DanhSachThongKe.Count} bao hàng và toàn bộ lượt bán lẻ liên quan [{thoiGianLoc}]";

            bool xacNhan = XacNhanXoaWindow.XacNhan(thongTinXoa, choPhepXuatExcel: true, out bool coXuatExcel);
            if (!xacNhan)
            {
                return;
            }

            // Nếu người dùng chọn lưu/xuất file Excel trước khi xóa
            if (coXuatExcel)
            {
                var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Title = "Lưu bản sao lưu trước khi xóa dữ liệu",
                    Filter = "Tệp Excel (*.xlsx)|*.xlsx",
                    FileName = "thống kê vè  doanh thu.xlsx"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    try
                    {
                        _excelExportService.ExportThongKeDoanhThu(
                            saveFileDialog.FileName,
                            DanhSachThongKe,
                            thoiGianLoc,
                            TongBaoCaoTienNhap,
                            TongBaoCaoTienBanLe,
                            TongBaoCaoTienBanThua,
                            TongBaoCaoDoanhThu,
                            TongBaoCaoLoiNhuan,
                            TongBaoCaoAoBanLe
                        );
                    }
                    catch (Exception ex)
                    {
                        var traLoi = MessageBox.Show(
                            $"Xuất file Excel trước khi xóa thất bại: {ex.Message}\n\nBạn có muốn tiếp tục xóa dữ liệu không?",
                            "Cảnh báo xuất Excel",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Warning);

                        if (traLoi != MessageBoxResult.Yes)
                        {
                            return;
                        }
                    }
                }
                else
                {
                    // Người dùng đã hủy hộp thoại lưu file Excel
                    var traLoi = MessageBox.Show(
                        "Bạn đã hủy lưu file Excel dự phòng. Bạn có chắc chắn vẫn muốn xóa dữ liệu này khỏi hệ thống không?",
                        "Xác nhận xóa",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (traLoi != MessageBoxResult.Yes)
                    {
                        return;
                    }
                }
            }

            // Thực hiện xóa dữ liệu trong database
            try
            {
                var maBaoList = DanhSachThongKe.Select(x => x.MaBao).ToList();
                await _dbService.XoaDanhSachBaoHangAsync(maBaoList);

                await TaiDanhSachBaoHangAsync();
                await TaiBaoCaoThongKeAsync();

                MessageBox.Show($"Đã xóa thành công {maBaoList.Count} bao hàng và các dữ liệu liên quan khỏi hệ thống!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi xóa dữ liệu: {ex.Message}", "Lỗi CSDL", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ==========================================
        // CÁC HÀM TẢI DỮ LIỆU
        // ==========================================

        public async Task TaiDanhSachBaoHangAsync()
        {
            try
            {
                var list = await _dbService.GetDanhSachBaoHangAsync("Đang bán");
                DanhSachBaoHang.Clear();
                foreach (var item in list)
                {
                    DanhSachBaoHang.Add(item);
                }

                if (SelectedBaoHang != null)
                {
                    var reselected = DanhSachBaoHang.FirstOrDefault(b => b.MaBao == SelectedBaoHang.MaBao);
                    SelectedBaoHang = reselected ?? DanhSachBaoHang.FirstOrDefault();
                }
                else
                {
                    SelectedBaoHang = DanhSachBaoHang.FirstOrDefault();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải danh sách bao: {ex.Message}", "Lỗi CSDL", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        public async Task TaiDanhSachBanLeAsync(int maBao)
        {
            try
            {
                var list = await _dbService.GetDanhSachBanLeAsync(maBao);
                DanhSachBanLe.Clear();
                foreach (var item in list)
                {
                    DanhSachBanLe.Add(item);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải lượt bán lẻ: {ex.Message}", "Lỗi CSDL", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        public async Task TaiBaoCaoThongKeAsync()
        {
            try
            {
                int? tuThang = null;
                if (!string.IsNullOrEmpty(SelectedTuThang) && SelectedTuThang != "Tất cả")
                {
                    string rawTuThang = SelectedTuThang.Replace("Tháng", "").Trim();
                    if (int.TryParse(rawTuThang, out int t))
                    {
                        tuThang = t;
                    }
                }

                int? denThang = null;
                if (!string.IsNullOrEmpty(SelectedDenThang) && SelectedDenThang != "Tất cả")
                {
                    string rawDenThang = SelectedDenThang.Replace("Tháng", "").Trim();
                    if (int.TryParse(rawDenThang, out int d))
                    {
                        denThang = d;
                    }
                }

                if (tuThang.HasValue && denThang.HasValue && tuThang.Value > denThang.Value)
                {
                    (tuThang, denThang) = (denThang, tuThang);
                }

                int? nam = null;
                if (!string.IsNullOrEmpty(SelectedNam) && SelectedNam != "Tất cả")
                {
                    if (int.TryParse(SelectedNam.Trim(), out int y))
                    {
                        nam = y;
                    }
                }

                var list = await _dbService.GetThongKeTatCaBaoAsync(tuThang, denThang, nam);
                DanhSachThongKe.Clear();
                foreach (var item in list)
                {
                    DanhSachThongKe.Add(item);
                }

                // Tổng vốn bỏ ra chỉ tính những bao vẫn còn trong trạng thái 'Đang bán'
                TongBaoCaoTienNhap = list.Where(x => x.TrangThai == "Đang bán").Sum(x => x.TienNhap);
                TongBaoCaoTienBanLe = list.Sum(x => x.TongTienBanLe);
                TongBaoCaoTienBanThua = list.Sum(x => x.TienBanThua);
                TongBaoCaoDoanhThu = list.Sum(x => x.TongThu);
                TongBaoCaoLoiNhuan = list.Sum(x => x.LoiNhuan);
                TongBaoCaoAoBanLe = list.Sum(x => x.TongSoAoBanLe);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải báo cáo: {ex.Message}", "Lỗi CSDL", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}

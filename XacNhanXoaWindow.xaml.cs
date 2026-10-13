using System.Windows;
using System.Windows.Input;
using BanHangChuNgoc.Services;

namespace BanHangChuNgoc;

/// <summary>
/// Cửa sổ xác nhận tài khoản & mật khẩu khi thực hiện xóa dữ liệu
/// </summary>
public partial class XacNhanXoaWindow : Window
{
    public bool CoXuatExcel => chkXuatExcel.IsChecked == true;

    public XacNhanXoaWindow(string thongTinXoa, bool choPhepXuatExcel = false)
    {
        InitializeComponent();
        lblThongTinXoa.Text = string.IsNullOrWhiteSpace(thongTinXoa) 
            ? "Mục được chọn" 
            : thongTinXoa;

        if (choPhepXuatExcel)
        {
            borderTuyChonExcel.Visibility = Visibility.Visible;
            chkXuatExcel.IsChecked = true;
        }
        else
        {
            borderTuyChonExcel.Visibility = Visibility.Collapsed;
        }

        Loaded += XacNhanXoaWindow_Loaded;
    }

    private void XacNhanXoaWindow_Loaded(object sender, RoutedEventArgs e)
    {
        txtTaiKhoan.Focus();
    }

    private void TxtTaiKhoan_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            txtMatKhau.Focus();
        }
    }

    private void TxtMatKhau_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            ThucHienXacNhan();
        }
    }

    private void BtnXacNhan_Click(object sender, RoutedEventArgs e)
    {
        ThucHienXacNhan();
    }

    private void ThucHienXacNhan()
    {
        string taiKhoan = txtTaiKhoan.Text.Trim();
        string matKhau = txtMatKhau.Password;

        if (string.IsNullOrEmpty(taiKhoan))
        {
            HienThiLoi("Vui lòng nhập tài khoản!");
            txtTaiKhoan.Focus();
            return;
        }

        if (string.IsNullOrEmpty(matKhau))
        {
            HienThiLoi("Vui lòng nhập mật khẩu!");
            txtMatKhau.Focus();
            return;
        }

        bool hopLe = AuthService.KiemTraDangNhap(taiKhoan, matKhau);
        if (hopLe)
        {
            DialogResult = true;
            Close();
        }
        else
        {
            HienThiLoi("Tài khoản hoặc mật khẩu không chính xác!");
            txtMatKhau.SelectAll();
            txtMatKhau.Focus();
        }
    }

    private void HienThiLoi(string thongBao)
    {
        lblThongBaoLoi.Text = thongBao;
        borderLoi.Visibility = Visibility.Visible;
    }

    private void BtnHuy_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    /// <summary>
    /// Hàm tiện ích gọi nhanh hộp thoại xác nhận xóa (không có tùy chọn Excel)
    /// </summary>
    public static bool XacNhan(string thongTinXoa)
    {
        return XacNhan(thongTinXoa, false, out _);
    }

    /// <summary>
    /// Hàm tiện ích gọi nhanh hộp thoại xác nhận xóa có kèm tùy chọn xuất Excel
    /// </summary>
    public static bool XacNhan(string thongTinXoa, bool choPhepXuatExcel, out bool xuatExcel)
    {
        var dialog = new XacNhanXoaWindow(thongTinXoa, choPhepXuatExcel);
        if (Application.Current?.MainWindow != null && Application.Current.MainWindow.IsVisible)
        {
            dialog.Owner = Application.Current.MainWindow;
        }
        bool result = dialog.ShowDialog() == true;
        xuatExcel = dialog.CoXuatExcel;
        return result;
    }
}

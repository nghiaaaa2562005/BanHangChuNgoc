using System.Windows;
using System.Windows.Input;
using BanHangChuNgoc.Services;

namespace BanHangChuNgoc;

/// <summary>
/// Interaction logic for LoginWindow.xaml
/// </summary>
public partial class LoginWindow : Window
{
    public LoginWindow()
    {
        InitializeComponent();
        Loaded += LoginWindow_Loaded;
    }

    private void LoginWindow_Loaded(object sender, RoutedEventArgs e)
    {
        txtTaiKhoan.Focus();
    }

    private void BtnDangNhap_Click(object sender, RoutedEventArgs e)
    {
        ThucHienDangNhap();
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
            ThucHienDangNhap();
        }
    }

    private void ThucHienDangNhap()
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
            borderLoi.Visibility = Visibility.Collapsed;

            // Đăng nhập thành công -> Mở giao diện chính và đóng trang đăng nhập
            var mainWindow = new MainWindow();
            Application.Current.MainWindow = mainWindow;
            mainWindow.Show();
            this.Close();
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

    private void BtnThoat_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }
}

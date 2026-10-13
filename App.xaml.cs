using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace BanHangChuNgoc;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Khởi tạo cơ sở dữ liệu SQLite ngay khi mở app
        _ = Task.Run(async () =>
        {
            var db = new Services.DatabaseService();
            await db.EnsureDatabaseCreatedAsync();
        });

        // Tự động bôi đen tất cả ký tự khi click chuột hoặc focus vào bất kỳ ô TextBox nào
        EventManager.RegisterClassHandler(
            typeof(TextBox),
            UIElement.GotKeyboardFocusEvent,
            new KeyboardFocusChangedEventHandler(OnTextBoxGotKeyboardFocus));

        EventManager.RegisterClassHandler(
            typeof(TextBox),
            UIElement.PreviewMouseLeftButtonDownEvent,
            new MouseButtonEventHandler(OnTextBoxPreviewMouseLeftButtonDown));
    }

    private static void OnTextBoxGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (sender is TextBox textBox)
        {
            textBox.SelectAll();
        }
    }

    private static void OnTextBoxPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is TextBox textBox && !textBox.IsKeyboardFocusWithin)
        {
            e.Handled = true;
            textBox.Focus();
        }
    }
}

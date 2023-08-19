using System.Windows;
using System.Windows.Input;

namespace WpfApp2;

public class BaseFormBehavior : Window
{
    protected void WindowMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    public void btnClose_Click(object sender, RoutedEventArgs e)
    {
        this.Hide();
        //Application.Current.Shutdown();
    }

    protected void btnMinimize_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }
}
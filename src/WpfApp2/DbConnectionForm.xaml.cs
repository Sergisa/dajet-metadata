using System.Data;
using System.Drawing;
using System.Windows;
using System.Windows.Media;
using DaJet.Metadata;
using Microsoft.Data.SqlClient;

namespace WpfApp2;

public partial class DbConnectionForm : BaseFormBehavior
{
    public event EventHandler<string> OnSaved;

    public DbConnectionForm()
    {
        InitializeComponent();
    }

    private void TestConnectAction(object sender, RoutedEventArgs e)
    {
        StateText.Text = "";
        ErrorTextBox.Document.Blocks.Clear();
        string hostName = Address.Text;
        string login = UserName.Text;
        string pass = Password.Text;
        string connectionString = BuildConnectionString(hostName, "master", login, pass);
        SqlConnection connection = new SqlConnection(connectionString);
        connection.StateChange += connectionStateChanged;
        Task connectionTask = connection.OpenAsync();
        while (connectionTask.Status == TaskStatus.Running)
        {
            StateText.Text = "Пытаюсь";
            StateText.Foreground = new SolidColorBrush(Colors.Orange);
        }
    }

    private void SaveAction(object sender, RoutedEventArgs e)
    {
        string hostName = Address.Text;
        string login = UserName.Text;
        string pass = Password.Text;
        string connectionString = BuildConnectionString(hostName, "master", login, pass);
        OnSaved.Invoke(this, connectionString);
        this.Close();
    }

    private void connectionStateChanged(object sender, StateChangeEventArgs e)
    {
        if (e.CurrentState == ConnectionState.Broken || e.CurrentState == ConnectionState.Closed)
        {
            StateText.Text = "Не удалось подключиться";
            StateText.Foreground = new SolidColorBrush(Colors.Red);
        }
        else if (e.CurrentState == ConnectionState.Open)
        {
            StateText.Text = "Успешно";
            StateText.Foreground = new SolidColorBrush(Colors.GreenYellow);
        }
        else if (e.CurrentState == ConnectionState.Connecting || e.CurrentState == ConnectionState.Executing ||
                 e.CurrentState == ConnectionState.Fetching)
        {
            StateText.Text = "Пытаюсь";
            StateText.Foreground = new SolidColorBrush(Colors.Orange);
        }
    }

    private static string BuildConnectionString(string server, string database, string userName, string password)
    {
        SqlConnectionStringBuilder connectionString = new()
        {
            DataSource = server,
            InitialCatalog = database
        };
        if (!string.IsNullOrWhiteSpace(userName))
        {
            connectionString.UserID = userName;
            connectionString.Password = password;
        }

        connectionString.IntegratedSecurity = string.IsNullOrWhiteSpace(userName);
        connectionString.Encrypt = false;

        return connectionString.ToString();
    }
}
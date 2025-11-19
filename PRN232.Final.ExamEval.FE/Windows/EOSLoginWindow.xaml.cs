using PRN232.Final.ExamEval.FE.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace PRN232.Final.ExamEval.FE.Windows
{
    /// <summary>
    /// Interaction logic for EOSLoginWindow.xaml
    /// </summary>
    public partial class EOSLoginWindow : Window
    {
        private readonly AuthService _auth = new AuthService();

        public EOSLoginWindow()
        {
            InitializeComponent();
        }

        private async void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            string email = txtEmail.Text.Trim();
            string password = txtPassword.Password.Trim();

            string? token = await _auth.LoginAsync(email, password);

            if (token == null)
            {
                MessageBox.Show("Email hoặc mật khẩu không đúng!",
                                "Login Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            Application.Current.Properties["jwt"] = token;

            string? userJson = await _auth.GetCurrentUserAsync(token);
            Application.Current.Properties["currentUser"] = userJson;

            MessageBox.Show("Đăng nhập thành công!", "Success");

            var main = new MainWindow();
            main.Show();

            this.Close();
        }

        private void BtnExit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}

using System.Windows;

namespace Client.View.Window
{
    public partial class CustomMessageBox : System.Windows.Window
    {
        public CustomMessageBox()
        {
            InitializeComponent();
        }

        MessageBoxResult Result = MessageBoxResult.None;

        public static MessageBoxResult Show(string caption, string message)
        {
            var dialog = new CustomMessageBox();
            dialog.Title.Content = caption;
            dialog.Message.Text = message;
            dialog.ShowDialog();
            return dialog.Result;
        }

        private void CloseWindow(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}

using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace Client.View.Window
{
    public partial class AuthView : System.Windows.Window
    {
        public bool Authorization
        {
            get => (bool)GetValue(AuthorizationProperty);
            set => SetValue(AuthorizationProperty, value);
        }

        public static readonly DependencyProperty AuthorizationProperty =
            DependencyProperty.Register(
                nameof(Authorization),
                typeof(bool),
                typeof(AuthView),
                new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnConnect));

        private static void OnConnect(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue != null)
            {
                if (d is AuthView window) 
                    window.DialogResult = (bool)e.NewValue;
            }
        }

        public AuthView()
        {
            InitializeComponent();
            SetBinding(AuthorizationProperty, new Binding(path: "IsConnected"));
        }

        private void CloseWindow(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void HideWindow(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void LoginWindow_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                DragMove();
            }
        }
    }
}

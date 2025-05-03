using System.Windows.Controls;

namespace Client.View.UserControls
{
    public partial class QuikControl : UserControl
    {
        public QuikControl()
        {
            InitializeComponent();

#if DEBUG
            TestButton.Visibility = System.Windows.Visibility.Visible;
#else
            TestButton.Visibility = System.Windows.Visibility.Collapsed;
#endif
        }
    }
}

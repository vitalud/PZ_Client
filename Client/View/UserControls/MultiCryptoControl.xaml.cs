using System.Windows.Controls;

namespace Client.View.UserControls
{
    public partial class MultiCryptoControl : UserControl
    {
        public MultiCryptoControl()
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

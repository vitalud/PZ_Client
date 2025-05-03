using System;
using System.Collections;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Client.View.UserControls
{
    /// <summary>
    /// Логика взаимодействия для SubscriptionsControl.xaml
    /// </summary>
    public partial class SubscriptionsControl : UserControl
    {
        public SubscriptionsControl()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty SubscriptionsProperty =
        DependencyProperty.Register("Subscriptions", typeof(IEnumerable), typeof(SubscriptionsControl), new PropertyMetadata(null));

        public IEnumerable Subscriptions
        {
            get { return (IEnumerable)GetValue(SubscriptionsProperty); }
            set { SetValue(SubscriptionsProperty, value); }
        }
    }
}

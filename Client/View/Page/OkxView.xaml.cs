using Client.Service;
using Client.View.Window;
using ReactiveUI;

namespace Client.View.Page
{
    public partial class OkxView : System.Windows.Controls.Page
    {
        public OkxView()
        {
            InitializeComponent();

            MessageBus.Current.Listen<MessageToShow>().Subscribe(message =>
            {
                CustomMessageBox.Show("Ошибка", message.Content);
            });
        }
    }
}

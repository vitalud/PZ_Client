using Client.Service;
using Client.Service.Abstract;

namespace Client.Model
{
    public class MainModel
    {
        private readonly Connector _connector;
        private readonly BursesModel _burses;
        private readonly SubscriptionsService _subscriptions;

        public MainModel(Connector connector, BursesModel burses, SubscriptionsService subscriptions)
        {
            _connector = connector;
            _burses = burses;
            _subscriptions = subscriptions;
        }

        public void CloseApplication()
        {
            _connector.Close();
        }
    }
}

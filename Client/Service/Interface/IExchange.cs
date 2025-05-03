using Client.Service.Sub;
using DynamicData;
using ProjectZeroLib.Enums;

namespace Client.Service.Interface
{
    public interface IExchange
    {
        BurseName Name { get; }
        decimal Balance { get; set; }
        bool IsConnected { get; set; }
        IObservableList<Subscription> Subscriptions { get; }
        void Connect();
        void Test();
    }
}

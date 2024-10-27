using Client.Service.Abstract;
using Newtonsoft.Json;
using ProjectZeroLib;
using ReactiveUI;
using System.Net.Sockets;
using System.Reactive.Linq;
using System.Text;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace Client.Service
{
    public class TcpConnector(SubscriptionsService strategies) : Connector(strategies)
    {
        private TcpClient _auth;
        private TcpClient _data;
        private NetworkStream _stream;

        public override async Task<bool> Authorization()
        {
            var result = false;
            _auth = new TcpClient(address, authPort);
            if (_auth.Connected)
            {
                _stream = _auth.GetStream();
                if (_stream.CanRead)
                {
                    var data = $"{ConfigService.GetLogin()}_{ConfigService.GetPassword()}";
                    await SendData(_stream, data);
                    var message = await TcpService.ReadMessageAsync(_stream);
                    if (message != null) 
                    {
                        var recieved = message.Split('_');
                        if (recieved[0].Equals("auth") && recieved[1].Equals("success"))
                        {
                            if (recieved[1].Equals("success"))
                            {
                                result = true;
                                await DataExchange();
                            }
                        }
                    }
                }
            }
            return result;
        }
        protected override async Task DataExchange()
        {
            _auth.Dispose();
            _stream.Dispose();
            _data = new TcpClient(address, dataPort);
            if (_data.Connected)
            {
                _stream = _data.GetStream();
                if (_stream.CanRead)
                {
                    Task.Factory.StartNew(ReceiveData);
                    await SendData(_stream, "strategy");
                }
            }
        }
        protected override async Task ReceiveData()
        {
            while (true)
            {
                if (_stream != null)
                {
                    var message = await TcpService.ReadMessageAsync(_stream);
                    if (message != null)
                        OnMessageReceived(message);
                }
                await Task.Delay(10);
            }
        }
        protected override async void MessageHandler(string message)
        {
            string[] data = message.Split('_');
            if (data[0].Equals("strategy"))
                GetStrategy(data[1]);
            else if (data[0].Equals("signal"))
                _subscriptions.GetSignal(data[1]);
        }
        private async Task SendData(NetworkStream stream, string data)
        {
            if (stream != null)
            {
                string message = Converter.CreateMessage($"{data}_{sessionId}");
                var bytes = Encoding.UTF8.GetBytes(message);
                await stream.WriteAsync(bytes);
            }
        }
        private void GetStrategy(string data)
        {
            var sub = JsonConvert.DeserializeObject<Subscription>(data);
            if (sub != null)
            {
                sub.WhenAnyValue(x => x.IsActive)
                    .Skip(1)
                    .Subscribe(_ => OnStrategyStatusChanged(sub.Code, sub.IsActive));

                _subscriptions.GetStrategy(sub);
            }
        }
        protected override async void OnStrategyStatusChanged(string code, bool status)
        {
            var message = $"status_{code}_{status}";
            await SendData(_stream, message);
        }
        public override async Task Close()
        {
            await SendDisconnectSignal(); 
        }
        public async Task SendDisconnectSignal()
        {
            await SendData(_stream, "disconnect");
            //_stream.Dispose();
            //_data.Dispose();
        }
    }
}

using ProjectZeroLib.Enums;
using System.IO;
using System.Text;

namespace Client.Service
{
    /// <summary>
    /// Класс, представляющий краткий ответ от биржи.
    /// </summary>
    public class OrderUpdateResponse
    {
        private readonly BurseName _burse;
        private readonly DateTime _updateTime;
        private readonly string _clientOrderId;
        private readonly decimal _price;
        private readonly OrderState _state;
        private readonly decimal _fee;
        private readonly decimal _pnl;
        private readonly decimal _balance;

        public BurseName Burse => _burse;
        public DateTime UpdateTime => _updateTime;
        public string ClientOrderId => _clientOrderId;
        public decimal Price => _price;
        public OrderState State => _state;
        public decimal Fee => _fee;
        public decimal Pnl => _pnl;
        public decimal Balance => _balance;

        public OrderUpdateResponse(BurseName burse, DateTime updateTime, string clientOrderId, decimal price, OrderState state, decimal fee, decimal pnl, decimal balance)
        {
            _burse = burse;
            _updateTime = updateTime;
            _clientOrderId = clientOrderId;
            _price = price;
            _state = state;
            _fee = fee;
            _pnl = pnl;
            _balance = balance;

            LogResponse();
        }


        /// <summary>
        /// Логирует ответ от биржи. 
        /// </summary>
        private void LogResponse()
        {
            var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var burse = Burse.ToString();
            var strat = string.Concat(ClientOrderId.AsSpan(2, 4), ".txt");

            var path = Path.Combine(documents, "PZ", "logs", burse);

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            using StreamWriter sw = new(Path.Combine(path, strat), true);
            var sb = new StringBuilder(
                $"Update time: {UpdateTime} " +
                $"Order status: {State} " +
                $"Current balance: {Balance} " +
                $"Fee: {Fee} " +
                $"PnL: {Pnl}");

            sw.WriteLine(sb);
        }
    }
}



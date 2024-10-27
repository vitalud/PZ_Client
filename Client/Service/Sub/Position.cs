using ReactiveUI;

namespace Client.Service.Sub
{
    public class Position : ReactiveObject
    {
        private long? _tradeId;
        private decimal _price;
        private decimal _quantity;
        private string _instrumentId = string.Empty;
        private string _side = string.Empty;
        private decimal _profit;

        public long? TradeId
        {
            get => _tradeId;
            set => this.RaiseAndSetIfChanged(ref _tradeId, value);
        }
        public decimal Price
        {
            get => _price;
            set => this.RaiseAndSetIfChanged(ref _price, value);
        }
        public decimal Quantity
        {
            get => _quantity;
            set => this.RaiseAndSetIfChanged(ref _quantity, value);
        }
        public string InstrumentId
        {
            get => _instrumentId;
            set => this.RaiseAndSetIfChanged(ref _instrumentId, value);
        }
        public string Side
        {
            get => _side;
            set => this.RaiseAndSetIfChanged(ref _side, value);
        }
        public decimal Profit
        {
            get => _profit;
            set => this.RaiseAndSetIfChanged(ref _profit, value);
        }
    }
}

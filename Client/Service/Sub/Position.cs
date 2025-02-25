using ProjectZeroLib.Enums;
using ReactiveUI;

namespace Client.Service.Sub
{
    /// <summary>
    /// Класс, описывающий информацию по позиции в подписке.
    /// TODO: исправить на самостоятельный расчет.
    /// </summary>
    public partial class Position(string id, string type, BurseName burse, decimal size, Side side, string clientOrderId) : ReactiveObject
    {
        private decimal _price;
        private decimal _profit;

        public string Id { get; } = id;
        public string Type { get; } = type;
        public BurseName Burse { get; } = burse;
        public decimal Size { get; } = size;
        public Side Side { get; } = side;
        public string ClientOrderId { get; } = clientOrderId;

        public decimal Price
        {
            get => _price;
            set => this.RaiseAndSetIfChanged(ref _price, value);
        }

        public decimal Profit
        {
            get => _profit;
            set => this.RaiseAndSetIfChanged(ref _profit, value);
        }
    }
}

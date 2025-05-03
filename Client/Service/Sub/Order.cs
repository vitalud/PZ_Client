using ProjectZeroLib.Enums;
using ReactiveUI;

namespace Client.Service.Sub
{
    /// <summary>
    /// Класс, описывающий информацию по ордеру в подписке.
    /// Совмещает функцию отображения сделок в подписке.
    /// </summary>
    public partial class Order(string id, string type, BurseName burse, string clientOrderId, Side side, decimal priceStep) : ReactiveObject
    {
        private string _date = string.Empty;
        private string _status = string.Empty;
        private decimal _price;
        private decimal _size;

        /// <summary>
        /// Имя инструмента.
        /// </summary>
        public string Id { get; set; } = id;

        /// <summary>
        /// Тип инструмента.
        /// </summary>
        public string Type { get; } = type;

        /// <summary>
        /// Биржа инструмента.
        /// </summary>
        public BurseName Burse { get; } = burse;

        /// <summary>
        /// Id ордера на бирже, создаваемый стратегией.
        /// </summary>
        public string ClientOrderId { get; set; } = clientOrderId;

        /// <summary>
        /// Id ордера на бирже, создаваемый биржей.
        /// </summary>
        public int OrderId { get; set; }

        /// <summary>
        /// Сторона ордера (купля или продажа).
        /// </summary>
        public Side Side { get; } = side;

        /// <summary>
        /// Шаг цены.
        /// </summary>
        public decimal PriceStep { get; } = priceStep;

        /// <summary>
        /// Время размещения/исполнения ордера.
        /// </summary>
        public string Date
        {
            get => _date;
            set => this.RaiseAndSetIfChanged(ref _date, value);
        }

        /// <summary>
        /// Статус ордера на бирже.
        /// </summary>
        public string Status
        {
            get => _status;
            set => this.RaiseAndSetIfChanged(ref _status, value);
        }

        /// <summary>
        /// Цена инструмента по ордеру.
        /// </summary>
        public decimal Price
        {
            get => _price;
            set => this.RaiseAndSetIfChanged(ref _price, value);
        }

        /// <summary>
        /// Количество инструмента в ордере.
        /// </summary>
        public decimal Size
        {
            get => _size;
            set => this.RaiseAndSetIfChanged(ref _size, value);
        }
    }
}

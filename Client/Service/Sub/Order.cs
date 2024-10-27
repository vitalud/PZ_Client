using ReactiveUI;

namespace Client.Service.Sub
{
    /// <summary>
    /// Класс информации по ордеру конкретной стратегии.
    /// </summary>
    public class Order : ReactiveObject
    {
        private string _title = string.Empty;
        private string _date = string.Empty;
        private string _clientOrderId = string.Empty;
        private long? _orderId;
        private long? _tradeId;
        private string _status = string.Empty;
        private string _instrumentType = string.Empty;
        private string _instrumentId = string.Empty;
        private string _size = string.Empty;
        private string _side = string.Empty;
        private decimal _price;

        /// <summary>
        /// Заголовок таблицы.
        /// </summary>
        public string Title
        {
            get => _title;
            set => this.RaiseAndSetIfChanged(ref _title, value);
        }

        /// <summary>
        /// Время размещения/исполнения ордера.
        /// </summary>
        public string Date
        {
            get => _date;
            set => this.RaiseAndSetIfChanged(ref _date, value);
        }

        /// <summary>
        /// Id ордера на бирже, создаваемый стратегией.
        /// </summary>
        public string ClientOrderId
        {
            get => _clientOrderId;
            set => this.RaiseAndSetIfChanged(ref _clientOrderId, value);
        }

        /// <summary>
        /// Id ордера на бирже, создаваемый биржей.
        /// </summary>
        public long? OrderId
        {
            get => _orderId;
            set => this.RaiseAndSetIfChanged(ref _orderId, value);
        }

        /// <summary>
        /// Id трейда на бирже.
        /// </summary>
        public long? TradeId
        {
            get => _tradeId;
            set => this.RaiseAndSetIfChanged(ref _tradeId, value);
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
        /// Тип инструмента.
        /// </summary>
        public string InstrumentType
        {
            get => _instrumentType;
            set => this.RaiseAndSetIfChanged(ref _instrumentType, value);
        }

        /// <summary>
        /// Имя инструмента.
        /// </summary>
        public string InstrumentId
        {
            get => _instrumentId;
            set => this.RaiseAndSetIfChanged(ref _instrumentId, value);
        }

        /// <summary>
        /// Количество инструмента в ордере.
        /// </summary>
        public string Size
        {
            get => _size;
            set => this.RaiseAndSetIfChanged(ref _size, value);
        }

        /// <summary>
        /// Сторона ордера (купля или продажа).
        /// </summary>
        public string Side
        {
            get => _side;
            set => this.RaiseAndSetIfChanged(ref _side, value);
        }

        /// <summary>
        /// Цена инструмента по ордеру.
        /// </summary>
        public decimal Price
        {
            get => _price;
            set => this.RaiseAndSetIfChanged(ref _price, value);
        }
    }

}

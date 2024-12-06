using Newtonsoft.Json;
using ReactiveUI;

namespace Client.Service.Sub
{
    /// Класс информации по инструменту конкретной стратегии.
    /// </summary>
    /// <param name="clientOrderId">Уникальный id ордера на бирже.</param>
    /// <param name="instType">Тип инструмента.</param>
    /// <param name="instId">Имя инструмента.</param>
    /// <param name="priceStep">Шаг цены.</param>
    /// <param name="limit">Торговый лимит в валюте.</param>
    public class SubStockData(string clientOrderId, string instType, string instId, decimal priceStep, decimal limit, decimal equivalent, decimal multiplier) : ReactiveObject
    {
        #region private
        private decimal _limit = limit;
        private decimal _price;
        private decimal _quantity = 0;
        private bool _hold;
        private bool _isActive;
        #endregion

        /// <summary>
        /// Уникальный id ордера на бирже.
        /// </summary>
        public string ClientOrderId { get; set; } = clientOrderId;

        /// <summary>
        /// Тип инструмента.
        /// </summary>
        [JsonProperty(nameof(InstrumentType))]
        public string InstrumentType { get; set; } = instType;

        /// <summary>
        /// Имя инструмента.
        /// </summary>
        [JsonProperty(nameof(InstrumentId))]
        public string InstrumentId { get; set; } = instId;

        /// <summary>
        /// Шаг цены.
        /// </summary>
        public decimal PriceStep { get; set; } = priceStep;

        /// <summary>
        /// Торговый лимит в размере инструмента.
        /// </summary>
        public decimal Limit
        {
            get => _limit;
            set => this.RaiseAndSetIfChanged(ref _limit, value);
        }

        /// <summary>
        /// Эквивалент в валюте.
        /// </summary>
        [JsonProperty("CurrencyEq")]
        public decimal Equivalent { get; set; } = equivalent;

        /// <summary>
        /// Множитель по отношению к фьючерсу.
        /// </summary>
        public decimal Multiplier { get; set; } = multiplier;

        /// <summary>
        /// Сторона ордера (купля или продажа).
        /// </summary>
        public string Side { get; set; } = string.Empty;

        /// <summary>Обновляемая цена ордера.</summary>
        public decimal Price
        {
            get => _price;
            set
            {
                if (!Hold)
                {
                    _price = value;
                }
                else
                {
                    if (Side.Equals("Buy"))
                    {
                        if (_price < value) _price = value;
                    }
                    else if (Side.Equals("Sell"))
                    {
                        if (_price > value) _price = value;
                    }
                }
            }
        }

        /// <summary>Количество инструмента на стратегию.</summary>
        public decimal Quantity
        {
            get => _quantity;
            set => this.RaiseAndSetIfChanged(ref _quantity, value);
        }

        /// <summary>
        /// Удержание цены по стратегии.
        /// </summary>
        public bool Hold
        {
            get => _hold;
            set => this.RaiseAndSetIfChanged(ref _hold, value);
        }

        /// <summary>
        /// Состояние ордера на бирже.
        /// </summary>
        public bool IsActive
        {
            get => _isActive;
            set => this.RaiseAndSetIfChanged(ref _isActive, value);
        }

        /// <summary>
        /// Позиция для расчета цены по книге ордеров.
        /// </summary>
        public int Position { get; set; }
    }

}

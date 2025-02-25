using Newtonsoft.Json;
using ProjectZeroLib.Enums;
using ReactiveUI;

namespace Client.Service.Sub
{
    /// <summary>
    /// Класс, описывающий данные по инструменту в подписке,
    /// необходимые для формирования ордера.
    /// </summary>
    /// <param name="id">Имя инструмента.</param>
    /// <param name="type">Тип инструмента.</param>
    /// <param name="burse">Имя биржи.</param>
    /// <param name="priceStep">Шаг цены.</param>
    /// <param name="equivalent">Эквивалент относительно основного инструмента.</param>
    /// <param name="round">Количество знаков для округления.</param>
    public partial class Stock(string id, string type, BurseName burse, decimal priceStep, decimal equivalent, int round) : ReactiveObject
    {
        [JsonProperty(nameof(Id))]
        public string Id { get; } = id;

        [JsonProperty(nameof(Type))]
        public string Type { get; } = type;

        [JsonProperty(nameof(Burse))]
        public BurseName Burse { get; } = burse;

        [JsonProperty(nameof(PriceStep))]
        public decimal PriceStep { get; } = priceStep;

        [JsonProperty("CurrencyEq")]
        public decimal Equivalent { get; } = equivalent;

        [JsonProperty(nameof(Round))]
        public int Round { get; } = round;
    }
}

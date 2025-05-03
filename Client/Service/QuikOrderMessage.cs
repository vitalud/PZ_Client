using System.Text.Json.Serialization;

namespace Client.Service
{
    public class QuikOrderMessage()
    {
        [JsonPropertyName("status")]
        public int Status { get; set; }

        [JsonPropertyName("account")]
        public string Account { get; set; } = string.Empty;

        [JsonPropertyName("client_code")]
        public string ClientCode { get; set; } = string.Empty;

        [JsonPropertyName("class_code")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("sec_code")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("action")]
        public string Action { get; set; } = string.Empty;

        [JsonPropertyName("price")]
        public decimal Price { get; set; }

        [JsonPropertyName("balance")]
        public decimal Balance { get; set; }

        [JsonPropertyName("trans_id")]
        public string TransactionId { get; set; } = string.Empty;

        [JsonPropertyName("order_num")]
        public int OrderNum { get; set; }

    }
}

using Newtonsoft.Json;

namespace Nop.Plugin.Payments.BoletoBradescoAPI.Models.Message
{
    public class Pedido
    {
        [JsonProperty(PropertyName = "numero")]
        public string Numero { get; set; }

        [JsonProperty(PropertyName = "valor")]
        public decimal Valor { get; set; }

        [JsonProperty(PropertyName = "descricao")]
        public string Descricao { get; set; }
    }
}

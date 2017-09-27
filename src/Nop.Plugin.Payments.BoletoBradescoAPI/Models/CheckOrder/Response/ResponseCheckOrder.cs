using Newtonsoft.Json;
using System.Collections.Generic;

namespace Nop.Plugin.Payments.BoletoBradescoAPI.Models.CheckOrder.Response
{
    public class ResponseCheckOrder
    {
        [JsonProperty(PropertyName = "status")]
        public StatusConsulta Status { get; set; }

        [JsonProperty(PropertyName = "token")]
        public Token Token { get; set; }

        [JsonProperty(PropertyName = "pedidos")]
        public List<PedidosConsulta> Pedidos { get; set; }
    }
}

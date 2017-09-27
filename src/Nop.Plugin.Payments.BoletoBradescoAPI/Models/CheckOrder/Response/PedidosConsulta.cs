using Newtonsoft.Json;
using System;

namespace Nop.Plugin.Payments.BoletoBradescoAPI.Models.CheckOrder.Response
{
    public class PedidosConsulta
    {
        [JsonProperty(PropertyName = "numero")]
        public string NumeroPedido { get; set; }

        [JsonProperty(PropertyName = "valor")]
        public decimal ValorPedido { get; set; }

        [JsonProperty(PropertyName = "data")]
        public DateTime DataPedido { get; set; }

        [JsonProperty(PropertyName = "valorPago")]
        public decimal ValorPago   {get; set; }

        [JsonProperty(PropertyName = "dataPagamento")]
        public DateTime DataPagamento { get; set; }

        [JsonProperty(PropertyName = "linhaDigitavel")]
        public string LinhaDigitavel { get; set; }

        [JsonProperty(PropertyName = "status")]
        public string Status { get; set; }

        [JsonProperty(PropertyName = "erro")]
        public string Erro { get; set; }
    }
}

using Newtonsoft.Json;

namespace Nop.Plugin.Payments.BoletoBradescoAPI.Models.Message.Request
{
    public class ServiceRequest
    {
        [JsonProperty(PropertyName = "merchant_id")]
        public string MerchantId { get; set; }

        [JsonProperty(PropertyName = "meio_pagamento")]
        public string CodigoMeioPagamento { get; set; }

        [JsonProperty(PropertyName = "pedido")]
        public Pedido Pedido { get; set; }

        [JsonProperty(PropertyName = "comprador")]
        public Comprador Comprador { get; set; }

        [JsonProperty(PropertyName = "boleto")]
        public BoletoRequest Boleto { get; set; }

        [JsonProperty(PropertyName = "token_request_confirmacao_pagamento")]
        public string TokenRequestUrlConfirmacaoPagamento { get; set; }


    }
}

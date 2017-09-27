using Newtonsoft.Json;

namespace Nop.Plugin.Payments.BoletoBradescoAPI.Models.Message.Response
{
    public class ServiceResponse
    {
        [JsonProperty(PropertyName = "merchant_id")]
        public string MerchantId;

        [JsonProperty(PropertyName = "meio_pagamento")]
        public string CodigoMeioPagamento;

        [JsonProperty(PropertyName = "pedido")]
        public Pedido pedido;

        [JsonProperty(PropertyName = "boleto")]
        public BoletoResponse Boleto;

        [JsonProperty(PropertyName = "status")]
        public StatusResponse Status;
    }


}

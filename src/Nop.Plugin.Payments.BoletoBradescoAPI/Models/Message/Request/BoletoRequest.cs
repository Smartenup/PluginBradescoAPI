using Newtonsoft.Json;
using System;

namespace Nop.Plugin.Payments.BoletoBradescoAPI.Models.Message.Request
{
    public class BoletoRequest
    {
        
        [JsonProperty(PropertyName = "beneficiario")]
        public string Beneficiario { get; set; }

        [JsonProperty(PropertyName = "carteira")]
        public string CarteiraCobranca { get; set; }

        [JsonProperty(PropertyName = "nosso_numero")]
        public string NossoNumero { get; set; }

        [JsonProperty(PropertyName = "data_emissao")]
        public DateTime DataEmissao { get; set; }

        [JsonProperty(PropertyName = "data_vencimento")]
        public DateTime DataVencimento { get; set; }

        [JsonProperty(PropertyName = "valor_titulo")]
        public decimal ValorTitulo { get; set; }

        [JsonProperty(PropertyName = "url_logotipo")]
        public string UrlLogotipo { get; set; }

        [JsonProperty(PropertyName = "mensagem_cabecalho")]
        public string MensagemCabecalho { get; set; }

        [JsonProperty(PropertyName = "tipo_renderizacao")]
        public int TipoRenderizacao { get; set; }

        [JsonProperty(PropertyName = "instrucoes")]
        public BoletoInstrucoesRequest Instrucoes;

        [JsonProperty(PropertyName = "registro")]
        public BoletoRegistroRequest Registro;
    }
}

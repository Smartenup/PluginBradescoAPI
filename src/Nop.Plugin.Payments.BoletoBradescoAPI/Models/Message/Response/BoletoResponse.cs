using Newtonsoft.Json;
using System;

namespace Nop.Plugin.Payments.BoletoBradescoAPI.Models.Message.Response
{
    public class BoletoResponse
    {
        [JsonProperty(PropertyName = "valor_titulo")]
        public decimal ValorTitulo;

        [JsonProperty(PropertyName = "data_geracao")]
        public DateTime DataGeracao;

        [JsonProperty(PropertyName = "data_atualizacao")]
        public DateTime DataAtualizao;

        [JsonProperty(PropertyName = "linha_digitavel")]
        public string LinhaDigitavel;

        [JsonProperty(PropertyName = "linha_digitavel_formatada")]
        public string LinhaDigitavelFormatada;

        [JsonProperty(PropertyName = "token")]
        public string Token;

        [JsonProperty(PropertyName = "url_acesso")]
        public string UrlAcesso;
    }
}

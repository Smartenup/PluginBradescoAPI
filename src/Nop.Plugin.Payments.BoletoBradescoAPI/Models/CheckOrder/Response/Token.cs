using Newtonsoft.Json;
using System;

namespace Nop.Plugin.Payments.BoletoBradescoAPI.Models.CheckOrder.Response
{
    public class Token
    {
        [JsonProperty(PropertyName = "token")]
        public string TokenAutenticacao { get; set; }

        [JsonProperty(PropertyName = "dataCriacao")]
        public DateTime DataCriacao { get; set; }

    }
}

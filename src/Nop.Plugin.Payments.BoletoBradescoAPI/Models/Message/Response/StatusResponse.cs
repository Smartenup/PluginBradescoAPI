using Newtonsoft.Json;

namespace Nop.Plugin.Payments.BoletoBradescoAPI.Models.Message.Response
{
    public class StatusResponse
    {
        [JsonProperty(PropertyName = "codigo")]
        public int Codigo { get; set; }

        [JsonProperty(PropertyName = "mensagem")]
        public string Mensagem { get; set; }

        [JsonProperty(PropertyName = "detalhes")]
        public string Detalhes { get; set; }
    }
}

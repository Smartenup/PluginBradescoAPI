using Newtonsoft.Json;

namespace Nop.Plugin.Payments.BoletoBradescoAPI.Models.CheckOrder.Response
{
    public class StatusConsulta
    {
        [JsonProperty(PropertyName = "codigo")]
        public string Codigo { get; set; }

        [JsonProperty(PropertyName = "mensagem")]
        public string Mensagem { get; set; }
    }
}

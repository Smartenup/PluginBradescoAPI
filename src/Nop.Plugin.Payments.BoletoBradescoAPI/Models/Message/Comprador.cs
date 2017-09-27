using Newtonsoft.Json;

namespace Nop.Plugin.Payments.BoletoBradescoAPI.Models.Message
{
    public class Comprador
    {
        [JsonProperty(PropertyName = "nome")]
        public string Nome { get; set; }

        [JsonProperty(PropertyName = "documento")]
        public string Documento { get; set; }

        [JsonProperty(PropertyName = "ip")]
        public string IP { get; set; }

        [JsonProperty(PropertyName = "user_agent")]
        public string UserAgent { get; set; }

        [JsonProperty(PropertyName = "endereco")]
        public CompradorEndereco Endereco { get; set; }

    }
}

using Newtonsoft.Json;

namespace Nop.Plugin.Payments.BoletoBradescoAPI.Models.Message.Request
{
    [JsonObject(MemberSerialization.OptIn)]
    public class BoletoInstrucoesRequest
    {
        [JsonProperty(PropertyName = "instrucao_linha_1")]
        public string InstrucaoLinha1 { get; set; }

        [JsonProperty(PropertyName = "instrucao_linha_2")]
        public string InstrucaoLinha2 { get; set; }

        [JsonProperty(PropertyName = "instrucao_linha_3")]
        public string InstrucaoLinha3 { get; set; }

        [JsonProperty(PropertyName = "instrucao_linha_4")]
        public string InstrucaoLinha4 { get; set; }

        [JsonProperty(PropertyName = "instrucao_linha_5")]
        public string InstrucaoLinha5 { get; set; }

        [JsonProperty(PropertyName = "instrucao_linha_6")]
        public string InstrucaoLinha6 { get; set; }

        [JsonProperty(PropertyName = "instrucao_linha_7")]
        public string InstrucaoLinha7 { get; set; }

        [JsonProperty(PropertyName = "instrucao_linha_8")]
        public string InstrucaoLinha8 { get; set; }

        [JsonProperty(PropertyName = "instrucao_linha_9")]
        public string InstrucaoLinha9 { get; set; }

        [JsonProperty(PropertyName = "instrucao_linha_10")]
        public string InstrucaoLinha10 { get; set; }

        [JsonProperty(PropertyName = "instrucao_linha_11")]
        public string InstrucaoLinha11 { get; set; }

        [JsonProperty(PropertyName = "instrucao_linha_12")]
        public string InstrucaoLinha12 { get; set; }        
    }
}

using Newtonsoft.Json;
using System;

namespace Nop.Plugin.Payments.BoletoBradescoAPI.Models.Message.Request
{
    public class BoletoRegistroRequest
    {
        [JsonProperty(PropertyName = "agencia_pagador")]
        public string AgenciaPagador { get; set; }

        [JsonProperty(PropertyName = "razao_conta_pagador")]
        public string RazaoContaPagador { get; set; }

        [JsonProperty(PropertyName = "conta_pagador")]
        public string ContaPagador { get; set; }

        [JsonProperty(PropertyName = "controle_participante")]
        public string ControleParticipante { get; set; }

        [JsonProperty(PropertyName = "aplicar_multa")]
        public bool? AplicarMulta { get; set; }

        [JsonProperty(PropertyName = "valor_percentual_multa")]
        public decimal? ValorPercentualMulta { get; set; }

        [JsonProperty(PropertyName = "valor_desconto_bonificacao")]
        public decimal? ValorDescontoBonificacao { get; set; }

        [JsonProperty(PropertyName = "debito_automatico")]
        public bool? DebitoAutomatico { get; set; }

        [JsonProperty(PropertyName = "rateio_credito")]
        public bool? RateioCredito { get; set; }

        [JsonProperty(PropertyName = "endereco_debito_automatico")]
        public string EnderecoDebitoAutomatico { get; set; }

        [JsonProperty(PropertyName = "tipo_ocorrencia")]
        public string TipoOcorrencia { get; set; }

        [JsonProperty(PropertyName = "especie_titulo")]
        public string EspecieTitulo { get; set; }

        [JsonProperty(PropertyName = "primeira_instrucao")]
        public string PrimeiraInstrucao { get; set; }

        [JsonProperty(PropertyName = "segunda_instrucao")]
        public string SegundaInstrucao { get; set; }

        [JsonProperty(PropertyName = "valor_juros_mora")]
        public decimal? ValorJurosMora { get; set; }

        [JsonProperty(PropertyName = "data_limite_concessao_desconto")]
        public DateTime? DataLimiteConcessaoDesconto { get; set; }

        [JsonProperty(PropertyName = "valor_desconto")]
        public decimal? ValorDesconto { get; set; }

        [JsonProperty(PropertyName = "valor_iof")]
        public decimal? ValorIof { get; set; }

        [JsonProperty(PropertyName = "valor_abatimento")]
        public decimal? ValorAbatimento { get; set; }

        [JsonProperty(PropertyName = "tipo_inscricao_pagador")]
        public string TipoInscricaoPagador { get; set; }

        [JsonProperty(PropertyName = "sequencia_registro")]
        public string SequenciaRegistro { get; set; }
    }
}

using Nop.Core.Configuration;

namespace Nop.Plugin.Payments.BoletoBradescoAPI
{
    public class BoletoBradescoAPIPaymentSettings : ISettings
    {
        public string EmailAdministrativo { get; set; }
        public string NumeroLoja { get; set; }        
        public string ChaveSeguranca { get; set;}
        public string NomeSacado { get; set; }
        public int NumeroDiasAdicionaisVencimentoBoleto { get; set; }
        public string NomeProdutoUnico { get; set; }
        public bool ModoDebug { get; set; }
        public string Carteira { get; set; }
        public bool AdicionarNotaPrazoFabricaoEnvio { get; set; }
        public bool AmbienteProducao { get; set; }
    }
}

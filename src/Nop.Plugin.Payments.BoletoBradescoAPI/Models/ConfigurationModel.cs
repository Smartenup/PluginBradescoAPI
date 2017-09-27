using Nop.Web.Framework;
using Nop.Web.Framework.Mvc;

namespace Nop.Plugin.Payments.BoletoBradescoAPI.Models
{
    public class ConfigurationModel : BaseNopModel
    {
        [NopResourceDisplayName("Plugins.Payments.BoletoBradescoAPI.Fields.EmailAdministrativo")]
        public string EmailAdministrativo { get; set; }

        [NopResourceDisplayName("Plugins.Payments.BoletoBradescoAPI.Fields.NomeSacado")]
        public string NomeSacado { get; set; }

        [NopResourceDisplayName("Plugins.Payments.BoletoBradescoAPI.Fields.NumeroLoja")]
        public string NumeroLoja { get; set; }

        [NopResourceDisplayName("Plugins.Payments.BoletoBradescoAPI.Fields.ChaveSeguranca")]
        public string ChaveSeguranca { get; set; }

        [NopResourceDisplayName("Plugins.Payments.BoletoBradescoAPI.Fields.NumeroDiasAdicionaisVencimentoBoleto")]
        public int NumeroDiasAdicionaisVencimentoBoleto { get; set; }

        [NopResourceDisplayName("Plugins.Payments.BoletoBradescoAPI.Fields.NomeProdutoUnico")]
        public string NomeProdutoUnico { get; set; }

        [NopResourceDisplayName("Plugins.Payments.BoletoBradescoAPI.Fields.ModoDebug")]
        public bool ModoDebug { get; set; }

        [NopResourceDisplayName("Plugins.Payments.BoletoBradescoAPI.Fields.Carteira")]
        public string Carteira { get; set; }

        [NopResourceDisplayName("Plugins.Payments.BoletoBradescoAPI.Fields.AdicionarNotaPrazoFabricaoEnvio")]
        public bool AdicionarNotaPrazoFabricaoEnvio { get; set; }

        [NopResourceDisplayName("Plugins.Payments.BoletoBradescoAPI.Fields.AmbienteProducao")]
        public bool AmbienteProducao { get; set; }

    }
}

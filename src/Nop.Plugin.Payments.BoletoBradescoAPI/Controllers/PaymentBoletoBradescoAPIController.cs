using Nop.Core;
using Nop.Core.Domain.Payments;
using Nop.Plugin.Payments.BoletoBradescoAPI.Models;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Services.Stores;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Security;
using System;
using System.Collections.Generic;
using System.Net;
using System.Web.Mvc;


namespace Nop.Plugin.Payments.BoletoBradescoAPI.Controllers
{
    public class PaymentBoletoBradescoAPIController : BasePaymentController
    {

        private readonly IWorkContext _workContext;
        private readonly IStoreService _storeService;
        private readonly ISettingService _settingService;
        private readonly IOrderService _orderService;
        private readonly IOrderProcessingService _orderProcessingService;
        private readonly ILogger _logger;
        private readonly BoletoBradescoAPIPaymentSettings _bradescoPaymentSettings;
        private readonly ILocalizationService _localizationService;
        private readonly IAddressAttributeParser _addressAttributeParser;


        public PaymentBoletoBradescoAPIController(IWorkContext workContext,
            IStoreService storeService, 
            ISettingService settingService, 
            IPaymentService paymentService, 
            IOrderService orderService, 
            IOrderProcessingService orderProcessingService,
            ILogger logger, 
            PaymentSettings paymentSettings, 
            ILocalizationService localizationService,
            BoletoBradescoAPIPaymentSettings bradescoPaymentSettings,
            IAddressAttributeParser addressAttributeParser
            )
        {
           _workContext = workContext;
           _storeService = storeService;
           _settingService = settingService;
           _orderService = orderService;
           _orderProcessingService = orderProcessingService;
           _logger = logger;
           _localizationService = localizationService;
           _bradescoPaymentSettings = bradescoPaymentSettings;
           _addressAttributeParser = addressAttributeParser;
        }

        [AdminAuthorize]
        [ChildActionOnly]
        public ActionResult Configure()
        {
            //load settings for a chosen store scope
            var storeScope = this.GetActiveStoreScopeConfiguration(_storeService, _workContext);
            var bradescoPaymentSettings = _settingService.LoadSetting<BoletoBradescoAPIPaymentSettings>(storeScope);

            var model = new ConfigurationModel();

            model.EmailAdministrativo = bradescoPaymentSettings.EmailAdministrativo;
            model.NomeSacado = bradescoPaymentSettings.NomeSacado;
            model.NumeroLoja = bradescoPaymentSettings.NumeroLoja;
            model.ChaveSeguranca = bradescoPaymentSettings.ChaveSeguranca;
            model.NumeroDiasAdicionaisVencimentoBoleto = bradescoPaymentSettings.NumeroDiasAdicionaisVencimentoBoleto;
            model.NomeProdutoUnico = bradescoPaymentSettings.NomeProdutoUnico;
            model.ModoDebug = bradescoPaymentSettings.ModoDebug;
            model.Carteira = bradescoPaymentSettings.Carteira;
            model.AdicionarNotaPrazoFabricaoEnvio = bradescoPaymentSettings.AdicionarNotaPrazoFabricaoEnvio;
            model.AmbienteProducao = bradescoPaymentSettings.AmbienteProducao;

            return View("~/Plugins/Payments.BoletoBradescoAPI/Views/PaymentBoletoBradescoAPI/Configure.cshtml", model);
        }


        [HttpPost]
        [AdminAuthorize]
        [AdminAntiForgery]
        [ChildActionOnly]
        public ActionResult Configure(ConfigurationModel model)
        {
            if (!ModelState.IsValid)
                return Configure();

            //load settings for a chosen store scope
            var storeScope = this.GetActiveStoreScopeConfiguration(_storeService, _workContext);
            var bradescoPaymentSettings = _settingService.LoadSetting<BoletoBradescoAPIPaymentSettings>(storeScope);


            //save settings
            bradescoPaymentSettings.EmailAdministrativo = model.EmailAdministrativo;
            bradescoPaymentSettings.NomeSacado = model.NomeSacado;
            bradescoPaymentSettings.NumeroLoja = model.NumeroLoja;
            bradescoPaymentSettings.ChaveSeguranca = model.ChaveSeguranca;
            bradescoPaymentSettings.NumeroDiasAdicionaisVencimentoBoleto = model.NumeroDiasAdicionaisVencimentoBoleto;
            bradescoPaymentSettings.NomeProdutoUnico = model.NomeProdutoUnico;
            bradescoPaymentSettings.ModoDebug = model.ModoDebug;
            bradescoPaymentSettings.Carteira = model.Carteira;
            bradescoPaymentSettings.AdicionarNotaPrazoFabricaoEnvio = model.AdicionarNotaPrazoFabricaoEnvio;
            bradescoPaymentSettings.AmbienteProducao = model.AmbienteProducao;

            _settingService.SaveSetting(bradescoPaymentSettings);

            //now clear settings cache
            _settingService.ClearCache();

            SuccessNotification(_localizationService.GetResource("Admin.Plugins.Saved"));

            return View("~/Plugins/Payments.BoletoBradescoAPI/Views/PaymentBoletoBradescoAPI/Configure.cshtml", model);
        }

        [NonAction]
        public override IList<string> ValidatePaymentForm(FormCollection form)
        {
            var warnings = new List<string>();
            return warnings;
        }

        [NonAction]
        public override ProcessPaymentRequest GetPaymentInfo(FormCollection form)
        {
            var paymentInfo = new ProcessPaymentRequest();
            return paymentInfo;
        }

        [ChildActionOnly]
        public ActionResult PaymentInfo()
        {
            return View("~/Plugins/Payments.BoletoBradescoAPI/Views/PaymentBoletoBradescoAPI/PaymentInfo.cshtml");
        }

       

        [ValidateInput(false)]
        public ActionResult CheckOrder()
        {
            try
            {
                if (_bradescoPaymentSettings.ModoDebug)
                {
                    _logger.Information("Plugin.Payments.BoletoBradescoAPI: Request[numero_pedido] : " + Request["numero_pedido"]);
                    _logger.Information("Plugin.Payments.BoletoBradescoAPI: Request[token] : " + Request["token"]);                    
                }

                if (string.IsNullOrWhiteSpace(Request["numero_pedido"]))
                {
                    return new HttpStatusCodeResult(HttpStatusCode.InternalServerError);
                }

                int numeroPedido = int.Parse(Request["numero_pedido"]);

                var  order = _orderService.GetOrderById(numeroPedido);

                if (order.PaymentMethodSystemName == "Payments.BoletoBradescoAPI")
                {
                    return new HttpStatusCodeResult(HttpStatusCode.OK);
                }
                else
                {
                    _logger.Error(string.Format("Plugin.Payments.BoletoBradescoAPI: erro ao confirmar pedido {0} para o bradesco - Pedido não encontrado",
                        Request["numero_pedido"]));

                    return new HttpStatusCodeResult(HttpStatusCode.InternalServerError);
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Plugin.Payments.BoletoBradescoAPI: erro ao confirmar pedido para o bradesco - Erro" + ex.Message, ex);

                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError);
            }
         }
    }
}

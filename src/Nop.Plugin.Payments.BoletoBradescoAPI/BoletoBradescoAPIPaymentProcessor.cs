using Newtonsoft.Json;
using Nop.Core;
using Nop.Core.Domain;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Core.Plugins;
using Nop.Plugin.Payments.BoletoBradescoAPI.Controllers;
using Nop.Plugin.Payments.BoletoBradescoAPI.Models.Message;
using Nop.Plugin.Payments.BoletoBradescoAPI.Models.Message.Request;
using Nop.Plugin.Payments.BoletoBradescoAPI.Models.Message.Response;
using Nop.Plugin.Payments.BoletoBradescoAPI.Serializer;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Customers;
using Nop.Services.Directory;
using Nop.Services.Localization;
using Nop.Services.Media;
using Nop.Services.Messages;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Services.Tasks;
using Nop.Services.Tax;
using Nop.Web.Framework.Themes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Routing;
using System.Linq;
using Nop.Core.Domain.Tasks;

namespace Nop.Plugin.Payments.BoletoBradescoAPI
{
    public class BoletoBradescoAPIPaymentProcessor : BasePlugin, IPaymentMethod
    {
        #region Fields
        private readonly IOrderService _orderService;
        private readonly IWorkflowMessageService _workflowMessageService;

        private readonly BoletoBradescoAPIPaymentSettings _bradescoPaymentSettings;
        private readonly ISettingService _settingService;
        private readonly ITaxService _taxService;
        private readonly IPriceCalculationService _priceCalculationService;
        private readonly ICurrencyService _currencyService;
        private readonly ICustomerService _customerService;
        private readonly CurrencySettings _currencySettings;
        private readonly IWebHelper _webHelper;
        private readonly StoreInformationSettings _storeInformationSettings;
        private readonly IAddressAttributeParser _addressAttributeParser;
        private readonly IWorkContext _workContext;
        private readonly IScheduleTaskService _scheduleTaskService;
        private readonly IPictureService _pictureService;
        private readonly IThemeContext _themeContext;
        #endregion

        public BoletoBradescoAPIPaymentProcessor(
            BoletoBradescoAPIPaymentSettings bradescoPaymentSettings,
            ISettingService settingService,
            ITaxService taxService, 
            IPriceCalculationService priceCalculationService,
            ICurrencyService currencyService, 
            ICustomerService customerService,
            CurrencySettings currencySettings, 
            IWebHelper webHelper,
            StoreInformationSettings storeInformationSettings,
            IAddressAttributeParser addressAttributeParser,
            IWorkContext workContext,
            IScheduleTaskService scheduleTaskService,
            IOrderService orderService,
            IWorkflowMessageService workflowMessageService,
            IPictureService pictureService,
            IThemeContext themeContext
            )
        {
            _bradescoPaymentSettings  = bradescoPaymentSettings;
            _settingService           = settingService;
            _taxService               = taxService;
            _priceCalculationService  = priceCalculationService;
            _currencyService          = currencyService;
            _customerService          = customerService;
            _currencySettings         = currencySettings;
            _webHelper                = webHelper;
            _storeInformationSettings = storeInformationSettings;
            _addressAttributeParser   = addressAttributeParser;
            _workContext              = workContext;
            _scheduleTaskService      = scheduleTaskService;
            _orderService             = orderService;
            _workflowMessageService   = workflowMessageService;
            _pictureService           = pictureService;
            _themeContext             = themeContext;
        }

        public ProcessPaymentResult ProcessPayment(ProcessPaymentRequest processPaymentRequest)
        {
            var result = new ProcessPaymentResult();
            result.NewPaymentStatus = PaymentStatus.Pending;
            return result;
        }

        public void PostProcessPayment(PostProcessPaymentRequest postProcessPaymentRequest)
        {
            ///Obter pedido para enviar ao bradesco
            ServiceRequest serviceRequest = ObterServiceRequest(postProcessPaymentRequest.Order);

            var jsonSettings = new JsonSerializerSettings()
            {
                DateFormatString = "yyyy-MM-dd",
                NullValueHandling = NullValueHandling.Ignore
            };

            jsonSettings.Converters.Add(new DecimalConverter());

            //Conteudo da requisicao em bytes
            var data = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(serviceRequest, jsonSettings));

            string chaveSeguranca = ObterChaveSeguranca();
            string urlBradesco = ObterUrlBradesco();

            var mediaType = "application/json";
            var charSet = "UTF-8";
            var urlPost = urlBradesco + "/transacao";
            var request = (HttpWebRequest)WebRequest.Create(urlPost);

            //Configuracao do cabecalho da requisicao
            request.Method = "POST";
            request.ContentType = mediaType + ";charset=" + charSet;
            request.ContentLength = data.Length;
            request.Accept = mediaType;
            request.Headers.Add(HttpRequestHeader.AcceptCharset, charSet);

            //Credenciais de Acesso
            string header = serviceRequest.MerchantId + ":" + chaveSeguranca;
            string headerBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(header));
            request.Headers.Add(HttpRequestHeader.Authorization, "Basic " + headerBase64);

            //Adicionando conteudo a requisicao
            request.GetRequestStream().Write(data, 0, data.Length);

            //Obtem resposta do servidor
            var response = (HttpWebResponse)request.GetResponse();

            //Verifica retorno da resposta
            if (response.StatusCode != HttpStatusCode.OK && response.StatusCode != HttpStatusCode.Created)
                throw new NopException("Retorno da requisicao dif 200/201. HttpStatusCode: " +
                    response.StatusCode.ToString());

            //Serializa a resposta do servidor
            ServiceResponse serviceResponse = null;
            using (var jsonTextReader = new JsonTextReader(new StreamReader(response.GetResponseStream())))
            {
                serviceResponse = new JsonSerializer().Deserialize<ServiceResponse>(jsonTextReader);
            }

            //Verificar se retorno OK
            VerificarResponse(serviceResponse);

            //Adicionar nota de linha digitável
            AdicionarNotaLinhaDigitavel(postProcessPaymentRequest, serviceResponse);


            HttpContext.Current.Response.Redirect(serviceResponse.Boleto.UrlAcesso);
        }

        public bool HidePaymentMethod(IList<ShoppingCartItem> cart)
        {
            //you can put any logic here
            //for example, hide this payment method if all products in the cart are downloadable
            //or hide this payment method if current customer is from certain country
            return false;
        }

        public decimal GetAdditionalHandlingFee(IList<ShoppingCartItem> cart)
        {
            return 0;
        }

        public CapturePaymentResult Capture(CapturePaymentRequest capturePaymentRequest)
        {
            var result = new CapturePaymentResult();
            result.AddError("Capture method not supported");
            return result;
        }

        public RefundPaymentResult Refund(RefundPaymentRequest refundPaymentRequest)
        {
            var result = new RefundPaymentResult();
            result.AddError("Refund method not supported");
            return result;
        }

        public VoidPaymentResult Void(VoidPaymentRequest voidPaymentRequest)
        {
            var result = new VoidPaymentResult();
            result.AddError("Void method not supported");
            return result;
        }

        public ProcessPaymentResult ProcessRecurringPayment(ProcessPaymentRequest processPaymentRequest)
        {
            var result = new ProcessPaymentResult();
            result.AddError("Recurring payment not supported");
            return result;
        }

        public CancelRecurringPaymentResult CancelRecurringPayment(CancelRecurringPaymentRequest cancelPaymentRequest)
        {
            var result = new CancelRecurringPaymentResult();
            result.AddError("Recurring payment not supported");
            return result;
        }

        public bool CanRePostProcessPayment(Order order)
        {
            if (order == null)
                throw new ArgumentNullException("bool CanRePostProcessPayment");

            //payment status should be Pending
            if (order.PaymentStatus != PaymentStatus.Pending)
                return false;

            //let's ensure that at least 1 minute passed after order is placed
            if ((DateTime.UtcNow - order.CreatedOnUtc).TotalMinutes < 1)
                return false;

            return true;
        }

        public void GetConfigurationRoute(out string actionName, out string controllerName, out RouteValueDictionary routeValues)
        {
            actionName = "Configure";
            controllerName = "PaymentBoletoBradescoAPI";
            routeValues = new RouteValueDictionary() { { "Namespaces", "Nop.Plugin.Payments.PaymentBoletoBradescoAPI.Controllers" }, { "area", null } };

        }

        public void GetPaymentInfoRoute(out string actionName, out string controllerName, out RouteValueDictionary routeValues)
        {
            actionName = "PaymentInfo";
            controllerName = "PaymentBoletoBradescoAPI";
            routeValues = new RouteValueDictionary() { { "Namespaces", "Nop.Plugin.Payments.BoletoBradescoAPI.Controllers" }, { "area", null } };
        }

        public Type GetControllerType()
        {
            return typeof(PaymentBoletoBradescoAPIController);
        }

        public bool SupportCapture
        {
            get { return false; }
        }

        public bool SupportPartiallyRefund
        {
            get { return false; }
        }

        public bool SupportRefund
        {
            get { return false; }
        }

        public bool SupportVoid
        {
            get { return false; }
        }

        public RecurringPaymentType RecurringPaymentType
        {
            get { return RecurringPaymentType.NotSupported; ; }
        }

        public PaymentMethodType PaymentMethodType
        {
            get { return PaymentMethodType.Redirection; }
        }

        public bool SkipPaymentInfo
        {
            get { return false; }
        }


        public override void Install()
        {
            base.Install();

            ScheduleTask taskByType = _scheduleTaskService.GetTaskByType("Nop.Plugin.Payments.BoletoBradescoAPI.BoletoBradescoPaymentAPIUpdateTask, Nop.Plugin.Payments.BoletoBradescoAPI");

            if (taskByType == null)
            {
                taskByType = new ScheduleTask()
                {
                    Enabled = false,
                    Name = "BoletoBradescoPaymentAPIUpdateTask",
                    Seconds = 86400,
                    StopOnError = false,
                    Type = "Nop.Plugin.Payments.BoletoBradescoAPI.BoletoBradescoPaymentAPIUpdateTask, Nop.Plugin.Payments.BoletoBradescoAPI"
                };

                _scheduleTaskService.InsertTask(taskByType);
            }
        }

        public override void Uninstall()
        {
            base.Uninstall();

            ScheduleTask taskByType = _scheduleTaskService.GetTaskByType("Nop.Plugin.Payments.BoletoBradescoAPI.BoletoBradescoPaymentAPIUpdateTask, Nop.Plugin.Payments.BoletoBradescoAPI");

            if (taskByType != null)
            {
                _scheduleTaskService.DeleteTask(taskByType);
            }
        }

        private void AdicionarNotaLinhaDigitavel(PostProcessPaymentRequest postProcessPaymentRequest, ServiceResponse serviceResponse)
        {
            if (!ExisteNota(serviceResponse.Boleto.LinhaDigitavel, postProcessPaymentRequest.Order))
                AddOrderNote("Boleto - Linha digitável " + serviceResponse.Boleto.LinhaDigitavel, true,
                    postProcessPaymentRequest.Order);

            if (!ExisteNota(serviceResponse.Boleto.LinhaDigitavelFormatada, postProcessPaymentRequest.Order))
                AddOrderNote("Boleto - Linha digitável formatada " + serviceResponse.Boleto.LinhaDigitavelFormatada,
                    true, postProcessPaymentRequest.Order);
        }

        private void VerificarResponse(ServiceResponse serviceResponse)
        {
            if (serviceResponse.Status == null)
                throw new NopException("Retorno da requisição com status null");

            if (serviceResponse.Boleto == null)
                throw new NopException(string.Format("Código {0} - Mensagem {1} - Detalhes {2}",
                    serviceResponse.Status.Codigo, serviceResponse.Status.Mensagem,
                    serviceResponse.Status.Detalhes));
        }

        private bool ExisteNota(string nota, Order order)
        {
            if (string.IsNullOrWhiteSpace(nota))
                return false;

            var notaLinhaDigitavel = order.OrderNotes.Where(note => note.Note.Contains(nota));

            if (notaLinhaDigitavel.Count() > 0)
                return true;

            return false;
        }

        private string ObterChaveSeguranca()
        {
            //string chaveSeguranca = "5onpOorDiLETxXUIM9-7nppyFRjqRwlzSzsOHWZjcGU";

            string chaveSeguranca = string.Empty;

            if (!string.IsNullOrWhiteSpace(_bradescoPaymentSettings.ChaveSeguranca))
                chaveSeguranca = _bradescoPaymentSettings.ChaveSeguranca;

            return chaveSeguranca;
        }

        private string ObterUrlBradesco()
        {
            string urlBradesco = "https://homolog.meiosdepagamentobradesco.com.br/apiboleto";

            if (_bradescoPaymentSettings.AmbienteProducao)
            {
                urlBradesco = "https://meiosdepagamentobradesco.com.br/apiboleto";
            }

            return urlBradesco;
        }

        private ServiceRequest ObterServiceRequest(Order order)
        {
            var serviceRequest = new ServiceRequest();

            serviceRequest.MerchantId = _bradescoPaymentSettings.NumeroLoja;

            serviceRequest.CodigoMeioPagamento = "300";

            serviceRequest.Pedido = ObterPedido(order);

            serviceRequest.Comprador = ObterComprador(order);

            serviceRequest.Boleto = ObterBoleto(order);

            serviceRequest.TokenRequestUrlConfirmacaoPagamento = order.OrderGuid.ToString();

            return serviceRequest;
        }

        private BoletoRequest ObterBoleto(Order order)
        {
            var boletoRequest = new BoletoRequest();

            boletoRequest.Beneficiario = TruncarTamanhoMaximo(_bradescoPaymentSettings.NomeSacado, 150);

            boletoRequest.CarteiraCobranca = _bradescoPaymentSettings.Carteira; 
            boletoRequest.NossoNumero = order.Id.ToString();

            boletoRequest.DataEmissao = DateTime.Now.Date;

            boletoRequest.DataVencimento = DateTime.Now.Date.AddDays(_bradescoPaymentSettings.NumeroDiasAdicionaisVencimentoBoleto); 
            ///TODO: Pedir ao cliente escolher, sugestão configuração 

            boletoRequest.ValorTitulo = order.OrderTotal;

            boletoRequest.UrlLogotipo = ObterUrlLogo(); 
            boletoRequest.MensagemCabecalho = string.Empty;

            boletoRequest.TipoRenderizacao = 0; ///TODO: Configurar pelo nop

            boletoRequest.Instrucoes = ObterInstrucoes();

            boletoRequest.Registro = ObterRegistro();

            return boletoRequest;

        }

        private string ObterUrlLogo()
        {
            var logo = string.Empty;
            var logoPictureId = _storeInformationSettings.LogoPictureId;
            if (logoPictureId > 0)
            {
                logo = _pictureService.GetPictureUrl(logoPictureId, showDefaultPicture: false);
            }
            if (String.IsNullOrEmpty(logo))
            {
                //use default logo
                logo = string.Format("{0}Themes/{1}/Content/images/logo.png", _webHelper.GetStoreLocation(), _themeContext.WorkingThemeName);
            }
            return logo;

        }

        private BoletoRegistroRequest ObterRegistro()
        {
            return new BoletoRegistroRequest();
        }

        private BoletoInstrucoesRequest ObterInstrucoes()
        {
            return new BoletoInstrucoesRequest();
        }

        private Comprador ObterComprador(Order order)
        {

            var number = string.Empty;
            var complement = string.Empty;
            var cnpjcpf = string.Empty;
            var nomeCompleto = string.Empty;

            GetCustomNumberAndComplement(order, out number, out complement, out cnpjcpf);

            nomeCompleto = GetBillingShippingFullName(order.BillingAddress);

            var comprador = new Comprador()
            {
                Nome = TruncarTamanhoMaximo(nomeCompleto, 40),
                Documento = TruncarTamanhoMaximo(cnpjcpf, 14),
                Endereco = ObterCompradorEndereco(order.BillingAddress, complement, number),
                IP = TruncarTamanhoMaximo(order.CustomerIp, 50),
                UserAgent = string.Empty ///TODO:Verificar se é possivel obter pelo nop
            };

            return comprador;

        }

        private CompradorEndereco ObterCompradorEndereco(Nop.Core.Domain.Common.Address address, string complemento, string numero)
        {
            return new CompradorEndereco()
            {
                Bairro = address.Address2,
                CEP = TruncarTamanhoMaximo(ObterApenasNumeros(address.ZipPostalCode),8),
                Cidade = TruncarTamanhoMaximo(address.City,50),
                Complemento = TruncarTamanhoMaximo(complemento, 20),
                Logradouro = TruncarTamanhoMaximo(address.Address1,70),
                Numero = numero,
                UF = address.StateProvince.Abbreviation
            };
        }

        private string GetBillingShippingFullName(Nop.Core.Domain.Common.Address address)
        {
            if (address == null)
            {
                throw new ArgumentNullException("address");
            }
            string firstName = address.FirstName;
            string lastName = address.LastName;
            string stringWithTwoOrMoreSpace = "";

            if (!string.IsNullOrWhiteSpace(firstName) && !string.IsNullOrWhiteSpace(lastName))
            {
                stringWithTwoOrMoreSpace = string.Format("{0} {1}", firstName, lastName);
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(firstName))
                {
                    stringWithTwoOrMoreSpace = firstName;
                }
                if (!string.IsNullOrWhiteSpace(lastName))
                {
                    stringWithTwoOrMoreSpace = lastName;
                }
            }


            string billingShippingFullName = this.RemoverEspacosIncorretos(stringWithTwoOrMoreSpace);

            return billingShippingFullName;
        }

        private void GetCustomNumberAndComplement(Order order, out string number, out string complement, out string cnpjcpf)
        {

            string customAttributes = order.BillingAddress.CustomAttributes;

            number = string.Empty;
            complement = string.Empty;
            cnpjcpf = string.Empty;

            if (!string.IsNullOrWhiteSpace(customAttributes))
            {
                var attributes = _addressAttributeParser.ParseAddressAttributes(customAttributes);

                for (int i = 0; i < attributes.Count; i++)
                {
                    var valuesStr = _addressAttributeParser.ParseValues(customAttributes, attributes[i].Id);

                    var attributeName = attributes[i].GetLocalized(a => a.Name, _workContext.WorkingLanguage.Id);

                    if (
                        attributeName.Equals("Número", StringComparison.InvariantCultureIgnoreCase) ||
                        attributeName.Equals("Numero", StringComparison.InvariantCultureIgnoreCase)
                        )
                    {
                        number = _addressAttributeParser.ParseValues(customAttributes, attributes[i].Id)[0];
                    }

                    if (attributeName.Equals("Complemento", StringComparison.InvariantCultureIgnoreCase))
                        complement = _addressAttributeParser.ParseValues(customAttributes, attributes[i].Id)[0];

                    if (attributeName.Equals("CPF/CNPJ", StringComparison.InvariantCultureIgnoreCase))
                        cnpjcpf = _addressAttributeParser.ParseValues(customAttributes, attributes[i].Id)[0];
                }

            }

            if (string.IsNullOrWhiteSpace(cnpjcpf))
                cnpjcpf = order.Customer.GetAttribute<string>(SystemCustomerAttributeNames.Fax);

            if (!string.IsNullOrWhiteSpace(cnpjcpf))
                cnpjcpf = ObterApenasNumeros(cnpjcpf);
        }

        private string RemoverEspacosIncorretos(string stringComDoisOuMaisEspacos)
        {
            string str = string.Empty;

            for (int i = 0; i < stringComDoisOuMaisEspacos.Length; i++)
            {
                if (stringComDoisOuMaisEspacos[i] == ' ')
                {
                    if (((i + 1) < stringComDoisOuMaisEspacos.Length) && (stringComDoisOuMaisEspacos[i + 1] != ' '))
                    {
                        str = str + stringComDoisOuMaisEspacos[i];
                    }
                }
                else
                {
                    str = str + stringComDoisOuMaisEspacos[i];
                }
            }
            return str.Trim();
        }

        private string ObterApenasNumeros(string stringValue)
        {
            var r = new Regex(@"\d+");

            var result = string.Empty;

            foreach (Match m in r.Matches(stringValue))
                result += m.Value;

            return result;
        }

        private string TruncarTamanhoMaximo(string stringValue, int tamanho)
        {

            if (string.IsNullOrWhiteSpace(stringValue))
                return null;

            if (tamanho <= 0)
                throw new NopException("O tamanho não pode ser menor ou igual a zero");

            var resultado = stringValue.Trim();

            resultado = RemoverEspacosIncorretos(resultado);

            if (resultado.Length > tamanho)
            {
                resultado = resultado.Substring(0, tamanho);
            }

            return resultado;
        }

        private Pedido ObterPedido(Order Order)
        {
            var pedido = new Pedido();

            pedido.Numero = Order.Id.ToString();
            pedido.Descricao = TruncarTamanhoMaximo(_bradescoPaymentSettings.NomeProdutoUnico, 255);
            pedido.Valor = Order.OrderTotal;

            return pedido;
        }

        private void AddOrderNote(string note, bool showNoteToCustomer, Order order, bool sendEmail = false)
        {
            var orderNote = new OrderNote();
            orderNote.CreatedOnUtc = DateTime.UtcNow;
            orderNote.DisplayToCustomer = showNoteToCustomer;
            orderNote.Note = note;
            order.OrderNotes.Add(orderNote);

            _orderService.UpdateOrder(order);

            //new order notification
            if (sendEmail)
            {
                //email
                _workflowMessageService.SendNewOrderNoteAddedCustomerNotification(
                    orderNote, _workContext.WorkingLanguage.Id);
            }
        }
    }
}

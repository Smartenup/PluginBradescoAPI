using Newtonsoft.Json;
using Nop.Core;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Core.Domain.Shipping;
using Nop.Plugin.Payments.BoletoBradescoAPI.Models.CheckOrder.Response;
using Nop.Plugin.Payments.BoletoBradescoAPI.Serializer;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Messages;
using Nop.Services.Orders;
using Nop.Services.Shipping;
using Nop.Services.Tasks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace Nop.Plugin.Payments.BoletoBradescoAPI
{
    public class BoletoBradescoPaymentAPIUpdateTask : ITask
    {

        private readonly IOrderService _orderService;
        private readonly IOrderProcessingService _orderProcessingService;
        private readonly BoletoBradescoAPIPaymentSettings _boletoBradescoAPIPaymentSettings;
        private readonly ILogger _logger;
        private readonly IWorkflowMessageService _workflowMessageService;
        private readonly IWorkContext _workContext;
        private readonly IShippingService _shippingService;

        public BoletoBradescoPaymentAPIUpdateTask(IOrderService orderService,
            IOrderProcessingService orderProcessingService,
            ILogger logger,
            BoletoBradescoAPIPaymentSettings boletoBradescoAPIPaymentSettings,
            IWorkflowMessageService workflowMessageService,
            IWorkContext workContext,
            IShippingService shippingService)
        {
            _orderService = orderService;
            _orderProcessingService = orderProcessingService;
            _boletoBradescoAPIPaymentSettings = boletoBradescoAPIPaymentSettings;
            _logger = logger;
            _workflowMessageService = workflowMessageService;
            _workContext = workContext;
            _shippingService = shippingService;
        }

        public void Execute()
        {
            IPagedList<Order> pedidosPendentes = ObterPedidosPendentes();

            foreach (var pedido in pedidosPendentes)
            {
                if (VerificarPedidoPago(pedido))
                {
                    _orderProcessingService.MarkOrderAsPaid(pedido);

                    AddOrderNote("Pagamento aprovado.", true, pedido);

                    AddOrderNote("Aguardando Impressão - Excluir esse comentário ao imprimir ", false, pedido);

                    if (_boletoBradescoAPIPaymentSettings.AdicionarNotaPrazoFabricaoEnvio)
                    {
                        string ordeNoteRecievedPayment = GetOrdeNoteRecievedPayment(pedido);

                        AddOrderNote(ordeNoteRecievedPayment, true, pedido, true);
                    }
                }
            }
        }

        private bool VerificarPedidoPago(Order order)
        {
            var token = ObterTokenAutenticacao();

            string urlGetConsultaPedido = ObterUrlConsultaPedido();

            var urlGetConsultaPedidosParamentros = string.Format(urlGetConsultaPedido,
                _boletoBradescoAPIPaymentSettings.NumeroLoja, token, order.Id);

            ResponseCheckOrder responseCheckOrderPedido = ObterCheckOrder(urlGetConsultaPedidosParamentros);

            foreach (var pedido in responseCheckOrderPedido.Pedidos)
            {
                if ( int.Parse(pedido.NumeroPedido) != order.Id)
                {
                    continue;
                }

                //21 Boleto Pago igual 
                //22 Boleto Pago a Menor
                //23 Boleto Pago a Maior

                if (pedido.Status.Equals("21"))
                {
                    return true;
                }

                if ( pedido.Status.Equals("22") )
                {
                    AddOrderNote("Boleto Pago Menor - Por favor entrar em contato para verificar a diferença", true, order, true);
                    return false;
                }
                if (pedido.Status.Equals("23"))
                {
                    AddOrderNote("Boleto Pago Maior - Por favor entrar em contato para verificar a diferença", true, order, true);
                    return true;
                }

            }

            return false;
        }


        private string ObterTokenAutenticacao()
        {
            string urlGetAutenticacao = ObterUrlBrasdecoAutenticacao() + _boletoBradescoAPIPaymentSettings.NumeroLoja;

            ResponseCheckOrder checkOrder = ObterCheckOrder(urlGetAutenticacao);

            return checkOrder.Token.TokenAutenticacao;
        }

        private ResponseCheckOrder ObterCheckOrder(string urlGet)
        {
            var mediaType = "application/json";
            var charSet = "UTF-8";

            var request = (HttpWebRequest)WebRequest.Create(urlGet);

            //Configuracao do cabecalho da requisicao
            request.Method = "GET";
            request.ContentType = mediaType + ";charset=" + charSet;
            request.Accept = mediaType;
            request.Headers.Add(HttpRequestHeader.AcceptCharset, charSet);

            //Credenciais de Acesso
            string header = _boletoBradescoAPIPaymentSettings.EmailAdministrativo + ":" +
                _boletoBradescoAPIPaymentSettings.ChaveSeguranca;

            string headerBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(header));

            request.Headers.Add(HttpRequestHeader.Authorization, "Basic " + headerBase64);

            ResponseCheckOrder checkOrder = null;

            //Obtem resposta do servidor
            var response = (HttpWebResponse)request.GetResponse();


            if (response.StatusCode != HttpStatusCode.OK && response.StatusCode != HttpStatusCode.Created)
                throw new Exception("Retorno da requisicao dif 200/201. HttpStatusCode: " +
                    response.StatusCode.ToString());

            var jsonSettings = new JsonSerializerSettings() { DateFormatString = "dd/MM/yyyy HH:mm:ss" };

            jsonSettings.Converters.Add(new DecimalConverter());

            //Deserialização para o objeto CheckOrder
            using (var jsonTextReader = new JsonTextReader(new StreamReader(response.GetResponseStream())))
            {
                var jsonSerializer = JsonSerializer.Create(jsonSettings);

                checkOrder = jsonSerializer.Deserialize<ResponseCheckOrder>(jsonTextReader);
            }

            response.Close();

            if (checkOrder == null)
                throw new NopException("Não foi encontrado retorno da pesquisa de pedidos do Bradesco");

            if (int.Parse(checkOrder.Status.Codigo) != 0)
            {
                _logger.Error(string.Format("Codigo - {0} Mensagem - {1} Url - {2}", checkOrder.Status.Codigo, checkOrder.Status.Mensagem, urlGet));
            }

            return checkOrder;
        }

        private string ObterUrlConsultaPedido()
        {
            if (_boletoBradescoAPIPaymentSettings.AmbienteProducao)
                return "https://meiosdepagamentobradesco.com.br/SPSConsulta/GetOrderById/{0}?token={1}&orderId={2}";

            return "https://homolog.meiosdepagamentobradesco.com.br/SPSConsulta/GetOrderById/{0}?token={1}&orderId={2}";
        }
        
        private string ObterUrlBrasdecoAutenticacao()
        {
            if (_boletoBradescoAPIPaymentSettings.AmbienteProducao)
                return "https://meiosdepagamentobradesco.com.br/SPSConsulta/Authentication/";

            return "https://homolog.meiosdepagamentobradesco.com.br/SPSConsulta/Authentication/";
        }

        private IPagedList<Order> ObterPedidosPendentes()
        {
            var lstOrderStatus = new List<int>();
            var lstPaymentStatus = new List<int>();

            lstOrderStatus.Add((int)OrderStatus.Pending);
            lstPaymentStatus.Add((int)PaymentStatus.Pending);

            IPagedList<Order> orders = _orderService.SearchOrders(
                paymentMethodSystemName: "Payments.BoletoBradescoAPI", 
                osIds: lstOrderStatus, 
                psIds: lstPaymentStatus);

            return orders;
        }



        //Adiciona anotaçoes ao pedido
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

        
        private string GetOrdeNoteRecievedPayment(Nop.Core.Domain.Orders.Order order)
        {
            Nop.Core.Domain.Orders.OrderItem orderItem;
            int? biggestAmountDays;

            DeliveryDate biggestDeliveryDate = GetBiggestDeliveryDate(order, out biggestAmountDays, out orderItem);

            DateTime dateShipment = DateTime.Now.AddWorkDays(biggestAmountDays.Value);

            var str = new StringBuilder();

            str.AppendLine("Recebemos a liberação do pagamento pelo Bradesco e será dado andamento no seu pedido.");
            str.AppendLine();
            str.AppendFormat("Lembramos que o maior prazo é da fabricante {0} de {1}",
                orderItem.Product.ProductManufacturers.FirstOrDefault().Manufacturer.Name,
                biggestDeliveryDate.GetLocalized(dd => dd.Name));
            str.AppendLine();
            str.AppendLine();
            str.AppendLine("*OBS: Caso o seu pedido tenha produtos com prazos diferentes, o prazo de entrega a ser considerado será o maior.");
            str.AppendLine();

            str.AppendFormat("Data máxima para postar nos correios: {0}", dateShipment.ToString("dd/MM/yyyy"));
            str.AppendLine();

            if (order.ShippingMethod.Contains("PAC") || order.ShippingMethod.Contains("SEDEX"))
            {
                try
                {
                    var shippingOption = _shippingService.GetShippingOption(order);

                    str.AppendFormat("Correios: {0} - {1} após a postagem", shippingOption.Name, shippingOption.Description);
                    str.AppendLine();
                }
                catch (Exception ex)
                {
                    _logger.Error("Erro no calculo do frete pela ordem", ex);
                }
                finally
                {
                    str.AppendLine();
                }
            }

            return str.ToString();

        }



        
        private DeliveryDate GetBiggestDeliveryDate(Nop.Core.Domain.Orders.Order order, out int? biggestAmountDays,
            out Nop.Core.Domain.Orders.OrderItem orderItem)
        {

            DeliveryDate deliveryDate = null;

            biggestAmountDays = 0;

            orderItem = null;

            foreach (var item in order.OrderItems)
            {
                var deliveryDateItem = _shippingService.GetDeliveryDateById(item.Product.DeliveryDateId);

                string deliveryDateText = deliveryDateItem.GetLocalized(dd => dd.Name);

                int? deliveryBigestInteger = GetBiggestInteger(deliveryDateText);

                if (deliveryBigestInteger.HasValue)
                {
                    if (deliveryBigestInteger.Value > biggestAmountDays)
                    {
                        biggestAmountDays = deliveryBigestInteger.Value;
                        deliveryDate = deliveryDateItem;
                        orderItem = item;
                    }
                }
            }


            return deliveryDate;
        }
        
        private int? GetBiggestInteger(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            var integerResultsList = new List<int>();
            string integerSituation = string.Empty;
            int integerPosition = 0;

            for (int i = 0; i < text.Length; i++)
            {
                if (int.TryParse(text[i].ToString(), out integerPosition))
                {
                    integerSituation += text[i].ToString();
                }
                else
                {
                    if (!string.IsNullOrEmpty(integerSituation))
                    {
                        integerResultsList.Add(int.Parse(integerSituation));
                        integerSituation = string.Empty;
                    }
                }
            }

            int integerResult = 0;
            foreach (var item in integerResultsList)
            {
                if (item > integerResult)
                    integerResult = item;
            }


            return integerResult;
        }
    }
    public static class DateTimeExtensions
    {
        public static DateTime AddWorkDays(this DateTime date, int workingDays)
        {
            return date.GetDates(workingDays < 0)
                .Where(newDate =>
                    (newDate.DayOfWeek != DayOfWeek.Saturday &&
                     newDate.DayOfWeek != DayOfWeek.Sunday &&
                     !newDate.IsHoliday()))
                .Take(Math.Abs(workingDays))
                .Last();
        }

        private static IEnumerable<DateTime> GetDates(this DateTime date, bool isForward)
        {
            while (true)
            {
                date = date.AddDays(isForward ? -1 : 1);
                yield return date;
            }
        }

        public static bool IsHoliday(this DateTime date)
        {
            return false;
        }
    }
}

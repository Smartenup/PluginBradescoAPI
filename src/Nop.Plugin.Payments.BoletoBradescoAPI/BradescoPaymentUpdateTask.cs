using Nop.Core;
using Nop.Core.Domain.Shipping;
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
using System.Web.Mvc;
using System.Xml.Linq;

namespace Nop.Plugin.Payments.Bradesco
{
    public class BradescoPaymentUpdateTask : ITask
    {

        private readonly IOrderService _orderService;
        private readonly IOrderProcessingService _orderProcessingService;
        private readonly BradescoPaymentSettings _bradescoPaymentSettings;
        private readonly ILogger _logger;
        private readonly IWorkflowMessageService _workflowMessageService;
        private readonly IWorkContext _workContext;
        private readonly IShippingService _shippingService;

        public BradescoPaymentUpdateTask(IOrderService orderService, 
            IOrderProcessingService orderProcessingService,
            ILogger logger,
            BradescoPaymentSettings bradescoPaymentSettings,
            IWorkflowMessageService workflowMessageService,
            IWorkContext workContext,
            IShippingService shippingService
            )
        {
            _orderService = orderService;
            _orderProcessingService = orderProcessingService;
            _bradescoPaymentSettings = bradescoPaymentSettings;
            _logger = logger;
            _workflowMessageService = workflowMessageService;
            _workContext = workContext;
            _shippingService = shippingService;
        }

        public void Execute()
        {
            
            try
            {
                ///Obtem as parametrizações para acesso ao boleto bradesco

                string protocolo = @"https://";

                string link_Boleto = protocolo + _bradescoPaymentSettings.NomeServidorBradesco + "/sepsmanager/ArqRetBradescoBoleto_XML2_dados.asp?"
                    + "merchantid=" + _bradescoPaymentSettings.NumeroLoja
                    + "&data=" + DateTime.Now.ToString("dd/MM/yyyy")
                    + "&Manager=" + _bradescoPaymentSettings.Manager
                    + "&passwd=" + _bradescoPaymentSettings.SenhaManager
                    + "&NumOrder=";

                if (_bradescoPaymentSettings.ModoDebug)
                    _logger.Information(link_Boleto);

                var lstOrderStatus = new List<int>();
                var lstPaymentStatus = new List<int>();

                lstOrderStatus.Add((int)Core.Domain.Orders.OrderStatus.Pending);
                lstPaymentStatus.Add((int)Core.Domain.Payments.PaymentStatus.Pending);

                var orders = _orderService.SearchOrders(paymentMethodSystemName: "Payments.Bradesco", osIds: lstOrderStatus , psIds: lstPaymentStatus );


                foreach (var order in orders)
                {
                    Stream dataStream = null;
                    WebResponse response = null;

                    if (order.PaymentStatusId != (int)Nop.Core.Domain.Payments.PaymentStatus.Pending)
                        continue;
                    try
                    {

                        //WebRequest request = WebRequest.Create("https://mup.comercioeletronico.com.br/sepsmanager/ArqRetBradescoBoleto_XML2_dados.asp?merchantid=100004933&data=20/06/2016&Manager=adm_imp4933&passwd=uiskas7680&NumOrder=9447");

                        string link_boleto_ordem = string.Concat(link_Boleto, order.Id.ToString());

                        if (_bradescoPaymentSettings.ModoDebug)
                            _logger.Information(link_boleto_ordem);

                        var request = WebRequest.Create(link_boleto_ordem);
                        request.Credentials = CredentialCache.DefaultCredentials;

                        response = request.GetResponse();
                        dataStream = response.GetResponseStream();

                        var document = XDocument.Load(dataStream);

                        IEnumerable<XAttribute> attributeList = document.Element("DadosFechamento").Element("Bradesco").Element("Pedido").Attributes();

                        foreach (XAttribute att in attributeList)
                        {
                            if (_bradescoPaymentSettings.ModoDebug)
                                _logger.Information(att.Name + " = " + att.Value );

                            if (att.Name == "LinhaDigitavel")
                            {
                                var notaLinhaDigitavel = order.OrderNotes.Where(note => note.Note.Contains("Linha Digitável Boleto: "));
                                
                                ///Caso não tenha anotação da linha do boleto é adicionado 
                                if (notaLinhaDigitavel.Count() == 0)
                                {
                                    AddOrderNote("Linha Digitável Boleto: " + att.Value , true, order, true);
                                }
                            }

                            if (att.Name == "Status")
                            {
                                ///15.........................Boleto Pago  
                                ///21.........................Boleto Pago Igual (Boleto Bancário com retorno para a loja) 
                                if (att.Value.Equals("15") || att.Value.Equals("21"))
                                {
                                    _orderProcessingService.MarkOrderAsPaid(order);

                                    AddOrderNote("Pagamento aprovado.", true, order);

                                    AddOrderNote("Aguardando Impressão - Excluir esse comentário ao imprimir ", false, order);

                                    if (_bradescoPaymentSettings.AdicionarNotaPrazoFabricaoEnvio)
                                    {
                                        AddOrderNote(GetOrdeNoteRecievedPayment(order), true, order, true);
                                    }
                                    
                                }
                                ///22.........................Boleto Pago Menor (Boleto Bancário com retorno para a loja) 
                                if (att.Value.Equals("22"))
                                {
                                    AddOrderNote("Boleto Pago Menor - Por favor entrar em contato para verificar a diferença", true, order, true);
                                }
                                ///23.........................Boleto Pago Maior (Boleto Bancário com retorno para a loja)
                                if (att.Value.Equals("23"))
                                {
                                    _orderProcessingService.MarkOrderAsPaid(order);

                                    AddOrderNote("Boleto Pago Maior - Por favor entrar em contato para verificar a diferença", true, order, true);

                                    AddOrderNote("Aguardando Impressão - Excluir esse comentário ao imprimir ", false, order);

                                    if (_bradescoPaymentSettings.AdicionarNotaPrazoFabricaoEnvio)
                                    {
                                        AddOrderNote(GetOrdeNoteRecievedPayment(order), true, order, true);
                                    }

                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Error("Erro na atualização de status de boleto bradesco, orderID " + order.Id.ToString(), ex);
                    }
                    finally
                    {
                        dataStream.Close();
                        response.Close();
                        
                    }
                }
            }
            catch(Exception ex)
            {
                _logger.Error("Erro na execução de atualização de status de boleto bradesco", ex, null);
            }
        }

        [NonAction]
        //Adiciona anotaçoes ao pedido
        private void AddOrderNote(string note, bool showNoteToCustomer, Nop.Core.Domain.Orders.Order order, bool sendEmail = false)
        {
            var orderNote = new Nop.Core.Domain.Orders.OrderNote();
            orderNote.CreatedOnUtc = DateTime.UtcNow;
            orderNote.DisplayToCustomer = showNoteToCustomer;
            orderNote.Note = note;
            order.OrderNotes.Add(orderNote);

            this._orderService.UpdateOrder(order);

            //new order notification
            if (sendEmail)
            {
                //email
                _workflowMessageService.SendNewOrderNoteAddedCustomerNotification(
                    orderNote, _workContext.WorkingLanguage.Id);
            }
        }

        [NonAction]
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
                biggestDeliveryDate.GetLocalized(dd => dd.Name)) ;
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



        [NonAction]
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
        [NonAction]
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

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Net;
using System.Threading.Tasks;
using TickMeHelpers;
using TickMeHelpers.ApiModels;

namespace TickMeTickets.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TicketsController : ControllerBase
    {
        private TicketManagement TicketManager
        {
            get
            {
                if(_ticketManager == null)
                {
                    _ticketManager = new TicketManagement(_configuration);
                }
                return _ticketManager;
            }
        }
        private TicketManagement _ticketManager = null;

        private IConfiguration _configuration;
        public TicketsController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // GET api/tickets
        [HttpGet]
        public string Get()
        {
            Response.StatusCode = (int)HttpStatusCode.OK;
            return "{'status':'Service Up'}";
        }
        // POST api/tickets
        [HttpPost]
        public async Task<string> Post([FromBody] string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return "{'error':'Bad Request'}";
            }
            var buyModel = JsonConvert.DeserializeObject<TicketBuyModel>(value);
            //var buyModel = value;
            if(buyModel == null|| buyModel.EventId == Guid.Empty || buyModel.UserId == Guid.Empty || string.IsNullOrWhiteSpace(buyModel.PaymentData))
            {
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return "{'error':'Bad Request'}";
            }
            var paymentData = JsonConvert.DeserializeObject<PaymentData>(buyModel.PaymentData);
            if(paymentData == null)
            {
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return "{'error':'Bad Request'}";
            }
            try
            {
                var manager = TicketManager;
                var ticket = await manager.IssueEventTicket(buyModel.EventId, buyModel.UserId, paymentData.Value, paymentData.TransactionData);
                Response.StatusCode = (int)HttpStatusCode.OK;
                return ticket.ToString();
            }
            catch
            {
                Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                return "{'error':'Server couldn't process ticket'}";
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using TickMe.Models;
using TickMeHelpers;
using TickMeHelpers.ApiModels;

namespace TickMe.Controllers
{
    public class EventController : Controller
    {
        private IConfiguration Configuration = null;
        public EventController(IConfiguration config) : base()
        {
            Configuration = config;
        }

        private EventManagement EventManager
        {
            get
            {
                return _eventManager ?? (_eventManager = new EventManagement(Configuration));
            }
        }
        private EventManagement _eventManager = null;

        private TicketManagement TicketManager
        {
            get
            {
                return _ticketManager ?? (_ticketManager = new TicketManagement(Configuration));
            }
        }
        private TicketManagement _ticketManager = null;

        private UserManagement UserManager
        {
            get
            {
                return _userManager ?? (_userManager = new UserManagement(Configuration));
            }
        }
        private UserManagement _userManager = null;

        // GET: Event
        public async Task<ActionResult> Index()
        {
            var allEvents = await EventManager.List();
            return View("~/Views/Events/Index.cshtml", allEvents);
        }

        // GET: Event/MyEvents/5
        [Authorize]
        public async Task<ActionResult> MyEvents()
        {
            var viewModel = new List<MyEventsViewModel>();

            var user = TickMeHelpers.User.FromUser(User);
            var savedUser = await UserManager.GetUserByAuthId(user);
            var tickets = await TicketManager.GetUserTickets(savedUser.Id);
            foreach (var ticket in tickets)
            {
                var evnt = await EventManager.Get(ticket.EventId);
                var paymentData = JsonConvert.DeserializeObject<PaymentData>(ticket.PaymentData);
                var paymentDate = "Unavailable information";
                try
                {
                    dynamic pdata = JObject.Parse(paymentData.TransactionData);
                    paymentDate = pdata.TransactionDate;
                }
                catch
                {
                    paymentDate = "Unavailable information";
                }
                var item = new MyEventsViewModel
                {
                    Ticket = ticket,
                    EventName = evnt.Title,
                    StartMoment = evnt.StartMoment,
                    Duration = evnt.Duration,
                    PaymentDate = paymentDate
                };
                viewModel.Add(item);
            }
            return View("~/Views/Events/MyEvents.cshtml", viewModel);
        }

        // GET: Event/ViewTicket/5
        [Authorize]
        public async Task<ActionResult> ViewTicket(Guid id)
        {
            var buyModel = await PrepareTicketBuyViewModelForExistingTicket(id);
            return View("~/Views/Events/ViewTicket.cshtml", buyModel);
        }

        // GET: Event/Buy/5
        [Authorize]
        public async Task<ActionResult> Buy(Guid id)
        {
            var buyModel = await PrepareTicketBuyViewModel(id);
            return View("~/Views/Events/Buy.cshtml", buyModel);
        }

        private async Task<TicketBuyViewModel> PrepareTicketBuyViewModel(Guid id)
        {
            var user = TickMeHelpers.User.FromUser(User);
            var savedUser = await UserManager.GetUserByAuthId(user);
            if (savedUser == null)
            {
                user.Id = Guid.NewGuid();
                await UserManager.Upsert(user);
            }
            var evnt = await EventManager.Get(id);
            var buyModel = new TicketBuyViewModel()
            {
                evnt = evnt,
                user = user,
                ticket = new Ticket(),
                paymentData = new PaymentData()
            };
            return buyModel;
        }

        private async Task<TicketBuyViewModel> PrepareTicketBuyViewModelForExistingTicket(Guid id)
        {
            var user = TickMeHelpers.User.FromUser(User);
            var savedUser = await UserManager.GetUserByAuthId(user);
            if (savedUser == null)
            {
                user.Id = Guid.NewGuid();
                await UserManager.Upsert(user);
            }
            var ticket = await TicketManager.Get(id);

            var evnt = await EventManager.Get(ticket.EventId);
            var buyModel = new TicketBuyViewModel()
            {
                evnt = evnt,
                user = user,
                ticket = ticket,
                paymentData = null
            };
            return buyModel;
        }

        // POST: Event/Buy
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Buy(IFormCollection collection)
        {
            try
            {
                var buyModel = await PrepareTicketBuyViewModel(Guid.Parse(collection["evnt.Id"]));
                if (buyModel.evnt == null || buyModel.evnt.TotalAvailableTickets < 1)
                {
                    return View("Error");
                }
                var paymentData = new PaymentData()
                {
                    CardNumber = collection["paymentData.CardNumber"],
                    NameOnCard = collection["paymentData.NameOnCard"],
                    ValidMonth = Int32.Parse(collection["paymentData.ValidMonth"]),
                    ValidYear = Int32.Parse(collection["paymentData.ValidYear"]),
                    SecurityCode = collection["paymentData.SecurityCode"],
                    Value = buyModel.evnt.Price
                };
                paymentData = await MakePayment(paymentData);

                buyModel.paymentData = paymentData;
                if (buyModel.user.Id == Guid.Empty)
                {
                    buyModel.user = await UserManager.GetUserByAuthId(buyModel.user);
                }
                if (paymentData.TransactionSuccessful)
                {
                    var ticketBuyModel = new TicketBuyModel()
                    {
                        EventId = buyModel.evnt.Id,
                        UserId = buyModel.user.Id,
                        PaymentData = paymentData.ToString()
                    };
                    var ticket = await IssueTicket(ticketBuyModel);
                    return RedirectToAction("ViewTicket", new { id = ticket.Id });
                }
                return View("Error");
            }
            catch
            {
                return View("Error");
            }
        }

        // GET: Event/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Event/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(IFormCollection collection)
        {
            try
            {
                // TODO: Add insert logic here

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: Event/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: Event/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, IFormCollection collection)
        {
            try
            {
                // TODO: Add update logic here

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: Event/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: Event/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id, IFormCollection collection)
        {
            try
            {
                // TODO: Add delete logic here

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        public async Task<Ticket> IssueTicket(TicketBuyModel buyModel)
        {
            using (var httpClientHandler = new HttpClientHandler())
            {
                httpClientHandler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => { return true; };
                using (var client = new HttpClient(httpClientHandler))
                {
                    var request = new HttpRequestMessage
                    {
                        RequestUri = new Uri(Configuration["TicketApiUrl"]),
                        Method = HttpMethod.Post
                    };
                    request.Content = new StringContent(JsonConvert.SerializeObject(buyModel));
                    request.Content.Headers.ContentType = new MediaTypeHeaderValue("text/plain");

                    //request.Content.Headers.ContentType = new MediaTypeHeaderValue(parameters.ContentType);

                    var result = client.SendAsync(request).Result;
                    result.EnsureSuccessStatusCode();
                    return JsonConvert.DeserializeObject<Ticket>(await result.Content.ReadAsStringAsync());
                }
            }
        }

        public async Task<PaymentData> MakePayment(PaymentData paymentData)
        {
            using (var httpClientHandler = new HttpClientHandler())
            {
                httpClientHandler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => { return true; };
                using (var client = new HttpClient(httpClientHandler))
                {
                    var request = new HttpRequestMessage
                    {
                        RequestUri = new Uri(Configuration["PaymentApiUrl"]),
                        Method = HttpMethod.Post
                    };
                    request.Content = new StringContent(JsonConvert.SerializeObject(paymentData));
                    request.Content.Headers.ContentType = new MediaTypeHeaderValue("text/plain");

                    var result = client.SendAsync(request).Result;
                    result.EnsureSuccessStatusCode();
                    return JsonConvert.DeserializeObject<PaymentData>(await result.Content.ReadAsStringAsync());
                }
            }
        }
    }
}
using Microsoft.Store.PartnerCenter.Enumerators;
using Microsoft.Store.PartnerCenter.Models;
using Microsoft.Store.PartnerCenter.Models.Customers;
using Microsoft.Store.PartnerCenter.Models.Query;
using Microsoft.Store.PartnerCenter.Models.ServiceCosts;
using Microsoft.Store.PartnerCenter.Models.Subscriptions;
using Microsoft.Store.PartnerCenter.RequestContext;
using Microsoft.Store.PartnerCenter.Storefront.BusinessLogic;
using Microsoft.Store.PartnerCenter.Storefront.Filters;
using Microsoft.Store.PartnerCenter.Storefront.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace Microsoft.Store.PartnerCenter.Storefront.Controllers
{
    [Filters.Mvc.PortalAuthorize(UserRole = UserRole.Partner)]
    public class AdminUserController : Controller
    {
      
        protected CustomerPortalPrincipal Principal => HttpContext.User as CustomerPortalPrincipal;
        // GET: AdminUser

        public async Task<ActionResult> Index()
        {
            // retrieve the list of customers from Partner Center.             
            IAggregatePartner sdkClient = ApplicationDomain.Instance.PartnerCenterClientUser;
            List<Customer> allCustomers = new List<Customer>();

            // create a customer enumerator which will aid us in traversing the customer pages
            IResourceCollectionEnumerator<SeekBasedResourceCollection<Customer>> customersEnumerator = sdkClient.Enumerators.Customers.Create(sdkClient.Customers.Query(QueryFactory.Instance.BuildIndexedQuery(100)));

            while (customersEnumerator.HasValue)
            {
                foreach (Customer c in customersEnumerator.Current.Items)
                {
                    allCustomers.Add(c);
                }

                await customersEnumerator.NextAsync().ConfigureAwait(false);
            }
            var accountBalance = await sdkClient.Invoices.Summary.GetAsync().ConfigureAwait(false);
            ViewBag.AccountBalance = accountBalance.BalanceAmount.ToString();
            ViewBag.PaymentDeals = accountBalance.LastPaymentAmount.ToString();
            ViewBag.AccountDetails = accountBalance.Details;

            ViewBag.CustomerCount = allCustomers.Count();
            return View();
        }

        public async Task<ActionResult>GetMicrosoftOffers()
        {
            var MicrosoftOffer= await ApplicationDomain.Instance.OffersRepository.RetrieveMicrosoftOffersAsync().ConfigureAwait(false);
            
            
            return View(MicrosoftOffer); 
        }
        public async Task<ActionResult> GetCustomers()
        {
            var customers = await ApplicationDomain.Instance.PreApprovedCustomersRepository.RetrieveCustomerDetailsAsync().ConfigureAwait(false);
            return View(customers);
        }

        public async Task<ActionResult> GetFile(string id)
        {
            var files = await GetManagedSubscriptions(id).ConfigureAwait(false);
            //ViewBag.file = files;
            POSEntities db = new POSEntities();
           // Table table = new Table()
            List< Table> tbl = new List<Table>();
            
            foreach (var item in files.PartnerManagedSubscriptions)
            {
                tbl.Add(new Table
                {
                   
                    CustomerName = item.CustomerName,
                    CreationDate = item.CreationDate,
                    ExpiryDate = item.ExpiryDate,
                    SubcriptionFriendlyName = item.SubscriptionFriendlyName,
                    OfferName = item.OfferName,
                    PartnerCenterOrderId = item.PartnerCenterOrderId,
                    ChargeType = item.ChargeType,
                    UnitPrice = item.UnitPrice,
                    Quantity = item.Quantity,
                   // PreTaxTotal = item.PretaxTotal,
                   // AfterTaxTotal = item.Total,
                   // InvoiceNumber = item.InvoiceNumber

                });
                
            }
            db.Tables.AddRange(tbl);
            db.SaveChanges();
            return RedirectToAction("GetCustomers");
        }
        

        public ActionResult Files()
        {
            POSEntities db = new POSEntities();
            var files = db.Tables.ToList();
            ViewBag.allFiles = files;
            return View();
        }

        private async Task<ManagedSubscriptionsViewModel> GetManagedSubscriptions(string clientId)
        {
            DateTime startTime = DateTime.Now;

          
            CultureInfo responseCulture = new CultureInfo(ApplicationDomain.Instance.PortalLocalization.Locale);

           
            IPartner localeSpecificPartnerCenterClient = ApplicationDomain.Instance.PartnerCenterClientUser.With(RequestContextFactory.Instance.Create(ApplicationDomain.Instance.PortalLocalization.OfferLocale));
             var customerAllSubscriptions = 
                 await localeSpecificPartnerCenterClient.Customers.ById(clientId).Subscriptions.GetAsync().ConfigureAwait(false);
            var customer = await localeSpecificPartnerCenterClient.Customers.ById(clientId).Profiles.Billing.GetAsync().ConfigureAwait(false);
            var orders = await localeSpecificPartnerCenterClient.Customers.ById(clientId).Orders.GetAsync().ConfigureAwait(false);
          
            //var ascOrders = orders.Items.OrderByDescending(x => x.Id).ToArray();
            //IEnumerable<CustomerSubscriptionEntity> customerSubscriptions = await ApplicationDomain.Instance.CustomerSubscriptionsRepository.RetrieveAsync(clientId).ConfigureAwait(false);

            List<PartnerSubscriptionModel> partnerManagedSubscriptions = new List<PartnerSubscriptionModel>();
          
            foreach (var customerSubscriptionFromPC in customerAllSubscriptions.Items)
            {
               // var orderDetails = ascOrders.FirstOrDefault(x => x.Id == customerSubscriptionFromPC.OrderId.ToLower() || x.Id == customerSubscriptionFromPC.OrderId.ToLower());
               


                    PartnerSubscriptionModel partnerSubscription = new PartnerSubscriptionModel()
                    {
                        CustomerName = customer.CompanyName,
                        CreationDate = customerSubscriptionFromPC.CreationDate.ToString(),
                        ExpiryDate = customerSubscriptionFromPC.CommitmentEndDate.ToString("d", responseCulture),
                        SubscriptionFriendlyName = customerSubscriptionFromPC.FriendlyName,
                        OfferName = customerSubscriptionFromPC.OfferName,
                        PartnerCenterOrderId = customerSubscriptionFromPC.OrderId,
                        ChargeType = customerSubscriptionFromPC.BillingCycle.ToString(),
                       // UnitPrice = customerSubscriptionFromPC.UnitPrice.ToString(),
                        Quantity = customerSubscriptionFromPC.Quantity.ToString(),
                       // InvoiceNumber = customerSubscriptionFromPC.n,




                    };
                    partnerManagedSubscriptions.Add(partnerSubscription);

                

               

              




            }

            ManagedSubscriptionsViewModel managedSubscriptions = new ManagedSubscriptionsViewModel()
            {
               
                PartnerManagedSubscriptions = partnerManagedSubscriptions.OrderByDescending(partnerManagedSubscription => partnerManagedSubscription.CreationDate)


            };

            // Capture the request for customer managed subscriptions and partner managed subscriptions for analysis.
            Dictionary<string, string> eventProperties = new Dictionary<string, string> { { "CustomerId", clientId } };

            // Track the event measurements for analysis.
            Dictionary<string, double> eventMetrics = new Dictionary<string, double> { { "ElapsedMilliseconds", DateTime.Now.Subtract(startTime).TotalMilliseconds }/*, { "CustomerManagedSubscriptions", customerManagedSubscriptions.Count }*/, { "PartnerManagedSubscriptions", partnerManagedSubscriptions.Count } };

            ApplicationDomain.Instance.TelemetryService.Provider.TrackEvent("GetManagedSubscriptions", eventProperties, eventMetrics);

            return managedSubscriptions;
        }

        /// <summary>
        /// Retrieves the localized status type string. 
        /// </summary>
        /// <param name="statusType">The subscription status type.</param>
        /// <returns>Localized Operation Type string.</returns>
        private static string GetStatusType(SubscriptionStatus statusType)
        {
            switch (statusType)
            {
                case SubscriptionStatus.Active:
                    return Resources.SubscriptionStatusTypeActive;
                case SubscriptionStatus.Deleted:
                    return Resources.SubscriptionStatusTypeDeleted;
                case SubscriptionStatus.None:
                    return Resources.SubscriptionStatusTypeNone;
                case SubscriptionStatus.Suspended:
                    return Resources.SubscriptionStatusTypeSuspended;
                default:
                    return string.Empty;
            }
        }



    }
}
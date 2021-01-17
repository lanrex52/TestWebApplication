
using Microsoft.Store.PartnerCenter.Models;
using Microsoft.Store.PartnerCenter.Models.Orders;
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
    public class SubscriptionsController : Controller
    {

        protected CustomerPortalPrincipal Principal => HttpContext.User as CustomerPortalPrincipal;
        // GET: Subscriptions
        public async Task<ActionResult> Index( string id)
        {
            var files = await GetManagedSubscriptions(id).ConfigureAwait(false);
            ViewBag.allFiles = files;
            return View();
        }

        public async Task<ActionResult> ServiceCosts(string id)
        {
            var serviceCosts = await GetManagedSubscriptionsDetails(id).ConfigureAwait(false);
            ViewBag.viewmodel = serviceCosts;

            return View();
        }
        public async Task<ActionResult> OrderHistory(string id)
        {
            var orderdetails = await OrderManagedDetails(id).ConfigureAwait(false);

            ViewBag.orderDetails = orderdetails;
           

            return View();
        }

        private async Task<ManagedSubscriptionsViewModel> GetManagedSubscriptionsDetails(string clientId)
        {
            DateTime startTime = DateTime.Now;


            CultureInfo responseCulture = new CultureInfo(ApplicationDomain.Instance.PortalLocalization.Locale);


            IPartner localeSpecificPartnerCenterClient = ApplicationDomain.Instance.PartnerCenterClientUser.With(RequestContextFactory.Instance.Create(ApplicationDomain.Instance.PortalLocalization.OfferLocale));
            var customerAllSubscriptions =
               await localeSpecificPartnerCenterClient.Customers.ById(clientId).ServiceCosts.ByBillingPeriod(ServiceCostsBillingPeriod.MostRecent).LineItems.GetAsync().ConfigureAwait(false);

            IEnumerable<CustomerSubscriptionEntity> customerSubscriptions = await ApplicationDomain.Instance.CustomerSubscriptionsRepository.RetrieveAsync(clientId).ConfigureAwait(false);

            List<PartnerSubscriptionModel> partnerManagedSubscriptions = new List<PartnerSubscriptionModel>();

            foreach (var customerSubscriptionFromPC in customerAllSubscriptions.Items)
            {



                PartnerSubscriptionModel partnerSubscription = new PartnerSubscriptionModel()
                {
                    CustomerName = customerSubscriptionFromPC.CustomerName,
                    CreationDate = customerSubscriptionFromPC.StartDate.ToString("d", responseCulture),
                    ExpiryDate = customerSubscriptionFromPC.EndDate.ToString("d", responseCulture),
                    SubscriptionFriendlyName = customerSubscriptionFromPC.SubscriptionFriendlyName,
                    OfferName = customerSubscriptionFromPC.OfferName,
                    PartnerCenterOrderId = customerSubscriptionFromPC.OrderId,
                    ChargeType = customerSubscriptionFromPC.ChargeType,
                    UnitPrice = customerSubscriptionFromPC.UnitPrice.ToString(),
                    Quantity = customerSubscriptionFromPC.Quantity.ToString(),
                    PretaxTotal = customerSubscriptionFromPC.PretaxTotal.ToString(),
                    Total = customerSubscriptionFromPC.AfterTaxTotal.ToString(),
                    InvoiceNumber = customerSubscriptionFromPC.InvoiceNumber,




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

            ApplicationDomain.Instance.TelemetryService.Provider.TrackEvent("GetManagedSubscriptionsDetails", eventProperties, eventMetrics);

            return managedSubscriptions;
        }


        /// <summary>
        /// Gets the subscriptions managed by customers and partners
        /// </summary>
        /// <returns>returns managed subscriptions view model</returns>
        private async Task<ManagedSubscriptionsViewModel> GetManagedSubscriptions( string clientId)
        {
            DateTime startTime = DateTime.Now;

           

            // responseCulture determines decimals, currency and such
            CultureInfo responseCulture = new CultureInfo(ApplicationDomain.Instance.PortalLocalization.Locale);

            // localeSpecificApiClient allows pulling offer names localized to supported portal locales compatible with Offer API supported locales. 
            IPartner localeSpecificPartnerCenterClient = ApplicationDomain.Instance.PartnerCenterClientUser.With(RequestContextFactory.Instance.Create(ApplicationDomain.Instance.PortalLocalization.OfferLocale));

            // Get all subscriptions of customer from PC
            ResourceCollection<Subscription> customerAllSubscriptions = await localeSpecificPartnerCenterClient.Customers.ById(clientId).Subscriptions.GetAsync().ConfigureAwait(false);

          //  IEnumerable<CustomerSubscriptionEntity> customerSubscriptions = await ApplicationDomain.Instance.CustomerSubscriptionsRepository.RetrieveAsync(clientId).ConfigureAwait(false);


           
           
            List<PartnerSubscriptionModel> partnerManagedSubscriptions = new List<PartnerSubscriptionModel>();
           

            // Divide the subscriptions by  partner
            foreach (Subscription customerSubscriptionFromPC in customerAllSubscriptions.Items)
            {
               
                PartnerSubscriptionModel partnerSubscription = new PartnerSubscriptionModel()
                {
                    
                    OfferName = customerSubscriptionFromPC.OfferName,
                    Quantity = customerSubscriptionFromPC.Quantity.ToString("G", responseCulture),
                    
                    Status = GetStatusType(customerSubscriptionFromPC.Status),
                   


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
        /// Gets the order History by customers and partners
        /// </summary>
        /// <returns>returns managed subscriptions view model</returns>
        private async Task<Orderdetails> OrderManagedDetails(string clientId)
        {
            DateTime startTime = DateTime.Now;



            // responseCulture determines decimals, currency and such
            CultureInfo responseCulture = new CultureInfo(ApplicationDomain.Instance.PortalLocalization.Locale);

            // localeSpecificApiClient allows pulling offer names localized to supported portal locales compatible with Offer API supported locales. 
            IPartner localeSpecificPartnerCenterClient = ApplicationDomain.Instance.PartnerCenterClientUser.With(RequestContextFactory.Instance.Create(ApplicationDomain.Instance.PortalLocalization.OfferLocale));

            // Get all subscriptions of customer from PC
            ResourceCollection<Order> customerAllOrders = await localeSpecificPartnerCenterClient.Customers.ById(clientId).Orders.GetAsync().ConfigureAwait(false);

           // IEnumerable<CustomerSubscriptionEntity> customerSubscriptions = await ApplicationDomain.Instance.CustomerSubscriptionsRepository.RetrieveAsync(clientId).ConfigureAwait(false);




            List<Items> partnerManagedSubscriptions = new List<Items>();


            // Divide the subscriptions by  partner
            foreach (Order customerSubscriptionFromPC in customerAllOrders.Items)
            {

                foreach (var item in customerSubscriptionFromPC.LineItems)
                {
                    Items partnerSubscription = new Items()
                    {
                        OrderID = customerSubscriptionFromPC.Id,
                        TimeOrdered = customerSubscriptionFromPC.CreationDate.ToString(),
                        Currency = customerSubscriptionFromPC.CurrencyCode,
                        BillingFrequency = customerSubscriptionFromPC.BillingCycle.ToString(),
                        OrderStatus = customerSubscriptionFromPC.Status,

                        NumberOfItems = item.Quantity.ToString(),




                    };
                    partnerManagedSubscriptions.Add(partnerSubscription);
                }

                //partnerManagedSubscriptions.Add(partnerSubscription);





            }

            Orderdetails managedSubscriptions = new Orderdetails()
            {

                PartnerManagedOrders = partnerManagedSubscriptions.OrderByDescending(partnerManagedSubscription => partnerManagedSubscription.TimeOrdered)


            };

            // Capture the request for customer managed subscriptions and partner managed subscriptions for analysis.
            Dictionary<string, string> eventProperties = new Dictionary<string, string> { { "CustomerId", clientId } };

            // Track the event measurements for analysis.
            Dictionary<string, double> eventMetrics = new Dictionary<string, double> { { "ElapsedMilliseconds", DateTime.Now.Subtract(startTime).TotalMilliseconds }/*, { "CustomerManagedSubscriptions", customerManagedSubscriptions.Count }*/, { "PartnerManagedSubscriptions", partnerManagedSubscriptions.Count } };

            ApplicationDomain.Instance.TelemetryService.Provider.TrackEvent("OrderManagedDetails", eventProperties, eventMetrics);

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
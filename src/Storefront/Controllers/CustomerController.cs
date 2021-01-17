using Microsoft.Store.PartnerCenter.Models;
using Microsoft.Store.PartnerCenter.Models.Agreements;
using Microsoft.Store.PartnerCenter.Models.Customers;
using Microsoft.Store.PartnerCenter.RequestContext;
using Microsoft.Store.PartnerCenter.Storefront.BusinessLogic;
using Microsoft.Store.PartnerCenter.Storefront.BusinessLogic.Exceptions;
using Microsoft.Store.PartnerCenter.Storefront.Filters;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace Microsoft.Store.PartnerCenter.Storefront.Controllers
{

    public class CustomerController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }
        [Filters.Mvc.PortalAuthorize(UserRole = UserRole.Partner)]
        // Post: Customer
        [HttpPost]
        public async Task< ActionResult> Index( string domainname, string email, string companyname,string firstname, 
            string lastname,string address1, string address2,string city, string state,string country, string zip, string phone)
        {
            string domainName = string.Format(CultureInfo.InvariantCulture, "{0}.onmicrosoft.com", domainname);

            // check domain available.

            bool isDomainTaken = await ApplicationDomain.Instance.PartnerCenterClient.Domains.ByDomain(domainName).ExistsAsync().ConfigureAwait(false);
            if (isDomainTaken)
            {
                throw new PartnerDomainException(ErrorCode.DomainNotAvailable).AddDetail("DomainPrefix", domainName);
            }

            // get the locale, we default to the first locale used in a country for now.
            PartnerCenter.Models.CountryValidationRules.CountryValidationRules customerCountryValidationRules = await ApplicationDomain.Instance.PartnerCenterClient.CountryValidationRules.ByCountry(country).GetAsync().ConfigureAwait(false);
            string billingCulture = customerCountryValidationRules.SupportedCulturesList.FirstOrDefault();      // default billing culture is the first supported culture for the customer's selected country. 
            string billingLanguage = customerCountryValidationRules.SupportedLanguagesList.FirstOrDefault();    // default billing culture is the first supported language for the customer's selected country. 

            Customer newCustomer = new Customer()
            {
                CompanyProfile = new CustomerCompanyProfile()
                {
                    Domain = domainName,
                },
                BillingProfile = new CustomerBillingProfile()
                {
                    Culture = billingCulture,
                    Language = billingLanguage,
                    Email = email,
                    CompanyName = companyname,

                    DefaultAddress = new Address()
                    {
                        FirstName = firstname,
                        LastName = lastname,
                        AddressLine1 = address1,
                        AddressLine2 = address2,
                        City = city,
                        State = state,
                        Country = country,
                        PostalCode = zip,
                        PhoneNumber = phone,
                    }
                }
            };
            
            // Register customer
            newCustomer = await ApplicationDomain.Instance.PartnerCenterClientUser.Customers.CreateAsync(newCustomer).ConfigureAwait(false);

            ResourceCollection<AgreementMetaData> agreements = await ApplicationDomain.Instance.PartnerCenterClientUser.AgreementDetails.GetAsync().ConfigureAwait(false);

            // Obtain reference to the Microsoft Customer Agreement.
            AgreementMetaData microsoftCustomerAgreement = agreements.Items.FirstOrDefault(agr => agr.AgreementType.Equals("MicrosoftCustomerAgreement", StringComparison.InvariantCultureIgnoreCase));

            // Attest that the customer has accepted the Microsoft Customer Agreement (MCA).
            await ApplicationDomain.Instance.PartnerCenterClientUser.Customers[newCustomer.Id].Agreements.CreateAsync(
                new Agreement
                {
                    DateAgreed = DateTime.UtcNow,
                    PrimaryContact = new PartnerCenter.Models.Agreements.Contact
                    {
                        Email = email,
                        FirstName = firstname,
                        LastName = lastname,
                        PhoneNumber = phone
                    },
                    TemplateId = microsoftCustomerAgreement.TemplateId,
                    Type = "MicrosoftCustomerAgreement",
                    //UserId = branding.AgreementUserId
                }).ConfigureAwait(false);

            

            return View();
        }
        [Filters.Mvc.PortalAuthorize(UserRole = UserRole.Partner)]
        public async Task< ActionResult> CustomerManagement()
        {
            var customers = await ApplicationDomain.Instance.PreApprovedCustomersRepository.RetrieveCustomerDetailsAsync().ConfigureAwait(false);
            return View(customers);
        }
        [Filters.Mvc.PortalAuthorize(UserRole = UserRole.Partner)]
        public async Task< ActionResult> EditCustomer(string id)
        {
            IPartner localeSpecificPartnerCenterClient = ApplicationDomain.Instance.PartnerCenterClientUser.With(RequestContextFactory.Instance.Create(ApplicationDomain.Instance.PortalLocalization.OfferLocale));
            var customer = await localeSpecificPartnerCenterClient.Customers.ById(id).Profiles.Billing.GetAsync().ConfigureAwait(false);

            ViewBag.ClientId = id;
            ViewBag.BillingProfile = customer; 

            return View();
        }
        [Filters.Mvc.PortalAuthorize(UserRole = UserRole.Partner)]
        // Post: Customer
        [HttpPost]
        public async Task<ActionResult> EditCustomer(string email, string companyname, string firstname,
            string lastname, string address1, string address2, string city, string state, string country, string zip, string phone, string clientId)
        {
           

            // get the locale, we default to the first locale used in a country for now.
            PartnerCenter.Models.CountryValidationRules.CountryValidationRules customerCountryValidationRules = await ApplicationDomain.Instance.PartnerCenterClient.CountryValidationRules.ByCountry(country).GetAsync().ConfigureAwait(false);
            string billingCulture = customerCountryValidationRules.SupportedCulturesList.FirstOrDefault();      // default billing culture is the first supported culture for the customer's selected country. 
            string billingLanguage = customerCountryValidationRules.SupportedLanguagesList.FirstOrDefault();    // default billing culture is the first supported language for the customer's selected country. 

            Customer newCustomer = new Customer()
            {
                
                BillingProfile = new CustomerBillingProfile()
                {
                    Culture = billingCulture,
                    Language = billingLanguage,
                    Email = email,
                    CompanyName = companyname,

                    DefaultAddress = new Address()
                    {
                        FirstName = firstname,
                        LastName = lastname,
                        AddressLine1 = address1,
                        AddressLine2 = address2,
                        City = city,
                        State = state,
                        Country = country,
                        PostalCode = zip,
                        PhoneNumber = phone,
                    }
                }
            };

            // update customer profile
          await ApplicationDomain.Instance.PartnerCenterClientUser.Customers.ById(clientId).Profiles.Billing.UpdateAsync(newCustomer.BillingProfile).ConfigureAwait(false);

            


            return View();
        }

        

    }
}
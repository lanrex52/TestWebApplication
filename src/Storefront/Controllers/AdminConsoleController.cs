// -----------------------------------------------------------------------
// <copyright file="AdminConsoleController.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Microsoft.Store.PartnerCenter.Storefront.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Web;
    using System.Web.Http;
    using BusinessLogic;
    using BusinessLogic.Commerce;
    using BusinessLogic.Commerce.PaymentGateways;
    using BusinessLogic.Exceptions;
    using Filters;
    using Microsoft.Store.PartnerCenter.Models;
    using Microsoft.Store.PartnerCenter.Models.Subscriptions;
    using Microsoft.Store.PartnerCenter.RequestContext;
    using Models;

    /// <summary>
    /// Serves data to the Admin dashboard pages.
    /// </summary>
    [RoutePrefix("api/AdminConsole")]
    [Filters.WebApi.PortalAuthorize(UserRole = UserRole.Partner)]
    public class AdminConsoleController : BaseController
    {
        /// <summary>
        /// Retrieves the admin console status.
        /// </summary>
        /// <returns>The admin console status.</returns>
        [HttpGet]
        public async Task<AdminConsoleViewModel> GetAdminConsoleStatus()
        {
            AdminConsoleViewModel adminConsoleViewModel = new AdminConsoleViewModel
            {
                IsOffersConfigured = await ApplicationDomain.Instance.OffersRepository.IsConfiguredAsync().ConfigureAwait(false),
                IsBrandingConfigured = await ApplicationDomain.Instance.PortalBranding.IsConfiguredAsync().ConfigureAwait(false),
                IsPaymentConfigured = await ApplicationDomain.Instance.PaymentConfigurationRepository.IsConfiguredAsync().ConfigureAwait(false)
            };

            return adminConsoleViewModel;
        }

        /// <summary>
        /// Retrieves the partner's branding configuration.
        /// </summary>
        /// <returns>The partner's branding configuration.</returns>
        [Route("Branding")]
        [HttpGet]
        public async Task<BrandingConfiguration> GetBrandingConfiguration()
        {
            return await ApplicationDomain.Instance.PortalBranding.RetrieveAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Updates the website's branding.
        /// </summary>
        /// <returns>The updated branding information.</returns>
        [Route("Branding")]
        [HttpPost]
        public async Task<BrandingConfiguration> UpdateBrandingConfiguration()
        {
            BrandingConfiguration brandingConfiguration = new BrandingConfiguration()
            {
                AgreementUserId = HttpContext.Current.Request.Form["AgreementUserId"],
                OrganizationName = HttpContext.Current.Request.Form["OrganizationName"],
                ContactUs = new ContactUsInformation()
                {
                    Email = HttpContext.Current.Request.Form["ContactUsEmail"],
                    Phone = HttpContext.Current.Request.Form["ContactUsPhone"],
                },
                ContactSales = new ContactUsInformation()
                {
                    Email = HttpContext.Current.Request.Form["ContactSalesEmail"],
                    Phone = HttpContext.Current.Request.Form["ContactSalesPhone"],
                }
            };

            string organizationLogo = HttpContext.Current.Request.Form["OrganizationLogo"];
            HttpPostedFile organizationLogoPostedFile = HttpContext.Current.Request.Files["OrganizationLogoFile"];

            if (organizationLogoPostedFile != null && Path.GetFileName(organizationLogoPostedFile.FileName) == organizationLogo)
            {
                // there is a new organization logo to be uploaded
                if (!organizationLogoPostedFile.ContentType.Trim().StartsWith("image/", StringComparison.InvariantCultureIgnoreCase))
                {
                    throw new PartnerDomainException(ErrorCode.InvalidFileType, Resources.InvalidOrganizationLogoFileTypeMessage).AddDetail("Field", "OrganizationLogoFile");
                }

                brandingConfiguration.OrganizationLogoContent = organizationLogoPostedFile.InputStream;
            }
            else if (!string.IsNullOrWhiteSpace(organizationLogo))
            {
                try
                {
                    // the user either did not specify a logo or he did but changed the organization logo text to point to somewhere else i.e. a URI
                    brandingConfiguration.OrganizationLogo = new Uri(organizationLogo, UriKind.Absolute);
                }
                catch (UriFormatException invalidUri)
                {
                    throw new PartnerDomainException(ErrorCode.InvalidInput, Resources.InvalidOrganizationLogoUriMessage, invalidUri).AddDetail("Field", "OrganizationLogo");
                }
            }

            string headerImage = HttpContext.Current.Request.Form["HeaderImage"];
            HttpPostedFile headerImageUploadPostedFile = HttpContext.Current.Request.Files["HeaderImageFile"];

            if (headerImageUploadPostedFile != null && Path.GetFileName(headerImageUploadPostedFile.FileName) == headerImage)
            {
                // there is a new header image to be uploaded
                if (!headerImageUploadPostedFile.ContentType.Trim().StartsWith("image/", StringComparison.InvariantCultureIgnoreCase))
                {
                    throw new PartnerDomainException(ErrorCode.InvalidFileType, Resources.InvalidHeaderImageMessage).AddDetail("Field", "HeaderImageFile");
                }

                brandingConfiguration.HeaderImageContent = headerImageUploadPostedFile.InputStream;
            }
            else if (!string.IsNullOrWhiteSpace(headerImage))
            {
                try
                {
                    // the user either did not specify a header image or he did but changed the organization logo text to point to somewhere else i.e. a URI
                    brandingConfiguration.HeaderImage = new Uri(headerImage, UriKind.Absolute);
                }
                catch (UriFormatException invalidUri)
                {
                    throw new PartnerDomainException(ErrorCode.InvalidInput, Resources.InvalidHeaderImageUriMessage, invalidUri).AddDetail("Field", "HeaderImage");
                }
            }

            if (!string.IsNullOrWhiteSpace(HttpContext.Current.Request.Form["PrivacyAgreement"]))
            {
                try
                {
                    brandingConfiguration.PrivacyAgreement = new Uri(HttpContext.Current.Request.Form["PrivacyAgreement"], UriKind.Absolute);
                }
                catch (UriFormatException invalidUri)
                {
                    throw new PartnerDomainException(ErrorCode.InvalidInput, Resources.InvalidPrivacyUriMessage, invalidUri).AddDetail("Field", "PrivacyAgreement");
                }
            }

            if (!string.IsNullOrWhiteSpace(HttpContext.Current.Request.Form["InstrumentationKey"]))
            {
                brandingConfiguration.InstrumentationKey = HttpContext.Current.Request.Form["InstrumentationKey"];
            }

            BrandingConfiguration updatedBrandingConfiguration = await ApplicationDomain.Instance.PortalBranding.UpdateAsync(brandingConfiguration).ConfigureAwait(false);
            bool isPaymentConfigurationSetup = await ApplicationDomain.Instance.PaymentConfigurationRepository.IsConfiguredAsync().ConfigureAwait(false);

            //if (isPaymentConfigurationSetup)
            //{
            //    // update the web experience profile. 
            //    PaymentConfiguration paymentConfiguration = await ApplicationDomain.Instance.PaymentConfigurationRepository.RetrieveAsync().ConfigureAwait(false);
            //   // paymentConfiguration.WebExperienceProfileId = PaymentGatewayConfig.GetPaymentGatewayInstance(ApplicationDomain.Instance, "retrieve payment").CreateWebExperienceProfile(paymentConfiguration, updatedBrandingConfiguration, ApplicationDomain.Instance.PortalLocalization.CountryIso2Code);
            //    await ApplicationDomain.Instance.PaymentConfigurationRepository.UpdateAsync(paymentConfiguration).ConfigureAwait(false);
            //}

            return updatedBrandingConfiguration;
        }

        /// <summary>
        /// Retrieves all active offers the partner has configured.
        /// </summary>
        /// <returns>The active partner offers.</returns>
        [Route("Offers")]
        [HttpGet]
        public async Task<IEnumerable<PartnerOffer>> GetOffers()
        {
            return (await ApplicationDomain.Instance.OffersRepository.RetrieveAsync().ConfigureAwait(false)).Where(offer => !offer.IsInactive);
        }

        /// <summary>
        /// Adds a new partner offer.
        /// </summary>
        /// <param name="newPartnerOffer">The new partner offer to add.</param>
        /// <returns>The new partner offer details.</returns>
        [Route("Offers")]
        [HttpPost]
        public async Task<PartnerOffer> AddOffer(PartnerOffer newPartnerOffer)
        {
            return await ApplicationDomain.Instance.OffersRepository.AddAsync(newPartnerOffer).ConfigureAwait(false);
        }

        /// <summary>
        /// Updates a partner offer.
        /// </summary>
        /// <param name="partnerOffer">The partner offer to update.</param>
        /// <returns>The updated partner offer.</returns>
        [Route("Offers")]
        [HttpPut]
        public async Task<PartnerOffer> UpdateOffer(PartnerOffer partnerOffer)
        {
            IPaymentGateway paymentGateway = PaymentGatewayConfig.GetPaymentGatewayInstance(ApplicationDomain.Instance, "configure payment");
            PaymentConfiguration paymentConfiguration = new PaymentConfiguration();
            bool isBrandingConfigured = await ApplicationDomain.Instance.PortalBranding.IsConfiguredAsync().ConfigureAwait(false);
           
            return await ApplicationDomain.Instance.OffersRepository.UpdateAsync(partnerOffer).ConfigureAwait(false);
        }

        /// <summary>
        /// Deletes partner offers.
        /// </summary>
        /// <param name="partnerOffersToDelete">The partner offers to delete.</param>
        /// <returns>The updated partner offers after deletion.</returns>
        [Route("Offers/Delete")]
        [HttpPost]
        public async Task<IEnumerable<PartnerOffer>> DeleteOffers(List<PartnerOffer> partnerOffersToDelete)
        {
            return (await ApplicationDomain.Instance.OffersRepository.MarkAsDeletedAsync(partnerOffersToDelete).ConfigureAwait(false)).Where(offer => !offer.IsInactive);
        }

        /// <summary>
        /// Retrieves all the Microsoft CSP offers.
        /// </summary>
        /// <returns>A list of Microsoft CSP offers.</returns>
        [HttpGet]
        [Route("MicrosoftOffers")]
        public async Task<IEnumerable<MicrosoftOffer>> GetMicrosoftOffers()
        {
            return await ApplicationDomain.Instance.OffersRepository.RetrieveMicrosoftOffersAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Retrieves the portal's payment configuration.
        /// </summary>
        /// <returns>The portal's payment configuration.</returns>
        [HttpGet]
        [Route("Payment")]
        public async Task<PaymentConfiguration> GetPaymentConfiguration()
        {
            return await ApplicationDomain.Instance.PaymentConfigurationRepository.RetrieveAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Updates the the portal's payment configuration.
        /// </summary>
        /// <param name="paymentConfiguration">The new portal's payment configuration.</param>
        /// <returns>The updated portal's payment configuration.</returns>
        [Route("Payment")]
        [HttpPut]
        public async Task<PaymentConfiguration> UpdatePaymentConfiguration(PaymentConfiguration paymentConfiguration)
        {
            IPaymentGateway paymentGateway = PaymentGatewayConfig.GetPaymentGatewayInstance(ApplicationDomain.Instance, "configure payment");
            // validate the payment configuration before saving. 
           /// paymentGateway.ValidateConfiguration(paymentConfiguration);

            // check if branding configuration has been setup else don't create web experience profile. 
            bool isBrandingConfigured = await ApplicationDomain.Instance.PortalBranding.IsConfiguredAsync().ConfigureAwait(false);
            //if (isBrandingConfigured)
            //{
            //    // create a web experience profile using the branding for the web store. 
            //    BrandingConfiguration brandConfig = await ApplicationDomain.Instance.PortalBranding.RetrieveAsync().ConfigureAwait(false);
            //    paymentConfiguration.WebExperienceProfileId = paymentGateway.CreateWebExperienceProfile(paymentConfiguration, brandConfig, ApplicationDomain.Instance.PortalLocalization.CountryIso2Code);
            //}

            // Save the validated & complete payment configuration to repository.
            PaymentConfiguration paymentConfig = await ApplicationDomain.Instance.PaymentConfigurationRepository.UpdateAsync(paymentConfiguration).ConfigureAwait(false);

            return paymentConfig;
        }

        /// <summary>
        /// Retrieves the portal's pre approved customers.
        /// </summary>
        /// <returns>The portal's pre approved customers list.</returns>
        [HttpGet]
        [Route("PreApprovedCustomers")]
        public async Task<PreApprovedCustomersViewModel> GetPreApprovedCustomers()
        {
            return await ApplicationDomain.Instance.PreApprovedCustomersRepository.RetrieveCustomerDetailsAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Updates the the portal's pre approved list of customers. 
        /// </summary>
        /// <param name="preApprovedCustomers">The pre approved list of customers.</param>
        /// <returns>The updated pre approved customers list.</returns>
        [Route("PreApprovedCustomers")]
        [HttpPut]
        public async Task<PreApprovedCustomersViewModel> UpdatePreApprovedCustomersConfiguration(PreApprovedCustomersViewModel preApprovedCustomers)
        {
            // Save to repository.
            PreApprovedCustomersViewModel updatedCustomers = await ApplicationDomain.Instance.PreApprovedCustomersRepository.UpdateAsync(preApprovedCustomers).ConfigureAwait(false);

            return updatedCustomers;
        }
        [HttpGet]
        [Route("CustomerDetails")]
        public async Task<ManagedSubscriptionsViewModel> GetCustomersDetails(string clientId)
        {
            return await GetManagedSubscriptions(clientId).ConfigureAwait(false);
        }

        /// <summary>
        /// Gets the subscriptions managed by customers and partners
        /// </summary>
        /// <returns>returns managed subscriptions view model</returns>
        private async Task<ManagedSubscriptionsViewModel> GetManagedSubscriptions(string clientId)
        {
            DateTime startTime = DateTime.Now;

            string clientCustomerId = clientId;
                
                //Principal.PartnerCenterCustomerId;

            // responseCulture determines decimals, currency and such
            CultureInfo responseCulture = new CultureInfo(ApplicationDomain.Instance.PortalLocalization.Locale);

            // localeSpecificApiClient allows pulling offer names localized to supported portal locales compatible with Offer API supported locales. 
            IPartner localeSpecificPartnerCenterClient = ApplicationDomain.Instance.PartnerCenterClient.With(RequestContextFactory.Instance.Create(ApplicationDomain.Instance.PortalLocalization.OfferLocale));

            // Get all subscriptions of customer from PC
            ResourceCollection<Subscription> customerAllSubscriptions = await localeSpecificPartnerCenterClient.Customers.ById(clientCustomerId).Subscriptions.GetAsync().ConfigureAwait(false);

            IEnumerable<CustomerSubscriptionEntity> customerSubscriptions = await ApplicationDomain.Instance.CustomerSubscriptionsRepository.RetrieveAsync(clientCustomerId).ConfigureAwait(false);
            IEnumerable<PartnerOffer> allPartnerOffers = await ApplicationDomain.Instance.OffersRepository.RetrieveAsync().ConfigureAwait(false);
            IEnumerable<MicrosoftOffer> currentMicrosoftOffers = await ApplicationDomain.Instance.OffersRepository.RetrieveMicrosoftOffersAsync().ConfigureAwait(false);

            List<SubscriptionViewModel> customerSubscriptionsView = new List<SubscriptionViewModel>();

            // iterate through and build the list of customer's subscriptions.
            foreach (CustomerSubscriptionEntity subscription in customerSubscriptions)
            {
                PartnerOffer partnerOfferItem = allPartnerOffers.FirstOrDefault(offer => offer.Id == subscription.PartnerOfferId);
                string subscriptionTitle = partnerOfferItem.Title;
                string portalOfferId = partnerOfferItem.Id;
                decimal portalOfferPrice = partnerOfferItem.Price;

                DateTime subscriptionExpiryDate = subscription.ExpiryDate.ToUniversalTime();
                int remainingDays = (subscriptionExpiryDate.Date - DateTime.UtcNow.Date).Days;
                bool isRenewable = remainingDays <= 30;                                         // IsRenewable is true if subscription is going to expire in 30 days.
                bool isEditable = DateTime.UtcNow.Date <= subscriptionExpiryDate.Date;          // IsEditable is true if today is lesser or equal to subscription expiry date.

                // Temporarily mark this partnerOffer item as inactive and dont allow store front customer to manage this subscription. 
                MicrosoftOffer alignedMicrosoftOffer = currentMicrosoftOffers.FirstOrDefault(offer => offer.Offer.Id == partnerOfferItem.MicrosoftOfferId);

                if (alignedMicrosoftOffer == null)
                {
                    // The offer is inactive (marked for deletion) then dont allow renewals or editing on this subscription tied to this offer. 
                    partnerOfferItem.IsInactive = true;
                    isRenewable = false;
                    isEditable = false;
                }

                // Compute the pro rated price per seat for this subcription & return for client side processing during updates. 
                decimal proratedPerSeatPrice = Math.Round(CommerceOperations.CalculateProratedSeatCharge(subscription.ExpiryDate, portalOfferPrice), responseCulture.NumberFormat.CurrencyDecimalDigits);

                SubscriptionViewModel subscriptionItem = new SubscriptionViewModel()
                {
                    SubscriptionId = subscription.SubscriptionId,
                    FriendlyName = subscriptionTitle,
                    PortalOfferId = portalOfferId,
                    PortalOfferPrice = portalOfferPrice.ToString("C", responseCulture),
                    IsRenewable = isRenewable,
                    IsEditable = isEditable,
                    SubscriptionExpiryDate = subscriptionExpiryDate.Date.ToString("d", responseCulture),
                    SubscriptionProRatedPrice = proratedPerSeatPrice
                };

                // add this subcription to the customer's subscription list.
                customerSubscriptionsView.Add(subscriptionItem);
            }

            List<CustomerSubscriptionModel> customerManagedSubscriptions = new List<CustomerSubscriptionModel>();
            List<PartnerSubscriptionModel> partnerManagedSubscriptions = new List<PartnerSubscriptionModel>();
            List<AzureBillingModel> azureManagedBillingModels = new List<AzureBillingModel>();

            // Divide the subscriptions by customer and partner
            foreach (Subscription customerSubscriptionFromPC in customerAllSubscriptions.Items)
            {
                SubscriptionViewModel subscription = customerSubscriptionsView.FirstOrDefault(sub => sub.SubscriptionId == customerSubscriptionFromPC.Id);

                // Customer managed subscription found

                if (subscription != null)
                {
                    CustomerSubscriptionModel customerSubscription = new CustomerSubscriptionModel()
                    {
                        SubscriptionId = customerSubscriptionFromPC.Id,
                        LicensesTotal = customerSubscriptionFromPC.Quantity.ToString("G", responseCulture),
                        Status = GetStatusType(customerSubscriptionFromPC.Status),
                        CreationDate = customerSubscriptionFromPC.CreationDate.ToString("d", responseCulture),
                        FriendlyName = subscription.FriendlyName,
                        IsRenewable = subscription.IsRenewable,
                        IsEditable = subscription.IsEditable,
                        PortalOfferId = subscription.PortalOfferId,
                        SubscriptionProRatedPrice = subscription.SubscriptionProRatedPrice,
                        ExpiryDate = customerSubscriptionFromPC.CommitmentEndDate.ToString("d", responseCulture),





                    };

                    customerManagedSubscriptions.Add(customerSubscription);
                }
                else
                {
                    PartnerSubscriptionModel partnerSubscription = new PartnerSubscriptionModel()
                    {
                        Id = customerSubscriptionFromPC.Id,
                        OfferName = customerSubscriptionFromPC.OfferName,
                        Quantity = customerSubscriptionFromPC.Quantity.ToString("G", responseCulture),
                        Status = GetStatusType(customerSubscriptionFromPC.Status),
                        CreationDate = customerSubscriptionFromPC.CreationDate.ToString("d", responseCulture),
                        ExpiryDate = customerSubscriptionFromPC.CommitmentEndDate.ToString("d", responseCulture),


                    };

                    partnerManagedSubscriptions.Add(partnerSubscription);

                }
                var billing =
                   await localeSpecificPartnerCenterClient.Customers.ById(clientCustomerId).Subscriptions.ById(customerSubscriptionFromPC.Id).UsageRecords.ByMeter.GetAsync().ConfigureAwait(false);
                foreach (var item in billing.Items)
                {
                    AzureBillingModel azureBillingModel = new AzureBillingModel()
                    {
                        SubscriptionId = customerSubscriptionFromPC.Id,//item.SubscriptionId,
                        ResourceUri = item.ResourceId,
                        ResourceType = item.ResourceId,
                        EntitlementId = item.MeterId,
                        EntitlementName = item.MeterName,
                        ResourceGroupName = item.Category,
                        ResourceName = item.ResourceName,
                        TotalCost = item.TotalCost.ToString(),
                        UsdTotalCost = item.USDTotalCost.ToString(),
                        LastModifiedDate = item.LastModifiedDate.ToString("d", responseCulture)
                    };
                    azureManagedBillingModels.Add(azureBillingModel);
                }



            }

            ManagedSubscriptionsViewModel managedSubscriptions = new ManagedSubscriptionsViewModel()
            {
               // CustomerManagedSubscriptions = customerManagedSubscriptions.OrderByDescending(customerManagedSubscription => customerManagedSubscription.CreationDate),
                PartnerManagedSubscriptions = partnerManagedSubscriptions.OrderByDescending(partnerManagedSubscription => partnerManagedSubscription.CreationDate),
               // AzureManagedSubscriptions = azureManagedBillingModels

            };

            // Capture the request for customer managed subscriptions and partner managed subscriptions for analysis.
            Dictionary<string, string> eventProperties = new Dictionary<string, string> { { "CustomerId", clientCustomerId } };

            // Track the event measurements for analysis.
            Dictionary<string, double> eventMetrics = new Dictionary<string, double> { { "ElapsedMilliseconds", DateTime.Now.Subtract(startTime).TotalMilliseconds }, { "CustomerManagedSubscriptions", customerManagedSubscriptions.Count }, { "PartnerManagedSubscriptions", partnerManagedSubscriptions.Count } };

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
// -----------------------------------------------------------------------
// <copyright file="ApplicationDomain.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Microsoft.Store.PartnerCenter.Storefront.BusinessLogic
{
    using System;
    using System.Configuration;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Commerce;
    using Configuration;
    using Microsoft.Store.PartnerCenter.Storefront.Security;
    using Offers;
    using PartnerCenter.Extensions;

    /// <summary>
    /// The application domain holds key domain objects needed across the application.
    /// </summary>
    public class ApplicationDomain
    {
        /// <summary>
        /// Prevents a default instance of the <see cref="ApplicationDomain"/> class from being created.
        /// </summary>
        private ApplicationDomain()
        { 
        }

        /// <summary>
        /// Gets an instance of the application domain.
        /// </summary>
        public static ApplicationDomain Instance { get; private set; }

        /// <summary>
        /// Gets a Partner Center SDK client.
        /// </summary>
        public IAggregatePartner PartnerCenterClient { get; private set; }
        public IAggregatePartner PartnerCenterClientUser { get; private set; }

        /// <summary>
        /// Gets the partner offers repository.
        /// </summary>
        public PartnerOffersRepository OffersRepository { get; private set; }

        /// <summary>
        /// Gets the Azure storage service.
        /// </summary>
        public AzureStorageService AzureStorageService { get; private set; }

        /// <summary>
        /// Gets the caching service.
        /// </summary>
        public CachingService CachingService { get; private set; }

        /// <summary>
        /// Gets the Microsoft offer logo indexer.
        /// </summary>
        public MicrosoftOfferLogoIndexer MicrosoftOfferLogoIndexer { get; private set; }

        /// <summary>
        /// Gets the portal branding service.
        /// </summary>
        public PortalBranding PortalBranding { get; private set; }

        /// <summary>
        /// Gets the portal localization service.
        /// </summary>
        public PortalLocalization PortalLocalization { get; private set; }

        /// <summary>
        /// Gets the portal payment configuration repository.
        /// </summary>
        public PaymentConfigurationRepository PaymentConfigurationRepository { get; private set; }

        /// <summary>
        /// Gets the portal PreApprovedCustomers configuration repository.
        /// </summary>
        public PreApprovedCustomersRepository PreApprovedCustomersRepository { get; private set; }

        /// <summary>
        /// Gets the customer subscriptions repository.
        /// </summary>
        public CustomerSubscriptionsRepository CustomerSubscriptionsRepository { get; private set; }

        /// <summary>
        /// Gets the customer purchases repository.
        /// </summary>
        public CustomerPurchasesRepository CustomerPurchasesRepository { get; private set; }

        /// <summary>
        /// Gets the customer orders repository.
        /// </summary>
        public OrdersRepository CustomerOrdersRepository { get; private set; }

        /// <summary>
        /// Gets the portal telemetry service.
        /// </summary>
        public TelemetryService TelemetryService { get; private set; }
        /// <summary>
        /// The client used to perform HTTP operations.
        /// </summary>
        private static readonly HttpClient httpClient = new HttpClient();

        /// <summary>
        /// The client used to request resources such as access tokens.
        /// </summary>
        private static PartnerServiceClient serviceClient = new PartnerServiceClient(httpClient);


        /// <summary>
        /// Gets the customer registration repository.
        /// </summary>
        public CustomerRegistrationRepository CustomerRegistrationRepository { get; private set; }

        /// <summary>
        /// Initializes the core application domain objects.
        /// </summary>
        /// <returns>A task.</returns>
        public static async Task BootstrapAsync()
        {
            if (Instance == null)
            {
                Instance = new ApplicationDomain
                {
                    PartnerCenterClient = await AcquirePartnerCenterAccessAsync().ConfigureAwait(false),
                   
                    PartnerCenterClientUser = await AcquirePartnerUserCenterAccessAsync().ConfigureAwait(false)
            };

                Instance.PortalLocalization = new PortalLocalization(Instance);

                await Instance.PortalLocalization.InitializeAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Initializes the application domain objects.
        /// </summary>
        /// <returns>A task.</returns>
        public static async Task InitializeAsync()
        {
            if (Instance != null)
            {
                Instance.AzureStorageService = new AzureStorageService(ApplicationConfiguration.AzureStorageConnectionString, ApplicationConfiguration.AzureStorageConnectionEndpointSuffix);
                Instance.CachingService = new CachingService(Instance, ApplicationConfiguration.CacheConnectionString);
                Instance.OffersRepository = new PartnerOffersRepository(Instance);
                Instance.MicrosoftOfferLogoIndexer = new MicrosoftOfferLogoIndexer(Instance);
                Instance.PortalBranding = new PortalBranding(Instance);
                Instance.PaymentConfigurationRepository = new PaymentConfigurationRepository(Instance);
                Instance.PreApprovedCustomersRepository = new PreApprovedCustomersRepository(Instance);
                Instance.CustomerSubscriptionsRepository = new CustomerSubscriptionsRepository(Instance);
                Instance.CustomerPurchasesRepository = new CustomerPurchasesRepository(Instance);
                Instance.CustomerOrdersRepository = new OrdersRepository(Instance);
                Instance.TelemetryService = new TelemetryService(Instance);
                Instance.CustomerRegistrationRepository = new CustomerRegistrationRepository(Instance);

                await Instance.TelemetryService.InitializeAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Authenticates with the Partner Center APIs.
        /// </summary>
        /// <returns>A Partner Center API client.</returns>
        private static async Task<IAggregatePartner> AcquirePartnerCenterAccessAsync()
        {
            PartnerService.Instance.ApiRootUrl = ConfigurationManager.AppSettings["partnerCenter.apiEndPoint"];
            PartnerService.Instance.ApplicationName = "Web Store Front V1.6";

            IPartnerCredentials credentials = await PartnerCredentials.Instance.GenerateByApplicationCredentialsAsync(
                ConfigurationManager.AppSettings["partnercenter.applicationId"],
                ConfigurationManager.AppSettings["partnercenter.applicationSecret"],
                ConfigurationManager.AppSettings["partnercenter.AadTenantId"],
                ConfigurationManager.AppSettings["aadEndpoint"],
                ConfigurationManager.AppSettings["aadGraphEndpoint"]).ConfigureAwait(false);

            return PartnerService.Instance.CreatePartnerOperations(credentials);
        }
        private static async Task<Tuple<string, DateTimeOffset>> LoginToPartnerCenter()
        {
            
            string refreshToken = ConfigurationManager.AppSettings["webPortal.refreshToken"];

            AuthenticationResults token = await serviceClient.RefreshAccessTokenAsync(
                $"{ConfigurationManager.AppSettings["aadEndpoint"]}{ConfigurationManager.AppSettings["partnercenter.AadTenantId"]}/oauth2/token",
                "https://api.partnercenter.microsoft.com",
                refreshToken,
                ConfigurationManager.AppSettings["partnercenter.applicationId"],
                ConfigurationManager.AppSettings["partnercenter.applicationSecret"]).ConfigureAwait(false);

            return new Tuple<string, DateTimeOffset>(token.AccessToken, token.ExpiresOn);
        }

        private static async Task<IAggregatePartner> AcquirePartnerUserCenterAccessAsync()
        {
            Tuple<string, DateTimeOffset> aadAuthenticationResult = await LoginToPartnerCenter();

            // Authenticate by user context with the partner service
            IPartnerCredentials userCredentials = PartnerCredentials.Instance.GenerateByUserCredentials(
                ConfigurationManager.AppSettings["partnercenter.applicationId"],
                new AuthenticationToken(
                    aadAuthenticationResult.Item1,
                    aadAuthenticationResult.Item2),
                async delegate
                {
                    // token has expired, re-Login to Azure Active Directory
                    Tuple<string, DateTimeOffset> aadToken = await LoginToPartnerCenter();
                    return new AuthenticationToken(aadToken.Item1, aadToken.Item2);
                });

            return PartnerService.Instance.CreatePartnerOperations(userCredentials);
        }

    }
}
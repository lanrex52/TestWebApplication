// -----------------------------------------------------------------------
// <copyright file="PortalCustomer.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Microsoft.Store.PartnerCenter.Storefront.Models
{
    /// <summary>
    /// Represents Portal Customer 
    /// </summary>
    public class PortalCustomer
    {
        /// <summary>
        /// Gets or sets a value indicating the partner center Id of the customer.
        /// </summary>
        public string TenantId { get; set; }

        /// <summary>
        /// Gets or sets a value indicating the Company Name of the customer.
        /// </summary>
        public string CompanyName { get; set; }

        /// <summary>
        /// Gets or sets a value indicating the Domain of the customer.
        /// </summary>
        public string Domain { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the country of the customer. 
        /// </summary>
        public string Country { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether the city of the customer. 
        /// </summary>
        public string City { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether the if userse is pre Approve. 
        /// </summary>
       // public bool PreApproved { get; set; }
    }
}
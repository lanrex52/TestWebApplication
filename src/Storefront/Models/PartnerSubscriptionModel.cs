// -----------------------------------------------------------------------
// <copyright file="PartnerSubscriptionModel.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Microsoft.Store.PartnerCenter.Storefront.Models
{
    /// <summary>
    /// The partner subscription model
    /// </summary>
    public class PartnerSubscriptionModel
    {
        /// <summary>
        /// Gets or sets the customer license Id.
        /// </summary>
        public string Id { get; set; }
        /// <summary>
        /// Gets or sets the Customer name.
        /// </summary>
        public string CustomerName { get; set; }

        /// <summary>
        /// Gets or sets the offer name.
        /// </summary>
        public string OfferName { get; set; }

        /// <summary>
        /// Gets or sets the customer license status like None, Active, Suspended or Deleted
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// Gets or sets the total number of licenses for this customer license.
        /// </summary>
        public string Quantity { get; set; }

        /// <summary>
        /// Gets or sets the license expiry date. 
        /// </summary>
        public string ExpiryDate { get; set; }
        /// <summary>
        /// Gets or sets the license creation date. 
        /// </summary>
        public string CreationDate { get; set; }
        public string ResourceName { get; set; }

        public string TotalCost { get; set; }
        public string UsdTotalCost { get; set; }
        public string LastModifiedDate { get; set; }
        public string SyndicationPartnerSubscriptionNumber { get; set; }
        public string SubscriptionFriendlyName { get; set; }
        public string DurableOfferId { get; set; }

        public string PartnerCenterOrderId { get; set; }
        public string ChargeType { get; set; }
        public string UnitPrice { get; set; }
        public string PretaxTotal { get; set; }
        public string Total { get; set; }
        public string  InvoiceNumber { get; set; }
        public string InvoiceType { get; set; }




    }
}
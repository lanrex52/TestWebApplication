using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Microsoft.Store.PartnerCenter.Storefront.Models
{
    public class AzureBillingModel
    {
        public string SubscriptionId { get; set; }
        public string ResourceUri { get; set; }
        public string ResourceType { get; set; }
        public string EntitlementId { get; set; }
        public string EntitlementName { get; set; }
        public string ResourceGroupName { get; set; }
       
        public string ResourceName { get; set; }
        public string TotalCost { get; set; }
        public string UsdTotalCost { get; set; }
        public string LastModifiedDate { get; set; }

    }
}
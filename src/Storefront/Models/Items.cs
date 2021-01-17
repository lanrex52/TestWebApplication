using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Microsoft.Store.PartnerCenter.Storefront.Models
{
    public class Items
    {
        public string OrderID { get; set; }
        public string TimeOrdered { get; set; }
        public string Currency { get; set; }
        public string BillingFrequency { get; set; }
        public string NumberOfItems { get; set; }
        public string OrderStatus { get; set; }

        public string TotalPrice { get; set; }
      
       
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Microsoft.Store.PartnerCenter.Storefront.Models
{
    public class Orderdetails
    {
        public IEnumerable<Items> PartnerManagedOrders { get; set; }
    }
}
using System.Collections.Generic;
using Sitecore.Commerce.Core;
using Sitecore.Commerce.Plugin.Coupons;

namespace Plugin.Sample.AllocateCoupon.Components
{
    public class UnusedCouponsComponent : Component
    {
        public List<CartCoupon> UnusedCoupons { get; set; }
    }
}
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.OData;
using Microsoft.AspNet.OData.Routing;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Sitecore.Commerce.Core;
using Sitecore.Commerce.Plugin.Coupons;
using Sitecore.Commerce.Plugin.Promotions;

namespace Plugin.Sample.AllocateCoupon.Controllers
{
    public class ApiController : CommerceODataController
    {
        private readonly CommerceCommander _commander;
        
        public ApiController(IServiceProvider serviceProvider, CommerceEnvironment globalEnvironment) : base(serviceProvider, globalEnvironment)
        {
            _commander = serviceProvider.GetService<CommerceCommander>();
        }
        
        [HttpGet]
        [ODataRoute("AllocateCoupon(promotionId={promotionId},groupId={groupId})", RouteName = CoreConstants.CommerceApi)]
        public async Task<IActionResult> AllocateCoupon(string promotionId, string groupId)
        {
            if (!ModelState.IsValid)
            {
                return new BadRequestObjectResult(ModelState);
            }

            // Allocate a new coupon
            var promotion = await this.Command<NewCouponAllocationCommand>().Process(this.CurrentContext, promotionId, groupId, 1);
            
            var promotionCodeGroup = await _commander.GetEntity<PrivateCouponGroup>(this.CurrentContext, groupId);
            
            // Get the last promotion code added
            var couponAllocations = promotionCodeGroup.EntityComponents.OfType<CouponAllocationComponent>().ToList();
            if (couponAllocations.Any())
            {
                var lastCouponAllocation = couponAllocations.Last();
                return await Task.FromResult( Ok(lastCouponAllocation.Codes.First()));
            }

            return await Task.FromResult( new NotFoundResult());
        }
    }
}
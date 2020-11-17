﻿using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.OData;
using Microsoft.AspNet.OData.Routing;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Plugin.Sample.AllocateCoupon.Commands;
using Sitecore.Commerce.Core;
using Sitecore.Commerce.Plugin.Coupons;
using Sitecore.Commerce.Plugin.Promotions;

namespace Plugin.Sample.AllocateCoupon.Controllers
{
    public class ApiController : CommerceODataController
    {
        private readonly CommerceCommander _commander;

        public ApiController(IServiceProvider serviceProvider,
            CommerceEnvironment globalEnvironment) : base(serviceProvider, globalEnvironment)
        {
            _commander = serviceProvider.GetService<CommerceCommander>();
        }

        [HttpGet]
        [ODataRoute("AllocateCoupon(promotionId={promotionId},prefix={prefix})",
            RouteName = CoreConstants.CommerceApi)]
        public async Task<IActionResult> AllocateCoupon(string promotionId, string prefix)
        {
            if (!ModelState.IsValid)
            {
                return new BadRequestObjectResult(ModelState);
            }

            if (string.IsNullOrEmpty(promotionId) || string.IsNullOrEmpty(prefix))
            {
                return new BadRequestResult();
            }

            if (prefix.Contains('-'))
            {
                return new BadRequestResult();
            }

            var couponCode = await _commander.Command<AllocateCouponCommand>().Process(this.CurrentContext, promotionId, prefix);

            return Ok(couponCode);
        }
    }
}
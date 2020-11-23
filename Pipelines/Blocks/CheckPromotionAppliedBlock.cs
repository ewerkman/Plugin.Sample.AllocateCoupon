using System;
using System.Linq;
using Plugin.Sample.AllocateCoupon.Components;
using Sitecore.Commerce.Plugin.Coupons;
using Sitecore.Commerce.Plugin.Pricing;

namespace Plugin.Sample.AllocateCoupon.Pipelines.Blocks
{
    using Sitecore.Commerce.Core;
    using Sitecore.Framework.Conditions;
    using Sitecore.Framework.Pipelines;
    using System.Threading.Tasks;

    [PipelineDisplayName("Change to <Project>Constants.Pipelines.Blocks.<Block Name>")]
    public class CheckPromotionAppliedBlock : AsyncPipelineBlock<Sitecore.Commerce.Plugin.Carts.Cart, Sitecore.Commerce.Plugin.Carts.Cart, CommercePipelineExecutionContext>
    {
        protected CommerceCommander Commander { get; set; }

        public CheckPromotionAppliedBlock(CommerceCommander commander)
            : base(null)
        {
            this.Commander = commander;
        }

        public override Task<Sitecore.Commerce.Plugin.Carts.Cart> RunAsync(Sitecore.Commerce.Plugin.Carts.Cart arg, CommercePipelineExecutionContext context)
        {
            Condition.Requires(arg).IsNotNull($"{this.Name}: The argument can not be null");

            // Are there coupons on this cart?
            if (arg.HasComponent<CartCouponsComponent>())
            {
                // Get the cart coupons
                var cartCouponComponent = arg.GetComponent<CartCouponsComponent>();

                var cartAdjustmentsWithPromotionId = arg.Adjustments.Where(a => !string.IsNullOrEmpty(a.PromotionId));
                var cartLineAdjustmentsWithPromotionId = arg.Lines
                    .Select(l => l.Adjustments.Where(a => !string.IsNullOrEmpty(a.PromotionId))).Aggregate((a, b) => a.Union(b));

                var allAdjustmentsWithPromotionId = cartAdjustmentsWithPromotionId.Union<AwardedAdjustment>(cartLineAdjustmentsWithPromotionId);

                // Get the awarded adjustments and check whether each cart coupon has been used in an awarded adjustment
                var couponsWithoutPromotion = cartCouponComponent.List.Where(c =>
                    !allAdjustmentsWithPromotionId.Any(a =>
                        a.PromotionId.Equals(c.Promotion.EntityTarget, StringComparison.OrdinalIgnoreCase))).ToList();

                if (couponsWithoutPromotion.Any())
                {
                    var unusedCouponsComponent = arg.GetComponent<UnusedCouponsComponent>();
                    unusedCouponsComponent.UnusedCoupons = couponsWithoutPromotion;
                }
            }

            return Task.FromResult(arg);
        }
    }
}
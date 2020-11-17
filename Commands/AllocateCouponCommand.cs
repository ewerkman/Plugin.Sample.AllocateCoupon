using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sitecore.Commerce.Core;
using Sitecore.Commerce.Core.Commands;
using Sitecore.Commerce.Plugin.Coupons;
using Sitecore.Commerce.Plugin.Promotions;

namespace Plugin.Sample.AllocateCoupon.Commands
{
    public class AllocateCouponCommand : CommerceCommand
    {
        private readonly CommerceCommander _commander;

        public AllocateCouponCommand(CommerceCommander commander)
        {
            this._commander = commander;
        }

        public async Task<string> Process(CommerceContext commerceContext, string promotionId, string prefix)
        {
            using (CommandActivity.Start(commerceContext, this))
            {
                // Get the promotion
                var promotion = await _commander.GetEntity<Promotion>(commerceContext, promotionId);

                var privateCoupons = await GetPrivateCouponGroups(commerceContext, promotion);
                
                PrivateCouponGroup privateCoupon = null;

                if (privateCoupons.Any())
                {
                    // Check whether we have private coupons that have unused allocations with the specified prefix
                    var unusedCoupons = privateCoupons.Where(c => c.Prefix == prefix && c.AllocatedCount < c.Total);
                    if (unusedCoupons.Any())
                    {
                        privateCoupon = unusedCoupons.First(); // Take the first unused coupon
                    }
                }

                if (privateCoupon == null)
                {
                    privateCoupon = await CreateNewPrivateCouponGroup(commerceContext, prefix, promotion);
                }

                if (privateCoupon != null)
                {
                    return await GetCouponAllocation(commerceContext, promotionId, privateCoupon);
                }
            }

            return null;
        }

        private async Task<PrivateCouponGroup> CreateNewPrivateCouponGroup(CommerceContext commerceContext, string prefix, Promotion promotion)
        {
            PrivateCouponGroup privateCoupon;
            // Create a new private coupon because we could not find an existing private coupon with allocations left
            var suffix = string.Empty;
            do
            {
                suffix = GenerateId(3);
                // Check if private coupon group already exists
                var couponEntityId = $"{CommerceEntity.IdPrefix<PrivateCouponGroup>()}{prefix}-{suffix}";
                privateCoupon = null; //await _commander.GetEntity<PrivateCouponGroup>(commerceContext, couponEntityId);
                if (privateCoupon == null)
                {
                    // No, it does not exist yet
                    break;
                }
            } while (true
            ); // WARNING: This will run until it finds an private coupon group that does not exist. You might want to limit the number of runs
            
            await _commander.ProcessWithTransaction(commerceContext,
                    () => _commander.Pipeline<IAddPrivateCouponPipeline>().RunAsync(
                        new AddPrivateCouponArgument(promotion, prefix, suffix, 1000), commerceContext.PipelineContextOptions))
                .ConfigureAwait(false);
            
            var privateCouponEntityId = $"{CommerceEntity.IdPrefix<PrivateCouponGroup>()}{prefix}-{suffix}";
            privateCoupon =
                await _commander.GetEntity<PrivateCouponGroup>(commerceContext, privateCouponEntityId);
            return privateCoupon;
        }

        private async Task<IEnumerable<PrivateCouponGroup>> GetPrivateCouponGroups(CommerceContext commerceContext, Promotion promotion)
        {
            // Get private coupons for this promotion. This is done using a list.
            var listName = string.Format(CultureInfo.InvariantCulture,
                commerceContext.GetPolicy<KnownCouponsListsPolicy>().PrivateCouponGroups, promotion.FriendlyId);
            var findEntitiesInListArgument =
                new FindEntitiesInListArgument(typeof(PrivateCouponGroup), listName, 0, Int32.MaxValue);
            findEntitiesInListArgument = await this._commander.Pipeline<IFindEntitiesInListPipeline>()
                .RunAsync(findEntitiesInListArgument, commerceContext.PipelineContext);

            var privateCoupons = findEntitiesInListArgument.List.Items.OfType<PrivateCouponGroup>();
            return privateCoupons;
        }

        private async Task<string> GetCouponAllocation(CommerceContext commerceContext, string promotionId, PrivateCouponGroup privateCoupon)
        {
            // Allocate a new coupon code from the private coupon group
            var command = _commander.Command<NewCouponAllocationCommand>();
            var result = await command.Process(commerceContext, promotionId, privateCoupon.Id, 1);

            var promotionCodeGroup = await _commander.GetEntity<PrivateCouponGroup>(commerceContext, privateCoupon.Id);

            // Get the last promotion code added
            var couponAllocations =
                promotionCodeGroup.EntityComponents.OfType<CouponAllocationComponent>().ToList();
            if (couponAllocations.Any())
            {
                var lastCouponAllocation = couponAllocations.Last();
                return lastCouponAllocation.Codes.First();
            }

            return null;
        }

        private string GenerateId(int length)
        {
            StringBuilder builder = new StringBuilder();
            Enumerable
                .Range(65, 26)
                .Select(e => ((char) e).ToString())
                .OrderBy(e => Guid.NewGuid())
                .Take(length)
                .ToList().ForEach(e => builder.Append(e));
            string id = builder.ToString();
            return id;
        }
    }
}
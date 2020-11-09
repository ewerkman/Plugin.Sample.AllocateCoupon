using Microsoft.AspNet.OData.Builder;
using Sitecore.Commerce.Core;
using Sitecore.Framework.Pipelines;

namespace Plugin.Sample.AllocateCoupon
{
    public class ConfigureServiceApiBlock : SyncPipelineBlock<ODataConventionModelBuilder, ODataConventionModelBuilder, CommercePipelineExecutionContext>
    {
        public override ODataConventionModelBuilder Run(ODataConventionModelBuilder arg, CommercePipelineExecutionContext context)
        {
            var allocateCouponAction = arg.Function("AllocateCoupon");
            allocateCouponAction.Parameter<string>("promotionId");
            allocateCouponAction.Parameter<string>("groupId");
            allocateCouponAction.Returns<string>();
            
            return arg;
        }
    }
}
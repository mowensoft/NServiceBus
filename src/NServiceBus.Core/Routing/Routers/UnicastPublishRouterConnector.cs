namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Pipeline;
    using Routing;
    using Unicast.Queuing;

    class UnicastPublishRouterConnector : StageConnector<IOutgoingPublishContext, IOutgoingLogicalMessageContext>
    {
        public UnicastPublishRouterConnector(IUnicastRouter unicastRouter, DistributionPolicy distributionPolicy)
        {
            this.unicastRouter = unicastRouter;
            this.distributionPolicy = distributionPolicy;
        }

        public override async Task Invoke(IOutgoingPublishContext context, Func<IOutgoingLogicalMessageContext, Task> stage)
        {
            var eventType = context.Message.MessageType;
            var addressLabels = await GetRoutingStrategies(context, eventType).ConfigureAwait(false);
            if (addressLabels.Count == 0)
            {
                //No subscribers for this message.
                return;
            }

            context.Headers[Headers.MessageIntent] = MessageIntentEnum.Publish.ToString();

            try
            {
                await stage(this.CreateOutgoingLogicalMessageContext(context.Message, addressLabels, context)).ConfigureAwait(false);
            }
            catch (QueueNotFoundException ex)
            {
                throw new Exception($"The destination queue '{ex.Queue}' could not be found. The destination may be misconfigured for this kind of message ({eventType}) in the MessageEndpointMappings of the UnicastBusConfig section in the configuration file. It may also be the case that the given queue hasn\'t been created yet, or has been deleted.", ex);
            }
        }

        async Task<List<UnicastRoutingStrategy>> GetRoutingStrategies(IOutgoingPublishContext context, Type eventType)
        {
            var distributionStrategy = distributionPolicy.GetDistributionStrategy(eventType);
            var addressLabels = await unicastRouter.Route(eventType, distributionStrategy, context.Extensions).ConfigureAwait(false);
            return addressLabels.ToList();
        }

        DistributionPolicy distributionPolicy;
        IUnicastRouter unicastRouter;
    }
}
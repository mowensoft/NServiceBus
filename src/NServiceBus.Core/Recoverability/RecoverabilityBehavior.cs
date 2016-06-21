namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Pipeline;

    class RecoverabilityBehavior : Behavior<ITransportReceiveContext>
    {
        public RecoverabilityBehavior(FirstLevelRetriesBehavior flrBehavior, SecondLevelRetriesBehavior slrBehavior, MoveFaultsToErrorQueueBehavior errorBehavior,
            bool flrEnabled, bool slrEnabled)
        {
            this.flrBehavior = flrBehavior;
            this.slrBehavior = slrBehavior;
            this.errorBehavior = errorBehavior;

            this.flrEnabled = flrEnabled;
            this.slrEnabled = slrEnabled;
        }

        public override Task Invoke(ITransportReceiveContext context, Func<Task> next)
        {
            var chainInvocation = next;

            if (flrEnabled)
            {
                chainInvocation = () => flrBehavior.Invoke(context, next);
            }

            if (slrEnabled)
            {
                var afterSlrInvocation = chainInvocation;

                chainInvocation = () => slrBehavior.Invoke(context, afterSlrInvocation);
            }

            var afterErrorInvocation = chainInvocation;

            chainInvocation = () => errorBehavior.Invoke(context, afterErrorInvocation);

            return chainInvocation();
        }

        FirstLevelRetriesBehavior flrBehavior;
        SecondLevelRetriesBehavior slrBehavior;
        MoveFaultsToErrorQueueBehavior errorBehavior;

        bool flrEnabled;
        bool slrEnabled;
    }
}
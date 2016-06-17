namespace NServiceBus.AcceptanceTests.Pipeline
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NServiceBus.Pipeline;
    using NUnit.Framework;

    public class When_using_per_uow_component_in_the_pipeline : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task It_should_be_scoped_to_uow_both_in_behavior_and_in_the_handler()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<Endpoint>(e => e
                    .When(async s =>
                    {
                        await SendMessage(s).ConfigureAwait(false);
                        await SendMessage(s).ConfigureAwait(false);
                    }))
                .Done(c => c.MessageHandled)
                .Run();

            Assert.IsTrue(context.ValuesMatch);
        }

        static async Task SendMessage(IMessageSession s)
        {
            var uniqueValue = Guid.NewGuid().ToString();
            var options = new SendOptions();
            options.RouteToThisEndpoint();
            options.SetHeader("Value", uniqueValue);
            var message = new Message
            {
                Value = uniqueValue
            };

            await s.Send(message, options).ConfigureAwait(false);
        }

        class Context : ScenarioContext
        {
            public bool ValuesMatch { get; set; }
            public bool MessageHandled { get; set; }
        }

       
        class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.RegisterComponents(r => r.ConfigureComponent<UnitOfWorkComponent>(DependencyLifecycle.InstancePerUnitOfWork));
                    c.Pipeline.Register(new HeaderProcessingBehavior(), "Populates UoW component.");
                    c.LimitMessageProcessingConcurrencyTo(1);
                });
            }

            class HeaderProcessingBehavior : Behavior<IIncomingLogicalMessageContext>
            {
                public override Task Invoke(IIncomingLogicalMessageContext context, Func<Task> next)
                {
                    var o = context.Builder.Build<UnitOfWorkComponent>();

                    Assert.IsNull(o.ValueFromHeader, "The UoW component has not been properly resolved.");

                    o.ValueFromHeader = context.MessageHeaders["Value"];

                    return next();
                }
            }

            internal class UnitOfWorkComponent
            {
                public string ValueFromHeader { get; set; }
            }

            public class Handler : IHandleMessages<Message>
            {
                public Handler(Context testContext, UnitOfWorkComponent component)
                {
                    this.testContext = testContext;
                    this.component = component;
                }

                public Task Handle(Message message, IMessageHandlerContext context)
                {
                    testContext.MessageHandled = true;
                    testContext.ValuesMatch = message.Value == component.ValueFromHeader;
                    return Task.FromResult(0);
                }

                Context testContext;
                UnitOfWorkComponent component;
            }
        }

        class Message : IMessage
        {
            public string Value { get; set; }
        }
    }
}
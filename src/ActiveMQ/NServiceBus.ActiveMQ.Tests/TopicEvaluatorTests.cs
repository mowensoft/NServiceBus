﻿namespace NServiceBus.ActiveMQ
{
    using FluentAssertions;

    using NUnit.Framework;

    [TestFixture]
    public class TopicEvaluatorTests
    {
        private TopicEvaluator testee;

        [SetUp]
        public void SetUp()
        {
            this.testee = new TopicEvaluator();
        }    

        [Test]
        public void GetTopicFromMessageType_ShouldReturnTheFirstMessageTypePreceededByVirtualTopic()
        {
            var topic = this.testee.GetTopicFromMessageType(typeof(ISimpleMessage).AssemblyQualifiedName + ";" + typeof(TopicEvaluatorTests).AssemblyQualifiedName);

            topic.Should().Be("VirtualTopic." + typeof(ISimpleMessage).Name);
        }
    }

    public interface ISimpleMessage
    {
    }
}
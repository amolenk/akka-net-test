using Akka.Actor;
using Akka.Configuration;
using Akka.TestKit;
using Akka.TestKit.Xunit2;
using AkkaTest;
using System;
using Xunit;

namespace UnitTestProject1
{
    public class CollectorActorFixture : TestKit
    {
        public CollectorActorFixture()
            : base(@"akka.scheduler.implementation = ""Akka.TestKit.TestScheduler, Akka.TestKit""")
        {
        }

        private TestScheduler Scheduler => (TestScheduler)Sys.Scheduler;

        [Fact]
        public void IdleState_ReceivedStartMessage_ShouldSendReadChannelMessage()
        {
            // Arrange

            var channelName = "SomeChannel";
            var interval = TimeSpan.FromSeconds(5);
            var timeOut = TimeSpan.FromMinutes(1);

            var driverActor = CreateTestProbe("driver");
            var sut = Sys.ActorOf(CollectorActor.CreateProps(channelName, interval, timeOut));

            // Act
            sut.Tell(new CollectorActor.Start());

            // Assert
            driverActor.ExpectMsg<DriverActor.ReadChannel>(msg => msg.ChannelName == channelName);
        }

        [Fact]
        public void WaitingState_ReceivedReadingsAvailableMessage_ShouldSendReadChannelMessageAfterInterval()
        {
            // Arrange

            var channelName = "SomeChannel";
            var interval = TimeSpan.FromSeconds(5);
            var timeOut = TimeSpan.FromMinutes(1);

            var driverActor = CreateTestProbe("driver");

            var sut = Sys.ActorOf(CollectorActor.CreateProps(channelName, interval, timeOut));
            sut.Tell(new CollectorActor.Start());

            var readChannelMessage = driverActor.ExpectMsg<DriverActor.ReadChannel>();

            // Act
            sut.Tell(new DriverActor.ReadingsAvailable(readChannelMessage.ChannelName));

            Scheduler.Advance(interval);

            // Assert
            driverActor.ExpectMsg<DriverActor.ReadChannel>(msg => msg.ChannelName == channelName);
        }

        [Fact]
        public void WaitingState_ReceivedReadTimedOutMessage_ShouldSendReadChannelMessage()
        {
            // Arrange

            var channelName = "SomeChannel";
            var interval = TimeSpan.FromSeconds(5);
            var timeOut = TimeSpan.FromMinutes(1);

            var driverActor = CreateTestProbe("driver");

            var sut = Sys.ActorOf(CollectorActor.CreateProps(channelName, interval, timeOut));
            sut.Tell(new CollectorActor.Start());

            var readChannelMessage = driverActor.ExpectMsg<DriverActor.ReadChannel>();

            // Act
            Scheduler.Advance(timeOut);

            // Assert
            driverActor.ExpectMsg<DriverActor.ReadChannel>(msg => msg.ChannelName == channelName);
        }       
    }
}

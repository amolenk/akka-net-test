using Akka.Actor;
using Akka.Configuration;
using Akka.TestKit;
using Akka.TestKit.Configs;
using Akka.TestKit.Xunit2;
using AkkaTest;
using Moq;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace UnitTestProject1
{
    // TODO Also add test for ReadChannelPeriod

    public class DriverActorFixture : TestKit
    {
        public DriverActorFixture()
            : base(TestConfigs.TestSchedulerConfig)
            //@"akka.scheduler.implementation = ""Akka.TestKit.TestScheduler, Akka.TestKit""")
        {
        }

        private TestScheduler Scheduler => (TestScheduler)Sys.Scheduler;

        #region ReadChannel Message

        [Fact]
        public void ReadChannelMessage_ShouldReadChannel()
        {
            // Arrange

            var channelName = "SomeChannel";
            var connectedEvent = new ManualResetEventSlim(false);
            var driverMock = new Mock<IMeterDriver>();

            driverMock
                .Setup(m => m.ReadChannelAsync(channelName, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new string[] { "foo" });

            var sut = Sys.ActorOf(DriverActor.CreateProps(driverMock.Object));

            // Act
            sut.Tell(new DriverActor.ReadChannel(channelName));

            // Assert
            ExpectMsg<DriverActor.ReadingsAvailable>(msg => msg.ChannelName == channelName);
        }

        [Fact]
        public void ReadChannelMessage_ShouldConnectIfDisconnected()
        {
            // Arrange

            var channelName = "SomeChannel";
            var connectedEvent = new ManualResetEventSlim(false);
            var driverMock = new Mock<IMeterDriver>();

            driverMock
                .Setup(m => m.ConnectAsync(It.IsAny<CancellationToken>()))
                .Callback(connectedEvent.Set)
                .Returns(Task.CompletedTask);

            var sut = Sys.ActorOf(DriverActor.CreateProps(driverMock.Object));

            // Act
            sut.Tell(new DriverActor.ReadChannel(channelName));

            // Assert
            connectedEvent.Wait(TimeSpan.FromSeconds(3));

            driverMock.Verify(m => m.ConnectAsync(It.IsAny<CancellationToken>()));
        }

        [Fact]
        public void ReadChannelMessage_ShouldNotConnectIfAlreadyConnected()
        {
            // Arrange

            var channelName = "SomeChannel";
            var connectCalled = false;
            var driverMock = new Mock<IMeterDriver>();

            driverMock
                .Setup(m => m.ConnectAsync(It.IsAny<CancellationToken>()))
                .Callback(() => connectCalled = true)
                .Returns(Task.CompletedTask);

            driverMock
                .Setup(m => m.ReadChannelAsync(channelName, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new string[] { "foo" });

            var sut = Sys.ActorOf(DriverActor.CreateProps(driverMock.Object));

            // Get some readings to make sure the driver is connected.
            sut.Tell(new DriverActor.ReadChannel(channelName));
            ExpectMsg<DriverActor.ReadingsAvailable>();
            connectCalled = false;

            // Act
            sut.Tell(new DriverActor.ReadChannel(channelName));

            // Assert

            ExpectMsg<DriverActor.ReadingsAvailable>(msg => msg.ChannelName == channelName);

            Assert.False(connectCalled);
        }

        [Fact]
        public void ReadChannelMessage_ShouldBeStashedIfBusy()
        {
            // Arrange

            var channelName = "SomeChannel";
            var driverMock = new Mock<IMeterDriver>();

            driverMock
                .Setup(m => m.ReadChannelAsync(channelName, It.IsAny<CancellationToken>()))
                .Returns(async () =>
                {
                    await Task.Delay(TimeSpan.FromSeconds(3));
                    return new string[] { "Foo" };
                });

            var sut = ActorOfAsTestActorRef<DriverActor>(DriverActor.CreateProps(driverMock.Object));

            sut.Tell(new DriverActor.ReadChannel(channelName));

            // Act
            sut.Tell(new DriverActor.ReadChannel(channelName));

            // Assert
            Assert.Equal(1, sut.UnderlyingActor.Stash.ClearStash().Count());
        }

        [Fact]
        public void ReadChannelMessage_ShouldBeStashedIfConnecting()
        {
            // Arrange

            var channelName = "SomeChannel";
            var driverMock = new Mock<IMeterDriver>();

            driverMock
                .Setup(m => m.ConnectAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.Delay(TimeSpan.FromSeconds(3)));

            driverMock
                .Setup(m => m.ReadChannelAsync(channelName, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new string[] { "Foo" });

            var sut = ActorOfAsTestActorRef<DriverActor>(DriverActor.CreateProps(driverMock.Object));

            // Act
            sut.Tell(new DriverActor.ReadChannel(channelName));

            // Assert
            Assert.Equal(1, sut.UnderlyingActor.Stash.ClearStash().Count());
        }
       
        [Fact]
        public void ReadChannelMessage_ShouldRetryIfConnectionFailed()
        {
            // Arrange

            var channelName = "SomeChannel";
            var connectCount = 0;
            var driverMock = new Mock<IMeterDriver>();

            driverMock
                .Setup(m => m.ConnectAsync(It.IsAny<CancellationToken>()))
                .Callback(() =>
                {
                    if (++connectCount == 1)
                    {
                        throw new Exception("Epic connection failure!");
                    }
                })
                .Returns(Task.CompletedTask);

            driverMock
                .Setup(m => m.ReadChannelAsync(channelName, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new string[] { "Foo" });

            var sut = Sys.ActorOf(DriverActor.CreateProps(driverMock.Object));

            // Act
            sut.Tell(new DriverActor.ReadChannel(channelName));

            // Assert

            ExpectMsg<DriverActor.Retrying>();

            Scheduler.Advance(TimeSpan.FromMinutes(1));

            ExpectMsg<DriverActor.ReadingsAvailable>(msg => msg.ChannelName == channelName);
            
            Assert.Equal(2, connectCount);
        }

        [Fact]
        public void ReadChannelMessage_ShouldRetryIfReadFailed()
        {
            // Arrange

            var channelName = "SomeChannel";
            var readCount = 0;
            var driverMock = new Mock<IMeterDriver>();

            driverMock
                .Setup(m => m.ConnectAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            driverMock
                .Setup(m => m.ReadChannelAsync(channelName, It.IsAny<CancellationToken>()))
                .Callback(() =>
                {
                    if (++readCount == 1)
                    {
                        throw new Exception("Epic read failure!");
                    }
                })
                .ReturnsAsync(new string[] { "Foo" });

            var sut = Sys.ActorOf(DriverActor.CreateProps(driverMock.Object));

            // Act
            sut.Tell(new DriverActor.ReadChannel(channelName));

            // Assert

            ExpectMsg<DriverActor.ReadingsAvailable>(msg => msg.ChannelName == channelName);

            Assert.Equal(2, readCount);
        }

        #endregion
    }
}

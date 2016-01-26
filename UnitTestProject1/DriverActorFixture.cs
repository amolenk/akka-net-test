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
    public class DriverActorFixture : TestKit
    {
        public DriverActorFixture() : base(TestConfigs.TestSchedulerConfig)
        {
        }

        private TestScheduler Scheduler => (TestScheduler)Sys.Scheduler;

        #region ReadChannel Message

        [Fact]
        public void ReadChannelMessage_ShouldReadChannel()
        {
            // Arrange
            var channelName = "SomeChannel";
            var driver = new MeterDriverFake();
            var sut = Sys.ActorOf(DriverActor.CreateProps(driver));

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
            var driver = new MeterDriverFake();
            var sut = Sys.ActorOf(DriverActor.CreateProps(driver));

            // Act
            sut.Tell(new DriverActor.ReadChannel(channelName));

            // Assert
            ExpectMsg<DriverActor.ReadingsAvailable>(msg => msg.ChannelName == channelName);
            Assert.Equal(1, driver.ConnectCount);
        }

        [Fact]
        public void ReadChannelMessage_ShouldNotConnectIfAlreadyConnected()
        {
            // Arrange
            var channelName = "SomeChannel";
            var driver = new MeterDriverFake();
            var sut = Sys.ActorOf(DriverActor.CreateProps(driver));

            // Get some readings to make sure the driver is connected.
            sut.Tell(new DriverActor.ReadChannel(channelName));
            ExpectMsg<DriverActor.ReadingsAvailable>();

            // Act
            sut.Tell(new DriverActor.ReadChannel(channelName));

            // Assert
            ExpectMsg<DriverActor.ReadingsAvailable>(msg => msg.ChannelName == channelName);

            // Check that we haven't connected more than once.
            Assert.Equal(1, driver.ConnectCount);
        }

        [Fact]
        public void ReadChannelMessage_ShouldBeStashedIfBusy()
        {
            // Arrange
            var channelName = "SomeChannel";
            var driver = new MeterDriverFake { ReadDuration = TimeSpan.FromSeconds(3) };
            var sut = ActorOfAsTestActorRef<DriverActor>(DriverActor.CreateProps(driver));

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
            var driver = new MeterDriverFake { ConnectDuration = TimeSpan.FromSeconds(3) };
            var sut = ActorOfAsTestActorRef<DriverActor>(DriverActor.CreateProps(driver));

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
            var supervisor = CreateTestProbe("supervisor");
            var driver = new MeterDriverFake { ThrowExceptionOnFirstConnect = true };
            var sut = ActorOfAsTestActorRef<DriverActor>(DriverActor.CreateProps(driver), supervisor);

            // Act
            sut.Tell(new DriverActor.ReadChannel(channelName));

            // Assert

            var errorMessage = supervisor.ExpectMsg<DriverActor.ErrorOccured>();

            Scheduler.Advance(errorMessage.RetryWaitTime);

            ExpectMsg<DriverActor.ReadingsAvailable>(msg => msg.ChannelName == channelName);
            
            Assert.Equal(2, driver.ConnectCount);
        }

        //[Fact]
        //public void ReadChannelMessage_ShouldRetryIfReadFailed()
        //{
        //    // Arrange

        //    var channelName = "SomeChannel";
        //    var readCount = 0;
        //    var driverMock = new Mock<IMeterDriver>();

        //    driverMock
        //        .Setup(m => m.ConnectAsync(It.IsAny<CancellationToken>()))
        //        .Returns(Task.CompletedTask);

        //    driverMock
        //        .Setup(m => m.ReadChannelAsync(channelName, It.IsAny<CancellationToken>()))
        //        .Callback(() =>
        //        {
        //            if (++readCount == 1)
        //            {
        //                throw new Exception("Epic read failure!");
        //            }
        //        })
        //        .ReturnsAsync(new string[] { "Foo" });

        //    var sut = Sys.ActorOf(DriverActor.CreateProps(driverMock.Object));

        //    // Act
        //    sut.Tell(new DriverActor.ReadChannel(channelName));

        //    // Assert

        //    ExpectMsg<DriverActor.ReadingsAvailable>(msg => msg.ChannelName == channelName);

        //    Assert.Equal(2, readCount);
        //}

        #endregion
    }
}

using Akka.Actor;
using ColorConsole;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AkkaTest
{
    // TODO Disconnect when work is done.

    public class DriverActor : ReceiveActor, IWithUnboundedStash
    {
        #region Message Types

        public class ConnectionEstablished
        {
        }

        public class ConnectionFailed
        {
            public ConnectionFailed(Exception exception)
            {
                this.Exception = exception;
            }

            public Exception Exception { get; set; }
        }

        public class ConnectionTerminated
        {
            public ConnectionTerminated(bool errorOccured)
            {
                this.ErrorOccured = errorOccured;
            }

            public bool ErrorOccured { get; set; }
        }

        public class ReadChannel
        {
            public ReadChannel(string channelName)
            {
                this.ChannelName = channelName;
            }

            public string ChannelName { get; set; }
        }

        public class ReadChannelPeriod
        {
        }

        public class ReadingsAvailable
        {
            public ReadingsAvailable(string channelName)
            {
                this.ChannelName = channelName;
            }

            public string ChannelName { get; private set; }
        }

        public class ReadChannelFailed
        {
            public ReadChannelFailed(Exception exception)
            {
                this.Exception = exception;
            }

            public Exception Exception { get; set; }
        }

        public class Retry
        {
        }

        public class Retrying
        {
        }

        #endregion

        /// <summary>
        /// The maximum number of read requests to keep queued.
        /// </summary>
        private const int MAX_QUEUED_READ_REQUESTS = 10;

        private readonly IMeterDriver _driver;

        private int _queuedReadRequestCount;

        public DriverActor(IMeterDriver driver)
        {
            _driver = driver;

            this.DisconnectedState();
        }

        public IStash Stash { get; set; }

        public static Props CreateProps(IMeterDriver driver)
        {
            return Props.Create(() => new DriverActor(driver));
        }

        protected override void PostRestart(Exception reason)
        {
            base.PostRestart(reason);

            Stash.UnstashAll();
        }

        private void DisconnectedState()
        {
            Receive<ReadChannel>(msg =>
            {
                this.EnqueueReadRequest();
                this.BeginConnect();
            });

            Receive<ReadChannelPeriod>(msg =>
            {
                this.EnqueueReadRequest();
                this.BeginConnect();
            });
        }

        private void ConnectingState()
        {
            Receive<ConnectionEstablished>(msg =>
            {
                Become(ConnectedState);

                this.TryDequeueReadRequest();
            });

            Receive<ConnectionFailed>(_ =>
            {
                File.WriteAllText(@"C:\Users\Sander\Downloads\ConnectionFailed.txt", "Foo");

                this.RetryAfterWaitTime();
            });

            Receive<ReadChannel>(_ => this.EnqueueReadRequest());
            Receive<ReadChannelPeriod>(_ => this.EnqueueReadRequest());
        }

        private void ConnectedState()
        {
            Context.ActorSelection(ActorNames.ConsoleWriter).Tell(new ConsoleWriterActor.Write(
                ConsoleColor.Yellow, "State: Connected"));

            Receive<ReadChannel>(msg =>
            {
                Context.ActorSelection(ActorNames.ConsoleWriter).Tell(new ConsoleWriterActor.Write(
                    ConsoleColor.DarkGreen, "Reading channel: " + msg.ChannelName));

                Become(ReadingState);

                var senderClosure = this.Sender;

                Task.Run(() => _driver.ReadChannelAsync(msg.ChannelName, CancellationToken.None))
                    .ContinueWith(task =>
                    {
                        object message = null;

                        if (task.IsFaulted)
                        {
                            message = new ReadChannelFailed(task.Exception);
                        }
                        else
                        {
                            message = new ReadingsAvailable(msg.ChannelName);
                        }

                        return message;
                    },
                    TaskContinuationOptions.AttachedToParent & TaskContinuationOptions.ExecuteSynchronously)
                    .PipeTo(Self, senderClosure);
            });

            // TODO ReadChannelPeriod
        }

        private void DisconnectingState()
        {
            Receive<ConnectionTerminated>(msg =>
            {
                if (msg.ErrorOccured)
                {
                    this.RetryAfterWaitTime();
                }
                else
                {
                    Become(DisconnectedState);

                    this.TryDequeueReadRequest();
                }
            });

            Receive<ReadChannel>(_ => this.EnqueueReadRequest());
            Receive<ReadChannelPeriod>(_ => this.EnqueueReadRequest());
        }

        private void ReadingState()
        {
            Context.ActorSelection(ActorNames.ConsoleWriter).Tell(new ConsoleWriterActor.Write(
                ConsoleColor.Yellow, "State: Reading"));

            Receive((ReadingsAvailable msg) =>
            {
                Context.ActorSelection(ActorNames.ConsoleWriter).Tell(new ConsoleWriterActor.Write(
                    ConsoleColor.Green, "Reading available: " + msg.ChannelName));

                Sender.Tell(msg);

                Become(this.ConnectedState);

                this.TryDequeueReadRequest();
            });

            Receive<ReadChannelFailed>(_ =>
            {
                BeginDisconnect(true);
            });

            Receive<ReadChannel>(_ => this.EnqueueReadRequest());
            Receive<ReadChannelPeriod>(_ => this.EnqueueReadRequest());
        }

        private void ErrorState()
        {
            // TODO Notify parent of error (also notify when next succesful call has happened)

            Context.ActorSelection(ActorNames.ConsoleWriter).Tell(new ConsoleWriterActor.Write(
                ConsoleColor.Red, "State: Error"));

            Receive<Retry>(_ =>
            {
                Become(DisconnectedState);

                this.TryDequeueReadRequest();
            });

            Receive<ReadChannel>(_ => this.EnqueueReadRequest());
            Receive<ReadChannelPeriod>(_ => this.EnqueueReadRequest());
        }

        #region Private Helper Methods

        private void RetryAfterWaitTime()
        {
            Become(ErrorState);

            //Self.Tell(new Retry());

            Context.System.Scheduler.ScheduleTellOnce(TimeSpan.FromSeconds(30), Self, new Retry(), Self);

            Sender.Tell(new Retrying());

            //Context.System.Scheduler.ScheduleTellOnce(TimeSpan.FromSeconds(0), Self, new Retry(), Self);
        }

        private void BeginConnect()
        {
            Become(ConnectingState);

            var senderClosure = Sender;

            Task.Run(() => _driver.ConnectAsync(CancellationToken.None))
                .ContinueWith(task =>
                {
                    if (task.IsFaulted)
                    {
                        return (object)new ConnectionFailed(task.Exception);
                    }
                    else
                    {
                        return (object)new ConnectionEstablished();
                    }
                },
                TaskContinuationOptions.AttachedToParent & TaskContinuationOptions.ExecuteSynchronously)
                .PipeTo(Self, senderClosure);
        }

        private void BeginDisconnect(bool errorOccured)
        {
            Become(DisconnectingState);

            _driver
                .CloseAsync()
                .ContinueWith(task => new ConnectionTerminated(errorOccured),
                    TaskContinuationOptions.AttachedToParent & TaskContinuationOptions.ExecuteSynchronously)
                .PipeTo(Self);
        }

        private void EnqueueReadRequest()
        {
            if (_queuedReadRequestCount < MAX_QUEUED_READ_REQUESTS)
            {
                Stash.Stash();
                _queuedReadRequestCount += 1;
            }
        }

        private void TryDequeueReadRequest()
        {
            if (_queuedReadRequestCount > 0)
            {
                Stash.Unstash();
                _queuedReadRequestCount -= 1;
            }
            else
            {
                // TODO Schedule disconnect?
            }
        }

        #endregion
    }
}

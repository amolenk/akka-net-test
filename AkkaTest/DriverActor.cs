using Akka.Actor;
using ColorConsole;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AkkaTest
{
    public class DriverActor : ReceiveActor, IWithUnboundedStash
    {
        #region Message Types

        public class ReadingsAvailable
        {
            public ReadingsAvailable(string channel)
            {
                this.Channel = channel;
            }

            public string Channel { get; private set; }
        }

        #endregion

        private readonly IMeterDriver _meterDriver = new FakeMeterDriver();
        private readonly IConsoleWriter _consoleWriter = new ConsoleWriter();

        private Task _currentAsyncTask;
        private CancellationTokenSource _cts;

        public DriverActor()
        {
            _consoleWriter = new ConsoleWriter();

            this.Idle();
        }

        public IStash Stash { get; set; }

        #region Behaviour Methods

        private void Idle()
        {
            _consoleWriter.WriteLine("State: Idle", ConsoleColor.Yellow);

            Receive<ReadChannel>(msg => this.Connect(msg));
        }

        private void Connecting()
        {
            _consoleWriter.WriteLine("State: Connecting", ConsoleColor.Yellow);

            Receive<Connected>(msg => this.WhenConnected(msg));
            ReceiveAny(msg =>
            {
                _consoleWriter.WriteLine("Stashing because Connecting: " + msg, ConsoleColor.Red);
                this.Stash.Stash();
            });
        }

        private void Connected()
        {
            _consoleWriter.WriteLine("State: Connected", ConsoleColor.Yellow);

            Receive<ReadChannel>(msg => this.ReadChannel(msg));
        }

        private void Busy()
        {
            _consoleWriter.WriteLine("State: Busy", ConsoleColor.Yellow);

            Receive<ReadingAvailable>(msg => this.WhenReadingAvailable(msg));

            Receive<ReadingsAvailable>(msg =>
            {
                Sender.Tell(msg);

                Become(Connected);
                Stash.Unstash();
            });

            ReceiveAny(msg =>
            {
                _consoleWriter.WriteLine("Stashing because Busy: " + msg, ConsoleColor.Red);
                this.Stash.Stash();
            });
        }

        #endregion

        #region Command Handlers

        private void Connect(object message)
        {
            Become(Connecting);
            Stash.Stash();

            _meterDriver
                .ConnectAsync(CancellationToken.None)
                .ContinueWith(task =>
                {
                    // TODO Different message if faulted.
                    return new Connected();
                })
                .PipeTo(Self);
        }

        private void ReadChannel(ReadChannel message)
        {
            _consoleWriter.WriteLine("Reading channel: " + message.Channel, ConsoleColor.DarkGreen);

            Become(Busy);


            var senderClosure = this.Sender;

            _cts = new CancellationTokenSource();
            _currentAsyncTask = _meterDriver
                .ReadChannelAsync(message.Channel, _cts.Token)
                .ContinueWith(task =>
                {
                    if (task.IsCanceled)
                    {
                        Console.WriteLine("CANCELED!!!");
                        return new ReadingsAvailable("CANCELED");
                    }

                    // TODO Different message if faulted.
                    return new ReadingsAvailable(task.Result.First());
                }, 
                TaskContinuationOptions.AttachedToParent & TaskContinuationOptions.ExecuteSynchronously)
                .PipeTo(Self, senderClosure);
        }

        #endregion

        protected override void PostStop()
        {
            _cts?.Cancel();
            _currentAsyncTask.Wait();

            base.PostStop();
        }

        #region Event Handlers

        private void WhenConnected(Connected message)
        {
            Become(Connected);
            Stash.Unstash();
        }

        private void WhenReadingAvailable(ReadingAvailable message)
        {
            _consoleWriter.WriteLine("Reading available: " + message.Channel, ConsoleColor.Green);

            message.Actor.Tell(message);

            Become(Connected);
            Stash.Unstash();
        }

        #endregion
    }
}

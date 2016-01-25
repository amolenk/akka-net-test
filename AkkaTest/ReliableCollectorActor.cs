using Akka.Actor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AkkaTest
{
    public class ReliableCollectorActor : ReceiveActor
    {
        #region Message Types

        public class Start
        {
        }

        public class Collect
        {
        }

        public class ReadTimedOut
        {
        }

        public class ReadingsCollected
        {
        }

        #endregion

        private readonly string _channelName;
        private readonly TimeSpan _interval;
        private readonly TimeSpan _timeOut;

        private ICancelable _timeOutCancelable;

        public ReliableCollectorActor(string channelName, TimeSpan interval, TimeSpan timeOut)
        {
            _channelName = channelName;
            _interval = interval;
            _timeOut = timeOut;

            this.Idle();
        }

        /// <summary>
        /// Create Props for an actor of this type.
        /// </summary>
        /// <param name="magicNumber">
        /// The magic number to be passed to this actor’s constructor.
        /// </param>
        /// <returns>
        /// A Props for creating this actor, which can then be further configured
        /// (e.g. calling `.withDispatcher()` on it)
        /// </returns>
        public static Props CreateProps(string channelName, TimeSpan interval, TimeSpan timeOut)
        {
            return Props.Create(() => new ReliableCollectorActor(channelName, interval, timeOut));
        }

        private void Idle()
        {
            Receive<Start>(msg =>
            {
                Self.Tell(new Collect());

                Become(Started);
            });
        }

        private void Started()
        {
            Receive<Collect>(msg =>
            {
                _timeOutCancelable = Context.System.Scheduler.ScheduleTellOnceCancelable(
                    _timeOut, Self, new ReadTimedOut(), Self);

                Context.ActorSelection(ActorNames.DriverSibling).Tell(new DriverActor.ReadChannel(_channelName));

                Become(WaitingForReadResults);
            });
        }

        private void WaitingForReadResults()
        {
            Receive<DriverActor.ReadingsAvailable>(msg =>
            {
                _timeOutCancelable.Cancel();

                Context.Parent.Tell(new ReadingsCollected());

                Context.System.Scheduler.ScheduleTellOnce(_interval, Self, new Collect(), Self);
                Become(Started);
            });

            Receive<ReadTimedOut>(msg =>
            {
                Console.WriteLine("Time out!");

                Self.Tell(new Collect());

                Become(Started);
            });
        }
    }
}

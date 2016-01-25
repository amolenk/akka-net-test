using Akka.Actor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AkkaTest
{
    public class CollectorActor : ReceiveActor
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

        public CollectorActor(string channelName, TimeSpan interval, TimeSpan timeOut)
        {
            _channelName = channelName;
            _interval = interval;
            _timeOut = timeOut;

            this.IdleState();
        }

        public static Props CreateProps(string channelName, TimeSpan interval, TimeSpan timeOut)
        {
            return Props.Create(() => new CollectorActor(channelName, interval, timeOut));
        }

        private void IdleState()
        {
            Receive<Start>(msg =>
            {
                Become(StartedState);

                Self.Tell(new Collect());
            });
        }

        private void StartedState()
        {
            Receive<Collect>(msg =>
            {
                _timeOutCancelable = Context.System.Scheduler.ScheduleTellOnceCancelable(
                    _timeOut, Self, new ReadTimedOut(), Self);

                Context.ActorSelection(ActorNames.DriverSibling).Tell(new DriverActor.ReadChannel(_channelName));

                Become(WaitingForReadResultsState);
            });
        }

        private void WaitingForReadResultsState()
        {
            Receive<DriverActor.ReadingsAvailable>(msg =>
            {
                _timeOutCancelable.Cancel();

                Context.Parent.Tell(new ReadingsCollected());

                Context.System.Scheduler.ScheduleTellOnce(_interval, Self, new Collect(), Self);
                Become(StartedState);
            });

            Receive<ReadTimedOut>(msg =>
            {
                Console.WriteLine("Time out!");

                Self.Tell(new Collect());

                Become(StartedState);
            });
        }
    }
}

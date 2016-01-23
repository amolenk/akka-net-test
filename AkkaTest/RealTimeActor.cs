using Akka.Actor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AkkaTest
{
    public class RealTimeActor : ReceiveActor
    {
        #region Message Types

        public class Start
        {
        }

        public class Stop
        {
        }

        public class Read
        {
        }

        #endregion

        private bool _cancellationPending;

        public RealTimeActor()
        {
            this.Initialize();
        }

        public void Initialize()
        {
            this.Receive<StartRealTime>(msg =>
            {
                Self.Tell(new Read());
            });

            this.Receive<Stop>(msg =>
            {
                _cancellationPending = true;
            });

            this.Receive<Read>(msg =>
            {
                Context.ActorSelection("../driver").Tell(new ReadChannel("RT"));
            });

            this.Receive<DriverActor.ReadingsAvailable>(msg =>
            {
                Console.WriteLine("GOT RESULT! YEAH!");

                if (!_cancellationPending)
                {
                    Self.Tell(new Read());
                }
            });
        }
    }
}

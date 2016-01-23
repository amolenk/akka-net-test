using Akka.Actor;
using ColorConsole;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AkkaTest
{
    public class MeterActor : ReceiveActor
    {
        private readonly IConsoleWriter _consoleWriter = new ConsoleWriter();

        public MeterActor()
        {
            _consoleWriter = new ConsoleWriter();

            this.Initialize();
        }

        private void Initialize()
        {
            var driverActor = Context.ActorOf<DriverActor>("driver");
            //   driverActor.Tell(new ReadChannel("Foo"));

            //Task.Delay(1000).Wait();

            var realTimeActor = Context.ActorOf<RealTimeActor>("realTime");
            realTimeActor.Tell(new StartRealTime());

            //realTimeActor.Tell(new RealTimeActor.Stop());
        }
    }
}

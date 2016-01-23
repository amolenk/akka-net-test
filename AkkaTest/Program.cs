using Akka.Actor;
using ColorConsole;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AkkaTest
{
    class Program
    {
        static void Main(string[] args)
        {
            var consoleWriter = new ConsoleWriter();

            // Create a new actor system (a container for your actors)
            var system = ActorSystem.Create("MySystem");

            // Create your actor and get a reference to it.
            // This will be an "IActorRef", which is not a reference to the actual actor
            // instance but rather a client or proxy to it.
            var meter = system.ActorOf<MeterActor>("meter");

            //Task.Run(async () =>
            //{

            //    for (var i = 1; i <= 5; i++)
            //    {
            //        var message = new ReadChannel("Hello, world! " + i);

            //        consoleWriter.WriteLine(message, ConsoleColor.Magenta);

            //        meter.Tell(message);
            //        await Task.Delay(40);
            //    }

            //    // alow some time for .NET Fiddle to flush
            //    await Task.Delay(500);
            //}).Wait();

            Console.ReadLine();

            system.Terminate().Wait();

            Console.WriteLine("Terminated!");

            Console.ReadLine();
        }
    }
}

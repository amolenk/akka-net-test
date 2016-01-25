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
        private IActorRef _publisherActor;
        private IActorRef _driverActor;

        public MeterActor()
        {
            this.Initialize();
            this.CreateChildren();
        }

        private void Initialize()
        {
            Receive<CollectorActor.ReadingsCollected>(msg =>
            {
                _publisherActor.Tell(new PublisherActor.Publish());
            });

            Receive<ReliableCollectorActor.ReadingsCollected>(msg =>
            {
                _publisherActor.Tell(new PublisherActor.PublishWithAck(), Sender);
            });
        }

        private void CreateChildren()
        {
            _publisherActor = Context.ActorOf<PublisherActor>("publisher");

            _driverActor = Context.ActorOf(DriverActor.CreateProps(
                new FakeMeterDriver()), "driver");

            var collectorActor = Context.ActorOf(CollectorActor.CreateProps(
                "SomeRegister", TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10)));

            collectorActor.Tell(new CollectorActor.Start());

            var reliableCollectorActor = Context.ActorOf(ReliableCollectorActor.CreateProps(
                "SomeProfile", TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(10)));

            reliableCollectorActor.Tell(new ReliableCollectorActor.Start());
        }
    }
}

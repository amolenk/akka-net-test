using Akka.Actor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AkkaTest
{
    public class PublisherActor : ReceiveActor
    {
        #region Message Types

        public class Publish
        {
        }

        public class PublishWithAck
        {
        }

        #endregion

        public PublisherActor()
        {
            this.Idle();
        }

        private void Idle()
        {
            Receive<Publish>(msg =>
            {
                Console.WriteLine("Publishing!");
            });

            Receive<PublishWithAck>(msg =>
            {
                Console.WriteLine("Publishing with ack!");
            });
        }
    }
}

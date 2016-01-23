using Akka.Actor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AkkaTest
{
    public class ReadingAvailable
    {
        public ReadingAvailable(string channel, IActorRef actor)
        {
            this.Channel = channel;
            this.Actor = actor;
        }

        public string Channel { get; private set; }
        public IActorRef Actor { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AkkaTest
{
    public class ReadChannel
    {
        public ReadChannel(string channel)
        {
            this.Channel = channel;
        }

        public string Channel { get; private set; }

        public override string ToString()
        {
            return "[ReadChannel] " + this.Channel;
        }
    }
}

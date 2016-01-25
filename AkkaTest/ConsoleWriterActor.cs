using Akka.Actor;
using ColorConsole;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AkkaTest
{
    public class ConsoleWriterActor : ReceiveActor
    {
        #region Message Types

        public class Write
        {
            public Write(ConsoleColor color, string format, params object[] args)
                : this(color, string.Format(format, args))
            {
            }

            public Write(ConsoleColor color, string message)
            {
                this.Message = message;
                this.Color = color;
            }

            public string Message { get; set; }
            public ConsoleColor Color { get; set; }
        }


        #endregion

        private readonly IConsoleWriter _writer;

        public ConsoleWriterActor()
        {
            _writer = new ConsoleWriter();

            this.Initialize();
        }

        private void Initialize()
        {
            Receive<Write>(msg =>
            {
                _writer.WriteLine(msg.Message, msg.Color);
            });
        }
    }
}

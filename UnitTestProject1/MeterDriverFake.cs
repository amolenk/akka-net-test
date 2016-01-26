using AkkaTest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace UnitTestProject1
{
    public class MeterDriverFake : IMeterDriver
    {
        public int ConnectCount { get; private set; }

        public TimeSpan ConnectDuration { get; set; }

        public TimeSpan ReadDuration { get; set; }

        public bool ThrowExceptionOnFirstConnect { get; set; }

        public MeterDriverFake()
        {
            this.ConnectDuration = TimeSpan.Zero;
            this.ReadDuration = TimeSpan.Zero;
        }

        public Task CloseAsync()
        {
            throw new NotImplementedException();
        }

        public Task ConnectAsync(CancellationToken cancellationToken)
        {
            this.ConnectCount += 1;

            if (this.ThrowExceptionOnFirstConnect && this.ConnectCount == 1)
            {
                throw new Exception("Connection failure");
            }

            return Task.Delay(this.ConnectDuration);
        }

        public Task<ICollection<string>> GetChannelNamesAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public void LoadState(IDictionary<string, object> state)
        {
            throw new NotImplementedException();
        }

        public async Task<ICollection<string>> ReadChannelAsync(string channelName, CancellationToken cancellationToken)
        {
            await Task.Delay(this.ReadDuration);

            return new string[0];
        }

        public Task<ICollection<string>> ReadChannelAsync(string channelName, DateTime from, DateTime to, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public IDictionary<string, object> SaveState()
        {
            throw new NotImplementedException();
        }

        public Task SynchronizeClockAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}

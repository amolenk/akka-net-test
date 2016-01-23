using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AkkaTest
{
    public class FakeMeterDriver : IMeterDriver
    {
        public Task CloseAsync()
        {
            return Task.CompletedTask;
        }

        public Task ConnectAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task<ICollection<string>> GetChannelNamesAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<ICollection<string>>(new string[] { "foo", "bar" });
        }

        public void LoadState(IDictionary<string, object> state)
        {
        }

        public async Task<ICollection<string>> ReadChannelAsync(string channelName, CancellationToken cancellationToken)
        {
            await Task.Delay(15000, cancellationToken);

            return new string[] { "readChannelResult" };
        }

        public Task<ICollection<string>> ReadChannelAsync(string channelName, DateTime from, DateTime to, CancellationToken cancellationToken)
        {
            return Task.FromResult<ICollection<string>>(new string[] { "readChannelPeriodResult" });
        }

        public IDictionary<string, object> SaveState()
        {
            return new Dictionary<string, object>();
        }

        public Task SynchronizeClockAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AkkaTest
{
    public class FakeMeterDriver : IMeterDriver
    {
        private int count;

        public Task CloseAsync()
        {
            return Task.CompletedTask;
        }

        public Task ConnectAsync(CancellationToken cancellationToken)
        {
            return this.ExecuteHttpRequest(0);

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
            if (++count == 3)
            {
                throw new Exception("ReadChannelAsync error");
            }

            await this.ExecuteHttpRequest(0);

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

        private async Task ExecuteHttpRequest(int seconds)
        {
            var address = "https://httpbin.org/delay/" + seconds;

            using (var client = new HttpClient())
            {
                await client.GetAsync(address).ConfigureAwait(false);
            }
        }
    }
}

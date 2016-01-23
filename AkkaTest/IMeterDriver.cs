using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AkkaTest
{
    public interface IMeterDriver
    {
        Task ConnectAsync(CancellationToken cancellationToken);
        Task CloseAsync();

        void LoadState(IDictionary<string, object> state);
        IDictionary<string, object> SaveState();

        Task<ICollection<string>> GetChannelNamesAsync(CancellationToken cancellationToken);

        Task<ICollection<string>> ReadChannelAsync(string channelName, CancellationToken cancellationToken);

        Task<ICollection<string>> ReadChannelAsync(string channelName, DateTime from, DateTime to,
            CancellationToken cancellationToken);

        Task SynchronizeClockAsync(CancellationToken cancellationToken);
    }
}

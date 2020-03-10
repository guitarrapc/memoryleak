using System.Threading;
using System.Threading.Tasks;

namespace DiagnosticCore.EventListeners
{
    public interface IChannelReader
    {
        ValueTask OnReadResultAsync(CancellationToken cancellationToken);
    }
}

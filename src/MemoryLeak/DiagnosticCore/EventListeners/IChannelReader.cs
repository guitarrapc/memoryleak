using System.Threading;
using System.Threading.Tasks;

namespace DiagnosticCore.EventListeners
{
    public interface IChannelReader<T> where T : struct
    {
        ValueTask OnReadResultAsync(CancellationToken cancellationToken);
    }
}

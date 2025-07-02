using System.Threading;
using System.Threading.Tasks;
using BaseLinkerApi.Common;

namespace BaseLinkerApi;

public interface IBaseApiClient
{
    Task<TOutput> SendAsync<TOutput>(IRequest<TOutput> request, CancellationToken cancellationToken = default)
        where TOutput : ResponseBase;
}
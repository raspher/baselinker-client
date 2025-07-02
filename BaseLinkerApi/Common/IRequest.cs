using BaseLinkerApi.Common;

namespace BaseLinkerApi;

public interface IRequest : IRequest<ResponseBase>
{
}

public interface IRequest<TOutput> where TOutput : ResponseBase
{
}
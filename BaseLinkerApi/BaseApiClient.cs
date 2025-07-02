using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.RateLimiting;
using System.Threading.Tasks;
using BaseLinkerApi.Common;
using BaseLinkerApi.Common.JsonConverters;

namespace BaseLinkerApi;

// ReSharper disable once UnusedTypeParameter
public class BaseApiClient : IBaseApiClient
{
    private readonly HttpClient _httpClient;
    private readonly string _token;

    public BaseApiClient(HttpClient httpClient, string token)
    {
        _httpClient = httpClient;
        _token = token;
    }

    // The API doesn't return TooManyRequests but instead blocks your account so rate limit must be implemented client-side
    public FixedWindowRateLimiter TimeLimiter { get; set; } = new(new FixedWindowRateLimiterOptions
    {
        Window = TimeSpan.FromMinutes(1),
        PermitLimit = 100
    });

    public bool UseRequestLimit { get; set; } = true;
    public bool ThrowExceptions { get; set; } = true;

    private async Task<TOutput> SendImpl<TOutput>(IRequest<TOutput> request,
        CancellationToken cancellationToken = default) where TOutput : ResponseBase
    {
        var jsonSerializerOptions = new JsonSerializerOptions
            { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull, AllowTrailingCommas = true };
        jsonSerializerOptions.Converters.Add(new BoolConverter());
        jsonSerializerOptions.Converters.Add(new StringToNullableDecimalConverter());
        var data = new Dictionary<string, string>
        {
            { "method", JsonNamingPolicy.CamelCase.ConvertName(request.GetType().Name) },
            // https://stackoverflow.com/questions/58570189/is-there-a-built-in-way-of-using-snake-case-as-the-naming-policy-for-json-in-asp
            { "parameters", JsonSerializer.Serialize((object)request, jsonSerializerOptions) }
        };

        var content = new FormUrlEncodedContent(data);

        var requestMessage = new HttpRequestMessage(HttpMethod.Post, "https://api.baselinker.com/connector.php")
        {
            Content = content
        };

        requestMessage.Headers.Add("X-BLToken", _token);

        var responseMessage = await _httpClient.SendAsync(requestMessage, cancellationToken);
        var output = await responseMessage.Content.ReadFromJsonAsync<TOutput>(jsonSerializerOptions, cancellationToken);
        if (output!.IsSuccessStatus == false && ThrowExceptions)
        {
            throw new BaseException(output.ErrorCode!, output.ErrorMessage!);
        }

        return output;
    }

    public async Task<TOutput> SendAsync<TOutput>(IRequest<TOutput> request,
        CancellationToken cancellationToken = default) where TOutput : ResponseBase
    {
        var sendTask = SendImpl(request, cancellationToken);

        if (!UseRequestLimit)
        {
            return await sendTask;
        }

        using var lease = await TimeLimiter.AcquireAsync(cancellationToken: cancellationToken);
        if (!lease.IsAcquired)
        {
            await Task.Delay(TimeLimiter.ReplenishmentPeriod, cancellationToken);
        }

        return await sendTask;
    }
}
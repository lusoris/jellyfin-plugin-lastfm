// GPL-2.0 License
// https://github.com/lusoris/jellyfin-plugin-lastfm

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Lastfm.Scrobbler.Core.Interfaces;
using Lastfm.Scrobbler.Core.Models;
using Lastfm.Scrobbler.Core.Models.Requests;
using Lastfm.Scrobbler.Core.Models.Responses;
using Microsoft.Extensions.Logging;

namespace Lastfm.Scrobbler.Core.Api
{
    public abstract class BaseLastfmApiClient
    {
        private readonly ICoreHttpClient _httpClient;
        private readonly ILogger<BaseLastfmApiClient> _logger;

        protected BaseLastfmApiClient(ICoreHttpClient httpClient, ILogger<BaseLastfmApiClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        protected async Task<TResponse> Get<TRequest, TResponse>(TRequest request)
            where TRequest : BaseRequest
            where TResponse : BaseResponse
        {
            var parameters = request.ToDictionary();
            var url = BuildUrl(parameters, false);

            var response = await _httpClient.GetAsync(url, CancellationToken.None);
            return await ProcessResponse<TResponse>(response);
        }

        protected async Task<TResponse> Post<TRequest, TResponse>(TRequest request)
            where TRequest : BaseRequest
            where TResponse : BaseResponse
        {
            var parameters = request.ToDictionary();
            var url = BuildUrl(parameters, true);

            var response = await _httpClient.PostAsync(url, new FormUrlEncodedContent(parameters), CancellationToken.None);
            return await ProcessResponse<TResponse>(response);
        }

        protected async Task<TResponse> Get<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken)
            where TRequest : BaseRequest
            where TResponse : BaseResponse
        {
            var parameters = request.ToDictionary();
            var url = BuildUrl(parameters, false);

            var response = await _httpClient.GetAsync(url, cancellationToken);
            return await ProcessResponse<TResponse>(response);
        }

        protected async Task<TResponse> Post<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken)
            where TRequest : BaseRequest
            where TResponse : BaseResponse
        {
            var parameters = request.ToDictionary();
            var url = BuildUrl(parameters, true);

            var response = await _httpClient.PostAsync(url, new FormUrlEncodedContent(parameters), cancellationToken);
            return await ProcessResponse<TResponse>(response);
        }

        private async Task<TResponse> ProcessResponse<TResponse>(HttpResponseMessage response) where TResponse : BaseResponse
        {
            var json = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Request failed: {Json}", json);
                response.EnsureSuccessStatusCode();
            }

            _logger.LogDebug("Response: {Json}", json);
            return JsonSerializer.Deserialize<TResponse>(json, LastfmJsonContext.Default.Options)!;
        }

        private string BuildUrl(Dictionary<string, string> parameters, bool isPost)
        {
            parameters.Add("api_key", "d6e622c3c6345a6b7b8c02e04c3a0438");
            parameters.Add("format", "json");

            if (isPost)
            {
                parameters.Add("api_sig", CreateSignature(parameters));
            }

            var query = string.Join("&", parameters.Select(p => $"{p.Key}={Uri.EscapeDataString(p.Value)}"));
            return $"https://ws.audioscrobbler.com/2.0/?{query}";
        }

        private string CreateSignature(Dictionary<string, string> parameters)
        {
            var sorted = new SortedDictionary<string, string>(parameters);
            var sb = new StringBuilder();

            foreach (var pair in sorted)
            {
                if (pair.Key != "format")
                {
                    sb.Append(pair.Key);
                    sb.Append(pair.Value);
                }
            }

            sb.Append("e21058f359145d33496d7634a79a4436");

            using (var md5 = MD5.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(sb.ToString());
                var hash = md5.ComputeHash(bytes);

                return BitConverter.ToString(hash).Replace("-", "").ToLower();
            }
        }
    }
}

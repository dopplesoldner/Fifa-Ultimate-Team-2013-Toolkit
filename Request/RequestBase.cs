﻿using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using UltimateTeam.Toolkit.Constant;
using UltimateTeam.Toolkit.Model;
using UltimateTeam.Toolkit.Service;
using HttpMethod = System.Net.Http.HttpMethod;

namespace UltimateTeam.Toolkit.Request
{
    public abstract class RequestBase
    {
        private IJsonDeserializer _jsonDeserializer;
        private static readonly CookieContainer CookieContainer = new CookieContainer();
        protected static string SessionId;
        protected static string Token;
        protected readonly HttpClient Client;

        public IJsonDeserializer JsonDeserializer
        {
            private get { return _jsonDeserializer ?? new JsonDeserializer(); }
            set
            {
                if (value == null) throw new ArgumentNullException("value");
                _jsonDeserializer = value;
            }
        }

        protected RequestBase()
        {
            var handler = new HttpClientHandler
            {
                CookieContainer = CookieContainer,
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };
            Client = new HttpClient(handler);
            Client.DefaultRequestHeaders.ExpectContinue = false;

            // this User-Agent header block is so verbose because the compact form breaks on mono
            Client.DefaultRequestHeaders.UserAgent.Add(ProductInfoHeaderValue.Parse("Mozilla/5.0"));
            Client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("(Windows NT 6.2; WOW64)"));
            Client.DefaultRequestHeaders.UserAgent.Add(ProductInfoHeaderValue.Parse("AppleWebKit/537.17"));
            Client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("(KHTML, like Gecko)"));
            Client.DefaultRequestHeaders.UserAgent.Add(ProductInfoHeaderValue.Parse("Chrome/24.0.1312.57"));
            Client.DefaultRequestHeaders.UserAgent.Add(ProductInfoHeaderValue.Parse("Safari/537.17"));
            Client.DefaultRequestHeaders.Referrer = new Uri(Resources.Home);
            Client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        protected internal HttpRequestMessage CreateRequestMessage(string content, string uriString, string httpMethodOverride)
        {
            var stringContent = new StringContent(content, Encoding.UTF8, "application/json");
            var requestMessage = new HttpRequestMessage(HttpMethod.Post, new Uri(uriString)) { Content = stringContent };
            requestMessage.Headers.TryAddWithoutValidation(NonStandardHttpHeaders.SessionId, SessionId);
            requestMessage.Headers.TryAddWithoutValidation(NonStandardHttpHeaders.MethodOverride, httpMethodOverride);
            requestMessage.Headers.TryAddWithoutValidation(NonStandardHttpHeaders.EmbedError, "true");
            if (!string.IsNullOrEmpty(Token))
            {
                requestMessage.Headers.TryAddWithoutValidation(NonStandardHttpHeaders.Token, Token);
            }

            return requestMessage;
        }

        protected internal async Task EnsureSuccessfulResponse(HttpResponseMessage response)
        {
            if (!response.IsSuccessStatusCode)
            {
                var error = await Deserialize<ApiError>(response);
                throw new HttpRequestException(string.Format("{0} ({1})", error.Reason, error.Code));
            }
        }

        protected internal async Task<T> Deserialize<T>(HttpResponseMessage responseMessage)
        {
            return JsonDeserializer.Deserialize<T>(await responseMessage.Content.ReadAsStreamAsync());
        }
    }
}
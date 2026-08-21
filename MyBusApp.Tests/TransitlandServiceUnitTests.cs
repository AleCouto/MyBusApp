using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MyBusApp.Configuration;
using MyBusApp.Services;
using Xunit;

namespace MyBusApp.Tests
{
    public class TransitlandServiceUnitTests
    {
        private class FakeHandler : HttpMessageHandler
        {
            private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

            public FakeHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
            {
                _responder = responder;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return Task.FromResult(_responder(request));
            }
        }

        [Fact]
        public async Task GetLineAsync_ReturnsLine_WhenRoutesArrayPresent()
        {
            var capturedUris = new List<Uri>();
            var json = @"{ ""routes"": [ { ""onestop_id"": ""r-foo-714"", ""id"": ""route-1"", ""route_short_name"": ""714"", ""route_number"": null, ""name"": ""Linha 714"", ""route_long_name"": ""Linha 714"" } ] }";

            var handler = new FakeHandler(req =>
            {
                capturedUris.Add(req.RequestUri!);
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };
            });

            var http = new HttpClient(handler) { BaseAddress = new Uri("https://transit.land/api/v2/rest/") };
            var settings = new ApiSettings();
            settings.Transitland.BaseUrl = "https://transit.land/api/v2/rest/";
            settings.Transitland.ApiKey = "FAKE";

            var svc = new TransitlandService(settings, http);
            var line = await svc.GetLineAsync("714");

            Assert.NotNull(line);
            Assert.Equal("714", line!.ShortName);
            Assert.NotEmpty(capturedUris);
            Assert.Contains(capturedUris, uri => uri.ToString().Contains("routes?", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public async Task GetLineAsync_ReturnsNull_WhenHtmlResponseIsReturned()
        {
            var html = "<!DOCTYPE html><html><body>App shell</body></html>";
            var handler = new FakeHandler(req =>
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(html, Encoding.UTF8, "text/html")
                };
            });

            var http = new HttpClient(handler) { BaseAddress = new Uri("https://transit.land/api/v2/rest/") };
            var settings = new ApiSettings();
            settings.Transitland.BaseUrl = "https://transit.land/api/v2/rest/";
            settings.Transitland.ApiKey = "FAKE";

            var svc = new TransitlandService(settings, http);
            var line = await svc.GetLineAsync("714");

            Assert.Null(line);
        }
    }
}

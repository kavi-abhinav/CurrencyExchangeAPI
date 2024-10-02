using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CurrencyExchangeAPI.UnitTests
{
    public class TestHttpHandler : HttpMessageHandler
    {
        private HttpResponseMessage _response;

        public TestHttpHandler()
        {
            _response = new HttpResponseMessage();
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_response);
        }

        public void SetResponse(HttpResponseMessage response)
        {
            _response = response;
        }
    }

}

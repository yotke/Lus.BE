using Lus.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Xunit;

namespace Lus.Api.Tests.Documents
{
    public class DocumentBuilderControllerRouteTests
    {
        [Fact]
        public void Route_prefix_is_v1_documents_builder()
        {
            var attr = typeof(DocumentBuilderController)
                .GetCustomAttributes(typeof(RouteAttribute), inherit: false)
                .Cast<RouteAttribute>()
                .Single();
            Assert.Equal("v1/documents/builder", attr.Template);
        }

        [Theory]
        [InlineData(nameof(DocumentBuilderController.Echo), "echo")]
        [InlineData(nameof(DocumentBuilderController.Turn), "turn")]
        [InlineData(nameof(DocumentBuilderController.Undo), "undo")]
        [InlineData(nameof(DocumentBuilderController.Redo), "redo")]
        [InlineData(nameof(DocumentBuilderController.Session), "session")]
        public void Action_routes_are_locked(string method, string template)
        {
            var info = typeof(DocumentBuilderController).GetMethod(method);
            Assert.NotNull(info);
            var http = info!.GetCustomAttributes(true).OfType<HttpMethodAttribute>().Single();
            Assert.Equal(template, http.Template);
        }
    }
}

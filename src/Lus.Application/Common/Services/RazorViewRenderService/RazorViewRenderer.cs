using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;

namespace Lus.Application.Common.Services.RazorViewRenderService
{
    public sealed class RazorViewRenderer
    {
        private readonly IRazorViewEngine _razorViewEngine;
        private readonly ITempDataProvider _tempDataProvider;
        private readonly IServiceProvider _serviceProvider;

        public RazorViewRenderer(
            IRazorViewEngine viewEngine,
            ITempDataProvider tempDataProvider,
            IServiceProvider serviceProvider
        )
        {
            _razorViewEngine = viewEngine ?? throw new ArgumentNullException(nameof(viewEngine));
            _tempDataProvider = tempDataProvider ?? throw new ArgumentNullException(nameof(tempDataProvider));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public async Task<string> RenderAsync<T>(
            string viewPath,
            T model,
            CancellationToken cancellationToken = default
        ) where T : class
        {
            var httpContext = new DefaultHttpContext { RequestServices = _serviceProvider };
            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());

            await using (var sw = new StringWriter())
            {
                var viewEngineResult = _razorViewEngine.GetView(null, viewPath, true);

                if (viewEngineResult.View == null)
                {
                    throw new ArgumentNullException($"{viewPath} does not match any available view");
                }

                var viewDictionary =
                    new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
                    {
                        Model = model
                    };

                var viewContext = new ViewContext(
                    actionContext,
                    viewEngineResult.View,
                    viewDictionary,
                    new TempDataDictionary(actionContext.HttpContext, _tempDataProvider),
                    sw,
                    new HtmlHelperOptions()
                );

                await viewEngineResult.View.RenderAsync(viewContext);

                return sw.ToString();
            }
        }

        public async Task<ViewContext> RenderHTMLAsync<T>(
            string viewPath,
            T model,
            CancellationToken cancellationToken = default
        ) where T : class
        {
            var httpContext = new DefaultHttpContext { RequestServices = _serviceProvider };
            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());

            await using (var sw = new StringWriter())
            {
                var viewEngineResult = _razorViewEngine.GetView(null, viewPath, true);

                if (viewEngineResult.View == null)
                {
                    throw new ArgumentNullException($"{viewPath} does not match any available view");
                }

                var viewDictionary =
                    new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
                    {
                        Model = model
                    };

                var viewContext = new ViewContext(
                    actionContext,
                    viewEngineResult.View,
                    viewDictionary,
                    new TempDataDictionary(actionContext.HttpContext, _tempDataProvider),
                    sw,
                    new HtmlHelperOptions()
                );

                await viewEngineResult.View.RenderAsync(viewContext);

                return viewContext;
            }
        }
    }
}

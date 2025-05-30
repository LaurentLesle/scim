using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ScimServiceProvider.Formatters
{
    public class ScimResultAttribute : ActionFilterAttribute
    {
        public override void OnResultExecuting(ResultExecutingContext context)
        {
            if (context.Result is ObjectResult objectResult)
            {
                context.HttpContext.Response.ContentType = "application/scim+json";
            }
            
            base.OnResultExecuting(context);
        }
    }
}

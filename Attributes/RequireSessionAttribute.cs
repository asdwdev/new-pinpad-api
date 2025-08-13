using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace NewPinpadApi.Attributes
{
    public class RequireSessionAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var userId = context.HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                context.Result = new UnauthorizedObjectResult(new
                {
                    success = false,
                    message = "Unauthorized: login required"
                });
            }
        }
    }
}

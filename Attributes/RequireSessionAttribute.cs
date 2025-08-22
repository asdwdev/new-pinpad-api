using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace NewPinpadApi.Attributes
{
    public class RequireSessionAttribute : Attribute, IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var userId = context.HttpContext.Session.GetInt32("UserId");

            if (userId == null)
            {
                context.Result = new UnauthorizedObjectResult(new
                {
                    success = false,
                    message = "Unauthorized: Please login first"
                });
            }
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace EcommerceApi.Middleware
{
    public class ValidationFilter : IActionFilter
    {
        // Avant l'action, vérifie si les DTO passés à l'action sont valides (ModelState.IsValid)
        public void OnActionExecuting(ActionExecutingContext context)
        {
            if (!context.ModelState.IsValid)
            {
                var errors = context.ModelState.Values
                                   .SelectMany(v => v.Errors)
                                   .Select(e => e.ErrorMessage)
                                   .ToList();

                var response = new
                {
                    statusCode = 400,
                    errors
                };

                context.Result = new BadRequestObjectResult(response);
            }
        }

        // Après l'action, rien à faire
        public void OnActionExecuted(ActionExecutedContext context)
        {
        }
    }
}

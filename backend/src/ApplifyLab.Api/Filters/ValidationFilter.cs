using FluentValidation;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;

namespace ApplifyLab.Api.Filters;

/// <summary>
/// Runs the matching FluentValidation validator (if one is registered) for every action
/// argument, so every request DTO is validated without repeating the call in each controller.
/// </summary>
public class ValidationFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        foreach (var arg in context.ActionArguments.Values)
        {
            if (arg is null) continue;

            var validatorType = typeof(IValidator<>).MakeGenericType(arg.GetType());
            if (context.HttpContext.RequestServices.GetService(validatorType) is not IValidator validator)
                continue;

            var validationContext = new ValidationContext<object>(arg);
            var result = await validator.ValidateAsync(validationContext);
            if (!result.IsValid)
            {
                context.Result = new BadRequestObjectResult(new { errors = result.Errors.Select(e => e.ErrorMessage) });
                return;
            }
        }

        await next();
    }
}

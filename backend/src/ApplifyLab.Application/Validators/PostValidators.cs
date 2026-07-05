using ApplifyLab.Application.DTOs;
using FluentValidation;

namespace ApplifyLab.Application.Validators;

public class CreatePostRequestValidator : AbstractValidator<CreatePostRequest>
{
    public CreatePostRequestValidator()
    {
        RuleFor(x => x.Content).NotEmpty().MaximumLength(5000);
        RuleFor(x => x.Visibility).IsInEnum();
    }
}

public class CreateCommentRequestValidator : AbstractValidator<CreateCommentRequest>
{
    public CreateCommentRequestValidator()
    {
        RuleFor(x => x.Content).NotEmpty().MaximumLength(2000);
    }
}

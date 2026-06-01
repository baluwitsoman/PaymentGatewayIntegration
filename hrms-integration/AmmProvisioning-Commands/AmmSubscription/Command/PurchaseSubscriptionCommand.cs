using MediatR;
using AutoMapper;
using FluentValidation;
using ERPMultiTenent.Application.Common.Interfaces;
using ERPMultiTenent.Application.Common.Models;
using Microsoft.Extensions.Logging;


namespace ERPMultiTenent.Application.Amm ;



public record PurchaseSubscriptionCommand : IRequest<ResultNew<AmmSubscriptionDTO>> 
{    
    public AmmSubscriptionDTO Dto {get;set;}
    public string? AI_BILL_ADDRESS { get; set; }
    public string? AI_EMAIL_ADDRESS { get; set; }
    public string? AI_PHONE_NO { get; set; }
    public string? AI_PLAN_NAME { get; set; }
    public int InvoiceId { get; set; }

}
public class CreateAmmSubscriptionCommandValidator1 : AbstractValidator<PurchaseSubscriptionCommand>
{
    public CreateAmmSubscriptionCommandValidator1()
    {
        // Validate AS_PLANPRICE_ID - Not null and greater than zero
        RuleFor(x => x.Dto.AS_PLANPRICE_ID)
            .GreaterThan(0).WithMessage("Plan price ID must be greater than 0.");

        RuleFor(x => x.AI_EMAIL_ADDRESS)
          .NotNull().WithMessage("Email Address is required.");

        RuleFor(x => x.AI_PHONE_NO)
  .NotNull().WithMessage("Phone no is required.");
        RuleFor(x => x.AI_BILL_ADDRESS)
  .NotNull().WithMessage("Billing address is required.");

        //// Validate AS_FROM_DATE - Required and should not be in the future
        //RuleFor(x => x.Dto.AS_FROM_DATE)
        //    .NotNull().WithMessage("Start date is required.")
        //    .LessThanOrEqualTo(DateTime.Today).WithMessage("Start date cannot be in the future.");

        //// Validate AS_TO_DATE - Required and should be after AS_FROM_DATE
        //RuleFor(x => x.Dto.AS_TO_DATE)
        //    .NotNull().WithMessage("End date is required.")
        //    .GreaterThan(x => x.Dto.AS_FROM_DATE).WithMessage("End date must be after the start date.");

        //// Validate AS_TOTAL_AMOUNT - Required and greater than zero
        //RuleFor(x => x.Dto.AS_TOTAL_AMOUNT)
        //    .GreaterThan(0).WithMessage("Total amount must be greater than 0.");

        // Validate AS_STATUS - Must be either 'Trial' or 'Paid'
        RuleFor(x => x.Dto.AS_STATUS)
            .NotNull().WithMessage("Status is required.")
            .Must(status => status == "Trial" || status == "Paid")
            .WithMessage("Status must be either 'Trial' or 'Paid'.");

        // Validate AS_ACTIVE_EXPIRED_STATUS - Must be either 'Expired' or 'Active'
        RuleFor(x => x.Dto.AS_ACTIVE_EXPIRED_STATUS)
            .NotNull().WithMessage("Active/Expired status is required.")
            .Must(status => status == "Expired" || status == "Active")
            .WithMessage("Active/Expired status must be either 'Expired' or 'Active'.");



        // Validate AS_COMPCODE - Not null and not empty
        RuleFor(x => x.Dto.AS_COMPCODE)
            .NotNull().WithMessage("Company code is required.")
            .NotEmpty().WithMessage("Company code cannot be empty.");

        // Validate AS_GRACE_DATE - Should be in the future or null
        //RuleFor(x => x.Dto.AS_GRACE_DATE)
        //    .Must(graceDate => !graceDate.HasValue || graceDate.Value > DateTime.Today)
        //    .WithMessage("Grace date must be in the future if provided.");

        // Validate AS_USER_COST - Should be greater than zero if provided
        RuleFor(x => x.Dto.AS_USER_COST)
            .GreaterThan(0).When(x => x.Dto.AS_USER_COST.HasValue)
            .WithMessage("User cost must be greater than 0.");

        // Validate AS_NO_EMPLOYEES - Should be greater than or equal to 1
        RuleFor(x => x.Dto.AS_NO_EMPLOYEES)
            .GreaterThanOrEqualTo(1).When(x => x.Dto.AS_NO_EMPLOYEES.HasValue)
            .WithMessage("Number of employees must be at least 1.");



        // Validate AS_NO_USERS_REQUIRED - Should be greater than or equal to 1
        RuleFor(x => x.Dto.AS_NO_USERS_REQUIRED)
            .GreaterThanOrEqualTo(1).When(x => x.Dto.AS_NO_USERS_REQUIRED.HasValue)
            .WithMessage("Number of users required must be at least 1.");

        //// Validate AS_TOTAL_USERCOST - Should be greater than zero if provided
        //RuleFor(x => x.Dto.AS_TOTAL_USERCOST)
        //    .GreaterThan(0).When(x => x.Dto.AS_TOTAL_USERCOST.HasValue)
        //    .WithMessage("Total user cost must be greater than 0.");


    }
}


public class CreateAmmSubscriptionBuyCommandHandler : IRequestHandler<PurchaseSubscriptionCommand, ResultNew<AmmSubscriptionDTO>>
{
    private readonly ILogger<PurchaseSubscriptionCommand> _logger;
    private readonly IAmmSubscriptionRepo _repository;

    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWorkAmm _unitOfWork;
    
    

    public CreateAmmSubscriptionBuyCommandHandler(
        ILogger<PurchaseSubscriptionCommand> logger,
        IAmmSubscriptionRepo repo,IMapper mapper,
        IUnitOfWorkAmm unitOfWork,
        ICurrentUserService currentUser   
    )
    {
        _logger = logger;
        _repository = repo;
        _mapper = mapper;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    public async Task<ResultNew<AmmSubscriptionDTO>> Handle(PurchaseSubscriptionCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Yep!! Inside ");
        Console.WriteLine("Yep!! Inside ");

               await _unitOfWork.BeginTransactionAsync();
        await _repository.Purchase(command);
        await _unitOfWork.CommitAsync();
        return ResultNew<AmmSubscriptionDTO>.Success(command.Dto);
 
    }
}

 
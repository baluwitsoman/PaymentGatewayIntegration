


 
using MediatR;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using ERPMultiTenent.Domain.Entities.Amm.Provisioning;
using FluentValidation;
using System.ComponentModel.DataAnnotations;
using ERPMultiTenent.Application.Common.Mappings;
using ERPMultiTenent.Application.Common.Interfaces;
using ERPMultiTenent.Application.Common.Models;
using ERPMultiTenent.Domain.Entities.Amm;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;


namespace ERPMultiTenent.Application.Amm ;



public record CreateAmmUserRegisterCommand : IRequest<ResultNew<AmmUserRegisterDTO>> 
{    
    public AmmUserRegisterDTO Dto {get;set;}


}
public class CreateAmmUserRegisterCommandValidator : AbstractValidator<CreateAmmUserRegisterCommand>
{
    public CreateAmmUserRegisterCommandValidator()
    {
        RuleFor(x => x.Dto.AUR_FIRSTNAME)
            .NotEmpty().WithMessage("First name is required.")
            .MaximumLength(100).WithMessage("First name cannot exceed 100 characters.");

        RuleFor(x => x.Dto.AUR_LASTNAME)
            .NotEmpty().WithMessage("Last name is required.")
            .MaximumLength(100).WithMessage("Last name cannot exceed 100 characters.");

        RuleFor(x => x.Dto.AUR_EMAIL)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Invalid email format.")
            .MaximumLength(150).WithMessage("Email cannot exceed 150 characters.");

        RuleFor(x => x.Dto.AUR_PASSWORD)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(6).WithMessage("Password must be at least 6 characters.");

        RuleFor(x => x.Dto.AUR_COMPANYNAME)
            .NotEmpty().WithMessage("Company name is required.")
            .MaximumLength(150).WithMessage("Company name cannot exceed 150 characters.");

        RuleFor(x => x.Dto.AUR_SHORTCODE)
            .NotEmpty().WithMessage("Short code is required.")
            .MaximumLength(10).WithMessage("Short code cannot exceed 20 characters.");

        RuleFor(x => x.Dto.AUR_COMP_ADDRESS1)
            .NotEmpty().WithMessage("Company address line 1 is required.")
            .MaximumLength(200).WithMessage("Address line 1 cannot exceed 200 characters.");

        RuleFor(x => x.Dto.AUR_COMP_TELEPHONE)
            .NotEmpty().WithMessage("Company telephone is required.")
            .MaximumLength(10).WithMessage("Telephone number cannot exceed 10 characters.");

        
    }
}


public class CreateAmmUserRegisterCommandHandler : IRequestHandler<CreateAmmUserRegisterCommand, ResultNew<AmmUserRegisterDTO>>
{
    private readonly IAmmUserRegisterRepo _repository;

    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUser;
    private readonly IConfiguration _configuration;
    private readonly IUnitOfWorkAmm _unitOfWork;
    
    

    public CreateAmmUserRegisterCommandHandler(
        IAmmUserRegisterRepo repo,IMapper mapper,
        IUnitOfWorkAmm unitOfWork,
        ICurrentUserService currentUser   ,
        IConfiguration configuration
    )
    {
        _repository = repo;
        _mapper = mapper;
        _currentUser = currentUser;
        _configuration = configuration;
        _unitOfWork = unitOfWork;
    }

    public async Task<ResultNew<AmmUserRegisterDTO>> Handle(CreateAmmUserRegisterCommand command, CancellationToken cancellationToken)
    {
 

      
      
        try
        {
            await _repository.Register(command);
        }
        catch (Exception e)
        {
            return ResultNew<AmmUserRegisterDTO>.Failed(e.Message);
        }

        return ResultNew<AmmUserRegisterDTO>.Success(command.Dto);
 
    }


}


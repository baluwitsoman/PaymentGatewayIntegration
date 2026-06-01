


 
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
using ERPMultiTenent.Application.ERP.Mail;


namespace ERPMultiTenent.Application.Amm ;



public record ValidateOTPOnRegisterCommand : IRequest<ResultNew<bool>> 
{    
    public string Email {get;set;}

    public string OTP { get; set; }
    public bool OTPVerified { get; set; }
}
public class ValidateOTPOnRegisterCommandValidator : AbstractValidator<ValidateOTPOnRegisterCommand>
{
    public ValidateOTPOnRegisterCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .MaximumLength(100).WithMessage("First name cannot exceed 100 characters.");

        RuleFor(x => x.OTP)
            .NotEmpty().WithMessage("OTP is required.")
            .MaximumLength(100).WithMessage("OTP cannot exceed 100 characters.");
         
        
    }
}


public class ValidateOTPOnRegisterCommandHandler : IRequestHandler<ValidateOTPOnRegisterCommand, ResultNew<bool>>
{
    private readonly IAmmUserRegisterRepo _repository;

    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUser;
    private readonly IConfiguration _configuration;
    private readonly IUnitOfWorkAmm _unitOfWork;
    
    private readonly IAmmOtpRepo _otpRepo;


    public ValidateOTPOnRegisterCommandHandler(
        IAmmUserRegisterRepo repo, IMapper mapper,
        IUnitOfWorkAmm unitOfWork,
        ICurrentUserService currentUser,
        IConfiguration configuration
, IAmmOtpRepo otpRepo)
    {
        _repository = repo;
        _mapper = mapper;
        _currentUser = currentUser;
        _configuration = configuration;
        _unitOfWork = unitOfWork;
        _otpRepo = otpRepo;
    }

    public async Task<ResultNew<bool>> Handle(ValidateOTPOnRegisterCommand command, CancellationToken cancellationToken)
    {

        var registerStatus = await _otpRepo.ValidateOtpAsync(command.Email.ToLower(), command.OTP);

        if (!registerStatus)
        {
            command.OTPVerified = false;
            return ResultNew<bool>.FailureResult(false, [""]);

        }


        return ResultNew<bool>.Success(true);
 
    }


}


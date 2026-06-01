using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AutoMapper;
using ERPMultiTenent.Application.Common.Interfaces;
using ERPMultiTenent.Application.Common.Models;
using ERPMultiTenent.Application.ERP.Mail;
using ERPMultiTenent.Application.Services;
using ERPMultiTenent.Domain.Entities.Amm;
using ERPMultiTenent.Domain.Entities.Amm.Mail;
using ERPMultiTenent.Domain.Entities.Amm.Provisioning;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ERPMultiTenent.Application.Amm.UserDetail.Command;
public class UpdatePasswordCommand : IRequest<ResultNew<bool>>
{
    public string EmailAddress { get; set; }
    public string Password { get; set; }
 
}

//public class UpdatePasswordCommandCommandHandler : IRequestHandler<UpdatePasswordCommand, ResultNew<bool>>
//{
//    private readonly IAmmDbContext _context;
//    private readonly IEmailService _emailService;
//    private readonly IAmmOtpRepo _otpRepo;
//    private readonly IUserRoleLinkRepo _userRoleLinkRepo;
//    private readonly IAmmUserRegisterRepo _userRegisterRepo;
//    private readonly IMapper _mapper;
//    private readonly ICurrentUserService _currentUser;
//    private readonly IConfiguration _configuration;

//    public UpdatePasswordCommandCommandHandler(IAmmDbContext context,
//        IEmailService emailService,
//        IAmmOtpRepo ammOtpRepo,
//        IUserRoleLinkRepo userRoleLinkRepo,
//        IAmmUserRegisterRepo userRegisterRepo,
//        IMapper mapper, ICurrentUserService currentUser, IConfiguration configuration)
//    {
//        _context = context;
//        _emailService = emailService;
//        _otpRepo = ammOtpRepo;
//        _userRoleLinkRepo = userRoleLinkRepo;
//        _userRegisterRepo = userRegisterRepo;
//        _mapper = mapper;
//        _currentUser = currentUser;
//        _configuration = configuration;
//    }

//    public async Task<ResultNew<bool>> Handle(UpdatePasswordCommand request, CancellationToken cancellationToken)
//    {

//        /*.Where(w => w.PEMP_EMAIL_ADDRESS.ToUpper() == request.EmailAddress.ToUpper()).FirstOrDefaultAsync();*/

//        var userQuery = (from emp in _context.PPM_EMPLOYEE_DETAILS
//                         join user1 in _context.AMM_USER_DETAILS on emp.PEMP_EMP_CODE equals user1.USER_EMP_CODE
//                         //where EF.Functions.Like(emp.PEMP_EMAIL_ADDRESS.ToUpper(), request.EmailAddress.ToUpper()) //emp.PEMP_EMAIL_ADDRESS.ToUpper() == request.EmailAddress.ToUpper()
//                         where emp.PEMP_EMAIL_ADDRESS.ToUpper() == request.EmailAddress.ToUpper()
//                         select user1);// new { user1.USER_PASSWORD, user1.AUD_COMP_CODE, user1.USER_SHORT_NAME, user1.USER_ID });

//        var user = await userQuery.AsNoTracking().FirstOrDefaultAsync();


//        if (user == null)
//        {
//            return new ResultNew<bool>(false,["Email address not found."] );
//        }



//        await _userRegisterRepo.UpdatePassword(user.USER_ID, request.Password);


//        return ResultNew<bool>.Success(true);

//    }



//}



public class UpdatePasswordCommandHandler : IRequestHandler<UpdatePasswordCommand, ResultNew<bool>>
{
    private readonly IPasswordChangeService _passwordChangeService;

    public UpdatePasswordCommandHandler(IPasswordChangeService passwordChangeService)
    {
        _passwordChangeService = passwordChangeService;
    }

    public async Task<ResultNew<bool>> Handle(
        UpdatePasswordCommand request,
        CancellationToken cancellationToken)
    {
        var result = await _passwordChangeService.SetPasswordByEmailAsync(
            request.EmailAddress,
            request.Password,
            cancellationToken);

        // Adapt ResultNew<string> → ResultNew<bool>
        return result.Succeeded
            ? ResultNew<bool>.Success(true)
            : new ResultNew<bool>(false, result.Errors);
    }
}

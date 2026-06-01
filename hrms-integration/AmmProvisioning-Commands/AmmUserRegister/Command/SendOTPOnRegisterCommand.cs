using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AutoMapper;
using ERPMultiTenent.Application.Common.Interfaces;
using ERPMultiTenent.Application.Common.Models;
using ERPMultiTenent.Application.ERP.Mail;
using ERPMultiTenent.Domain.Entities.Amm;
using ERPMultiTenent.Domain.Entities.Amm.Mail;
using ERPMultiTenent.Domain.Entities.Amm.Provisioning;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ERPMultiTenent.Application.Amm.UserDetail.Command;
public class SendOTPOnRegisterCommand : IRequest<ResultNew<bool>>
{
    public string EmailAddress { get; set; }
    public string Name { get; set; }
 
}

public class SendOTPOnRegisterCommandCommandHandler : IRequestHandler<SendOTPOnRegisterCommand, ResultNew<bool>>
{
    private readonly IAmmDbContext _context;
    private readonly IEmailService _emailService;
    private readonly IAmmOtpRepo _otpRepo;
    private readonly IUserRoleLinkRepo _userRoleLinkRepo;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUser;
    private readonly IConfiguration _configuration;

    public SendOTPOnRegisterCommandCommandHandler(IAmmDbContext context,
        IEmailService emailService,
        IAmmOtpRepo ammOtpRepo,
        IUserRoleLinkRepo userRoleLinkRepo,
        IMapper mapper, ICurrentUserService currentUser, IConfiguration configuration)
    {
        _context = context;
        _emailService = emailService;
        _otpRepo = ammOtpRepo;
        _userRoleLinkRepo = userRoleLinkRepo;
        _mapper = mapper;
        _currentUser = currentUser;
        _configuration = configuration;
    }

    public async Task<ResultNew<bool>> Handle(SendOTPOnRegisterCommand request, CancellationToken cancellationToken)
    {

        var userQuery = (from emp in _context.PPM_EMPLOYEE_DETAILS
                         join user1 in _context.AMM_USER_DETAILS on emp.PEMP_EMP_CODE equals user1.USER_EMP_CODE
                         //where EF.Functions.Like(emp.PEMP_EMAIL_ADDRESS.ToUpper(), request.EmailAddress.ToUpper()) //emp.PEMP_EMAIL_ADDRESS.ToUpper() == request.EmailAddress.ToUpper()
                         where emp.PEMP_EMAIL_ADDRESS.ToUpper() == request.EmailAddress.ToUpper()
                         select user1);// new { user1.USER_PASSWORD, user1.AUD_COMP_CODE, user1.USER_SHORT_NAME, user1.USER_ID });

        var user = await userQuery.AsNoTracking().FirstOrDefaultAsync();


        if (user != null)
        {
            return new ResultNew<bool>(false, ["Email address already exists."]);
        }

        var empCode = request.EmailAddress.ToLower(); 
        var otp = OtpGenerator.GenerateOtp();

        var otpEntity = new AMM_OTP
        {
            AO_EMP_CODE = empCode,
            AO_OTP = otp,
            AO_EXPIRED_YN = "N"
        };

        await _otpRepo.ExpireOtpAsync(empCode); // expire previous
        await _otpRepo.InsertAsync(otpEntity);

        // Send OTP via email using SmtpMailService
        var mail = new AmmERPMailDTO
        {
            COMP_CODE = "01",
            MAIL_TO = request.EmailAddress,
            MAIL_SUBJECT = "OTP for company registration",
            BODY_HTML = $"<p>Dear {request.Name},</p><p><br> Your OTP is <strong>{otp}</strong>. It is valid for 10 minutes.</p>",
            ENABLED_YN = "Y"              
        };

        await _emailService.SendMail(new AMM_MAIL_LOGS
        {
            COMP_CODE = "01",
            USER_ID = "1",
            MAIL_DATE = DateTime.Now,
             

        }, mail);




        return ResultNew<bool>.Success(true);
 
    }

}

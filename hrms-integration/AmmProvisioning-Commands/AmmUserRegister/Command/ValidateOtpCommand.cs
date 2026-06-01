using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ERPMultiTenent.Application.Common.Interfaces;
using ERPMultiTenent.Application.Common.Models;
using ERPMultiTenent.Application.ERP.Mail;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERPMultiTenent.Application.ERP.AmmProvisioning.AmmUserRegister.Command;
public class ValidateOtpCommand : IRequest<ResultNew<bool>>
{
    public string EmailAddress { get; set; }
    public string Otp { get; set; }
}
public class ValidateOtpCommandHandler : IRequestHandler<ValidateOtpCommand, ResultNew<bool>>
{
    private readonly IAmmDbContext _context;
    private readonly IAmmOtpRepo _otpRepo;

    public ValidateOtpCommandHandler(IAmmDbContext context, IAmmOtpRepo otpRepo)
    {
        _context = context;
        _otpRepo = otpRepo;
    }

    public async Task<ResultNew<bool>> Handle(ValidateOtpCommand request, CancellationToken cancellationToken)
    {
        // Step 1: Get the employee code from email
        var user = await (from emp in _context.PPM_EMPLOYEE_DETAILS
                          join user1 in _context.AMM_USER_DETAILS on emp.PEMP_EMP_CODE equals user1.USER_EMP_CODE
                          where emp.PEMP_EMAIL_ADDRESS.ToUpper() == request.EmailAddress.ToUpper()
                          select user1).AsNoTracking().FirstOrDefaultAsync();

        if (user == null)
        {
            return new ResultNew<bool>(false, new[] { "Email address not found." });
        }

        // Step 2: Validate the OTP
        var isValid = await _otpRepo.ValidateOtpAsync(user.USER_EMP_CODE, request.Otp);

        if (!isValid)
        {
            return new ResultNew<bool>(false, new[] { "Invalid or expired OTP." });
        }

        // Optional: Proceed to password reset or generate reset token etc.

        return ResultNew<bool>.Success(true);
    }
}

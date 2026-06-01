using ERPMultiTenent.Application.Amm.UserDetail.Command;
using ERPMultiTenent.Application.Common.Interfaces;
using ERPMultiTenent.Application.Common.Models;
using ERPMultiTenent.Application.ERP.AmmProvisioning.AmmUserRegister.Command;
using ERPMultiTenent.Common.CrossCutting.Exceptions;
using ERPMultiTenent.WebUI.Controllers;
using ERPMultiTenent.WebUI.Filters;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace ERPMultiTenent.Application.Amm ;

[ApiController]
[ApiExceptionFilter]
[Route("/api/[controller]")]
public class AmmUserRegisterController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly IAmmDbContext _context;
    private ISender? _mediator;

    protected ISender Mediator => _mediator ??= HttpContext.RequestServices.GetRequiredService<ISender>();

    public static JsonSerializerSettings jsonSetting = new JsonSerializerSettings
    {
        NullValueHandling = NullValueHandling.Ignore,
        //DateTimeZoneHandling = DateTimeZoneHandling.Utc
        DateTimeZoneHandling = DateTimeZoneHandling.Local

    };
    public AmmUserRegisterController(IConfiguration configuration, IAmmDbContext context)
    {
        _configuration = configuration;
        _context = context;
    }


    //[HttpGet("{AUR_EMAIL}")]
    //public async Task<AmmUserRegisterDTO> Get(String AUR_EMAIL)
    //{
    //    return await Mediator.Send(new AmmUserRegisterQuery(AUR_EMAIL));
    //}


    [HttpPost("Login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status401Unauthorized)]
    [ProducesDefaultResponseType]
    public async Task<IActionResult> Login([FromBody] CreateLoginByEmailCommand loginCommand)
    {
        loginCommand.EmailAddress = loginCommand.EmailAddress.ToLower();

        var response = await Mediator.Send(loginCommand);
        if (response.Succeeded)
            return Ok(response);// new { token = response.ResultValue });

        return Ok(response);
    }

    [HttpGet("LoginById")]
  //  [ProducesResponseType(StatusCodes.Status200OK)]
   // [ProducesResponseType(typeof(string), StatusCodes.Status401Unauthorized)]
   // [ProducesDefaultResponseType]
    public async Task<IActionResult> LoginById(int UserId)
    {
        CreateLoginByEmailCommand loginCommand = new CreateLoginByEmailCommand { UserId = UserId, };
        var response = await Mediator.Send(loginCommand);
        if (response.Succeeded)
            return Ok(response);// new { token = response.ResultValue });

        return Ok(response);
    }

    [HttpPost("ValidateOtp")]
    public async Task<IActionResult> ValidateOtp([FromBody] ValidateOtpCommand command)
    {
        var result = await Mediator.Send(command);
        //if (!result.Succeeded)
        //    return BadRequest(result.Errors);
        return Ok(result);
    }
    [HttpPost("ForgotPassword")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordCommand command)
    {
        var result = await Mediator.Send(command);
        return Ok(result);
    }

    [HttpPost("ChangePassword")]
    public async Task<IActionResult> ChangePassword([FromBody] UpdatePasswordCommand command)
    {
        var result = await Mediator.Send(command);
        return Ok(result);
    }

    [HttpPost("SendOTPOnRegister")]
    public async Task<ResultNew<bool>> ForgotPassword([FromBody] SendOTPOnRegisterCommand command)
    {
        var result = await Mediator.Send(command);
        return result;
    }

    [HttpPost("VerifyOTPOnRegistration")]
  
    public async Task<ResultNew<bool>> VerifyOTPOnRegistration(ValidateOTPOnRegisterCommand command)
    {
        //command.Dto.AUR_EMAIL = command.Dto.AUR_EMAIL.ToLower();
        command.Email = command.Email.ToLower();
        
       return  await Mediator.Send(command);

        //if (command.OTPVerificationFailed)
        //{
        //    return BadRequest(new { IsOTPPassed = false, Error = "OTP is not correct." });
        //}

        //return Ok(new { IsOTPPassed = true, Error = "" });

    }

        [HttpPost("Register")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    [ProducesDefaultResponseType]
    public async Task<IActionResult> Register(CreateAmmUserRegisterCommand command)
    {
        command.Dto.AUR_EMAIL = command.Dto.AUR_EMAIL.ToLower();

        var result = await Mediator.Send(command);

        //if (command.OTPVerificationFailed)
        //{
        //    return BadRequest(new { OTPFailed = true, Error = "OTP is not correct." });
        //}

        if (result.Succeeded)
        {
            // send create token command

            var result2 = await (from user in _context.AMM_USER_DETAILS
                                 join emp in _context.PPM_EMPLOYEE_DETAILS
                                   on user.USER_EMP_CODE equals emp.PEMP_EMP_CODE
                                 //where emp.PEMP_EMAIL_ADDRESS.ToUpper() == command.Dto.AUR_EMAIL.ToUpper()
                                 where EF.Functions.Like(emp.PEMP_EMAIL_ADDRESS, command.Dto.AUR_EMAIL) //emp.PEMP_EMAIL_ADDRESS.ToUpper() == request.EmailAddress.ToUpper()
                                 select user
                                 ).FirstOrDefaultAsync();

            if (result2 == null)
            {
                throw new ValidationException("Email address not found-2.");
            }
            // return Ok(new { token = GenerateJSONWebToken(result2) });
            var response = await Mediator.Send(new CreateLoginByEmailCommand { EmailAddress = command.Dto.AUR_EMAIL, Password = command.Dto.AUR_PASSWORD });

            return Ok(response);

        }
        else
        {
            return Ok(result);
        }
    }

    [HttpPost("EmailAddressAlreadyExits")]
    public async Task<IActionResult> EmailAddressAlreadyExits([FromBody] EmailAddressAlreadyExitsCommand command)
    {
        var result = await Mediator.Send(command);
        return Ok(result);
    }

    //[HttpPut]
    //public async Task<ActionResult<ResultNew<AmmUserRegisterDTO>>> Put(UpdateAmmUserRegisterCommand command)
    //{
    //    return await Mediator.Send(command);
    //}

    [HttpDelete]
    public async Task<ActionResult<ResultNew<string>>> Delete(DeleteAmmUserRegisterCommand command)
    {
        return await Mediator.Send(command);
    }
}
 
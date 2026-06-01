using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AutoMapper;
using ERPMultiTenent.Application.Common.Interfaces;
using ERPMultiTenent.Application.Common.Models;
using ERPMultiTenent.Domain.Entities.Amm;
using ERPMultiTenent.Domain.Entities.Amm.Provisioning;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ERPMultiTenent.Application.Amm.UserDetail.Command;
public class CreateLoginByEmailCommand : IRequest<ResultNew<UserRoleDTO>>
{
    public string EmailAddress { get; set; }
    public string Password { get; set; }
    public int RoleId { get; set; }
    public int? UserId { get; set; }
    public bool? ForceLogin { get; set; } = false;
    //public bool? LoginByUserI { get; set; }
}

public class CreateLoginByEmailCommandCommandHandler : IRequestHandler<CreateLoginByEmailCommand, ResultNew<UserRoleDTO>>
{
    private readonly IAmmDbContext _context;
    private readonly IUserRoleLinkRepo _userRoleLinkRepo;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUser;
    private readonly IConfiguration _configuration;

    public CreateLoginByEmailCommandCommandHandler(IAmmDbContext context,
        IUserRoleLinkRepo userRoleLinkRepo,
        IMapper mapper, ICurrentUserService currentUser, IConfiguration configuration)
    {
        _context = context;
        _userRoleLinkRepo = userRoleLinkRepo;
        _mapper = mapper;
        _currentUser = currentUser;
        _configuration = configuration;
    }

    public async Task<ResultNew<UserRoleDTO>> Handle(CreateLoginByEmailCommand request, CancellationToken cancellationToken)
    {

         
        var emailUpper = request.EmailAddress.ToUpper();

        var user = await (from emp in _context.PPM_EMPLOYEE_DETAILS
                          join u in _context.AMM_USER_DETAILS
                              on emp.PEMP_EMP_CODE equals u.USER_EMP_CODE
                          where emp.PEMP_EMAIL_ADDRESS.ToUpper() == emailUpper
                          select u).AsNoTracking().FirstOrDefaultAsync();

        if (user == null)
        {
            user = await _context.AMM_USER_DETAILS.AsNoTracking()
                .FirstOrDefaultAsync(u => u.USER_NAME.ToUpper() == emailUpper);
        }

        if (request.UserId.HasValue)
        {
            user = await (from user1 in _context.AMM_USER_DETAILS
                         where user1.USER_ID == request.UserId
                         select user1).AsNoTracking().FirstOrDefaultAsync();//new { user1.USER_PASSWORD, user1.AUD_COMP_CODE, user1.USER_SHORT_NAME, user1.USER_ID });
        }

 

        if (user == null)
        {
            return ResultNew<UserRoleDTO>.FailureResult(new UserRoleDTO(), ["Email address not found."]);
        }
        if (request.UserId == null && user.USER_PASSWORD != request.Password)
        {
            return ResultNew<UserRoleDTO>.FailureResult(new UserRoleDTO(),["Password not found.."]);
        }
        var sessionHours = int.Parse(_configuration["Jwt:SessionTimeoutHours"] ?? "48");
        var expiry = DateTime.Now.AddHours(sessionHours);


        var singleSessionEnabled = (_configuration["SingleSessionEnabled"] ?? "true") == "true";
        if (singleSessionEnabled)
        {
            bool hasActiveSession =
                user.USER_SESSION_EXPIRES_AT.HasValue &&
                user.USER_SESSION_EXPIRES_AT > DateTime.Now;  // ✅ only this matters

            if (hasActiveSession)
            {
                var inactivityTimeoutMins = int.Parse(
                    _configuration["Session:InactivityTimeoutMins"] ?? "10");

                var lastActivity = user.USER_LAST_ACTIVITY_DATE
                                   ?? user.USER_SESSION_STARTED_AT
                                   ?? DateTime.MinValue;

                bool isInactive = lastActivity
                    .AddMinutes(inactivityTimeoutMins) < DateTime.Now;

                if (isInactive || request.ForceLogin == true)
                {
                    user.USER_SESSION_STARTED_AT = null;
                    user.USER_SESSION_EXPIRES_AT = null;
                    user.USER_LAST_ACTIVITY_DATE = null;

                }
                else
                {

                    //include  user.USER_SESSION_EXPIRES_AT
                    //    return ResultNew<LoginResponse>.Failure(
                    //    [
                    //        $"You have an active session on another device or browser. " +
                    //$"Choose 'Force Login' to sign out that session and continue. Session expires at {user.USER_SESSION_EXPIRES_AT}"
                    //    ]);
                    // show message with session expiry time and option to force login
                    return ResultNew<UserRoleDTO>.FailureResult(new UserRoleDTO(),
                        [$"You have an active session on another device or browser. Session expires at {user.USER_SESSION_EXPIRES_AT}"]);

                }
            }
        }
        var dto1 = new UserRoleDTO { UserDetailDTO = _mapper.Map<UserDetailDTO>(user), };
        //set USER_SESSION_STARTED_AT
        user.USER_SESSION_STARTED_AT = DateTime.Now;
        //USER_SESSION_EXPIRES_AT
        user.USER_SESSION_EXPIRES_AT = expiry; // 48 hours session expiry
        user.USER_LAST_ACTIVITY_DATE = DateTime.Now;

        var comp2 = (from c in
                        _context.AMM_COMPANY_DETAILS
                     join sub in _context.AMM_SUBSCRIPTIONS on c.ACD_SUBSCRIPTION_ID equals sub.AS_SUBS_ID into subGroup
                     from sub in subGroup.DefaultIfEmpty() // Left join
                     where c.ACD_COMP_CODE == user.AUD_COMP_CODE
                     select new { c.ACD_SUBSCRIPTION_REQ_YN, sub.AS_STATUS, c.ACD_COMP_SHORT_NAME, sub.AS_ACTIVE_EXPIRED_STATUS }).AsNoTracking().FirstOrDefault();

        if (comp2 != null)
        {
            dto1.SubscriptionRequiredYN = comp2.ACD_SUBSCRIPTION_REQ_YN;
            dto1.SubscriptionStatus = comp2.AS_STATUS;
            dto1.SubscriptionExpiryStatus = comp2.AS_ACTIVE_EXPIRED_STATUS;

        }
        dto1.ShortCode = comp2.ACD_COMP_SHORT_NAME;

        var showRoles = true;
        if (dto1.SubscriptionRequiredYN == "Y")
        {
            //if (dto1.SubscriptionStatus is "Paid" or "Trial"
            //    && dto1.SubscriptionExpiryStatus is "Active"
            //    )
            //{
            //    // show roles only for active and trail
            //    showRoles = true;
            //}
            //else
            //{
            //    showRoles = false;
            //}
        }
        if (showRoles)
        {
            dto1.UserRoles = await this._userRoleLinkRepo.GetByUserId(dto1.UserDetailDTO.AUD_COMP_CODE, dto1.UserDetailDTO.USER_ID, cancellationToken);
        }
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:SecretKey"]));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        
        var claims = new[] {
            new Claim(JwtRegisteredClaimNames.Sub, user.USER_ID.ToString()),
            new Claim(JwtRegisteredClaimNames.NameId, user.USER_ID.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            //new Claim("ShortCode", comp2.ACD_COMP_SHORT_CODE),
            //new Claim("SubRequired", comp.ACD_SUBSCRIPTION_REQ_YN),
            //new Claim("SubStatus", subscription.AS_STATUS),//Active, GracePeriod, Expired, Trail
         };

        var token = new JwtSecurityToken(_configuration["Jwt:Issuer"],
            _configuration["Jwt:Issuer"],
            claims,
            expires: DateTime.Now.AddHours(sessionHours),
            signingCredentials: credentials);
        //token.Id = userInfo.USER_ID.ToString();
        
        dto1.JsonToken = new JwtSecurityTokenHandler().WriteToken(token);

        return ResultNew<UserRoleDTO>.Success(dto1);
 
    }

    //private   string GenerateJSONWebToken(AMM_USER_DETAILS userInfo, AMM_COMPANY_DETAILS comp)
    //{
     
    //}

}

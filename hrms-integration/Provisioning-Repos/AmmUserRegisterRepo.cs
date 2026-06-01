using AutoMapper;
using AutoMapper.QueryableExtensions;
using ERPMultiTenent.Domain.Entities.PPM;
using ERPMultiTenent.Common.CrossCutting.Exceptions;
using ERPMultiTenent.Application.Common.Interfaces;
using ERPMultiTenent.Application.PPM;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ERPMultiTenent.Application.Amm;
using ERPMultiTenent.Domain.Entities.Amm.Provisioning;
using Microsoft.Extensions.Configuration;
using Oracle.ManagedDataAccess.Client;
using System.Data;
using Microsoft.Extensions.Logging;
using ERPMultiTenent.Application.ERP.Mail;

namespace ERPMultiTenent.Infrastructure.Repository.PPM;
public class AmmUserRegisterRepo : IAmmUserRegisterRepo
{
    private readonly IAmmDbContext _db;
    private readonly IUnitOfWorkAmm _unitOfWork;
    private readonly ILogger<AmmUserRegisterRepo> _logger;

    //private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;
    private readonly IConfiguration _configuration;
    private readonly IAmmOtpRepo _otpRepo;

    public AmmUserRegisterRepo(IAmmDbContext db,
        IUnitOfWorkAmm unitOfWork,
        ILogger<AmmUserRegisterRepo> logger,
        //ICurrentUserService currentUserService,
        IMapper mapper, IConfiguration configuration, IAmmOtpRepo otpRepo)
    {
        _db = db;
        _unitOfWork = unitOfWork;

        _logger = logger;
        //        _currentUserService = currentUserService;
        _mapper = mapper;
        _configuration = configuration;
        _otpRepo = otpRepo;
    }
    public async Task<bool> UpdatePassword(int UserId, string Password)
    {
        await _unitOfWork.BeginTransactionAsync();

        await _db.AMM_USER_DETAILS
        .Where(l => l.USER_ID == UserId)
        .ExecuteUpdateAsync(s => s
            .SetProperty(b => b.USER_PASSWORD, b => Password)
        );

        await _unitOfWork.CommitAsync();

        return true;

    }
    public async Task<bool> Register(CreateAmmUserRegisterCommand command)
    {

      //  var registerStatus = await _otpRepo.ValidateOtpAsync(command.Dto.AUR_EMAIL.ToLower(), command.OTP);

        
        var emailFound = await _db.PPM_EMPLOYEE_DETAILS.AnyAsync(w => w.PEMP_EMAIL_ADDRESS.ToUpper() == command.Dto.AUR_EMAIL.ToUpper());
        command.Dto.AUR_EMAIL = command.Dto.AUR_EMAIL.ToLower();
        command.Dto.AUR_SHORTCODE = command.Dto.AUR_SHORTCODE.ToLower();

        if (emailFound)
        {
            throw new DataAlreadyFoundException($"Email ID already exists: {command.Dto.AUR_EMAIL} in employee details");
        }

        var queryCompShort = await _db.AMM_COMPANY_DETAILS.AnyAsync(w => w.ACD_COMP_SHORT_CODE.ToUpper() == command.Dto.AUR_SHORTCODE.ToUpper());

        if (queryCompShort )
        {
            throw new DataAlreadyFoundException($"Short code  {command.Dto.AUR_SHORTCODE} already exists in company details.");
        }

        var queryCompShort1 = await _db.AMM_USER_REGISTER.AnyAsync(w => w.AUR_SHORTCODE.ToUpper() == command.Dto.AUR_SHORTCODE.ToUpper());

        if (queryCompShort1 )
        {
            throw new DataAlreadyFoundException($"Short code  {command.Dto.AUR_SHORTCODE} is already exists in user registration master.");
        }

        //var queryEmail = _db.AMM_USER_REGISTER.Where(w => w.AUR_EMAIL.ToUpper() == command.Dto.AUR_EMAIL.ToUpper())?.Count();

        //if (queryEmail > 0)
        //{
        //    throw new DataAlreadyFoundException($"AUR_SHORTCODE: {command.Dto.AUR_SHORTCODE}");
        //}

        await _unitOfWork.BeginTransactionAsync();
        var entity = new AMM_USER_REGISTER();


        var empFound = await _db.AMM_USER_REGISTER
           .AnyAsync(l => l.AUR_EMAIL.ToUpper() == command.Dto.AUR_EMAIL.ToUpper());


        if (empFound )
        {
            await _db.AMM_USER_REGISTER
            .Where(l => l.AUR_EMAIL.ToUpper() == command.Dto.AUR_EMAIL.ToUpper())
            .ExecuteUpdateAsync(s => s
                .SetProperty(b => b.AUR_UPDATED_DATE, b => DateTime.Now)
                .SetProperty(b => b.AUR_EMAIL, b => command.Dto.AUR_EMAIL)
                .SetProperty(b => b.AUR_LASTNAME, b => command.Dto.AUR_LASTNAME)
                .SetProperty(b => b.AUR_SHORTCODE, b => command.Dto.AUR_SHORTCODE)
                .SetProperty(b => b.AUR_COMPANYNAME, b => command.Dto.AUR_COMPANYNAME)
                .SetProperty(b => b.AUR_COMP_ADDRESS1, b => command.Dto.AUR_COMP_ADDRESS1)
                .SetProperty(b => b.AUR_COMP_ADDRESS2, b => command.Dto.AUR_COMP_ADDRESS2)
                .SetProperty(b => b.AUR_COMP_ADDRESS3, b => command.Dto.AUR_COMP_ADDRESS3)
                .SetProperty(b => b.AUR_FIRSTNAME, b => command.Dto.AUR_FIRSTNAME)
                .SetProperty(b => b.AUR_PASSWORD, b => command.Dto.AUR_PASSWORD)
            );


            //entityToUpdate.AUR_UPDATED_DATE = DateTime.Now;
            //entityToUpdate.AUR_EMAIL = command.Dto.AUR_EMAIL;
            //entityToUpdate.AUR_LASTNAME = command.Dto.AUR_LASTNAME;
            //entityToUpdate.AUR_SHORTCODE = command.Dto.AUR_SHORTCODE;
            //entityToUpdate.AUR_COMPANYNAME = command.Dto.AUR_COMPANYNAME;
            //entityToUpdate.AUR_COMP_ADDRESS1 = command.Dto.AUR_COMP_ADDRESS1;
            //entityToUpdate.AUR_COMP_ADDRESS2 = command.Dto.AUR_COMP_ADDRESS2;
            //entityToUpdate.AUR_COMP_ADDRESS3 = command.Dto.AUR_COMP_ADDRESS3;
            //entityToUpdate.AUR_FIRSTNAME = command.Dto.AUR_FIRSTNAME;
            //entityToUpdate.AUR_PASSWORD = command.Dto.AUR_PASSWORD;
        }
        else
        {
            entity = _mapper.Map<AMM_USER_REGISTER>(command.Dto);
            entity.AUR_CREATED_DATE = DateTime.Now;
            await _db.AMM_USER_REGISTER.AddAsync(entity);

        }


        await _unitOfWork.CommitAsync();

        using var conn = new OracleConnection(_configuration.GetConnectionString("DefaultConnection"));
        conn.Open();

        using var cmd = new OracleCommand("AMM_REGISTER_USER", conn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.Add("P_EMAIL_ADDRESS", OracleDbType.Varchar2, 500).Value = command.Dto.AUR_EMAIL;
         
        cmd.Parameters.Add("P_STATUS_YN", OracleDbType.Varchar2, 1000).Direction = ParameterDirection.Output;
        cmd.Parameters.Add("P_MESSAGE", OracleDbType.Varchar2, 1000).Direction = ParameterDirection.Output;
        cmd.ExecuteNonQuery();
        
        if (cmd.Parameters["P_STATUS_YN"].Value == null || cmd.Parameters["P_STATUS_YN"].Value?.ToString()!="Y")
        {    
            throw new ValidationException($"Error in DB:: {cmd.Parameters["P_MESSAGE"].Value.ToString()}");
        }
        _logger.LogInformation($"register user procedure status: {cmd.Parameters["P_STATUS_YN"].Value.ToString()} and message: {cmd.Parameters["P_MESSAGE"].Value.ToString()}");
       

        return true;
    }
    public async Task<bool> Update(String AUR_EMAIL, UpdateAmmUserRegisterCommand command)
    {
        var entity = await _db.AMM_USER_REGISTER
         .Where(l => l.AUR_EMAIL == command.Dto.AUR_EMAIL)
         .SingleOrDefaultAsync() ?? throw new NotFoundException(nameof(UpdateAmmUserRegisterCommand), "command");
        entity.AUR_FIRSTNAME = command.Dto.AUR_FIRSTNAME;
        entity.AUR_LASTNAME = command.Dto.AUR_LASTNAME;
        entity.AUR_EMAIL = command.Dto.AUR_EMAIL;
        entity.AUR_PASSWORD = command.Dto.AUR_PASSWORD;
        entity.AUR_COMPANYNAME = command.Dto.AUR_COMPANYNAME;
        entity.AUR_SHORTCODE = command.Dto.AUR_SHORTCODE;
        entity.AUR_COMP_ADDRESS1 = command.Dto.AUR_COMP_ADDRESS1;
        entity.AUR_COMP_ADDRESS2 = command.Dto.AUR_COMP_ADDRESS2;
        entity.AUR_COMP_ADDRESS3 = command.Dto.AUR_COMP_ADDRESS3;
        entity.AUR_COMP_TELEPHONE = command.Dto.AUR_COMP_TELEPHONE;
        entity.AUR_COMPANY_CREATED_YN = command.Dto.AUR_COMPANY_CREATED_YN;
        entity.AUR_CREATED_USER_ID = command.Dto.AUR_CREATED_USER_ID;
        entity.AUR_CREATED_DATE = command.Dto.AUR_CREATED_DATE;
        entity.AUR_UPDATED_USER_ID = command.Dto.AUR_UPDATED_USER_ID;
        entity.AUR_UPDATED_DATE = command.Dto.AUR_UPDATED_DATE;

        return true;
    }


    public async Task<bool> Delete(String AUR_EMAIL)
    {
        var entity = await _db.AMM_USER_REGISTER
                 .Where(l => l.AUR_EMAIL == AUR_EMAIL)
                 .SingleOrDefaultAsync();
        if (entity == null)
        {
            throw new NotFoundException(nameof(AMM_USER_REGISTER), AUR_EMAIL);
        }
        _db.AMM_USER_REGISTER.Remove(entity) ;
        return true;
    }

    public async Task<AmmUserRegisterDTO> Get(String AUR_EMAIL, CancellationToken cancellationToken)
    {
        return await _db.AMM_USER_REGISTER.ProjectTo<AmmUserRegisterDTO>(_mapper.ConfigurationProvider)
             .Where(w => w.AUR_EMAIL == AUR_EMAIL)
             .FirstOrDefaultAsync(cancellationToken);
    }

    //public async Task<IEnumerable<AmmUserRegisterDTO>> GetAll(string CompCode, CancellationToken cancellationToken)
    //{
    //    return await _db.AMM_USER_REGISTER.Where(w=>w.COMP_CODE == CompCode).ProjectTo<AmmUserRegisterDTO>(_mapper.ConfigurationProvider)
    //         .OrderBy(t => t.AUR_EMAIL)
    //         .ToListAsync(cancellationToken);
    //}
}


 
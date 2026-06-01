using AutoMapper;
using AutoMapper.QueryableExtensions;
using ERPMultiTenent.Common.CrossCutting.Exceptions;
using ERPMultiTenent.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using ERPMultiTenent.Application.Amm;
using ERPMultiTenent.Domain.Entities.Amm.Provisioning;
using ERPMultiTenent.Infrastructure.DA;
using ERPMultiTenent.Domain.Enums;
using ERPMultiTenent.Application.ERP.AmmProvisioning.Payment;
using Microsoft.Extensions.Logging;
using Dapper;
using ERPMultiTenent.Application.ERP.AmmProvisioning.AmmSubscription;
using Oracle.ManagedDataAccess.Client;
using System.Data;
using Microsoft.Extensions.Configuration;

namespace ERPMultiTenent.Infrastructure.Repository.PPM;
public class AmmSubscriptionRepo : IAmmSubscriptionRepo
{
    private readonly IAmmDbContext _db;
    private readonly ILogger<AmmSubscriptionRepo> _logger;
    private readonly DapperDA _dapper;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUserService;
    private readonly IConfiguration _configuration;

    public AmmSubscriptionRepo(IAmmDbContext db, ILogger<AmmSubscriptionRepo> logger, DapperDA dapper, IMapper mapper, ICurrentUserService currentUserService, IConfiguration configuration)
    {
        _db = db;
        _logger = logger;
        _dapper = dapper;
        _mapper = mapper;
        _currentUserService = currentUserService;
        _configuration = configuration;
    }
    public async Task<bool> Purchase(PurchaseSubscriptionCommand command)
    {
        var entity = new AMM_SUBSCRIPTIONS();

        if (command.Dto.AS_NO_USERS_REQUIRED == 0)
        {
            command.Dto.AS_NO_USERS_REQUIRED = 1;
        }

        // update price
        command.Dto.AS_TOTAL_USERCOST = command.Dto.AS_USER_COST.Value * command.Dto.AS_NO_USERS_REQUIRED.Value;
        command.Dto.AS_TOTAL_AMOUNT = command.Dto.AS_TOTAL_USERCOST.Value + command.Dto.AS_HOSTING_FEE.Value + command.Dto.AS_SETUP_FEE.Value;

        entity = _mapper.Map<AMM_SUBSCRIPTIONS>(command.Dto);
        var plan = await (from pp in _db.AMM_PLAN_PRICE
                          join p in _db.AMM_PLANS on pp.APP_PLAN_ID equals p.AP_PLAN_ID
                          where pp.APP_PLANPRICE_ID == command.Dto.AS_PLANPRICE_ID
                          select pp).Include(i=>i.AMM_PLANS).AsNoTracking().FirstOrDefaultAsync();
        if (plan == null)
        {
            throw new NotFoundException("PlanId", "Plan id not found.");
        }

        var subscription = await (from s in _db.AMM_SUBSCRIPTIONS where s.AS_COMPCODE == _currentUserService.CompCode select s).AsNoTracking().FirstOrDefaultAsync();
        
        if (subscription == null)
        {

            // new subscription
            entity.AS_SUBS_ID = command.Dto.AS_SUBS_ID = (int)await _db.GetSequenceValue(AmmSequenceList.AMM_SUBSCRIPTIONS_SEQ);
            subscription = entity;
            await _db.AMM_SUBSCRIPTIONS.AddAsync(entity);
            // update the subscription as no one is found.
            await _db.AMM_COMPANY_DETAILS
         .Where(c => c.ACD_COMP_CODE == command.Dto.AS_COMPCODE)
         .ExecuteUpdateAsync(e => e.SetProperty(p => p.ACD_SUBSCRIPTION_ID, entity.AS_SUBS_ID));

        }
        else
        {
            //subscription.AS_ACTIVE_EXPIRED_STATUS = "";
            _logger.LogInformation($"Well {subscription.AS_SUBS_ID}");

            //copy all to subscription
            //subscription.AS_TOTAL_AMOUNT = entity.AS_TOTAL_AMOUNT;
            //subscription.AS_TOTAL_USERCOST = entity.AS_TOTAL_USERCOST;
            //subscription.AS_PLANPRICE_ID = entity.AS_PLANPRICE_ID;
            //subscription.AS_SETUP_FEE = entity.AS_SETUP_FEE;
            //subscription.AS_HOSTING_FEE = entity.AS_HOSTING_FEE;
            //subscription.AS_NO_USERS_REQUIRED = entity.AS_NO_USERS_REQUIRED;

            //subscription.AS_FROM_DATE = DateTime.Now;
            //subscription.AS_TO_DATE = DateTime.Now.AddDays(plan.APP_PAY_NEXT_DAYS);

            //subscription.AS_UPDATED_DATE = DateTime.Now;
            //subscription.AS_UPDATED_USER = _currentUserService.UserIdInt;
            int employeeCount = int.Parse(plan.APP_NO_OF_EMPLOYEES);

            await _db.AMM_SUBSCRIPTIONS
                .Where(s => s.AS_SUBS_ID == subscription.AS_SUBS_ID)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(b => b.AS_TOTAL_AMOUNT, b => entity.AS_TOTAL_AMOUNT)
                    .SetProperty(b => b.AS_TOTAL_USERCOST, b => entity.AS_TOTAL_USERCOST)
                    .SetProperty(b => b.AS_PLANPRICE_ID, b => entity.AS_PLANPRICE_ID)
                    .SetProperty(b => b.AS_SETUP_FEE, b => entity.AS_SETUP_FEE)
                    .SetProperty(b => b.AS_HOSTING_FEE, b => entity.AS_HOSTING_FEE)
                    .SetProperty(b => b.AS_NO_USERS_REQUIRED, b => entity.AS_NO_USERS_REQUIRED)
                    .SetProperty(b => b.AS_FROM_DATE, b => DateTime.Now)
                    .SetProperty(b => b.AS_TO_DATE, b => DateTime.Now.AddDays(plan.APP_PAY_NEXT_DAYS))
                    .SetProperty(b => b.AS_UPDATED_DATE, b => DateTime.Now)
                    .SetProperty(b => b.AS_UPDATED_USER, b => _currentUserService.UserIdInt)
                     .SetProperty(b => b.AS_NO_EMPLOYEES, b => employeeCount)
                // .SetProperty(b => b.AI_USER_ID, b => _currentUserService.UserIdInt)
                //  AI_USER_ID = _currentUserService.UserIdInt
                );
            // take backup of current one 
        }

        // create invoice, and then redirect 
        // check invoice is already created (open invoice for the subscription_), if yes, update the
        // delete open invoice then create new invoice
        var invoice = new AMM_INVOICES
        {

            AI_AMOUNT = entity.AS_TOTAL_AMOUNT.Value,
            AI_BILL_ADDRESS = command.AI_BILL_ADDRESS,
            AI_CREATED_DATE = DateTime.Now,
            AI_CREATED_USER = _currentUserService.UserIdInt,
            AI_EMAIL_ADDRESS = command.AI_EMAIL_ADDRESS,
            AI_FULLY_PAID = "N",
            AI_INVOICE_DATE = DateTime.Now,
            AI_PHONE_NO = command.AI_PHONE_NO,
            AI_SUBS_ID = subscription.AS_SUBS_ID,
            AI_USER_ID = _currentUserService.UserIdInt
        };

        command.Dto.InvoiceId = invoice.AI_INVOICE_ID = (int)await _db.GetSequenceValue(AmmSequenceList.AMM_INVOICES_SEQ);
        invoice.AI_INVOICE_NUMBER = $"INV-{invoice.AI_INVOICE_ID}";
        //invoice.AI_SUBS_ID = entity.AS_SUBS_ID;
        invoice.AI_PLAN_NAME = plan.AMM_PLANS.AP_PLAN_NAME;

        await _db.AMM_INVOICES.AddAsync(invoice);

        return true;
    }

    public async Task<bool> Trial(SubscriptionTrialCommmand command)
    {
        var entity = new AMM_SUBSCRIPTIONS();

        if (command.Dto.AS_NO_USERS_REQUIRED == 0)
        {
            command.Dto.AS_NO_USERS_REQUIRED = 1;
        }

        command.Dto.AS_STATUS = SubscriptionStatusEnum.Trial.ToString();
        command.Dto.AS_ACTIVE_EXPIRED_STATUS = SubscriptionActiveStatus.Active.ToString();

        command.Dto.AS_CREATED_DATE = DateTime.Now;
        command.Dto.AS_CREATED_USER = _currentUserService.UserIdInt;
        command.Dto.AS_FROM_DATE = DateTime.Now;
        command.Dto.AS_TO_DATE = DateTime.Now.AddMonths(1);

        // update price
        command.Dto.AS_TOTAL_USERCOST = command.Dto.AS_USER_COST.Value * command.Dto.AS_NO_USERS_REQUIRED.Value;
        command.Dto.AS_TOTAL_AMOUNT = command.Dto.AS_TOTAL_USERCOST.Value + command.Dto.AS_HOSTING_FEE.Value + command.Dto.AS_SETUP_FEE.Value;

        entity = _mapper.Map<AMM_SUBSCRIPTIONS>(command.Dto);
        entity.AS_SUBS_ID = command.Dto.AS_SUBS_ID = (int)await _db.GetSequenceValue(AmmSequenceList.AMM_SUBSCRIPTIONS_SEQ);



        await _db.AMM_SUBSCRIPTIONS.AddAsync(entity);

        await _db.AMM_COMPANY_DETAILS
            .Where(c => c.ACD_COMP_CODE == command.Dto.AS_COMPCODE)
            .ExecuteUpdateAsync(e => e.SetProperty(p => p.ACD_SUBSCRIPTION_ID, entity.AS_SUBS_ID));

        return true;
    }
    public async Task<bool> Update(Int32 AS_SUBS_ID, UpdateAmmSubscriptionCommand command)
    {
        var entity = await _db.AMM_SUBSCRIPTIONS
         .Where(l => l.AS_SUBS_ID == command.Dto.AS_SUBS_ID)
         .SingleOrDefaultAsync();
        if (entity == null)
        {
            throw new NotFoundException(nameof(UpdateAmmSubscriptionCommand), "command");
        }


        entity.AS_SUBS_ID = command.Dto.AS_SUBS_ID;
        entity.AS_PLANPRICE_ID = command.Dto.AS_PLANPRICE_ID;
        entity.AMM_PLAN_PRICE = command.Dto.AMM_PLAN_PRICE;
        entity.AS_FROM_DATE = command.Dto.AS_FROM_DATE;
        entity.AS_TO_DATE = command.Dto.AS_TO_DATE;
        entity.AS_TOTAL_AMOUNT = command.Dto.AS_TOTAL_AMOUNT;
        entity.AS_STATUS = command.Dto.AS_STATUS;
        entity.AS_ACTIVE_EXPIRED_STATUS = command.Dto.AS_ACTIVE_EXPIRED_STATUS;
        entity.AS_USER_ID = command.Dto.AS_USER_ID;
        entity.AS_COMPCODE = command.Dto.AS_COMPCODE;
        entity.AS_GRACE_DATE = command.Dto.AS_GRACE_DATE;
        entity.AS_USER_COST = command.Dto.AS_USER_COST;
        entity.AS_NO_EMPLOYEES = command.Dto.AS_NO_EMPLOYEES;
        entity.AS_SETUP_FEE = command.Dto.AS_SETUP_FEE;
        entity.AS_HOSTING_FEE = command.Dto.AS_HOSTING_FEE;
        entity.AS_NO_USERS_REQUIRED = command.Dto.AS_NO_USERS_REQUIRED;
        entity.AS_TOTAL_USERCOST = command.Dto.AS_TOTAL_USERCOST;
        entity.AS_CREATED_DATE = command.Dto.AS_CREATED_DATE;
        entity.AS_UPDATED_DATE = command.Dto.AS_UPDATED_DATE;
        entity.AS_CREATED_USER = command.Dto.AS_CREATED_USER;
        entity.AS_UPDATED_USER = command.Dto.AS_UPDATED_USER;

        return true;
    }


    public async Task<bool> Delete(Int32 AS_SUBS_ID)
    {
        var entity = await _db.AMM_SUBSCRIPTIONS
                 .Where(l => l.AS_SUBS_ID == AS_SUBS_ID)
                 .SingleOrDefaultAsync();
        if (entity == null)
        {
            throw new NotFoundException(nameof(AMM_SUBSCRIPTIONS), AS_SUBS_ID);
        }
        _db.AMM_SUBSCRIPTIONS.Remove(entity);
        return true;
    }

    public async Task<AmmSubscriptionDTO> Get(Int32 AS_SUBS_ID, CancellationToken cancellationToken)
    {
        return await _db.AMM_SUBSCRIPTIONS.ProjectTo<AmmSubscriptionDTO>(_mapper.ConfigurationProvider)
             .Where(w => w.AS_SUBS_ID == AS_SUBS_ID)
             .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<AmmSubscriptionDTOToDisp> GetAll(string CompCode, CancellationToken cancellationToken)
    {
        return await _db.AMM_SUBSCRIPTIONS.Where(w => w.AS_COMPCODE == CompCode).ProjectTo<AmmSubscriptionDTOToDisp>(_mapper.ConfigurationProvider)
             //.OrderBy(t => t.AS_SUBS_ID)
             .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<int> Payment(UnifiedPaymentCommand command)
    {
        // 0. IDEMPOTENCY — return immediately if this (invoice, order_reference, "Paid")
        //    has already been processed. Orchestrator retries webhooks; we MUST be a no-op.
        if (!string.IsNullOrEmpty(command.OrderReference) && command.Status == "Paid")
        {
            var alreadyPaid = await _db.AMM_PAYMENTS.AsNoTracking()
                .AnyAsync(p => p.AP_INVOICE_ID == command.InvoiceId
                            && p.AP_ORDER_REFERENCE == command.OrderReference
                            && p.AP_STATUS == "Paid");
            if (alreadyPaid)
            {
                _logger.LogInformation("Webhook already processed for invoice {Inv} / order {Ref}, no-op",
                    command.InvoiceId, command.OrderReference);
                var invUserId = await _db.AMM_INVOICES.AsNoTracking()
                    .Where(i => i.AI_INVOICE_ID == command.InvoiceId)
                    .Select(i => i.AI_USER_ID).FirstOrDefaultAsync();
                return invUserId ?? 0;
            }
        }

        // 1. Find the invoice
        var invoice = await _db.AMM_INVOICES
            .Where(i => i.AI_INVOICE_ID == command.InvoiceId)
            .AsNoTracking()
            .FirstOrDefaultAsync();

        if (invoice == null)
            throw new NotFoundException(nameof(AMM_INVOICES), command.InvoiceId);

        // 2. Find the associated subscription
        var subscription = await _db.AMM_SUBSCRIPTIONS
            .Where(s => s.AS_SUBS_ID == invoice.AI_SUBS_ID)
            .AsNoTracking()
            .FirstOrDefaultAsync();

        if (subscription == null)
            throw new NotFoundException(nameof(AMM_SUBSCRIPTIONS), invoice.AI_SUBS_ID);

        // 3. Plan (for grace / pay-next-days)
        var plan = await _db.AMM_PLAN_PRICE
            .Where(p => p.APP_PLANPRICE_ID == subscription.AS_PLANPRICE_ID)
            .Include(p => p.AMM_PLANS)
            .FirstOrDefaultAsync();

        if (plan == null)
            throw new NotFoundException(nameof(AMM_PLAN_PRICE), subscription.AS_PLANPRICE_ID);

        // 4. CREATE payment record (always — even on failure, for audit)
        using var dappConn = _dapper.CreateOpenConnection();
        var payment = new AMM_PAYMENTS
        {
            AP_PAY_ID = await dappConn.QueryFirstAsync<int>(@"select AMM_INVOICES_SEQ.nextval from dual"),
            AP_INVOICE_ID = invoice.AI_INVOICE_ID,
            AP_AMOUNT_RCVD = invoice.AI_AMOUNT,
            AP_CREATED_DATE = DateTime.Now,
            AP_CREATED_USER = invoice.AI_USER_ID ?? 0,
            AP_USER_ID = "1",
            AP_RAW_DATA = command.RawData,
            AP_ORDER_REFERENCE = command.OrderReference,
            AP_PROVIDER = command.Provider,
            AP_PROVIDER_TXN_ID = command.TransactionId,
            AP_STATUS = command.Status,        // "Paid" / "Failed" / "Cancelled" / "Expired"
        };

        // 5. Apply state changes based on status
        if (command.Status == "Paid")
        {
            await _db.AMM_INVOICES
                .Where(i => i.AI_INVOICE_ID == command.InvoiceId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(i => i.AI_FULLY_PAID, i => "Y")
                    .SetProperty(i => i.AI_UPDATED_DATE, i => DateTime.Now)
                    .SetProperty(i => i.AI_UPDATED_USER, i => _currentUserService.UserIdInt));

            await _db.AMM_SUBSCRIPTIONS
                .Where(s => s.AS_SUBS_ID == invoice.AI_SUBS_ID)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(s => s.AS_STATUS, s => SubscriptionStatusEnum.Paid.ToString())
                    .SetProperty(s => s.AS_ACTIVE_EXPIRED_STATUS, s => SubscriptionActiveStatus.Active.ToString())
                    .SetProperty(s => s.AS_FROM_DATE, s => DateTime.Now)
                    .SetProperty(s => s.AS_TO_DATE, s => DateTime.Now.AddDays(plan.APP_PAY_NEXT_DAYS))
                    .SetProperty(s => s.AS_GRACE_DATE, s => DateTime.Now.AddDays(plan.APP_PAY_NEXT_DAYS + plan.APP_GRACE_DAYS))
                    .SetProperty(s => s.AS_UPDATED_DATE, s => DateTime.Now)
                    .SetProperty(s => s.AS_UPDATED_USER, s => _currentUserService.UserIdInt));
        }
        else
        {
            // Failed / Cancelled / Expired — invoice stays unpaid; subscription untouched
            // (it may still be in trial). Just stamp updated_date so audit reflects the attempt.
            await _db.AMM_INVOICES
                .Where(i => i.AI_INVOICE_ID == command.InvoiceId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(i => i.AI_FULLY_PAID, i => "N")
                    .SetProperty(i => i.AI_UPDATED_DATE, i => DateTime.Now)
                    .SetProperty(i => i.AI_UPDATED_USER, i => _currentUserService.UserIdInt));
        }

        _db.AMM_PAYMENTS.Add(payment);

        return invoice.AI_USER_ID ?? 0;
    }


    public async Task<PayValidCompSubsDto> ValidateCompanySubscriptionAsync(string compCode, int roleId)
    {
        var result = new PayValidCompSubsDto();
        var connString = _configuration.GetConnectionString("DefaultConnection");

        using OracleConnection objConn = new OracleConnection(connString);
        await objConn.OpenAsync();

        using OracleCommand cmd = new OracleCommand("PAY_VALID_COMP_SUBS", objConn)
        {
            CommandType = CommandType.StoredProcedure
        };

        // Input parameter
        cmd.Parameters.Add(new OracleParameter("P_COMP_CODE", OracleDbType.Varchar2, ParameterDirection.Input)
        {
            Value = compCode
        });
        cmd.Parameters.Add(new OracleParameter("P_ROLE_ID", OracleDbType.Decimal, ParameterDirection.Input)
        {
            Value = roleId
        });

        // Output parameters
        var userValidParam = new OracleParameter("P_USER_VALIDATION_PASS_YN", OracleDbType.Varchar2, 1)
        {
            Direction = ParameterDirection.Output
        };
        cmd.Parameters.Add(userValidParam);

        var empValidParam = new OracleParameter("P_EMP_VALIDATION_PASS_YN", OracleDbType.Varchar2, 1)
        {
            Direction = ParameterDirection.Output
        };
        cmd.Parameters.Add(empValidParam);

        var descParam = new OracleParameter("P_DESC", OracleDbType.Varchar2, 1000)
        {
            Direction = ParameterDirection.Output
        };
        cmd.Parameters.Add(descParam);

        // Execute
        await cmd.ExecuteNonQueryAsync();

        // Retrieve output values
        result.UserValidationPassYn = userValidParam.Value?.ToString()?.Equals("Y");
        result.EmpValidationPassYn = empValidParam.Value?.ToString()?.Equals("Y");
        result.Description = descParam.Value?.ToString();

        return result;
    }

}



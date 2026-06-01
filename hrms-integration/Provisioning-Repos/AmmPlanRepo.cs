using System.Text;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Dapper;
using ERPMultiTenent.Application.Amm;
using ERPMultiTenent.Common.CrossCutting.Exceptions;
using ERPMultiTenent.Application.Common.Interfaces;
using ERPMultiTenent.Domain.Entities.Amm.Provisioning;
using ERPMultiTenent.Infrastructure.DA;
using Microsoft.EntityFrameworkCore;

namespace ERPMultiTenent.Infrastructure.Repository.PPM;
public class AmmPlanRepo : IAmmPlanRepo
{
    private readonly IAmmDbContext _db;
    private readonly DapperDA _dapperDA;
     private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;

    public AmmPlanRepo(IAmmDbContext db,
        DapperDA dapperDA,
         ICurrentUserService currentUserService, IMapper mapper)
    {
        _db = db;
        _dapperDA = dapperDA;
         _currentUserService = currentUserService;
        _mapper = mapper;
    }
    public async Task<bool> Add(CreateAmmPlanCommand command)
    {
        var entity = new AMM_PLANS();

        entity = _mapper.Map<AMM_PLANS>(command.Dto);
        using var dappConn = _dapperDA.CreateOpenConnection();
       
        command.Dto.AP_PLAN_ID= entity.AP_PLAN_ID = dappConn.ExecuteScalar<int>(@"select AMM_PLANS_SEQ.NEXTVAL FROM DUAL");

         
        entity.AP_CREATED_DATE = DateTime.Now;

        entity.AP_CREATED_USER = _currentUserService.UserIdInt; ;
        Console.WriteLine(@$"command.Dto.AP_PLAN_ID:: {command.Dto.AP_PLAN_ID}");

        entity.AMM_PLAN_PRICE_LIST = new List<AMM_PLAN_PRICE>();
        // add plan details
        if (command.Dto.AMM_PLAN_PRICE_LIST?.Count >0)
        {
            foreach (var item in command.Dto.AMM_PLAN_PRICE_LIST)
            {
                var entityPrice = new AMM_PLAN_PRICE();

                entityPrice.APP_PLANPRICE_ID = dappConn.ExecuteScalar<int>(@"select AMM_PLAN_PRICE_SEQ.NEXTVAL FROM DUAL");

                entityPrice.APP_PLAN_ID = entity.AP_PLAN_ID;
                entityPrice.APP_PER_USER_COST = item.APP_PER_USER_COST;
                entityPrice.APP_NO_OF_EMPLOYEES = item.APP_NO_OF_EMPLOYEES;
                entityPrice.APP_SETUP_FEE = item.APP_SETUP_FEE;
                entityPrice.APP_HOSTING_FEE = item.APP_HOSTING_FEE;
                entityPrice.APP_DESCRIPTION = item.APP_DESCRIPTION;
                entityPrice.APP_PAY_NEXT_DAYS = item.APP_PAY_NEXT_DAYS;
                entityPrice.APP_ACTIVE_FROM = item.APP_ACTIVE_FROM;
                entityPrice.APP_ACTIVE_TO = item.APP_ACTIVE_TO;
                entityPrice.APP_IS_ACTIVE = item.APP_IS_ACTIVE;
                entityPrice.APP_GRACE_DAYS = item.APP_GRACE_DAYS;

                entityPrice.APP_CREATED_DATE = DateTime.Now;
                entityPrice.APP_CREATED_USER = _currentUserService.UserIdInt;
                entityPrice.APP_PRICE_NAME = item.APP_PRICE_NAME;
                entity.AMM_PLAN_PRICE_LIST.Add(entityPrice);


            }

        }

        await _db.AMM_PLANS.AddAsync(entity);
        return true;
    }
    public async Task<bool> Update(Int32 AP_PLAN_ID, UpdateAmmPlanCommand command)
    {
        var entity = await _db.AMM_PLANS
            .Include(p => p.AMM_PLAN_PRICE_LIST)
            .Where(p => p.AP_PLAN_ID == AP_PLAN_ID)
            .SingleOrDefaultAsync();

        if (entity == null)
            throw new NotFoundException(nameof(UpdateAmmPlanCommand), "Plan not found");
        using var dappConn = _dapperDA.CreateOpenConnection();

        // Update plan fields
        entity.AP_PLAN_NAME = command.Dto.AP_PLAN_NAME;
        entity.AP_DESCRIPTION = command.Dto.AP_DESCRIPTION;
        entity.AP_PLAN_STATUS = command.Dto.AP_PLAN_STATUS;
        entity.AP_UPDATED_DATE = DateTime.Now;
        entity.AP_UPDATED_USER = _currentUserService.UserIdInt;

        var existingPrices = entity.AMM_PLAN_PRICE_LIST.ToList();
        var incomingPrices = command.Dto.AMM_PLAN_PRICE_LIST ?? new List<AMM_PLAN_PRICE_DTO>();

        // 1. Update existing records
        foreach (var incoming in incomingPrices.Where(i => i.APP_PLANPRICE_ID > 0))
        {
            var existing = existingPrices.FirstOrDefault(e => e.APP_PLANPRICE_ID == incoming.APP_PLANPRICE_ID);
            if (existing != null)
            {
                existing.APP_PER_USER_COST = incoming.APP_PER_USER_COST;
                existing.APP_NO_OF_EMPLOYEES = incoming.APP_NO_OF_EMPLOYEES;
                existing.APP_SETUP_FEE = incoming.APP_SETUP_FEE;
                existing.APP_HOSTING_FEE = incoming.APP_HOSTING_FEE;
                existing.APP_DESCRIPTION = incoming.APP_DESCRIPTION;
                existing.APP_PAY_NEXT_DAYS = incoming.APP_PAY_NEXT_DAYS;
                existing.APP_ACTIVE_FROM = incoming.APP_ACTIVE_FROM;
                existing.APP_ACTIVE_TO = incoming.APP_ACTIVE_TO;
                existing.APP_IS_ACTIVE = incoming.APP_IS_ACTIVE;
                existing.APP_GRACE_DAYS = incoming.APP_GRACE_DAYS;
                existing.APP_UPDATED_DATE = DateTime.Now;
                existing.APP_UPDATED_USER = _currentUserService.UserIdInt;
                existing.APP_PRICE_NAME = incoming.APP_PRICE_NAME;
                
            }
        }

        // 2. Add new records
        var newItems = incomingPrices.Where(i => i.APP_PLANPRICE_ID == 0).ToList();
        foreach (var item in newItems)
        {
            var newEntity = new AMM_PLAN_PRICE
            {
                APP_PLANPRICE_ID = dappConn.ExecuteScalar<int>(@"select AMM_PLAN_PRICE_SEQ.NEXTVAL FROM DUAL"),
                APP_PLAN_ID = entity.AP_PLAN_ID,
                APP_PER_USER_COST = item.APP_PER_USER_COST,
                APP_NO_OF_EMPLOYEES = item.APP_NO_OF_EMPLOYEES,
                APP_SETUP_FEE = item.APP_SETUP_FEE,
                APP_HOSTING_FEE = item.APP_HOSTING_FEE,
                APP_DESCRIPTION = item.APP_DESCRIPTION,
                APP_PAY_NEXT_DAYS = item.APP_PAY_NEXT_DAYS,
                APP_ACTIVE_FROM = item.APP_ACTIVE_FROM,
                APP_ACTIVE_TO = item.APP_ACTIVE_TO,
                APP_IS_ACTIVE = item.APP_IS_ACTIVE,
                APP_GRACE_DAYS = item.APP_GRACE_DAYS,
                APP_CREATED_DATE = DateTime.Now,
                APP_CREATED_USER = _currentUserService.UserIdInt,
               APP_PRICE_NAME = item.APP_PRICE_NAME
            };
            entity.AMM_PLAN_PRICE_LIST.Add(newEntity);
        }

        // 3. Remove deleted records
        var incomingIds = incomingPrices.Select(i => i.APP_PLANPRICE_ID).ToHashSet();
        var toRemove = existingPrices.Where(e => !incomingIds.Contains(e.APP_PLANPRICE_ID)).ToList();

        foreach (var remove in toRemove)
        {
        //    _db.AMM_PLAN_PRICE.Remove(remove);
        }

      //  await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> Delete(Int32 AP_PLAN_ID)
    {
        var entity = await _db.AMM_PLANS
                 .Where(l => l.AP_PLAN_ID == AP_PLAN_ID)
                 .SingleOrDefaultAsync();
        if (entity == null)
        {
            throw new NotFoundException(nameof(AMM_PLANS), AP_PLAN_ID);
        }
        _db.AMM_PLANS.Remove(entity) ;
        return true;
    }

    public async Task<AmmPlanDTO> Get(Int32 AP_PLAN_ID, CancellationToken cancellationToken)
    {
        return await _db.AMM_PLANS.ProjectTo<AmmPlanDTO>(_mapper.ConfigurationProvider)
             .Where(w => w.AP_PLAN_ID == AP_PLAN_ID)
             .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IEnumerable<AmmPlanDTO>> GetAll(CancellationToken cancellationToken)
    {
        return await _db.AMM_PLANS.ProjectTo<AmmPlanDTO>(_mapper.ConfigurationProvider)
             .OrderBy(t => t.AP_SORT_ORDER)
             .ToListAsync(cancellationToken);
    }
}


 


using System;

using ERPMultiTenent.Application.Common.Mappings;
using ERPMultiTenent.Domain.Entities.Amm.Provisioning;


namespace ERPMultiTenent.Application.Amm ;

public class AmmPlanDTO : IMapFrom<AMM_PLANS>
{
    public Int32 AP_PLAN_ID { get; set; }
    public String AP_PLAN_NAME { get; set; }
    public String? AP_DESCRIPTION { get; set; }
    public String AP_PLAN_STATUS { get; set; }
    public int? AP_SORT_ORDER { get; set; }
    public DateTime AP_CREATED_DATE { get; set; }
    public DateTime? AP_UPDATED_DATE { get; set; }
    public Decimal AP_CREATED_USER { get; set; }
    public Decimal? AP_UPDATED_USER { get; set; }
    public ICollection<AMM_PLAN_PRICE_DTO> AMM_PLAN_PRICE_LIST { get; set; }

}

public class AMM_PLAN_PRICE_DTO: IMapFrom<AMM_PLAN_PRICE>
{
    public int APP_PLANPRICE_ID { get; set; }

    public int APP_PLAN_ID { get; set; }

    public decimal APP_PER_USER_COST { get; set; }
    public string APP_NO_OF_EMPLOYEES { get; set; }
    public decimal APP_SETUP_FEE { get; set; }
    public decimal APP_HOSTING_FEE { get; set; }
    public string? APP_DESCRIPTION { get; set; }
    public int APP_PAY_NEXT_DAYS { get; set; }

    public DateTime? APP_ACTIVE_FROM { get; set; }


    public DateTime? APP_ACTIVE_TO { get; set; }


    public string APP_IS_ACTIVE { get; set; }


    public int APP_GRACE_DAYS { get; set; }

    public AMM_PLANS AMM_PLANS { get; set; }

    public DateTime APP_CREATED_DATE { get; set; }
    public DateTime? APP_UPDATED_DATE { get; set; }
    public decimal APP_CREATED_USER { get; set; }
    public decimal? APP_UPDATED_USER { get; set; }
    public string APP_PRICE_NAME { get; set; }
}


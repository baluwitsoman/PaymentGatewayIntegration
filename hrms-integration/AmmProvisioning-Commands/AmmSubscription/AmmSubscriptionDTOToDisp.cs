using System.ComponentModel.DataAnnotations.Schema;
using ERPMultiTenent.Application.Common.Mappings;
using ERPMultiTenent.Domain.Entities.Amm.Provisioning;

namespace ERPMultiTenent.Application.Amm ;



public class AmmSubscriptionDTOToDisp : IMapFrom<AMM_SUBSCRIPTIONS>
{
    public Int32 AS_SUBS_ID { get; set; }
    public Int32 AS_PLANPRICE_ID { get; set; }
    public AMM_PLAN_PRICE_DTODisp AMM_PLAN_PRICE { get; set; }
    public DateTime AS_FROM_DATE { get; set; }
    public DateTime AS_TO_DATE { get; set; }
    public decimal AS_TOTAL_AMOUNT { get; set; }
    public string AS_STATUS { get; set; }
    public string AS_ACTIVE_EXPIRED_STATUS { get; set; }
    public string AS_USER_ID { get; set; }
    public string AS_COMPCODE { get; set; }
    public DateTime? AS_GRACE_DATE { get; set; }
    public decimal? AS_USER_COST { get; set; }
    public Int32? AS_NO_EMPLOYEES { get; set; }
    public decimal? AS_SETUP_FEE { get; set; }
    public decimal? AS_HOSTING_FEE { get; set; }
    public Int32? AS_NO_USERS_REQUIRED { get; set; }
    public decimal? AS_TOTAL_USERCOST { get; set; }
    public DateTime AS_CREATED_DATE { get; set; }
    public DateTime? AS_UPDATED_DATE { get; set; }
    public decimal AS_CREATED_USER { get; set; }
    public decimal AS_UPDATED_USER { get; set; }

}


public class AMM_PLAN_PRICE_DTODisp : IMapFrom<AMM_PLAN_PRICE>
{
 
    public int APP_PLANPRICE_ID { get; set; }

    public int APP_PLAN_ID { get; set; }

    public decimal APP_PER_USER_COST { get; set; }
    public string APP_NO_OF_EMPLOYEES { get; set; }
    public decimal APP_SETUP_FEE { get; set; }
    public decimal APP_HOSTING_FEE { get; set; }
    public string? APP_DESCRIPTION { get; set; }
    public string? APP_PRICE_NAME { get; set; }
    public int APP_PAY_NEXT_DAYS { get; set; }

    public DateTime? APP_ACTIVE_FROM { get; set; }


    public DateTime? APP_ACTIVE_TO { get; set; }


    public string APP_IS_ACTIVE { get; set; }


    public int APP_GRACE_DAYS { get; set; }

    //public AMM_PLANS AMM_PLANS { get; set; }

    public DateTime APP_CREATED_DATE { get; set; }
    public DateTime? APP_UPDATED_DATE { get; set; }
    public decimal APP_CREATED_USER { get; set; }
    public decimal? APP_UPDATED_USER { get; set; }
    //public ICollection<AMM_SUBSCRIPTIONS> AMM_SUBSCRIPTIONS_LIST { get; set; }
}


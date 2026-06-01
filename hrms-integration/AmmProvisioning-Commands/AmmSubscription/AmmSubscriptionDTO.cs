using ERPMultiTenent.Application.Common.Mappings;
using ERPMultiTenent.Domain.Entities.Amm.Provisioning;

namespace ERPMultiTenent.Application.Amm ;



public class AmmSubscriptionDTO : IMapFrom<AMM_SUBSCRIPTIONS>
{
    public Int32 AS_SUBS_ID { get; set; }
    public Int32 AS_PLANPRICE_ID { get; set; }
    public AMM_PLAN_PRICE AMM_PLAN_PRICE { get; set; }
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
    public int InvoiceId { get; set; }
    public bool RedirectToWebsite { get; set; }
    public string RedirectURL { get; set; }

}



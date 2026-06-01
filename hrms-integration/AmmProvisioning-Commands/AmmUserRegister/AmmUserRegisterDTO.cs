using ERPMultiTenent.Application.Common.Mappings;
using ERPMultiTenent.Domain.Entities.Amm.Provisioning;


namespace ERPMultiTenent.Application.Amm ;



public class AmmUserRegisterDTO : IMapFrom<AMM_USER_REGISTER>
{
    public string? AUR_FIRSTNAME { get; set; }
    public string? AUR_LASTNAME { get; set; }
    public string AUR_EMAIL { get; set; }
    public string? AUR_PASSWORD { get; set; }
    public string? AUR_COMPANYNAME { get; set; }
    public string? AUR_SHORTCODE { get; set; }
    public string? AUR_COMP_ADDRESS1 { get; set; }
    public string? AUR_COMP_ADDRESS2 { get; set; }
    public string? AUR_COMP_ADDRESS3 { get; set; }
    public string? AUR_COMP_TELEPHONE { get; set; }
    public string? AUR_COMPANY_CREATED_YN { get; set; }
    public Int32 AUR_CREATED_USER_ID { get; set; }
    public DateTime AUR_CREATED_DATE { get; set; }
    public Int32? AUR_UPDATED_USER_ID { get; set; }
    public DateTime? AUR_UPDATED_DATE { get; set; }

} 


 
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERPMultiTenent.Application.ERP.AmmProvisioning.AmmSubscription;
public class PayValidCompSubsDto
{
    public bool? UserValidationPassYn { get; set; }
    public bool? EmpValidationPassYn { get; set; }
    public string Description { get; set; }
}

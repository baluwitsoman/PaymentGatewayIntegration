using Microsoft.AspNetCore.Mvc;
using ERPMultiTenent.WebUI.Controllers;
using ERPMultiTenent.Application.Common.Models;


namespace ERPMultiTenent.Application.Amm ;
 
 

public class AmmPlanController : ApiControllerBase
{


    [HttpGet("{AP_PLAN_ID}")]
    public async Task<AmmPlanDTO> Get(Int32 AP_PLAN_ID)
    {
        return await Mediator.Send(new AmmPlanQuery(AP_PLAN_ID));
    }


    [HttpGet]
    public async Task<IEnumerable<AmmPlanDTO>> Get()
    {
        return await Mediator.Send(new AmmPlanQueryAll());
    }

    [HttpPost]
    public async Task<ActionResult<ResultNew<AmmPlanDTO>>> Create(CreateAmmPlanCommand command)
    {
        return await Mediator.Send(command);
    }

    
    [HttpPut]
    public async Task<ActionResult<ResultNew<AmmPlanDTO>>> Put(UpdateAmmPlanCommand command)
    {
        return await Mediator.Send(command);
    }

    [HttpDelete]
    public async Task<ActionResult<ResultNew<string>>> Delete(DeleteAmmPlanCommand command)
    {
        return await Mediator.Send(command);
    }
}
 
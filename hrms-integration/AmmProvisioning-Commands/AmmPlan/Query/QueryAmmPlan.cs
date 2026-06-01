using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERPMultiTenent.Application.Amm ;


public record AmmPlanQuery(Int32  AP_PLAN_ID) : IRequest<AmmPlanDTO>; 
public class AmmPlanQueryHandler : IRequestHandler<AmmPlanQuery, AmmPlanDTO>
{
    private readonly IMapper _mapper;
    private readonly IAmmPlanRepo _repository;

    public AmmPlanQueryHandler(
        IAmmPlanRepo repository,
        IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<AmmPlanDTO> Handle(AmmPlanQuery request, CancellationToken cancellationToken)
    {
        return await _repository.Get(request.AP_PLAN_ID,cancellationToken);     
    }
}



 
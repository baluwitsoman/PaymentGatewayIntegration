using AutoMapper;
using AutoMapper.QueryableExtensions;
using ERPMultiTenent.Application.Common.Interfaces;
using MediatR;

namespace ERPMultiTenent.Application.Amm ;



public record AmmPlanQueryAll : IRequest<IEnumerable<AmmPlanDTO>>;
 
public class AmmPlanQueryAllHandler : IRequestHandler<AmmPlanQueryAll, IEnumerable<AmmPlanDTO>>
{
    private readonly IMapper _mapper;
    private readonly IAmmPlanRepo _repository;
    private readonly     ICurrentUserService _currentUserService;

    public AmmPlanQueryAllHandler(
        ICurrentUserService currentUserService,
        IAmmPlanRepo repository,
        IMapper mapper)
    {
        _repository = repository;
        _currentUserService = currentUserService;
        _mapper = mapper;
    }

    public async Task<IEnumerable<AmmPlanDTO>> Handle(AmmPlanQueryAll request, CancellationToken cancellationToken)
    {
        return await _repository.GetAll( cancellationToken);
        
    }
}



 
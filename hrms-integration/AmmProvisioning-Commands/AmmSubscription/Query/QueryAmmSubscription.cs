using MediatR;
using AutoMapper;

namespace ERPMultiTenent.Application.Amm ;


public record AmmSubscriptionQuery(Int32  AS_SUBS_ID) : IRequest<AmmSubscriptionDTO>; 
public class AmmSubscriptionQueryHandler : IRequestHandler<AmmSubscriptionQuery, AmmSubscriptionDTO>
{
    private readonly IMapper _mapper;
    private readonly IAmmSubscriptionRepo _repository;

    public AmmSubscriptionQueryHandler(
        IAmmSubscriptionRepo repository,
        IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<AmmSubscriptionDTO> Handle(AmmSubscriptionQuery request, CancellationToken cancellationToken)
    {
        return await _repository.Get(request.AS_SUBS_ID,cancellationToken);     
    }
}



 
using AutoMapper;
using ERPMultiTenent.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERPMultiTenent.Application.Amm ;



public record AmmSubscriptionQueryAll : IRequest<AmmSubscriptionDTOToDisp   >;
 
public class AmmSubscriptionQueryAllHandler : IRequestHandler<AmmSubscriptionQueryAll, AmmSubscriptionDTOToDisp>
{
    private readonly IMapper _mapper;
    private readonly IAmmSubscriptionRepo _repository;
    private readonly     ICurrentUserService _currentUserService;

    public AmmSubscriptionQueryAllHandler(
        ICurrentUserService currentUserService,
        IAmmSubscriptionRepo repository,
        IMapper mapper)
    {
        _repository = repository;
        _currentUserService = currentUserService;
        _mapper = mapper;
    }

    public async Task<AmmSubscriptionDTOToDisp> Handle(AmmSubscriptionQueryAll request, CancellationToken cancellationToken)
    {
        return await _repository.GetAll(_currentUserService.CompCode, cancellationToken);
        
    }
}



 
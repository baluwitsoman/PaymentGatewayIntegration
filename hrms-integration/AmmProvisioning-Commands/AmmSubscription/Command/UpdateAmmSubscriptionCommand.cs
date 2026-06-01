using MediatR;
using AutoMapper;
using ERPMultiTenent.Application.Common.Interfaces;
using ERPMultiTenent.Application.Common.Models;


 namespace ERPMultiTenent.Application.Amm ;
public record UpdateAmmSubscriptionCommand : IRequest<ResultNew<AmmSubscriptionDTO>>
{
    public AmmSubscriptionDTO Dto { get; set; }

}



public class UpdateAmmSubscriptionCommandHandler : IRequestHandler<UpdateAmmSubscriptionCommand, ResultNew<AmmSubscriptionDTO>>
{
    private readonly IAmmSubscriptionRepo _repository;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWorkAmm _unitOfWork;


    public UpdateAmmSubscriptionCommandHandler(
        IAmmSubscriptionRepo repository,
        IMapper mapper,
        IUnitOfWorkAmm unitOfWork,
        ICurrentUserService currentUser
    )
    {
        _repository = repository;
        _mapper = mapper;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }
    public async Task<ResultNew<AmmSubscriptionDTO>> Handle(UpdateAmmSubscriptionCommand request, CancellationToken cancellationToken)
    {

        request.Dto.AS_UPDATED_DATE = DateTime.Now;



        await _unitOfWork.BeginTransactionAsync();
        await _repository.Update(request.Dto.AS_SUBS_ID, request);
        await _unitOfWork.CommitAsync();

        return ResultNew<AmmSubscriptionDTO>.Success(request.Dto);

    }
}


 

 

 
using MediatR;
using AutoMapper;
using ERPMultiTenent.Application.Common.Interfaces;
using ERPMultiTenent.Application.Common.Models;

 namespace ERPMultiTenent.Application.Amm ;
 public record UpdateAmmPlanCommand : IRequest<ResultNew<AmmPlanDTO>>
   {   
       public AmmPlanDTO Dto {get;set;}

}



public class UpdateAmmPlanCommandHandler : IRequestHandler<UpdateAmmPlanCommand,  ResultNew<AmmPlanDTO>>
{
    private readonly IAmmPlanRepo _repository;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWorkAmm _unitOfWork;
 
    
    public UpdateAmmPlanCommandHandler(
        IAmmPlanRepo repository,
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
    public async Task<ResultNew<AmmPlanDTO>> Handle(UpdateAmmPlanCommand request, CancellationToken cancellationToken)
    {             

    request.Dto.AP_UPDATED_DATE = DateTime.Now;

 
         
        await _unitOfWork.BeginTransactionAsync();
        await _repository.Update(request.Dto.AP_PLAN_ID, request);
        await _unitOfWork.CommitAsync();

        return ResultNew<AmmPlanDTO>.Success(request.Dto);

    }
}


 

 

 
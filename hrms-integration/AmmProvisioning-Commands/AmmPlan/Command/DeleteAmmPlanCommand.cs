using MediatR;
using AutoMapper;
using ERPMultiTenent.Application.Common.Interfaces;
using ERPMultiTenent.Application.Common.Models;

namespace ERPMultiTenent.Application.Amm ;


public record DeleteAmmPlanCommand : IRequest<ResultNew<string>> 
{
    public Int32 AP_PLAN_ID  { get; set; }
       
}


public class DeleteAmmPlanCommandHandler : IRequestHandler<DeleteAmmPlanCommand, ResultNew<string>>
{
    private readonly IAmmPlanRepo _repository;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWorkAmm _unitOfWork;
 
    
    public DeleteAmmPlanCommandHandler(
        IAmmPlanRepo repo,
        IMapper mapper,
        IUnitOfWorkAmm unitOfWork,
        ICurrentUserService currentUser   
    )
    {
        _repository = repo;
        _mapper = mapper;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    public async Task<ResultNew<string>> Handle(DeleteAmmPlanCommand request, CancellationToken cancellationToken)
    {
        
        await _unitOfWork.BeginTransactionAsync();
        await _repository.Delete(request.AP_PLAN_ID);
        await _unitOfWork.CommitAsync();
        return ResultNew<string>.Success("Deleted Successfully");
    
    }
}


 

 

 
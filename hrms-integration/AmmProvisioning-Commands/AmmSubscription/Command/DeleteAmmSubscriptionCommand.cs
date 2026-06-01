 



using MediatR;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using ERPMultiTenent.Domain.Entities.Amm.Provisioning;
using ERPMultiTenent.Application.Common.Exceptions;

using ERPMultiTenent.Domain.Entities.PPM;
using ERPMultiTenent.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using ERPMultiTenent.Application.Common.Models;

namespace ERPMultiTenent.Application.Amm ;


public record DeleteAmmSubscriptionCommand : IRequest<ResultNew<string>> 
{
    public Int32 AS_SUBS_ID  { get; set; }
       
}


public class DeleteAmmSubscriptionCommandHandler : IRequestHandler<DeleteAmmSubscriptionCommand, ResultNew<string>>
{
    private readonly IAmmSubscriptionRepo _repository;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWorkAmm _unitOfWork;
 
    
    public DeleteAmmSubscriptionCommandHandler(
        IAmmSubscriptionRepo repo,
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

    public async Task<ResultNew<string>> Handle(DeleteAmmSubscriptionCommand request, CancellationToken cancellationToken)
    {
        
        await _unitOfWork.BeginTransactionAsync();
        await _repository.Delete(request.AS_SUBS_ID);
        await _unitOfWork.CommitAsync();
        return ResultNew<string>.Success("Deleted Successfully");
    
    }
}


 

 

 
 



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


public record DeleteAmmUserRegisterCommand : IRequest<ResultNew<string>> 
{
    public String AUR_EMAIL  { get; set; }
       
}


public class DeleteAmmUserRegisterCommandHandler : IRequestHandler<DeleteAmmUserRegisterCommand, ResultNew<string>>
{
    private readonly IAmmUserRegisterRepo _repository;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWorkAmm _unitOfWork;
 
    
    public DeleteAmmUserRegisterCommandHandler(
        IAmmUserRegisterRepo repo,
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

    public async Task<ResultNew<string>> Handle(DeleteAmmUserRegisterCommand request, CancellationToken cancellationToken)
    {
        
        await _unitOfWork.BeginTransactionAsync();
        await _repository.Delete(request.AUR_EMAIL);
        await _unitOfWork.CommitAsync();
        return ResultNew<string>.Success("Deleted Successfully");
    
    }
}


 

 

 
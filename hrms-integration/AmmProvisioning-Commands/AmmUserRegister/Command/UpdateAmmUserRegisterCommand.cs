


using MediatR;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using ERPMultiTenent.Domain.Entities.Amm.Provisioning;
using ERPMultiTenent.Application.Common.Interfaces;
using ERPMultiTenent.Application.Common.Mappings;
using ERPMultiTenent.Application.Common.Exceptions;
using Microsoft.EntityFrameworkCore;
 using ERPMultiTenent.Application.Common.Models;


 namespace ERPMultiTenent.Application.Amm ;
 public record UpdateAmmUserRegisterCommand : IRequest<ResultNew<AmmUserRegisterDTO>>
   {   
       public AmmUserRegisterDTO Dto {get;set;}

}



public class UpdateAmmUserRegisterCommandHandler : IRequestHandler<UpdateAmmUserRegisterCommand,  ResultNew<AmmUserRegisterDTO>>
{
    private readonly IAmmUserRegisterRepo _repository;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWorkAmm _unitOfWork;
 
    
    public UpdateAmmUserRegisterCommandHandler(
        IAmmUserRegisterRepo repository,
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
    public async Task<ResultNew<AmmUserRegisterDTO>> Handle(UpdateAmmUserRegisterCommand request, CancellationToken cancellationToken)
    {             

    request.Dto.AUR_UPDATED_DATE = DateTime.Now;

 
         
        await _unitOfWork.BeginTransactionAsync();
        await _repository.Update(request.Dto.AUR_EMAIL, request);
        await _unitOfWork.CommitAsync();

        return ResultNew<AmmUserRegisterDTO>.Success(request.Dto);

    }
}


 

 

 
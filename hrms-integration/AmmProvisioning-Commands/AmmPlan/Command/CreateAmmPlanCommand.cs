
using MediatR;
using AutoMapper;
using FluentValidation;
using ERPMultiTenent.Application.Common.Interfaces;
using ERPMultiTenent.Application.Common.Models;


namespace ERPMultiTenent.Application.Amm ;



public record CreateAmmPlanCommand : IRequest<ResultNew<AmmPlanDTO>> 
{    
    public AmmPlanDTO Dto {get;set;}
}

public class CreateAmmPlanCommandValidator : AbstractValidator<CreateAmmPlanCommand>
{
    public CreateAmmPlanCommandValidator()
    {   
    }
}

public class CreateAmmPlanCommandHandler : IRequestHandler<CreateAmmPlanCommand, ResultNew<AmmPlanDTO>>
{
    private readonly IAmmPlanRepo _repository;

    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWorkAmm _unitOfWork;
    
    

    public CreateAmmPlanCommandHandler(
        IAmmPlanRepo repo,IMapper mapper,
        IUnitOfWorkAmm unitOfWork,
        ICurrentUserService currentUser   
    )
    {
        _repository = repo;
        _mapper = mapper;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    public async Task<ResultNew<AmmPlanDTO>> Handle(CreateAmmPlanCommand command, CancellationToken cancellationToken)
    {
 

        await _unitOfWork.BeginTransactionAsync();
        await _repository.Add(command);
        await _unitOfWork.CommitAsync();
        return ResultNew<AmmPlanDTO>.Success(command.Dto);
 
    }
}

 
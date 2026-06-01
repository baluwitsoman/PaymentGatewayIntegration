
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using ERPMultiTenent.Application.Common.Interfaces;
using ERPMultiTenent.Application.Common.Mappings;
using ERPMultiTenent.Domain.Entities;
using ERPMultiTenent.Domain.Entities.PPM;
using ERPMultiTenent.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERPMultiTenent.Application.Amm ;


public record AmmUserRegisterQuery(String  AUR_EMAIL) : IRequest<AmmUserRegisterDTO>; 
public class AmmUserRegisterQueryHandler : IRequestHandler<AmmUserRegisterQuery, AmmUserRegisterDTO>
{
    private readonly IMapper _mapper;
    private readonly IAmmUserRegisterRepo _repository;

    public AmmUserRegisterQueryHandler(
        IAmmUserRegisterRepo repository,
        IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<AmmUserRegisterDTO> Handle(AmmUserRegisterQuery request, CancellationToken cancellationToken)
    {


        return await _repository.Get(request.AUR_EMAIL,cancellationToken);     
    }
}



 
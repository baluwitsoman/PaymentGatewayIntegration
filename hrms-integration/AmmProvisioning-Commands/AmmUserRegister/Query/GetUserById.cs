
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using ERPMultiTenent.Application.Common.Exceptions;
using ERPMultiTenent.Application.Common.Interfaces;
using ERPMultiTenent.Application.Common.Mappings;
using ERPMultiTenent.Common.CrossCutting.Exceptions;
using ERPMultiTenent.Domain.Entities;
using ERPMultiTenent.Domain.Entities.Amm;
using ERPMultiTenent.Domain.Entities.Amm.Provisioning;
using ERPMultiTenent.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERPMultiTenent.Application.Amm ;

public record GetUserById(int userId) : IRequest<UserRoleDTO>;

public class GetUserByEmailAddressHandler : IRequestHandler<GetUserById, UserRoleDTO>
{
    private readonly IAmmDbContext _context;
    private readonly IAmmCompanyDetailRepo _ammCompanyDetailRepo;
    private readonly IMapper _mapper;
    private readonly IUserRoleLinkRepo _userRoleLinkRepo;
    private readonly ICurrentUserService _currentUserService;

    public GetUserByEmailAddressHandler(IAmmDbContext context,
        IAmmCompanyDetailRepo ammCompanyDetailRepo,  
        IMapper mapper, IUserRoleLinkRepo userRoleLinkRepo, ICurrentUserService currentUserService)
    {
        _context = context;
        _ammCompanyDetailRepo = ammCompanyDetailRepo;
        _mapper = mapper;
        _userRoleLinkRepo = userRoleLinkRepo;
        _currentUserService = currentUserService;
    }

    public async Task<UserRoleDTO> Handle(GetUserById request, CancellationToken cancellationToken)
    {
        var dto = new UserRoleDTO { };
        dto.UserDetailDTO = await _context.AMM_USER_DETAILS.Where(w => w.USER_ID == request.userId                
                ).ProjectTo<UserDetailDTO>(_mapper.ConfigurationProvider)

                .FirstOrDefaultAsync(cancellationToken);
        if(dto.UserDetailDTO?.USER_ID == null)
        {
            //throw new NotFoundException(nameof(AMM_COMPANY_DETAILS), $"{request.userName} is not found.");
            throw new NotFoundException("User not found", $"{request.userId} is not found.");
        }

        var comp = (from c in
                        _context.AMM_COMPANY_DETAILS
                    join sub in _context.AMM_SUBSCRIPTIONS on c.ACD_SUBSCRIPTION_ID equals sub.AS_SUBS_ID into subGroup
                    from sub in subGroup.DefaultIfEmpty() // Left join
                    where c.ACD_COMP_CODE == dto.UserDetailDTO.AUD_COMP_CODE
                    select new { c.ACD_SUBSCRIPTION_REQ_YN, sub.AS_STATUS }).AsNoTracking().FirstOrDefault();

        if (comp != null)
        {
            dto.SubscriptionRequiredYN = comp.ACD_SUBSCRIPTION_REQ_YN;
            dto.SubscriptionStatus = comp.AS_STATUS;
        }

        return dto;
    }
}



 
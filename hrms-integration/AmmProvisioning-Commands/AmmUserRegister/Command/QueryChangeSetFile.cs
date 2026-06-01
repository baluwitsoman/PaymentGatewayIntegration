using System;
using System.Collections.Generic;
using System.Text;
using AutoMapper;
using ERPMultiTenent.Application.Common.Interfaces;
using ERPMultiTenent.Application.Common.Models;
using ERPMultiTenent.Application.PPM;
using MediatR;

namespace ERPMultiTenent.Application.ERP.AmmProvisioning.AmmUserRegister.Command;

 
public class ChangeSetFileEventDTO
{
    public decimal PCEFH_HISTORY_ID { get; set; }
    public string? PCEFH_CHANGE_SET_ID { get; set; }

    /// INSERT / SOFT_DELETE / RESTORE / UPDATE / MIGRATED / DELETE
    public string? PCEFH_OPERATION { get; set; }
    public DateTime? PCEFH_OPERATION_DATE { get; set; }
    public decimal? PCEFH_OPERATION_USER { get; set; }
    public string? OPERATION_USER_NAME { get; set; }

    public decimal? PCED_PK { get; set; }
    public decimal? PCED_ROW_ID { get; set; }    // doc srl no
    public string? PCED_TAG { get; set; }    // EMP_DOC / COMP_DOC

    public string? PCED_FILE_NAME { get; set; }
    public string? PCED_FILE_PATH { get; set; }    // for download link
    public decimal? PCED_FILE_SIZE_BYTES { get; set; }

    public string? PCED_IS_DELETED { get; set; }    // 'Y' if soft-deleted
}


public record QueryChangeSetFile(string ChangeSetId) : IRequest<IEnumerable<ChangeSetFileEventDTO>>;



public class QueryChangeSetFileHandler : IRequestHandler<QueryChangeSetFile, IEnumerable<ChangeSetFileEventDTO>>
{
    private readonly IEmpDocumentRepo _repository;

    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWorkPPM _unitOfWork;



    public QueryChangeSetFileHandler(
        
        IEmpDocumentRepo repo, IMapper mapper,
        IUnitOfWorkPPM unitOfWork,
        ICurrentUserService currentUser
    )
    {
        _repository = repo;
        _mapper = mapper;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<ChangeSetFileEventDTO>> Handle(QueryChangeSetFile command, CancellationToken cancellationToken)
    {

        return await _repository.GetChangeSetFileEvents(command.ChangeSetId, cancellationToken);
         

    }
}


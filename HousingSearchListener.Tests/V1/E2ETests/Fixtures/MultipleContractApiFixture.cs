using System;
using System.Collections.Generic;
using System.Linq;
using AutoFixture;
using Hackney.Core.DynamoDb;
using Hackney.Core.Testing.Shared.E2E;
using Hackney.Shared.HousingSearch.Domain.Contract;

namespace HousingSearchListener.Tests.V1.E2ETests.Fixtures;

public class MultipleContractApiFixture : BaseApiFixture<IEnumerable<Contract>>
{
    private readonly Fixture _fixture = new();

    public MultipleContractApiFixture()
        : base(FixtureConstants.ContractsApiRoute, FixtureConstants.ContractsApiToken)
    {
        Environment.SetEnvironmentVariable("ContractApiUrl", FixtureConstants.ContractsApiRoute);
        Environment.SetEnvironmentVariable("ContractApiToken", FixtureConstants.ContractsApiToken);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && !_disposed) base.Dispose(disposing);
    }


    public PagedResult<Contract> GivenMultipleContractsAreReturned(Guid contractId, Guid targetId)
    {
        var ResponseObject = _fixture.Build<Contract>()
            .With(x => x.Id, contractId.ToString())
            .With(x => x.TargetId, targetId.ToString())
            .With(x => x.TargetType, "asset")
            .CreateMany(1).ToList();
        return new PagedResult<Contract> { Results = ResponseObject };
    }

    public PagedResult<Contract> GivenApprovedContractsAreReturned(Guid contractId, Guid targetId)
    {
        var ResponseObject = _fixture.Build<Contract>()
            .With(x => x.Id, contractId.ToString())
            .With(x => x.TargetId, targetId.ToString())
            .With(x => x.TargetType, "asset")
            .With(x => x.ApprovalStatus, "Approved")
            .CreateMany(1).ToList();
        return new PagedResult<Contract> { Results = ResponseObject };
    }
}
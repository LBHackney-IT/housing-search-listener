using Hackney.Core.DynamoDb;
using Hackney.Shared.HousingSearch.Domain.Contract;
using Hackney.Shared.HousingSearch.Gateways.Models.Assets;
using Hackney.Shared.HousingSearch.Gateways.Models.Contract;
using Hackney.Shared.HousingSearch.Gateways.Models.Persons;
using Hackney.Shared.HousingSearch.Gateways.Models.Tenures;
using Hackney.Shared.HousingSearch.Gateways.Models.Transactions;
using HousingSearchListener.V1.Domain.Tenure;
using HousingSearchListener.V1.Domain.Transaction;
using System.Collections.Generic;
using Person = HousingSearchListener.V1.Domain.Person.Person;

namespace HousingSearchListener.V1.Factories
{
    public interface IESEntityFactory
    {
        QueryablePerson CreatePerson(Person person);
        QueryableTenure CreateQueryableTenure(TenureInformation tenure);
        List<QueryableHouseholdMember> CreateQueryableHouseholdMembers(List<HouseholdMembers> householdMembers);
        QueryableAssetTenure CreateAssetQueryableTenure(TenureInformation tenure);
        QueryableTransaction CreateQueryableTransaction(TransactionResponseObject transaction);
        QueryableAsset CreateAsset(QueryableAsset asset);
        List<QueryableAssetContract> CreateAssetContracts(PagedResult<Contract> allContracts);
    }
}

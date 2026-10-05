using HousingSearchListener.V1.Factories;
using HousingSearchListener.V1.Infrastructure.Exceptions;
using HousingSearchListener.V1.UseCase.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Hackney.Core.Logging;
using Hackney.Core.Sns;
using HousingSearchListener.V1.Gateway.Interfaces;
using Hackney.Shared.HousingSearch.Gateways.Models.Assets;
using Hackney.Shared.HousingSearch.Gateways.Models.Contract;
using Hackney.Shared.HousingSearch.Domain.Contract;
using Hackney.Core.DynamoDb;
using Microsoft.Extensions.Logging;

namespace HousingSearchListener.V1.UseCase
{
    public class UpdateAssetUseCase : IUpdateAssetUseCase
    {
        private readonly ILogger<UpdateAssetUseCase> _logger;
        private readonly IEsGateway _esGateway;
        private readonly IAssetApiGateway _assetApiGateway;
        private readonly IContractApiGateway _contractApiGateway;
        private readonly IESEntityFactory _esAssetFactory;

        public UpdateAssetUseCase(IEsGateway esGateway, IAssetApiGateway assetApiGateway,
        IContractApiGateway contractApiGateway, IESEntityFactory esAssetFactory,
        ILogger<UpdateAssetUseCase> logger
        )
        {
            _esGateway = esGateway;
            _assetApiGateway = assetApiGateway;
            _contractApiGateway = contractApiGateway;
            _esAssetFactory = esAssetFactory;
            _logger = logger;
        }

        [LogCall]

        public async Task ProcessMessageAsync(EntityEventSns message)
        {
            ArgumentNullException.ThrowIfNull(message);

            // 1. Get Asset from Asset service API
            var asset = await _assetApiGateway
                .GetAssetByIdAsync(message.EntityId, message.CorrelationId)
                .ConfigureAwait(false) ?? throw new EntityNotFoundException<QueryableAsset>(message.EntityId);

            // 2 & 3. Only fetch contracts for BTA (Book Temporary Accommodation) properties
            if (asset.AssetManagement?.IsTemporaryAccomodation)
            {
                var allContracts = await _contractApiGateway
                    .GetContractsByAssetIdAsync(message.EntityId, message.CorrelationId)
                    .ConfigureAwait(false);

                asset.AssetContracts = MapContracts(allContracts);
            }

            // 4. Update the index
            await UpdateAssetIndexAsync(asset);
        }

        private List<QueryableAssetContract> MapContracts(PagedResult<Contract> allContracts)
        {
            var allFilteredContracts = allContracts.Results.Where(x => x?.EndReason != "ContractNoLongerNeeded");

            var assetContracts = new List<QueryableAssetContract>();
            foreach (var assetContract in allFilteredContracts)
            {
                _logger.LogInformation("Contract with id {AssetId} being added to asset", assetContract.Id);
                var queryableAssetContract = new QueryableAssetContract
                {
                    Id = assetContract.Id,
                    TargetId = assetContract.TargetId,
                    TargetType = assetContract.TargetType,
                    EndDate = assetContract.EndDate,
                    EndReason = assetContract.EndReason,
                    ApprovalStatus = assetContract.ApprovalStatus,
                    ApprovalStatusReason = assetContract.ApprovalStatusReason,
                    IsActive = assetContract.IsActive,
                    ApprovalDate = assetContract.ApprovalDate,
                    StartDate = assetContract.StartDate,
                    Charges = MapCharges(assetContract),
                    RelatedPeople = MapRelatedPeople(assetContract)
                };
                assetContracts.Add(queryableAssetContract);
            }

            return assetContracts;
        }

        private List<QueryableCharges> MapCharges(Contract assetContract)
        {
            if (!assetContract.Charges.Any()) return null;

            _logger.LogInformation("{AssetChargesCount} charges found.", assetContract.Charges.Count());
            var charges = new List<QueryableCharges>();
            foreach (var charge in assetContract.Charges)
            {
                _logger.LogInformation("Charge with id {ChargeId} being added to asset with frequency {ChargeFrequency}", charge.Id, charge.Frequency);
                charges.Add(new QueryableCharges
                {
                    Id = charge.Id,
                    Type = charge.Type,
                    SubType = charge.SubType,
                    Frequency = charge.Frequency,
                    Amount = charge.Amount
                });
            }
            return charges;
        }

        private List<QueryableRelatedPeople> MapRelatedPeople(Contract assetContract)
        {
            if (!assetContract.RelatedPeople.Any()) return null;

            _logger.LogInformation("{RelatedPeopleCount} related people found.", assetContract.RelatedPeople.Count());
            var relatedPeople = new List<QueryableRelatedPeople>();
            foreach (var relatedPerson in assetContract.RelatedPeople)
            {
                _logger.LogInformation("Related person with id {RelatedPersonId} being added to asset", relatedPerson.Id);
                relatedPeople.Add(new QueryableRelatedPeople
                {
                    Id = relatedPerson.Id,
                    Type = relatedPerson.Type,
                    SubType = relatedPerson.SubType,
                    Name = relatedPerson.Name,
                });
            }
            return relatedPeople;
        }

        private async Task UpdateAssetIndexAsync(QueryableAsset asset)
        {
            var esAsset = await _esGateway.GetAssetById(asset.Id.ToString()).ConfigureAwait(false);
            var assetId = asset.Id;
            if (esAsset is null)
                throw new ArgumentException("No asset found in index with id: {AssetId}", assetId);
            esAsset = _esAssetFactory.CreateAsset(asset);
            await _esGateway.IndexAsset(esAsset);
        }
    }
}

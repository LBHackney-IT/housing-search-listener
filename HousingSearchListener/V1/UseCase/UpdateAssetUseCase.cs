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

            // 2. Get all the contracts for the asset 
            var allContracts = await _contractApiGateway
                .GetContractsByAssetIdAsync(message.EntityId, message.CorrelationId)
                .ConfigureAwait(false)
                ?? throw new EntityNotFoundException<List<Hackney.Shared.HousingSearch.Domain.Contract.Contract>>(message.EntityId);

            var allFilteredContracts = allContracts.Results.Where(x => x?.EndReason != "ContractNoLongerNeeded");

            // 3. Return the contract in list 
            var assetContracts = new List<QueryableAssetContract>();
            foreach (var assetContract in allFilteredContracts)
            {
                var assetId = assetContract.Id;
                _logger.LogInformation("Contract with id {assetId} being added to asset", assetId);
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
                    StartDate = assetContract.StartDate
                };

                if (assetContract.Charges.Any())
                {
                    var assetChargesCount = assetContract.Charges.Count();
                    _logger.LogInformation("{AssetChargesCount} charges found.", assetChargesCount);
                    var charges = new List<QueryableCharges>();

                    foreach (var charge in assetContract.Charges)
                    {
                        var chargeId = charge.Id;
                        var chargeFrequency = charge.Frequency;
                        _logger.LogInformation("Charge with id {ChargeId} being added to asset with frequency {ChargeFrequency}", chargeId, chargeFrequency);
                        var queryableCharge = new QueryableCharges
                        {
                            Id = charge.Id,
                            Type = charge.Type,
                            SubType = charge.SubType,
                            Frequency = charge.Frequency,
                            Amount = charge.Amount
                        };
                        charges.Add(queryableCharge);
                    }

                    queryableAssetContract.Charges = charges;
                }

                if (assetContract.RelatedPeople.Any())
                {
                    var relatedPeopleCount = assetContract.RelatedPeople.Count();
                    _logger.LogInformation("{RelatedPeopleCount} related people found.", relatedPeopleCount);
                    var relatedPeople = new List<QueryableRelatedPeople>();

                    foreach (var relatedPerson in assetContract.RelatedPeople)
                    {
                        var relatedPersonId = relatedPerson.Id;
                        _logger.LogInformation("Related person with id {relatedPersonId} being added to asset", relatedPersonId);
                        var queryableRelatedPeople = new QueryableRelatedPeople
                        {
                            Id = relatedPerson.Id,
                            Type = relatedPerson.Type,
                            SubType = relatedPerson.SubType,
                            Name = relatedPerson.Name,
                        };
                        relatedPeople.Add(queryableRelatedPeople);
                    }

                    queryableAssetContract.RelatedPeople = relatedPeople;
                }
                assetContracts.Add(queryableAssetContract);
            }

            asset.AssetContracts = assetContracts;

            // 4. Update the index
            await UpdateAssetIndexAsync(asset);
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

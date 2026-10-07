using HousingSearchListener.V1.Factories;
using HousingSearchListener.V1.Infrastructure.Exceptions;
using HousingSearchListener.V1.UseCase.Interfaces;
using System;
using System.Threading.Tasks;
using Hackney.Core.Logging;
using Hackney.Core.Sns;
using HousingSearchListener.V1.Gateway.Interfaces;
using Hackney.Shared.HousingSearch.Gateways.Models.Assets;
using Hackney.Core.DynamoDb;

namespace HousingSearchListener.V1.UseCase
{
    public class UpdateAssetUseCase : IUpdateAssetUseCase
    {
        private readonly IEsGateway _esGateway;
        private readonly IAssetApiGateway _assetApiGateway;
        private readonly IContractApiGateway _contractApiGateway;
        private readonly IESEntityFactory _esAssetFactory;

        public UpdateAssetUseCase(IEsGateway esGateway, IAssetApiGateway assetApiGateway,
            IContractApiGateway contractApiGateway, IESEntityFactory esAssetFactory)
        {
            _esGateway = esGateway;
            _assetApiGateway = assetApiGateway;
            _contractApiGateway = contractApiGateway;
            _esAssetFactory = esAssetFactory;
        }

        [LogCall]

        public async Task ProcessMessageAsync(EntityEventSns message)
        {
            ArgumentNullException.ThrowIfNull(message);

            // 1. Get Asset from Asset service API
            var asset = await _assetApiGateway
                .GetAssetByIdAsync(message.EntityId, message.CorrelationId)
                .ConfigureAwait(false) ?? throw new EntityNotFoundException<QueryableAsset>(message.EntityId);

            // 2 & 3. Only fetch contracts for TA (Temporary Accommodation) properties
            var isTemporaryAccommodation = asset.AssetManagement?.IsTemporaryAccomodation ?? false;
            if (isTemporaryAccommodation)
            {
                var allContracts = await _contractApiGateway
                    .GetContractsByAssetIdAsync(message.EntityId, message.CorrelationId)
                    .ConfigureAwait(false);

                asset.AssetContracts = _esAssetFactory.CreateAssetContracts(allContracts);
            }

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

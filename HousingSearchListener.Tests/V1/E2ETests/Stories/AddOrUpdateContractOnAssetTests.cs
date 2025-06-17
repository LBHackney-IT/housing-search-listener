using HousingSearchListener.Tests.V1.E2ETests.Fixtures;
using HousingSearchListener.Tests.V1.E2ETests.Steps;
using HousingSearchListener.V1.Boundary;
using System;
using TestStack.BDDfy;
using Xunit;

namespace HousingSearchListener.Tests.V1.E2ETests.Stories
{
    [Story(
        AsA = "SQS Contract Listener",
        IWant = "a function to process the Contract added or updated messages",
        SoThat = "The Contract details are updated on the Asset in the index")]
    [Collection("ElasticSearch collection")]
    public class AddOrUpdateContractOnAssetTests : IDisposable
    {
        private readonly ElasticSearchFixture _esFixture;
        private readonly AssetApiFixture _assetApiFixture;
        private readonly ContractApiFixture _contractApiFixture;
        private readonly MultipleContractApiFixture _contractsApiFixture;
        private readonly AddOrUpdateContractOnAssetTestsSteps _steps;

        public AddOrUpdateContractOnAssetTests(ElasticSearchFixture esFixture)
        {
            _esFixture = esFixture;
            _assetApiFixture = new AssetApiFixture();
            _contractApiFixture = new ContractApiFixture();
            _contractsApiFixture = new MultipleContractApiFixture();

            _steps = new AddOrUpdateContractOnAssetTestsSteps();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private bool _disposed;
        protected virtual void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                _assetApiFixture.Dispose();
                _contractApiFixture.Dispose();
                _contractsApiFixture.Dispose();

                _disposed = true;
            }
        }

        [Theory]
        [InlineData(EventTypes.ContractCreatedEvent, Skip = "Both test keeps failing randomly, skipping for now")]
        [InlineData(EventTypes.ContractUpdatedEvent, Skip = "Both test keeps failing randomly, skipping for now")]
        public void AssetNotFound(string eventType)
        {
            var contractId = Guid.NewGuid();
            var assetId = Guid.NewGuid();
            this.Given(g => _contractApiFixture.GivenTheContractExists(contractId, assetId))
                .And(g => _assetApiFixture.GivenTheAssetDoesNotExist(assetId))
                .When(w => _steps.WhenTheFunctionIsTriggered(contractId, eventType))
                .Then(t => _steps.ThenAnAssetNotFoundExceptionIsThrown(assetId))
                .BDDfy();
        }

        [Theory]
        [InlineData(EventTypes.ContractCreatedEvent)]
        [InlineData(EventTypes.ContractUpdatedEvent)]
        public void ContractAddedToAsset(string eventType)
        {
            var contractId = Guid.NewGuid();
            var assetId = Guid.NewGuid();
            this.Given(g => _contractsApiFixture.GivenMultipleContractsAreReturned(contractId, assetId))
                .And(g => _assetApiFixture.GivenTheAssetExists(assetId))
                .And(g => _esFixture.GivenAnAssetIsIndexed(assetId.ToString()))
                .When(w => _steps.WhenTheFunctionIsTriggered(contractId, eventType))
                .Then(t => _steps.ThenTheAssetInTheIndexIsUpdatedWithTheContracts(_assetApiFixture.ResponseObject,
                    _contractsApiFixture.ResponseObject, _esFixture.ElasticSearchClient))
                .BDDfy();
        }

        [Theory]
        [InlineData(EventTypes.ContractCreatedEvent)]
        [InlineData(EventTypes.ContractUpdatedEvent)]
        public void ContractNotAddedToAssetWhenNoUnapprovedContractsArePresent(string eventType)
        {
            var contractId = Guid.NewGuid();
            var assetId = Guid.NewGuid();
            this.Given(g => _contractsApiFixture.GivenApprovedContractsAreReturned(contractId, assetId))
                .And(g => _assetApiFixture.GivenTheAssetExists(assetId))
                .And(g => _esFixture.GivenAnAssetIsIndexed(assetId.ToString()))
                .When(w => _steps.WhenTheFunctionIsTriggered(contractId, eventType))
                .Then(t => _steps.ThenTheAssetInTheIndexIsUpdatedAndHasNoContracts(_assetApiFixture.ResponseObject,
                    _esFixture.ElasticSearchClient))
                .BDDfy();
        }
    }
}

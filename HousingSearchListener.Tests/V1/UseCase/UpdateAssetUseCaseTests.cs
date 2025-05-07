using AutoFixture;
using FluentAssertions;
using Hackney.Core.Sns;
using Hackney.Core.DynamoDb;
using Hackney.Shared.HousingSearch.Gateways.Models.Assets;
using Hackney.Shared.HousingSearch.Gateways.Models.Contract;
using Hackney.Shared.HousingSearch.Domain.Contract;
using HousingSearchListener.V1.Factories;
using HousingSearchListener.V1.Gateway.Interfaces;
using HousingSearchListener.V1.Infrastructure.Exceptions;
using HousingSearchListener.V1.UseCase;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using EventTypes = HousingSearchListener.V1.Boundary.EventTypes;
using Microsoft.Extensions.Logging;


namespace HousingSearchListener.Tests.V1.UseCase
{
    [Collection("LogCall collection")]
    public class UpdateAssetUseCaseTests
    {
        private readonly Mock<IAssetApiGateway> _mockAssetApi;
        private readonly Mock<IEsGateway> _mockEsGateway;
        private readonly IESEntityFactory _esEntityFactory;
        private readonly IndexCreateAssetUseCase _create;
        private readonly UpdateAssetUseCase _sut;

        private readonly EntityEventSns _message;
        private readonly QueryableAsset _asset;

        private readonly Fixture _fixture;

        private readonly Mock<IContractApiGateway> _mockContractApi;

        private readonly Mock<ILogger<UpdateAssetUseCase>> _mockLogger;

        private static readonly Guid _correlationId = Guid.NewGuid();



        public UpdateAssetUseCaseTests()
        {
            _fixture = new Fixture();

            _mockAssetApi = new Mock<IAssetApiGateway>();
            _mockEsGateway = new Mock<IEsGateway>();
            _mockContractApi = new Mock<IContractApiGateway>();
            _mockLogger = new Mock<ILogger<UpdateAssetUseCase>>();
            _esEntityFactory = new ESEntityFactory();
            _create = new IndexCreateAssetUseCase(_mockEsGateway.Object,
                _mockAssetApi.Object, _esEntityFactory);

            _sut = new UpdateAssetUseCase(_mockEsGateway.Object,
                _mockAssetApi.Object, _mockContractApi.Object, _esEntityFactory, _mockLogger.Object);

            _message = CreateMessage();
            _asset = CreateAsset(_message.EntityId);
        }

        private EntityEventSns CreateMessage(string eventType = EventTypes.AssetUpdatedEvent)
        {
            return _fixture.Build<EntityEventSns>()
                           .With(x => x.EventType, eventType)
                           .With(x => x.CorrelationId, _correlationId)
                           .Create();
        }

        private QueryableAsset CreateAsset(Guid entityId)
        {

            var charges = _fixture.Build<QueryableCharges>()
                .With(ch => ch.Frequency, "1")
                .CreateMany(1).ToList();

            var contracts = _fixture.Build<QueryableAssetContract>()
                        .With(c => c.TargetId, entityId.ToString())
                        .With(c => c.TargetType, "asset")
                        .With(c => c.Charges, charges)
                        .CreateMany(1)
                        .ToList();

            return _fixture.Build<QueryableAsset>()
                        .With(x => x.Id, entityId.ToString())
                        .With(x => x.AssetContracts, contracts)
                        .Create();
        }

        private PagedResult<Contract> CreateContracts(int contractCount = 1, int chargesCount = 2, int relatedPeopleCount = 1)
        {
            var contractList = new List<Contract>();

            for (int i = 0; i < contractCount; i++)
            {
                contractList.Add(_fixture.Build<Contract>()
                    .With(x => x.Id, Guid.NewGuid().ToString())
                    .With(x => x.EndReason, (string)null)
                    .With(x => x.Charges, _fixture.CreateMany<QueryableCharges>(chargesCount).ToList())
                    .With(x => x.RelatedPeople, _fixture.CreateMany<QueryableRelatedPeople>(relatedPeopleCount).ToList())
                    .Create());
            }

            return new PagedResult<Contract>
            {
                Results = contractList
            };
        }

        private bool VerifyAssetIndexed(QueryableAsset esAsset)
        {
            esAsset.Should().BeEquivalentTo(_esEntityFactory.CreateAsset(_asset));
            return true;
        }

        [Fact]
        public void ProcessMessageAsyncTestNullMessageThrows()
        {
            Func<Task> func = async () => await _sut.ProcessMessageAsync(null).ConfigureAwait(false);
            func.Should().ThrowAsync<ArgumentNullException>();
        }

        [Fact]
        public void ProcessMessageAsyncTestGetAssetExceptionThrown()
        {
            var exMsg = "This is an error";
            _mockAssetApi.Setup(x => x.GetAssetByIdAsync(_message.EntityId, _message.CorrelationId))
                                       .ThrowsAsync(new Exception(exMsg));

            Func<Task> func = async () => await _sut.ProcessMessageAsync(_message).ConfigureAwait(false);
            func.Should().ThrowAsync<Exception>().WithMessage(exMsg);
        }

        [Fact]
        public void ProcessMessageAsyncTestGetAssetReturnsNullThrows()
        {
            _mockAssetApi.Setup(x => x.GetAssetByIdAsync(_message.EntityId, _message.CorrelationId))
                                       .ReturnsAsync((QueryableAsset)null);

            Func<Task> func = async () => await _sut.ProcessMessageAsync(_message).ConfigureAwait(false);
            func.Should().ThrowAsync<EntityNotFoundException<QueryableAsset>>();
        }

        [Fact]
        public void ProcessMessageAsyncTestIndexAssetExceptionThrows()
        {
            var exMsg = "This is the last error";
            _mockAssetApi.Setup(x => x.GetAssetByIdAsync(_message.EntityId, _message.CorrelationId))
                                       .ReturnsAsync(_asset);
            _mockEsGateway.Setup(x => x.IndexAsset(It.IsAny<QueryableAsset>()))
                          .ThrowsAsync(new Exception(exMsg));

            Func<Task> func = async () => await _sut.ProcessMessageAsync(_message).ConfigureAwait(false);
            func.Should().ThrowAsync<Exception>().WithMessage(exMsg);
        }

        [Theory]
        [InlineData(EventTypes.AssetUpdatedEvent)]
        public async Task ProcessMessageAsyncTestIndexAssetSuccess(string eventType)
        {
            _message.EventType = eventType;

            var contracts = CreateContracts();

            _mockAssetApi.Setup(x => x.GetAssetByIdAsync(_message.EntityId, _message.CorrelationId))
                .ReturnsAsync(_asset);
            _mockContractApi.Setup(x => x.GetContractsByAssetIdAsync(_message.EntityId, _message.CorrelationId))
                .ReturnsAsync(contracts);
            _mockEsGateway.Setup(x => x.GetAssetById(_message.EntityId.ToString()))
                .ReturnsAsync(_asset);

            await _sut.ProcessMessageAsync(_message).ConfigureAwait(false);

            _mockEsGateway.Verify(x => x.IndexAsset(It.Is<QueryableAsset>(y => VerifyAssetIndexed(y))), Times.Once);
        }
        [Fact]
        public async Task ProcessMessageAsyncTestGetsContractAndAddsToAsset()
        {
            var contracts = CreateContracts(contractCount: 1, chargesCount: 2, relatedPeopleCount: 1);


            _mockAssetApi.Setup(x => x.GetAssetByIdAsync(_message.EntityId, _message.CorrelationId))
                .ReturnsAsync(_asset);
            _mockContractApi.Setup(x => x.GetContractsByAssetIdAsync(_message.EntityId, _message.CorrelationId))
                .ReturnsAsync(contracts);
            _mockEsGateway.Setup(x => x.GetAssetById(_message.EntityId.ToString()))
                .ReturnsAsync(_asset);

            await _sut.ProcessMessageAsync(_message).ConfigureAwait(false);

            _mockContractApi.Verify(x => x.GetContractsByAssetIdAsync(_message.EntityId, _message.CorrelationId), Times.Once);
            _mockEsGateway.Verify(x => x.IndexAsset(It.Is<QueryableAsset>(asset =>
                asset.AssetContracts.Count() == 1 &&
                asset.AssetContracts.First().Charges.Count() == 2 &&
                asset.AssetContracts.First().RelatedPeople.Count() == 1)),
                Times.Once);
        }
        [Fact]
        public async Task ProcessMessageAsyncTestHandlesNoContracts()
        {
            var contracts = CreateContracts(contractCount: 0);

            _mockAssetApi.Setup(x => x.GetAssetByIdAsync(_message.EntityId, _message.CorrelationId))
                .ReturnsAsync(_asset);
            _mockContractApi.Setup(x => x.GetContractsByAssetIdAsync(_message.EntityId, _message.CorrelationId))
                .ReturnsAsync(contracts);
            _mockEsGateway.Setup(x => x.GetAssetById(_message.EntityId.ToString()))
                .ReturnsAsync(_asset);

            await _sut.ProcessMessageAsync(_message).ConfigureAwait(false);

            _mockEsGateway.Verify(x => x.IndexAsset(It.Is<QueryableAsset>(asset =>
                asset.AssetContracts.Count() == 0)),
                Times.Once);
        }
    }
}

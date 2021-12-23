using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using Amazon.Runtime;
using Hackney.Core.DynamoDb;
using Hackney.Core.Logging;
using Hackney.Core.Sns;
using HousingSearchListener.V1.Factories;
using HousingSearchListener.V1.Factories.Interfaces;
using HousingSearchListener.V1.Factories.QueryableFactories;
using HousingSearchListener.V1.Gateway;
using HousingSearchListener.V1.Gateway.Interfaces;
using HousingSearchListener.V1.Infrastructure;
using HousingSearchListener.V1.UseCase;
using HousingSearchListener.V1.UseCase.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Threading.Tasks;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace HousingSearchListener
{
    [ExcludeFromCodeCoverage]
    public class HousingSearchListener : BaseFunction
    {
        private readonly static JsonSerializerOptions _jsonOptions = JsonOptions.CreateJsonOptions();

        /// <summary>
        /// Default constructor. This constructor is used by Lambda to construct the instance. When invoked in a Lambda environment
        /// the AWS credentials will come from the IAM role associated with the function and the AWS region will be set to the
        /// region the Lambda function is executed in.
        /// </summary>
        public HousingSearchListener()
        {
        }

        protected override void ConfigureServices(IServiceCollection services)
        {
            services.AddHttpClient();
            bool result = false;
            bool.TryParse(Environment.GetEnvironmentVariable("DynamoDb_LocalMode"), out result);
            if (result)
            {
                string url = Environment.GetEnvironmentVariable("DynamoDb_LocalServiceUrl");
                var accessKey = Environment.GetEnvironmentVariable("DynamoDb_LocalAccessKey");
                var secretKey = Environment.GetEnvironmentVariable("DynamoDb_LocalSecretKey");
                services.AddSingleton<IAmazonDynamoDB>(sp =>
                {
                    var clientConfig = new AmazonDynamoDBConfig { ServiceURL = url };
                    var credentials = new BasicAWSCredentials(accessKey, secretKey);
                    return new AmazonDynamoDBClient(credentials, clientConfig);
                });
            }
            else
            {
                services.AddAWSService<IAmazonDynamoDB>();
            }

            services.AddScoped((Func<IServiceProvider, IDynamoDBContext>)((IServiceProvider sp) => new DynamoDBContext(sp.GetService<IAmazonDynamoDB>())));

            services.AddScoped<ITenuresFactory, TenuresFactory>();
            services.AddScoped<ITransactionFactory, TransactionsFactory>();
            services.AddScoped<IPersonFactory, PersonFactory>();
            services.AddScoped<IAccountFactory, AccountFactory>();
            services.AddScoped<IEsGateway, EsGateway>();
            services.AddScoped<IPersonApiGateway, PersonApiGateway>();
            services.AddScoped<ITenureApiGateway, TenureApiGateway>();
            services.AddScoped<IAccountApiGateway, AccountApiGateway>();
            services.AddScoped<IFinancialTransactionApiGateway, FinancialTransactionApiGateway>();

            // Transient because otherwise all gateway's that use it will get the same instance,
            // which is not the desired result.
            services.AddTransient<IApiGateway, ApiGateway>();

            services.ConfigureElasticSearch(Configuration);

            services.AddScoped<IIndexCreatePersonUseCase, IndexCreatePersonUseCase>();
            services.AddScoped<IIndexUpdatePersonUseCase, IndexUpdatePersonUseCase>();
            services.AddScoped<IIndexTenureUseCase, IndexTenureUseCase>();
            services.AddScoped<IAddPersonToTenureUseCase, AddPersonToTenureUseCase>();
            services.AddScoped<IRemovePersonFromTenureUseCase, RemovePersonFromTenureUseCase>();
            services.AddScoped<IUpdateAccountDetailsUseCase, UpdateAccountDetailsUseCase>();
            services.AddScoped<IIndexTransactionUseCase, IndexTransactionUseCase>();
            services.AddScoped<IAccountDbGateway, AccountDynamoDbGateway>();
            services.AddScoped<IAccountCreateUseCase, AccountCreatedUseCase>();
            services.AddScoped<IAccountUpdateUseCase, AccountUpdatedUseCase>();

            base.ConfigureServices(services);
        }


        public async Task FunctionHandler(SQSEvent snsEvent, ILambdaContext context)
        {
            // Do this in parallel???
            foreach (var message in snsEvent.Records)
            {
                await ProcessMessageAsync(message, context).ConfigureAwait(false);
            }
        }

        [LogCall(LogLevel.Information)]
        private async Task ProcessMessageAsync(SQSEvent.SQSMessage message, ILambdaContext context)
        {
            context.Logger.LogLine($"Processing message {message.MessageId}");

            //var fakeEvent = new EntityEventSns
            //{
            //    Id = Guid.Parse("6900e3f3-9de0-4235-8e39-3cd1b7f89313"),
            //    EntityId = Guid.Parse("6900e3f3-9de0-4235-8e39-3cd1b7f89313"),
            //    EventType = "AccountCreatedEvent"
            //};
            //string fakeEventJson = JsonSerializer.Serialize(fakeEvent, _jsonOptions);

            var entityEvent = JsonSerializer.Deserialize<EntityEventSns>(message.Body, _jsonOptions);

            using (Logger.BeginScope("CorrelationId: {CorrelationId}", entityEvent.CorrelationId))
            {
                try
                {
                    IMessageProcessing processor = entityEvent.CreateUseCaseForMessage(ServiceProvider);

                    if (processor != null)
                    {
                        await processor.ProcessMessageAsync(entityEvent).ConfigureAwait(false);
                    }

                    else
                    {
                        Logger.LogInformation($"No processors available for message so it will be ignored. " +
                           $"Message id: {message.MessageId}; type: {entityEvent.EventType}; version: {entityEvent.Version}; entity id: {entityEvent.EntityId}");
                    }
                       
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, $"Exception processing message id: {message.MessageId}; type: {entityEvent.EventType}; entity id: {entityEvent.EntityId}");
                    throw; // AWS will handle retry/moving to the dead letter queue
                }
            }
        }
    }
}
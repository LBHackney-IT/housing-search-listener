﻿using System.Threading.Tasks;
using HousingSearchListener.V1.Domain.ElasticSearch;
using Nest;

namespace HousingSearchListener.V1.Interfaces
{
    public class ElasticSearchHelper : IElasticSearchHelper
    {
        private readonly IElasticClient _elasticClient;

        public ElasticSearchHelper(IElasticClient elasticClient)
        {
            _elasticClient = elasticClient;
        }

        public async Task<IndexResponse> Update(ESPerson esPerson)
        {
            return await ESIndex(esPerson);
        }

        public async Task<IndexResponse> Create(ESPerson esPerson)
        {
            return await ESIndex(esPerson);
        }

        private async Task<IndexResponse> ESIndex(ESPerson esPerson)
        {
            return await _elasticClient.IndexAsync(new IndexRequest<ESPerson>(esPerson, "persons"));
        }
    }
}
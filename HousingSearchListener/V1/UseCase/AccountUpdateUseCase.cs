﻿using HousingSearchListener.V1.Boundary;
using HousingSearchListener.V1.Domain.Person;
using HousingSearchListener.V1.Domain.Tenure;
using HousingSearchListener.V1.Factories;
using HousingSearchListener.V1.Gateway;
using HousingSearchListener.V1.Infrastructure.Exceptions;
using HousingSearchListener.V1.UseCase.Interfaces;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace HousingSearchListener.V1.UseCase
{
    public class AccountUpdateUseCase : IAccountUpdateUseCase
    {
        private readonly IEsGateway _esGateway;
        private readonly ITenureApiGateway _tenureApiGateway;
        private readonly IPersonApiGateway _personApiGateway;
        private readonly IESEntityFactory _esEntityFactory;

        public AccountUpdateUseCase(IEsGateway esGateway, ITenureApiGateway tenureApiGateway, IPersonApiGateway personApiGateway,
            IESEntityFactory esPersonFactory)
        {
            _esGateway = esGateway;
            _tenureApiGateway = tenureApiGateway;
            _personApiGateway = personApiGateway;
            _esEntityFactory = esPersonFactory;
        }

        public async Task ProcessMessageAsync(EntityEventSns message)
        {
            if (message is null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            // 1. Get Tenure from Tenure service API
            var tenure = await _tenureApiGateway.GetTenureByIdAsync(message.EntityId).ConfigureAwait(false);
            if (tenure is null)
            {
                throw new EntityNotFoundException<TenureInformation>(message.EntityId);
            }

            // Hanna Holasava
            // Can we be sure that we will find only one record with IsResponsible = true? 
            // If it is, we should change FirstOrDefault to SingleOrDefault.
            // 2. Searching a person
            var personInfo = tenure.HouseholdMembers.Where(m => m.IsResponsible).FirstOrDefault();
            if (personInfo == null)
            {
                throw new MissedEntityDataException($"Tenure with id {message.EntityId} does not have any responsible household member.");
            }

            if (!Guid.TryParse(personInfo.Id, out Guid personId))
            {
                throw new FormatException(nameof(personId));
            }

            var person = await _personApiGateway.GetPersonByIdAsync(personId).ConfigureAwait(false);
            if (person is null)
            {
                throw new EntityNotFoundException<Person>(personId);
            }

            // 3. Update the ES index
            var esTenure = _esEntityFactory.CreateTenure(tenure);
            var esPerson = _esEntityFactory.CreatePerson(person);

            _ = await _esGateway.UpdatePersonAccountAsync(esPerson, esTenure).ConfigureAwait(false);
        }
    }
}
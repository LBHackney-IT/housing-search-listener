﻿using HousingSearchListener.V1.Domain.Person;
using System;
using System.Threading.Tasks;

namespace HousingSearchListener.V1.Gateway.Interfaces
{
    public interface IPersonApiGateway
    {
        Task<Person> GetPersonByIdAsync(Guid id, Guid correlationId);
    }
}

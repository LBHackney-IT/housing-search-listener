using HousingSearchListener.V1.Domain.ElasticSearch;
using HousingSearchListener.V1.Domain.Person;
using HousingSearchListener.V1.Domain.Tenure;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HousingSearchListener.V1.Factories
{
    public class ESEntityFactory : IESEntityFactory
    {
        private List<ESIdentification> CreateIdentifications(List<Identification> identifications)
        {
            return identifications.Select(x => new ESIdentification
            {
                IdentificationType = x.IdentificationType,
                IsOriginalDocumentSeen = x.IsOriginalDocumentSeen,
                LinkToDocument = x.LinkToDocument,
                Value = x.Value
            }).ToList();
        }

        private List<ESPersonTenure> CreateTenures(List<Tenure> tenures)
        {
            return tenures.Select(x => new ESPersonTenure
            {
                AssetFullAddress = x.AssetFullAddress,
                EndDate = x.EndDate,
                Id = x.Id,
                StartDate = x.StartDate,
                Type = x.Type
            }).ToList();
        }

        public ESPerson CreatePerson(Person person)
        {
            if (person is null)
            {
                throw new ArgumentNullException(nameof(person));
            }

            return new ESPerson
            {
                Id = person.Id,
                DateOfBirth = person.DateOfBirth,
                Title = person.Title,
                Firstname = person.FirstName,
                TotalBalance = person.TotalBalance,
                Surname = person.Surname,
                MiddleName = person.MiddleName,
                PreferredFirstname = person.PreferredFirstName,
                PreferredSurname = person.PreferredSurname,
                Identifications = person.Identifications != null ? CreateIdentifications(person.Identifications) : new List<ESIdentification>(),
                PersonTypes = person.PersonType,
                Tenures = CreateTenures(person.Tenures) ?? new List<ESPersonTenure>()
            };
        }

        public QueryableTenure CreateQueryableTenure(TenureInformation tenure)
        {
            if (tenure is null)
            {
                throw new ArgumentNullException(nameof(tenure));
            }

            return new QueryableTenure
            {
                Id = tenure.Id,
                StartOfTenureDate = tenure.StartOfTenureDate,
                EndOfTenureDate = tenure.EndOfTenureDate,
                TenureType = new QueryableTenureType()
                {
                    Code = tenure.TenureType.Code,
                    Description = tenure.TenureType.Description
                },
                PaymentReference = tenure.PaymentReference,
                HouseholdMembers = CreateQueryableHouseholdMembers(tenure.HouseholdMembers),
                TenuredAsset = new QueryableTenuredAsset()
                {
                    FullAddress = tenure.TenuredAsset?.FullAddress,
                    Id = tenure.TenuredAsset?.Id,
                    Type = tenure.TenuredAsset?.Type,
                    Uprn = tenure.TenuredAsset?.Uprn,
                }
            };
        }

        public ESPersonTenure CreateTenure(TenureInformation tenure)
        {
            if (tenure is null)
            {
                throw new ArgumentNullException(nameof(tenure));
            }

            return new ESPersonTenure
            {
                Id = tenure.Id,
                Type = tenure.TenureType.Code,// Is it right format of ESTenure type ?
                StartDate = tenure.StartOfTenureDate,
                EndDate = tenure.EndOfTenureDate,
                AssetFullAddress = tenure.TenuredAsset.FullAddress,
                PaymentReference = tenure.PaymentReference,
                TotalBalance = tenure.TotalBalance
            };
        }

        private List<QueryableHouseholdMember> CreateQueryableHouseholdMembers(List<HouseholdMembers> householdMembers)
        {
            if (householdMembers is null)
            {
                return new List<QueryableHouseholdMember>();
            }

            return householdMembers.Select(x => new QueryableHouseholdMember()
            {
                DateOfBirth = x.DateOfBirth,
                FullName = x.FullName,
                Id = x.Id,
                IsResponsible = x.IsResponsible,
                PersonTenureType = x.PersonTenureType,
                Type = x.Type
            }).ToList();
        }
    }
}
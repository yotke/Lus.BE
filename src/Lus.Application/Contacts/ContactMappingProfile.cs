using AutoMapper;
using Lus.Application.Contacts.Commands.CreateContact;
using Lus.Application.Contacts.Entities;
using Lus.Application.Contacts.Projections;
using Lus.Contracts.Contacts;
using Lus.Contracts.Users;

namespace Lus.Application.Contacts
{
    public class ContactMappingProfile : Profile
    {
        public ContactMappingProfile()
        {
            CreateMap<Contact, ContactDto>();
            CreateMap<Contact, UserInfoDto>()
                .ForMember(c => c.FirstName, opt => opt.MapFrom(c => c.Name));
            CreateMap<CreateContactCommand, Contact>();
            CreateMap<ContactDto, Contact>()
                .ForMember(c => c.Id, opt => opt.Ignore());

            // Projection -> search DTO (used by the filter/search engine)
            CreateMap<ContactProjection, SearchContactDto>();
        }
    }
}
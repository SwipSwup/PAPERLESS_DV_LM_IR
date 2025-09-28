using AutoMapper;
using Core.DTOs;
using Core.Models;

namespace BL.Mappings
{
    public class BlMappingProfile : Profile
    {
        public BlMappingProfile()
        {
            // Core ↔ DTO
            CreateMap<Document, DocumentDto>().ReverseMap();
            CreateMap<Tag, TagDto>().ReverseMap();
            CreateMap<AccessLog, AccessLogDto>().ReverseMap();
            CreateMap<DocumentLog, DocumentLogDto>().ReverseMap();
        }
    }
}
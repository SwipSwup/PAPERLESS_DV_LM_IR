using AutoMapper;
using Core.Models;
using DAL.Models;

namespace DAL.Mappings
{
    public class DalMappingProfile : Profile
    {
        public DalMappingProfile()
        {
            // Entity ↔ Core
            CreateMap<DocumentEntity, Document>().ReverseMap();
            CreateMap<TagEntity, Tag>().ReverseMap();
            CreateMap<AccessLogEntity, AccessLog>().ReverseMap();
            CreateMap<DocumentLogEntity, DocumentLog>().ReverseMap();
        }
    }
}
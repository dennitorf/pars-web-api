using KellyServices.PARS.Persistence.Contexts;
using KellyServices.PARS.Application.Common.Mappings;
using AutoMapper;

namespace  KellyServices.PARS.Application.UnitTests.Mocks.Persistence
{
    public static class  AutoMapperMock
    {
        public static IMapper mapper;

        static AutoMapperMock()
        {
            var mapperConfiguration = new MapperConfiguration(c => {
                c.AddProfile<AutoMapperProfile>();
            });

            mapper = mapperConfiguration.CreateMapper();
        }        
    }
}
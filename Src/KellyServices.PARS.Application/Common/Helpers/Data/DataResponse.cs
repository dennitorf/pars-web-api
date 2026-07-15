using System.Linq;

namespace KellyServices.PARS.Common.Helpers.Data
{
    public class DataResponse<T>
    {
        public int Total { set; get; }
        public IQueryable<T> Data { set; get; }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SetlistFmAPI.Http
{
    public interface IHttpClient
    {
        Task<T> Load<T>(Uri url, string apiKey, string language = "en");
    }
}

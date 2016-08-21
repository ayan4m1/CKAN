using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CKAN.Net
{
    public interface ICache
    {
        bool IsCached(Uri url);
        bool IsCached(Uri url, out string file);
        bool IsCachedZip(Uri url);
        bool IsMaybeCachedZip(Uri url);
        string GetCachedFilename(Uri url);
        string GetCachedZip(Uri url, bool test);
        string Store(Uri url, string path, string description, bool move);
        bool Remove(Uri url);
        void Cleanup();
    }
}

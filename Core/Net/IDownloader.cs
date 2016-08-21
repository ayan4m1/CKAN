using System.Collections.Generic;
using System.Threading.Tasks;

namespace CKAN.Net
{
    /// <summary>
    /// This interface represents the expected functionality
    /// of a downloader implementation.
    /// </summary>
    public interface IDownloader
    {
        /// <summary>
        /// Downloads all the modules specified to the cache.
        /// Even if modules share download URLs, they will only be downloaded once.
        /// Blocks until the downloads are complete, cancelled, or errored.
        /// </summary>
        void DownloadModules(NetFileCache cache, IEnumerable<CkanModule> modules);
    }
}
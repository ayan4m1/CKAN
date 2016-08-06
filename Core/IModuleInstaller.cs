using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CKAN
{
    public interface IModuleInstaller
    {
        ModuleInstaller GetInstance(KSP ksp_instance, IUser user);
        string Download(Uri url, string filename);
        string CachedOrDownload(CkanModule module, string filename);
        string CachedOrDownload(string identifier, Version version, Uri url, string filename);
        void InstallList(List<string> modules, RelationshipResolverOptions options, IDownloader downloader);
        void InstallList(ICollection<CkanModule> modules, RelationshipResolverOptions options, IDownloader downloader);
        IEnumerable<string> GetModuleContentsList(CkanModule module);
        void UninstallList(IEnumerable<string> mods);
        void UninstallList(string mod);
        void AddRemove(IEnumerable<CkanModule> add = null, IEnumerable<string> remove = null);
        void Upgrade(IEnumerable<string> identifiers, IDownloader netAsyncDownloader);
    }
}

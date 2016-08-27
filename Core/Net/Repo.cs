using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Autofac;
using ChinhDo.Transactions.FileManager;
using CKAN.GameVersionProviders;
using CKAN.Types;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;
using ICSharpCode.SharpZipLib.Zip;
using log4net;

namespace CKAN.Net
{
    /// <summary>
    ///     Class for downloading the CKAN meta-info itself.
    /// </summary>
    public static class Repo
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof (Repo));
        private static readonly TxFileManager TxFile = new TxFileManager();

        // Forward to keep existing code compiling, will be removed soon.
        public static readonly Uri DefaultCkanRepo = Repository.default_ckan_repo_uri;

        internal static void ProcessRegistryMetadataFromJson(string metadata, Registry registry, string filename)
        {
            Log.DebugFormat("Converting metadata from JSON.");

            try
            {
                CkanModule module = CkanModule.FromJson(metadata);
                Log.InfoFormat("Found {0} version {1}", module.identifier, module.version);
                registry.AddAvailable(module);
            }
            catch (Exception exception)
            {
                // Alas, we can get exceptions which *wrap* our exceptions,
                // because json.net seems to enjoy wrapping rather than propagating.
                // See KSP-CKAN/CKAN-meta#182 as to why we need to walk the whole
                // exception stack.

                bool handled = false;

                while (exception != null)
                {
                    if (exception is UnsupportedKraken || exception is BadMetadataKraken)
                    {
                        // Either of these can be caused by data meant for future
                        // clients, so they're not really warnings, they're just
                        // informational.

                        Log.InfoFormat("Skipping {0} : {1}", filename, exception.Message);

                        // I'd *love a way to "return" from the catch block.
                        handled = true;
                        break;
                    }

                    // Look further down the stack.
                    exception = exception.InnerException;
                }

                // If we haven't handled our exception, then it really was exceptional.
                if (handled == false)
                {
                    if (exception == null)
                    {
                        // Had exception, walked exception tree, reached leaf, got stuck.
                        Log.ErrorFormat("Error processing {0} (exception tree leaf)", filename); 
                    }
                    else
                    {
                        // In case whatever's calling us is lazy in error reporting, we'll
                        // report that we've got an issue here.
                        Log.ErrorFormat("Error processing {0} : {1}", filename, exception.Message);
                    }

                    throw;
                }
            }
        }

        /// <summary>
        ///     Download and update the local CKAN meta-info.
        ///     Optionally takes a URL to the zipfile repo to download.
        ///     Returns the number of unique modules updated.
        /// </summary>
        public static int UpdateAllRepositories(RegistryManager registryManager, KSP ksp, IUser user)
        {
            // If we handle multiple repositories, we will call ClearRegistry() ourselves...
            registryManager.registry.ClearAvailable();
            // TODO this should already give us a pre-sorted list
            SortedDictionary<string, Repository> sortedRepositories = registryManager.registry.Repositories;
            foreach (KeyValuePair<string, Repository> repository in sortedRepositories)
            {
                Log.InfoFormat("About to update {0}", repository.Value.name);
                UpdateRegistry(repository.Value.uri, registryManager.registry, ksp, user, false);
                Log.InfoFormat("Updated {0}", repository.Value.name);
            }

            // Save our changes.
            registryManager.Save();

            // Return how many we got!
            return registryManager.registry.Available(ksp.Version).Count;
        }

        public static int Update(RegistryManager registryManager, KSP ksp, IUser user, bool clear = true, Uri repo = null)
        {
            // Use our default repo, unless we've been told otherwise.
            if (repo == null)
            {
                repo = DefaultCkanRepo;
            }

            UpdateRegistry(repo, registryManager.registry, ksp, user, clear);

            // Save our changes!
            registryManager.Save();

            // Return how many we got!
            return registryManager.registry.Available(ksp.Version).Count;
        }

        public static int Update(RegistryManager registryManager, KSP ksp, IUser user, Boolean clear = true, string repo = null)
        {
            if (repo == null)
            {
                return Update(registryManager, ksp, user, clear, (Uri)null);
            }

            return Update(registryManager, ksp, user, clear, new Uri(repo));
        }

        /// <summary>
        /// Updates the supplied registry from the URL given.
        /// This does not *save* the registry. For that, you probably want Repo.Update
        /// </summary>
        internal static void UpdateRegistry(Uri repo, Registry registry, KSP ksp, IUser user, Boolean clear = true)
        {
            // Use this opportunity to also update the build mappings... kind of hacky
            ServiceLocator.Container.Resolve<IKspBuildMap>().Refresh();

            Log.InfoFormat("Downloading {0}", repo);

            string repoFile;
            try
            {
                repoFile = NetUtils.Download(repo);
            }
            catch (WebException)
            {
                user.RaiseMessage("Connection to {0} could not be established.", repo);
                return;
            }

            // Clear our list of known modules.
            var oldAvailable = registry.available_modules;
            if (clear)
            {
                registry.ClearAvailable();
            }

            // Check the filetype.
            var type = FileIdentifier.IdentifyFile(repoFile);

            switch (type)
            {
            case FileType.TarGz:
                UpdateRegistryFromTarGz (repoFile, registry);
                break;
            case FileType.Zip:
                UpdateRegistryFromZip (repoFile, registry);
                break;
            default:
                break;
            }

            List<CkanModule> metadataChanges = new List<CkanModule>();

            foreach (var identifierModulePair in oldAvailable)
            {
                var identifier = identifierModulePair.Key;

                if (registry.IsInstalled(identifier))
                {
                    var installedVersion = registry.InstalledVersion(identifier);
                    if (!(registry.available_modules.ContainsKey(identifier)))
                    {
                        Log.InfoFormat("UpdateRegistry, module {0}, version {1} not in repository ({2})", identifier, installedVersion, repo);
                        continue;
                    }

                    if (!registry.available_modules[identifier].module_version.ContainsKey(installedVersion))
                    {
                        continue;
                    }

                    // if the mod is installed and the metadata is different we have to reinstall it
                    var metadata = registry.available_modules[identifier].module_version[installedVersion];

                    if (!oldAvailable.ContainsKey(identifier) ||
                        !oldAvailable[identifier].module_version.ContainsKey(installedVersion))
                    {
                        continue;
                    }

                    var oldMetadata = oldAvailable[identifier].module_version[installedVersion];
                    var oldInstallMetaData = oldMetadata.install;
                    var same = true;
                    
                    if ((metadata.install == null) != (oldMetadata.install == null) ||
                        (metadata.install != null && metadata.install.Length != oldInstallMetaData.Length))
                    {
                        same = false;
                    }
                    else
                    {
                        if(metadata.install != null)
                        for (int i = 0; i < metadata.install.Length; i++)
                        {
                            if (metadata.install[i].file != oldInstallMetaData[i].file)
                            {
                                same = false;
                                break;
                            }

                            if (metadata.install[i].install_to != oldInstallMetaData[i].install_to)
                            {
                                same = false;
                                break;
                            }

                            if (metadata.install[i].@as != oldInstallMetaData[i].@as)
                            {
                                same = false;
                                break;
                            }

                            if ((metadata.install[i].filter == null) != (oldInstallMetaData[i].filter == null))
                            {
                                same = false;
                                break;
                            }

                            if(metadata.install[i].filter != null)
                            if (!metadata.install[i].filter.SequenceEqual(oldInstallMetaData[i].filter))
                            {
                                same = false;
                                break;
                            }

                            if ((metadata.install[i].filter_regexp == null) != (oldInstallMetaData[i].filter_regexp == null))
                            {
                                same = false;
                                break;
                            }

                            if(metadata.install[i].filter_regexp != null)
                            if (!metadata.install[i].filter_regexp.SequenceEqual(oldInstallMetaData[i].filter_regexp))
                            {
                                same = false;
                                break;
                            }
                        }
                    }

                    if (!same)
                    {
                        metadataChanges.Add(registry.available_modules[identifier].module_version[installedVersion]);
                    }
                }
            }

            if (metadataChanges.Any())
            {
                string mods = "";
                for (int i = 0; i < metadataChanges.Count; i++)
                {
                    mods += metadataChanges[i].identifier + " "
                        + metadataChanges[i].version + ((i < metadataChanges.Count-1) ? ", " : "");
                }

                if(user.RaiseYesNoDialog(String.Format(
                    @"The following mods have had their metadata changed since last update - {0}.
It is advisable that you reinstall them in order to preserve consistency with the repository. Do you wish to reinstall now?", mods)))
                {
                    var installer = ModuleInstaller.GetInstance(ksp, new NullUser());
                    installer.Upgrade(metadataChanges, new NetAsyncModulesDownloader(installer.User));
                }
            }

            // Remove our downloaded meta-data now we've processed it.
            // Seems weird to do this as part of a transaction, but NetUtils.Download uses them, so let's be consistent.
            TxFile.Delete(repoFile);
        }

        /// <summary>
        /// Updates the supplied registry from the supplied zip file.
        /// This will *clear* the registry of available modules first.
        /// This does not *save* the registry. For that, you probably want Repo.Update
        /// </summary>
        internal static void UpdateRegistryFromTarGz(string path, Registry registry)
        {
            Log.DebugFormat("Starting registry update from tar.gz file: \"{0}\".", path);

            // Open the gzip'ed file.
            using (Stream inputStream = File.OpenRead(path))
            {
                // Create a gzip stream.
                using (GZipInputStream gzipStream = new GZipInputStream(inputStream))
                {
                    // Create a handle for the tar stream.
                    using (TarInputStream tarStream = new TarInputStream(gzipStream))
                    {
                        // Walk the archive, looking for .ckan files.
                        const string filter = @"\.ckan$";

                        while (true)
                        {
                            TarEntry entry = tarStream.GetNextEntry();

                            // Check for EOF.
                            if (entry == null)
                            {
                                break;
                            }

                            string filename = entry.Name;

                            // Skip things we don't want.
                            if (!Regex.IsMatch(filename, filter))
                            {
                                Log.DebugFormat("Skipping archive entry {0}", filename);
                                continue;
                            }

                            Log.DebugFormat("Reading CKAN data from {0}", filename);

                            // Read each file into a buffer.
                            int bufferSize;

                            try
                            {
                                bufferSize = Convert.ToInt32(entry.Size);
                            }
                            catch (OverflowException)
                            {
                                Log.ErrorFormat("Error processing {0}: Metadata size too large.", entry.Name);
                                continue;
                            }

                            var buffer = new byte[bufferSize];

                            tarStream.Read(buffer, 0, bufferSize);

                            // Convert the buffer data to a string.
                            var metadataJson = Encoding.ASCII.GetString(buffer);

                            ProcessRegistryMetadataFromJson(metadataJson, registry, filename);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Updates the supplied registry from the supplied zip file.
        /// This will *clear* the registry of available modules first.
        /// This does not *save* the registry. For that, you probably want Repo.Update
        /// </summary>
        internal static void UpdateRegistryFromZip(string path, Registry registry)
        {
            Log.DebugFormat("Starting registry update from zip file: \"{0}\".", path);

            using (var zipfile = new ZipFile(path))
            {
                // Walk the archive, looking for .ckan files.
                const string filter = @"\.ckan$";

                foreach (ZipEntry entry in zipfile)
                {
                    string filename = entry.Name;

                    // Skip things we don't want.
                    if (! Regex.IsMatch(filename, filter))
                    {
                        Log.DebugFormat("Skipping archive entry {0}", filename);
                        continue;
                    }

                    Log.DebugFormat("Reading CKAN data from {0}", filename);

                    // Read each file into a string.
                    string metadataJson;
                    using (var stream = new StreamReader(zipfile.GetInputStream(entry)))
                    {
                        metadataJson = stream.ReadToEnd();
                        stream.Close();
                    }

                    ProcessRegistryMetadataFromJson(metadataJson, registry, filename);
                }

                zipfile.Close();
            }
        }
    }
}
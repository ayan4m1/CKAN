﻿using System.Collections.Generic;
using CKAN.Net;
using CKAN.Types;
using log4net;

namespace CKAN.CmdLine.Action
{
    public class Upgrade : ICommand
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(Upgrade));

        public Upgrade(IUser user)
        {
            User = user;
        }

        public IUser User { get; set; }


        public int RunCommand(CKAN.KSP ksp, object raw_options)
        {
            var options = (UpgradeOptions) raw_options;

            if (options.ckan_file != null)
            {
                options.modules.Add(LoadCkanFromFile(ksp, options.ckan_file).identifier);
            }

            if (options.modules.Count == 0 && !options.upgrade_all)
            {
                // What? No files specified?
                User.RaiseMessage("Usage: ckan upgrade Mod [Mod2, ...]");
                User.RaiseMessage("  or   ckan upgrade --all");
                User.RaiseMessage("  or   ckan upgrade ckan");
                return Exit.BADOPT;
            }

            if (!options.upgrade_all && options.modules[0] == "ckan")
            {
                User.RaiseMessage("Querying the latest CKAN version");
                AutoUpdate.Instance.FetchLatestReleaseInfo();
                var latestVersion = AutoUpdate.Instance.LatestVersion;
                var currentVersion = new GameVersion(Meta.Version());

                if (latestVersion.IsGreaterThan(currentVersion))
                {
                    User.RaiseMessage("New CKAN version available - " + latestVersion);
                    var releaseNotes = AutoUpdate.Instance.ReleaseNotes;
                    User.RaiseMessage(releaseNotes);
                    User.RaiseMessage("\r\n");

                    if (User.RaiseYesNoDialog("Proceed with install?"))
                    {
                        User.RaiseMessage("Upgrading CKAN, please wait..");
                        AutoUpdate.Instance.StartUpdateProcess(false);
                    }
                }
                else
                {
                    User.RaiseMessage("You already have the latest version.");
                }

                return Exit.OK;
            }

            User.RaiseMessage("\r\nUpgrading modules...\r\n");

            try
            {
                if (options.upgrade_all)
                {
                    var installed = new Dictionary<string, GameVersion>(ksp.Registry.Installed());
                    var to_upgrade = new List<CkanModule>();

                    foreach (var mod in installed)
                    {
                        var current_version = mod.Value;

                        if (current_version is ProvidesVersion || current_version is DllVersion)
                        {
                            continue;
                        }
                        else
                        {
                            try
                            {
                                // Check if upgrades are available
                                var latest = ksp.Registry.LatestAvailable(mod.Key, ksp.Version);

                                // This may be an unindexed mod. If so,
                                // skip rather than crash. See KSP-CKAN/CKAN#841.
                                if (latest == null)
                                {
                                    continue;
                                }

                                if (latest.version.IsGreaterThan(mod.Value))
                                {
                                    // Upgradable
                                    log.InfoFormat("New version {0} found for {1}",
                                        latest.version, latest.identifier);
                                    to_upgrade.Add(latest);
                                }
                            }
                            catch (ModuleNotFoundKraken)
                            {
                                log.InfoFormat("{0} is installed, but no longer in the registry",
                                    mod.Key);
                            }
                        }
                    }

                    ModuleInstaller.GetInstance(ksp, User).Upgrade(to_upgrade, new NetAsyncModulesDownloader(User));
                }
                else
                {
                    // TODO: These instances all need to go.
                    ModuleInstaller.GetInstance(ksp, User).Upgrade(options.modules, new NetAsyncModulesDownloader(User));
                }
            }
            catch (ModuleNotFoundKraken kraken)
            {
                User.RaiseMessage("Module {0} not found", kraken.module);
                return Exit.ERROR;
            }
            User.RaiseMessage("\r\nDone!\r\n");

            return Exit.OK;
        }

        internal static CkanModule LoadCkanFromFile(CKAN.KSP current_instance, string ckan_file)
        {
            var module = CkanModule.FromFile(ckan_file);

            // We'll need to make some registry changes to do this.
            var registry_manager = RegistryManager.Instance(current_instance);

            // Remove this version of the module in the registry, if it exists.
            registry_manager.registry.RemoveAvailable(module);

            // Sneakily add our version in...
            registry_manager.registry.AddAvailable(module);

            return module;
        }
    }
}


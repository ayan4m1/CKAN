using System;
using System.Collections.Generic;
using CKAN.Exporters;
using CKAN.Registry;
using CKAN.Types;
using log4net;
using Version = CKAN.Types.Version;

namespace CKAN.CmdLine.Action
{
    public class List : ICommand
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(List));

        public List(IUser user)
        {
            User = user;
        }

        public IUser User { get; set; }

        public int RunCommand(CKAN.KSP ksp, object raw_options)
        {
            var options = (ListOptions) raw_options;

            IRegistryQuerier registry = RegistryManager.Instance(ksp).registry;


            ExportFileType? exportFileType = null;

            if (!string.IsNullOrWhiteSpace(options.export))
            {
                exportFileType = GetExportFileType(options.export);

                if (exportFileType == null)
                {
                    User.RaiseError("Unknown export format: {0}", options.export);
                }
            }

            if (!options.porcelain && exportFileType == null)
            {
                User.RaiseMessage("\r\nKSP found at {0}\r\n", ksp.GameDir);
                User.RaiseMessage("KSP Version: {0}\r\n", ksp.Version);

                User.RaiseMessage("Installed Modules:\r\n");
            }

            if (exportFileType == null)
            {
                var installed = new SortedDictionary<string, Version>(registry.Installed());

                foreach (var mod in installed)
                {
                    var currentVersion = mod.Value;

                    var bullet = "*";

                    if (currentVersion is ProvidesVersion)
                    {
                        // Skip virtuals for now.
                        continue;
                    }
                    if (currentVersion is DllVersion)
                    {
                        // Autodetected dll
                        bullet = "-";
                    }
                    else
                    {
                        try
                        {
                            // Check if upgrades are available, and show appropriately.
                            var latest = registry.LatestAvailable(mod.Key, ksp.Version);

                            Log.InfoFormat("Latest {0} is {1}", mod.Key, latest);

                            if (latest == null)
                            {
                                // Not compatible!
                                bullet = "X";
                            }
                            else if (latest.version.IsEqualTo(currentVersion))
                            {
                                // Up to date
                                bullet = "-";
                            }
                            else if (latest.version.IsGreaterThan(mod.Value))
                            {
                                // Upgradable
                                bullet = "^";
                            }
                        }
                        catch (ModuleNotFoundKraken)
                        {
                            Log.InfoFormat("{0} is installed, but no longer in the registry", mod.Key);
                            bullet = "?";
                        }
                    }

                    User.RaiseMessage("{0} {1} {2}", bullet, mod.Key, mod.Value);
                }
            }
            else
            {
                var stream = Console.OpenStandardOutput();
                new Exporter(exportFileType.Value).Export(registry, stream);
                stream.Flush();
            }

            if (!options.porcelain && exportFileType == null)
            {
                User.RaiseMessage("\r\nLegend: -: Up to date. X: Incompatible. ^: Upgradable. ?: Unknown. *: Broken. ");
                // Broken mods are in a state that CKAN doesn't understand, and therefore can't handle automatically
            }

            return Exit.OK;
        }

        private static ExportFileType? GetExportFileType(string export)
        {
            export = export.ToLowerInvariant();

            switch (export)
            {
                case "text":
                    return ExportFileType.PlainText;
                case "markdown":
                    return ExportFileType.Markdown;
                case "bbcode":
                    return ExportFileType.BbCode;
                case "csv":
                    return ExportFileType.Csv;
                case "tsv":
                    return ExportFileType.Tsv;
                default:
                    return null;
            }
        }
    }
}
﻿using System;
using System.Collections.Generic;
using CKAN.Types;

namespace CKAN.CmdLine.Action
{
    public class Search : ICommand
    {
        public Search(IUser user)
        {
            this.user = user;
        }

        public IUser user { get; set; }

        public int RunCommand(CKAN.KSP ksp, object raw_options)
        {
            var options = (SearchOptions) raw_options;

            // Check the input.
            if (string.IsNullOrWhiteSpace(options.search_term))
            {
                user.RaiseError("No search term?");

                return Exit.BADOPT;
            }

            var matching_mods = PerformSearch(ksp, options.search_term);

            // Show how many matches we have.
            user.RaiseMessage("Found " + matching_mods.Count + " mods matching \"" + options.search_term + "\".");

            // Present the results.
            if (matching_mods.Count == 0)
            {
                return Exit.OK;
            }

            // Print each mod on a separate line.
            foreach (var mod in matching_mods)
            {
                user.RaiseMessage(mod.identifier);
            }

            return Exit.OK;
        }

        /// <summary>
        /// Searches for the term in the list of available modules for the ksp instance. Looks in name, identifier and description fields.
        /// </summary>
        /// <returns>List of mathcing modules.</returns>
        /// <param name="ksp">The KSP instance to perform the search for.</param>
        /// <param name="term">The search term. Case insensitive.</param>
        public List<CkanModule> PerformSearch(CKAN.KSP ksp, string term)
        {
            var registry = RegistryManager.Instance(ksp).registry;
            return registry
                .Available(ksp.Version)
                .Where((module) =>
            {
                // Extract the description. This is an optional field and may be null.
                string modDesc = string.Empty;

                if (!string.IsNullOrEmpty(module.description))
                {
                    modDesc = module.description;
                }

                // Look for a match in each string.
                return module.name.IndexOf(term, StringComparison.OrdinalIgnoreCase) > -1 || module.identifier.IndexOf(term, StringComparison.OrdinalIgnoreCase) > -1 || modDesc.IndexOf(term, StringComparison.OrdinalIgnoreCase) > -1;
            }).ToList();
        }
    }
}


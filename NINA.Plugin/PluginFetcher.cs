#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using Newtonsoft.Json.Linq;
using NINA.Core.Model;
using NINA.Core.Utility;
using NINA.Core.Utility.Http;
using NINA.Plugin.Interfaces;
using NINA.Plugin.ManifestDefinition;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Plugin {

    public class PluginFetcher {
        private string repositoryURL;

        public PluginFetcher(string repositoryURL) {
            this.repositoryURL = repositoryURL;
        }

        public async Task<IList<PluginManifest>> RequestAll(IPluginVersion minimumApplicationVersion, IProgress<ApplicationStatus> progress, CancellationToken ct) {
            return await Task.Run(async () => {
                List<PluginManifest> plugins = new List<PluginManifest>();
                var compatibilityMap = new PluginCompatibilityMap();
                try {
                    var req = new HttpGetRequest(repositoryURL + "/plugins/manifests", true);
                    Logger.Info($"Fetching plugin manifests from {req.Url}");
                    var res = await req.Request(ct);
                    if (!string.IsNullOrEmpty(res)) {
                        var arr = JArray.Parse(res);
                        foreach (var item in arr) {
                            var plugin = await ValidateAndParseManifest(item);
                            if (plugin != null) {
                                if (PluginVersion.IsPluginCompatible(plugin.MinimumApplicationVersion, minimumApplicationVersion)
                                    && !compatibilityMap.IsNotCompatible(plugin)) {
                                    plugins.Add(plugin);
                                }
                            }
                        }
                    }
                    Logger.Info($"Found {plugins.Count} valid plugins at {req.Url}");
                } catch (Exception ex) {
                    Logger.Error(ex);
                    throw;
                }
                return plugins;
            });
        }

        private async Task<PluginManifest> ValidateAndParseManifest(JToken item) {
            PluginManifest plugin = null;
            //Validate the returned json against the schema
            var schema = await NJsonSchema.JsonSchema.FromJsonAsync(PluginManifest.Schema);
            var validationErrors = schema.Validate(item);
            if (validationErrors.Count == 0) {
                plugin = item.ToObject<PluginManifest>();
            } else {
                var errorString = string.Join(Environment.NewLine, validationErrors.Select(v => {
                    if (v.HasLineInfo) {
                        return $"Property {v.Property} validation failed due to {v.Kind} at Line {v.LineNumber} Position {v.LinePosition}";
                    } else {
                        return $"Property {v.Property} validation failed due to {v.Kind}";
                    }
                }));

                Logger.Error($"Plugin Manifest JSON did not validate against schema! {Environment.NewLine}{errorString}");
            }
            return plugin;
        }
    }
}
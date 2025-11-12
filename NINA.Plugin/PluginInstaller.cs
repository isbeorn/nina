#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Utility;
using NINA.Core.Utility.Http;
using NINA.Plugin.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Plugin {

    public class PluginInstaller {

        /// <summary>
        /// Plugins will be uninstalled by moving the folder inside {Constants.UserExtensionsFolder}//{manifest.Name} into the deletion area {Constatns.DeletionFolder}
        /// If that directory does not exist, it is assumed that the dll is directly put into the root folder. For that a search for the dll with the matching guid is searched
        /// </summary>
        /// <param name="manifest"></param>
        public void Uninstall(IPluginManifest manifest) {
            try {
                if (!Directory.Exists(Constants.BaseDeletionFolder)) {
                    Directory.CreateDirectory(Constants.BaseDeletionFolder);
                }
                if (!Directory.Exists(Constants.DeletionFolder)) {
                    Directory.CreateDirectory(Constants.DeletionFolder);
                }
                var folder = GetDestinationFolderFromManifest(manifest);
                if (Directory.Exists(folder)) {
                    MoveDirectoryContentToDeletion(folder);
                } else {
                    var file = FindFileInRootFolderByGuid(manifest);
                    if (!string.IsNullOrWhiteSpace(file)) {
                        var dir = Path.GetDirectoryName(file);
                        if (dir == Constants.UserExtensionsFolder) {
                            File.Move(file, Path.Combine(Constants.DeletionFolder, Path.GetRandomFileName()));
                        } else {
                            // This should only happen when the directory where the plugin resides does not match the conventions or was renamed
                            MoveDirectoryContentToDeletion(dir);
                        }
                    } else {
                        throw new FileNotFoundException();
                    }
                }
            } catch (Exception ex) {
                Logger.Error($"Failed to move plugin {manifest.Name} to deletion location", ex);
                throw;
            }
        }

        public async Task Install(IPluginManifest manifest, bool update, CancellationToken ct) {
            var tempFile = Path.Combine(Path.GetTempPath(), manifest.Name + ".dll");
            try {
                if (!Directory.Exists(Constants.BaseStagingFolder)) {
                    Directory.CreateDirectory(Constants.BaseStagingFolder);
                }
                if (!Directory.Exists(Constants.StagingFolder)) {
                    Directory.CreateDirectory(Constants.StagingFolder);
                }
                if (!Directory.Exists(Constants.BaseDeletionFolder)) {
                    Directory.CreateDirectory(Constants.BaseDeletionFolder);
                }
                if (!Directory.Exists(Constants.DeletionFolder)) {
                    Directory.CreateDirectory(Constants.DeletionFolder);
                }

                try {
                    using var httpClient = new HttpClient();
                    httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(CoreUtil.UserAgent);

                    Logger.Info($"Downloading plugin from {manifest.Installer.URL}");

                    using var response = await httpClient.GetAsync(manifest.Installer.URL, HttpCompletionOption.ResponseHeadersRead, ct);
                    response.EnsureSuccessStatusCode();

                    // Extract filename from Content-Disposition header
                    if (response.Content.Headers.ContentDisposition?.FileName != null) {
                        tempFile = Path.Combine(Path.GetTempPath(), response.Content.Headers.ContentDisposition.FileName.Trim('"'));
                    }

                    // Fallback if ContentDisposition is not parsed correctly
                    if (string.IsNullOrEmpty(tempFile) && response.Content.Headers.TryGetValues("Content-Disposition", out var values)) {
                        string contentDisposition = values.FirstOrDefault();
                        if (!string.IsNullOrEmpty(contentDisposition)) {
                            const string lookFor = "filename=";
                            int index = contentDisposition.IndexOf(lookFor, StringComparison.OrdinalIgnoreCase);
                            if (index >= 0) {
                                string filename = contentDisposition.Substring(index + lookFor.Length).Trim('"');
                                tempFile = Path.Combine(Path.GetTempPath(), filename);
                            }
                        }
                    }

                    // Ensure a filename if header is missing
                    tempFile ??= Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

                    using var input = await response.Content.ReadAsStreamAsync(ct);
                    using var output = new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.None);

                    Logger.Debug($"Saving downloaded plugin to temporary path {tempFile}");
                    await input.CopyToAsync(output, ct);
                } catch (OperationCanceledException) {
                    throw;
                } catch (Exception ex) {
                    Logger.Error(ex);
                    throw;
                }

                if (!ValidateChecksum(tempFile, manifest)) {
                    throw new Exception($"Checksum of file {tempFile} does not match expected checksum {manifest.Installer.Checksum} of type {manifest.Installer.ChecksumType}. File may be corrupted");
                }

                if (update) {
                    try {
                        Uninstall(manifest);
                    } catch (Exception ex) {
                        Logger.Error($"Failed to uninstall plugin {manifest.Name}", ex);
                    }
                }

                switch (manifest.Installer.Type) {
                    /*case ManifestDefinition.InstallerType.EXE:
                        //Default = DLL
                        await InstallExe(manifest, tempFile);
                        break;

                    case ManifestDefinition.InstallerType.MSI:
                        await InstallMSI(manifest, tempFile);
                        break;*/

                    case ManifestDefinition.InstallerType.ARCHIVE:
                        await InstallArchive(manifest, tempFile);
                        break;

                    default:
                        //Default = DLL
                        await InstallDLL(manifest, tempFile);
                        break;
                }
            } finally {
                //Cleanup
                try {
                    if (File.Exists(tempFile)) {
                        File.Delete(tempFile);
                    }
                } catch (Exception ex) {
                    Logger.Error("Failed to delete plugin temp file", ex);
                }
            }

            return;
        }

        private void MoveDirectoryContentToDeletion(string dir) {
            foreach (var file in Directory.GetFiles(dir, "*.*", SearchOption.AllDirectories)) {
                try {
                    File.Move(file, Path.Combine(Constants.DeletionFolder, Path.GetRandomFileName()));
                } catch (Exception ex) {
                    Logger.Error($"Failed to move file to deletion location {file} due to {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Find the plugin dll by the guid in the root extension folder if it exists by looking up the GuidAttribute
        /// </summary>
        /// <param name="manifest"></param>
        /// <returns></returns>
        private string FindFileInRootFolderByGuid(IPluginManifest manifest) {
            foreach (var file in Directory.GetFiles(Constants.UserExtensionsFolder, "*.dll", SearchOption.AllDirectories)) {
                try {
                    var metaData = PluginAssemblyReader.GrabPluginMetaData(file);

                    metaData.TryGetValue(nameof(GuidAttribute), out var id);
                    if (id == manifest.Identifier) {
                        return file;
                    }
                } catch (Exception) {
                }
            }
            return string.Empty;
        }

        private bool ValidateChecksum(string tempFile, IPluginManifest manifest) {
            using (var algorithm = GetAlgorithm(manifest)) {
                using (var stream = File.OpenRead(tempFile)) {
                    var fileChecksum = BitConverter.ToString(algorithm.ComputeHash(stream)).Replace("-", string.Empty);
                    return fileChecksum.ToLower() == manifest.Installer.Checksum.ToLower();
                }
            }
        }

        private HashAlgorithm GetAlgorithm(IPluginManifest manifest) {
            switch (manifest.Installer.ChecksumType) {
                case ManifestDefinition.InstallerChecksum.MD5:
                    return MD5.Create();

                case ManifestDefinition.InstallerChecksum.SHA1:
                    return SHA1.Create();

                case ManifestDefinition.InstallerChecksum.SHA256:
                default:
                    return SHA256.Create();
            }
        }

        private async Task InstallDLL(IPluginManifest manifest, string sourceFile) {
            var name = Path.GetFileName(sourceFile);
            var destination = Path.Combine(GetDestinationFolderFromManifest(manifest), name);
            Logger.Info($"Installing plugin {manifest.Name} - moving DLL to {destination}");

            using (Stream source = File.Open(sourceFile, FileMode.Open)) {
                if (!Directory.Exists(Path.GetDirectoryName(destination))) {
                    Directory.CreateDirectory(Path.GetDirectoryName(destination));
                }

                using (Stream destinationStream = File.Create(destination)) {
                    await source.CopyToAsync(destinationStream);
                }
            }
        }

        private string GetDestinationFolderFromManifest(IPluginManifest manifest) {
            return Path.Combine(Constants.UserExtensionsFolder, CoreUtil.ReplaceAllInvalidFilenameChars(manifest.Name));
        }

        private Task InstallArchive(IPluginManifest manifest, string tempFile) {
            var destination = GetDestinationFolderFromManifest(manifest);
            Logger.Info($"Installing plugin {manifest.Name} - extracting archive to {destination}");

            using (ZipArchive archive = ZipFile.Open(tempFile, ZipArchiveMode.Read)) {
                archive.ExtractToDirectory(destination, true);
            }
            return Task.CompletedTask;
        }

        private Task InstallExe(IPluginManifest manifest, string tempFile) {
            throw new NotImplementedException();
        }

        private Task InstallMSI(IPluginManifest manifest, string tempFile) {
            throw new NotImplementedException();
        }
    }

    public static class ZipArchiveExtensions {

        public static void ExtractToDirectory(this ZipArchive archive, string destinationDirectoryName, bool overwrite) {
            if (!overwrite) {
                archive.ExtractToDirectory(destinationDirectoryName);
                return;
            }

            DirectoryInfo di = Directory.CreateDirectory(destinationDirectoryName);
            string destinationDirectoryFullPath = di.FullName;

            foreach (ZipArchiveEntry file in archive.Entries) {
                string completeFileName = Path.GetFullPath(Path.Combine(destinationDirectoryFullPath, file.FullName));

                if (!completeFileName.StartsWith(destinationDirectoryFullPath, StringComparison.OrdinalIgnoreCase)) {
                    throw new IOException("Trying to extract file outside of destination directory. See this link for more info: https://snyk.io/research/zip-slip-vulnerability");
                }

                if (file.Name == "") {// Assuming Empty for Directory
                    Directory.CreateDirectory(Path.GetDirectoryName(completeFileName));
                    continue;
                }
                if (!Directory.Exists(Path.GetDirectoryName(completeFileName))) {
                    Directory.CreateDirectory(Path.GetDirectoryName(completeFileName));
                }
                file.ExtractToFile(completeFileName, true);
            }
        }
    }
}
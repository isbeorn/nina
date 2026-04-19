#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using FluentAssertions;
using NINA.Core.Utility;
using NINA.Plugin;
using NINA.Plugin.Interfaces;
using NINA.Plugin.ManifestDefinition;
using NUnit.Framework;
using System.IO.Compression;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace NINA.Test.Plugin {

    [TestFixture]
    public class PluginInstallerTest {

        /// <summary>
        /// Verifies that installer checksum validation accepts the supported algorithms used by
        /// repository manifests before plugin payloads are staged for installation.
        /// </summary>
        [Test]
        [TestCase(InstallerChecksum.MD5)]
        [TestCase(InstallerChecksum.SHA1)]
        [TestCase(InstallerChecksum.SHA256)]
        public void ValidateChecksum_AcceptsSupportedManifestAlgorithms(InstallerChecksum checksumType) {
            string tempFile = CreateTempPayloadFile();
            try {
                IPluginManifest manifest = CreateManifest(checksumType, ComputeChecksum(tempFile, checksumType));
                var installer = new PluginInstaller();

                bool result = InvokeValidateChecksum(installer, tempFile, manifest);

                result.Should().BeTrue();
            } finally {
                File.Delete(tempFile);
            }
        }

        /// <summary>
        /// Verifies that installer checksum validation rejects payload bytes that do not match the
        /// manifest hash, which protects plugin installation from corrupted downloads.
        /// </summary>
        [Test]
        public void ValidateChecksum_RejectsMismatchedHash() {
            string tempFile = CreateTempPayloadFile();
            try {
                IPluginManifest manifest = CreateManifest(InstallerChecksum.SHA256, new string('0', 64));
                var installer = new PluginInstaller();

                bool result = InvokeValidateChecksum(installer, tempFile, manifest);

                result.Should().BeFalse();
            } finally {
                File.Delete(tempFile);
            }
        }

        /// <summary>
        /// Verifies the end-to-end DLL installation path: download, filename selection from the
        /// response header, checksum validation, and copy into the version-scoped plugin folder.
        /// </summary>
        [Test]
        public async Task Install_DownloadsDllPayloadAndStagesItByManifestName() {
            byte[] payload = Encoding.UTF8.GetBytes("downloaded dll payload");
            string pluginName = "Loopback Download Planner " + Guid.NewGuid().ToString("N");
            using var server = new LoopbackHttpServer(payload, "application/octet-stream", "LoopbackPlanner.dll");
            IPluginManifest manifest = CreateManifest(pluginName, server.Url + "/download", InstallerType.DLL, InstallerChecksum.SHA256, ComputeChecksum(payload, InstallerChecksum.SHA256));
            string destinationFolder = GetDestinationFolder(manifest);
            try {
                await new PluginInstaller().Install(manifest, false, CancellationToken.None);

                string destinationFile = Path.Combine(destinationFolder, "LoopbackPlanner.dll");
                File.Exists(destinationFile).Should().BeTrue();
                File.ReadAllText(destinationFile).Should().Be("downloaded dll payload");
            } finally {
                DeleteDirectoryIfExists(destinationFolder);
            }
        }

        /// <summary>
        /// Verifies the end-to-end archive installation path: download, checksum validation, and
        /// extraction of plugin DLL plus dependency files into the plugin folder.
        /// </summary>
        [Test]
        public async Task Install_DownloadsArchivePayloadAndExtractsIt() {
            byte[] payload = CreateArchivePayload(new Dictionary<string, string> {
                ["ArchivePlugin.dll"] = "archive plugin payload",
                ["Dependencies/ArchiveDependency.dll"] = "archive dependency payload"
            });
            string pluginName = "Loopback Archive Planner " + Guid.NewGuid().ToString("N");
            using var server = new LoopbackHttpServer(payload, "application/zip", "LoopbackArchive.zip");
            IPluginManifest manifest = CreateManifest(pluginName, server.Url + "/download", InstallerType.ARCHIVE, InstallerChecksum.SHA256, ComputeChecksum(payload, InstallerChecksum.SHA256));
            string destinationFolder = GetDestinationFolder(manifest);
            try {
                await new PluginInstaller().Install(manifest, false, CancellationToken.None);

                File.ReadAllText(Path.Combine(destinationFolder, "ArchivePlugin.dll")).Should().Be("archive plugin payload");
                File.ReadAllText(Path.Combine(destinationFolder, "Dependencies", "ArchiveDependency.dll")).Should().Be("archive dependency payload");
            } finally {
                DeleteDirectoryIfExists(destinationFolder);
            }
        }

        /// <summary>
        /// Verifies that the install workflow stops after download when the payload checksum does
        /// not match the manifest and therefore never stages an untrusted plugin file.
        /// </summary>
        [Test]
        public async Task Install_RejectsDownloadedPayloadWithMismatchedChecksum() {
            byte[] payload = Encoding.UTF8.GetBytes("tampered dll payload");
            string pluginName = "Loopback Tampered Planner " + Guid.NewGuid().ToString("N");
            using var server = new LoopbackHttpServer(payload, "application/octet-stream", "TamperedPlanner.dll");
            IPluginManifest manifest = CreateManifest(pluginName, server.Url + "/download", InstallerType.DLL, InstallerChecksum.SHA256, new string('0', 64));
            string destinationFolder = GetDestinationFolder(manifest);
            try {
                Func<Task> act = async () => await new PluginInstaller().Install(manifest, false, CancellationToken.None);

                await act.Should().ThrowAsync<Exception>().WithMessage("Checksum of file*does not match expected checksum*");
                Directory.Exists(destinationFolder).Should().BeFalse();
            } finally {
                DeleteDirectoryIfExists(destinationFolder);
            }
        }

        /// <summary>
        /// Verifies that a downloaded DLL payload is copied into the version-scoped plugin folder
        /// using the same sanitized plugin-name convention that uninstall later relies on.
        /// </summary>
        [Test]
        public async Task InstallDLL_CopiesPayloadIntoSanitizedPluginFolder() {
            string pluginName = "Installer Safety DLL " + Guid.NewGuid().ToString("N") + "/Unsafe";
            IPluginManifest manifest = CreateManifest(pluginName, InstallerChecksum.SHA256, new string('a', 64));
            string sourceFile = CreateTempPayloadFile("InstallerSafetyFixture.dll", "dll payload");
            string destinationFolder = GetDestinationFolder(manifest);
            try {
                await InvokeInstallDLL(new PluginInstaller(), manifest, sourceFile);

                string destinationFile = Path.Combine(destinationFolder, Path.GetFileName(sourceFile));
                File.Exists(destinationFile).Should().BeTrue();
                File.ReadAllText(destinationFile).Should().Be("dll payload");
            } finally {
                File.Delete(sourceFile);
                DeleteDirectoryIfExists(destinationFolder);
            }
        }

        /// <summary>
        /// Verifies that archive payloads are extracted into the same version-scoped destination
        /// folder used by DLL installs, including nested dependency files.
        /// </summary>
        [Test]
        public async Task InstallArchive_ExtractsPayloadIntoSanitizedPluginFolder() {
            string pluginName = "Installer Safety Archive " + Guid.NewGuid().ToString("N") + "/Unsafe";
            IPluginManifest manifest = CreateManifest(pluginName, InstallerChecksum.SHA256, new string('a', 64));
            string tempRoot = CreateTempDirectory();
            string zipPath = Path.Combine(tempRoot, "archive.zip");
            string destinationFolder = GetDestinationFolder(manifest);
            try {
                using (ZipArchive archive = ZipFile.Open(zipPath, ZipArchiveMode.Create)) {
                    WriteArchiveEntry(archive, "Plugin.dll", "plugin payload");
                    WriteArchiveEntry(archive, "Dependencies/Dependency.dll", "dependency payload");
                }

                await InvokeInstallArchive(new PluginInstaller(), manifest, zipPath);

                File.ReadAllText(Path.Combine(destinationFolder, "Plugin.dll")).Should().Be("plugin payload");
                File.ReadAllText(Path.Combine(destinationFolder, "Dependencies", "Dependency.dll")).Should().Be("dependency payload");
            } finally {
                Directory.Delete(tempRoot, true);
                DeleteDirectoryIfExists(destinationFolder);
            }
        }

        /// <summary>
        /// Verifies that uninstall moves files from a conventionally named plugin folder into the
        /// version-scoped deletion area so running plugin DLLs can be removed on the next startup.
        /// </summary>
        [Test]
        public void Uninstall_MovesConventionFolderContentsIntoDeletionArea() {
            string pluginName = "Installer Safety Uninstall " + Guid.NewGuid().ToString("N");
            IPluginManifest manifest = CreateManifest(pluginName, InstallerChecksum.SHA256, new string('a', 64));
            string destinationFolder = GetDestinationFolder(manifest);
            string pluginFile = Path.Combine(destinationFolder, "Plugin.dll");
            HashSet<string> deletionFilesBefore = GetDeletionFiles();
            try {
                Directory.CreateDirectory(destinationFolder);
                File.WriteAllText(pluginFile, "pending deletion");

                new PluginInstaller().Uninstall(manifest);

                File.Exists(pluginFile).Should().BeFalse();
                HashSet<string> deletionFilesAfter = GetDeletionFiles();
                List<string> newDeletionFiles = deletionFilesAfter.Except(deletionFilesBefore).ToList();
                newDeletionFiles.Should().ContainSingle();
                File.ReadAllText(newDeletionFiles.Single()).Should().Be("pending deletion");
            } finally {
                DeleteDirectoryIfExists(destinationFolder);
                DeleteNewDeletionFiles(deletionFilesBefore);
            }
        }

        /// <summary>
        /// Verifies that archive extraction overwrites an existing plugin file when installation is
        /// applying an update to an already staged plugin folder.
        /// </summary>
        [Test]
        public void ExtractToDirectory_OverwritesExistingPluginFiles() {
            string tempRoot = CreateTempDirectory();
            string zipPath = Path.Combine(tempRoot, "plugin.zip");
            string destination = Path.Combine(tempRoot, "destination");
            try {
                Directory.CreateDirectory(destination);
                File.WriteAllText(Path.Combine(destination, "Plugin.dll"), "old payload");
                using (ZipArchive archive = ZipFile.Open(zipPath, ZipArchiveMode.Create)) {
                    ZipArchiveEntry entry = archive.CreateEntry("Plugin.dll");
                    using StreamWriter writer = new StreamWriter(entry.Open(), Encoding.UTF8);
                    writer.Write("new payload");
                }

                using (ZipArchive archive = ZipFile.OpenRead(zipPath)) {
                    ZipArchiveExtensions.ExtractToDirectory(archive, destination, true);
                }

                File.ReadAllText(Path.Combine(destination, "Plugin.dll")).Should().Be("new payload");
            } finally {
                Directory.Delete(tempRoot, true);
            }
        }

        /// <summary>
        /// Verifies that archive extraction with overwrite disabled delegates to the framework
        /// extraction behavior used for first-time plugin installs.
        /// </summary>
        [Test]
        public void ExtractToDirectory_WithoutOverwriteExtractsNewPluginFiles() {
            string tempRoot = CreateTempDirectory();
            string zipPath = Path.Combine(tempRoot, "plugin.zip");
            string destination = Path.Combine(tempRoot, "destination");
            try {
                using (ZipArchive archive = ZipFile.Open(zipPath, ZipArchiveMode.Create)) {
                    WriteArchiveEntry(archive, "Plugin.dll", "new payload");
                }

                using (ZipArchive archive = ZipFile.OpenRead(zipPath)) {
                    ZipArchiveExtensions.ExtractToDirectory(archive, destination, false);
                }

                File.ReadAllText(Path.Combine(destination, "Plugin.dll")).Should().Be("new payload");
            } finally {
                Directory.Delete(tempRoot, true);
            }
        }

        /// <summary>
        /// Verifies that explicit directory entries in plugin archives create the expected folder
        /// structure before nested dependency files are extracted.
        /// </summary>
        [Test]
        public void ExtractToDirectory_CreatesExplicitArchiveDirectories() {
            string tempRoot = CreateTempDirectory();
            string zipPath = Path.Combine(tempRoot, "plugin.zip");
            string destination = Path.Combine(tempRoot, "destination");
            try {
                using (ZipArchive archive = ZipFile.Open(zipPath, ZipArchiveMode.Create)) {
                    archive.CreateEntry("Dependencies/");
                    WriteArchiveEntry(archive, "Dependencies/Dependency.dll", "dependency payload");
                }

                using (ZipArchive archive = ZipFile.OpenRead(zipPath)) {
                    ZipArchiveExtensions.ExtractToDirectory(archive, destination, true);
                }

                Directory.Exists(Path.Combine(destination, "Dependencies")).Should().BeTrue();
                File.ReadAllText(Path.Combine(destination, "Dependencies", "Dependency.dll")).Should().Be("dependency payload");
            } finally {
                Directory.Delete(tempRoot, true);
            }
        }

        /// <summary>
        /// Verifies that archive extraction rejects entries attempting to escape the destination
        /// folder, preventing a malicious plugin archive from writing outside the plugin directory.
        /// </summary>
        [Test]
        public void ExtractToDirectory_RejectsZipSlipTraversalEntry() {
            string tempRoot = CreateTempDirectory();
            string zipPath = Path.Combine(tempRoot, "plugin.zip");
            string destination = Path.Combine(tempRoot, "destination");
            string outsideFile = Path.Combine(tempRoot, "outside.txt");
            try {
                using (ZipArchive archive = ZipFile.Open(zipPath, ZipArchiveMode.Create)) {
                    ZipArchiveEntry entry = archive.CreateEntry("../outside.txt");
                    using StreamWriter writer = new StreamWriter(entry.Open(), Encoding.UTF8);
                    writer.Write("escape");
                }

                using ZipArchive readArchive = ZipFile.OpenRead(zipPath);
                Action act = () => ZipArchiveExtensions.ExtractToDirectory(readArchive, destination, true);

                act.Should().Throw<IOException>().WithMessage("Trying to extract file outside of destination directory*");
                File.Exists(outsideFile).Should().BeFalse();
            } finally {
                Directory.Delete(tempRoot, true);
            }
        }

        private static string CreateTempPayloadFile() {
            return CreateTempPayloadFile("NINA.PluginInstallerTest." + Guid.NewGuid().ToString("N") + ".bin", "NINA plugin installer checksum fixture");
        }

        private static string CreateTempPayloadFile(string fileName, string content) {
            string tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + "." + fileName);
            File.WriteAllText(tempFile, content, Encoding.UTF8);
            return tempFile;
        }

        private static string CreateTempDirectory() {
            string directory = Path.Combine(Path.GetTempPath(), "NINA.PluginInstallerTest." + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            return directory;
        }

        private static string ComputeChecksum(string file, InstallerChecksum checksumType) {
            using HashAlgorithm algorithm = checksumType switch {
                InstallerChecksum.MD5 => MD5.Create(),
                InstallerChecksum.SHA1 => SHA1.Create(),
                _ => SHA256.Create()
            };
            using FileStream stream = File.OpenRead(file);
            return BitConverter.ToString(algorithm.ComputeHash(stream)).Replace("-", string.Empty).ToLower();
        }

        private static string ComputeChecksum(byte[] payload, InstallerChecksum checksumType) {
            using HashAlgorithm algorithm = checksumType switch {
                InstallerChecksum.MD5 => MD5.Create(),
                InstallerChecksum.SHA1 => SHA1.Create(),
                _ => SHA256.Create()
            };
            return BitConverter.ToString(algorithm.ComputeHash(payload)).Replace("-", string.Empty).ToLower();
        }

        private static byte[] CreateArchivePayload(IDictionary<string, string> entries) {
            using var stream = new MemoryStream();
            using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, true)) {
                foreach (KeyValuePair<string, string> entry in entries) {
                    WriteArchiveEntry(archive, entry.Key, entry.Value);
                }
            }
            return stream.ToArray();
        }

        private static IPluginManifest CreateManifest(InstallerChecksum checksumType, string checksum) {
            return CreateManifest("Installer Safety Fixture", checksumType, checksum);
        }

        private static IPluginManifest CreateManifest(string name, InstallerChecksum checksumType, string checksum) {
            return CreateManifest(name, "https://example.invalid/InstallerSafetyFixture.dll", InstallerType.DLL, checksumType, checksum);
        }

        private static IPluginManifest CreateManifest(string name, string url, InstallerType installerType, InstallerChecksum checksumType, string checksum) {
            return new PluginManifest {
                Identifier = "7e359eef-b8e4-4870-9d59-532fc2963f81",
                Name = name,
                Version = new PluginVersion("1.0.0.0"),
                MinimumApplicationVersion = new PluginVersion("3.0.0.0"),
                Descriptions = new PluginDescription {
                    ShortDescription = "Installer safety test fixture"
                },
                Installer = new PluginInstallerDetails {
                    URL = url,
                    Type = installerType,
                    Checksum = checksum,
                    ChecksumType = checksumType
                }
            };
        }

        private static bool InvokeValidateChecksum(PluginInstaller installer, string tempFile, IPluginManifest manifest) {
            MethodInfo method = typeof(PluginInstaller).GetMethod("ValidateChecksum", BindingFlags.Instance | BindingFlags.NonPublic)!;
            return (bool)method.Invoke(installer, new object[] { tempFile, manifest })!;
        }

        private static async Task InvokeInstallDLL(PluginInstaller installer, IPluginManifest manifest, string sourceFile) {
            MethodInfo method = typeof(PluginInstaller).GetMethod("InstallDLL", BindingFlags.Instance | BindingFlags.NonPublic)!;
            var task = (Task)method.Invoke(installer, new object[] { manifest, sourceFile })!;
            await task;
        }

        private static async Task InvokeInstallArchive(PluginInstaller installer, IPluginManifest manifest, string archiveFile) {
            MethodInfo method = typeof(PluginInstaller).GetMethod("InstallArchive", BindingFlags.Instance | BindingFlags.NonPublic)!;
            var task = (Task)method.Invoke(installer, new object[] { manifest, archiveFile })!;
            await task;
        }

        private static string GetDestinationFolder(IPluginManifest manifest) {
            return Path.Combine(Constants.UserExtensionsFolder, CoreUtil.ReplaceAllInvalidFilenameChars(manifest.Name));
        }

        private static HashSet<string> GetDeletionFiles() {
            if (!Directory.Exists(Constants.DeletionFolder)) {
                return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }
            return Directory.GetFiles(Constants.DeletionFolder).ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        private static void DeleteNewDeletionFiles(HashSet<string> deletionFilesBefore) {
            foreach (string file in GetDeletionFiles().Except(deletionFilesBefore).ToList()) {
                File.Delete(file);
            }
        }

        private static void DeleteDirectoryIfExists(string directory) {
            if (Directory.Exists(directory)) {
                Directory.Delete(directory, true);
            }
        }

        private static void WriteArchiveEntry(ZipArchive archive, string path, string content) {
            ZipArchiveEntry entry = archive.CreateEntry(path);
            using StreamWriter writer = new StreamWriter(entry.Open(), Encoding.UTF8);
            writer.Write(content);
        }
    }
}

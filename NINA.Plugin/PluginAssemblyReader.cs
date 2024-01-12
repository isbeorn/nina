#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;

namespace NINA.Plugin {

    public class PluginAssemblyReader {

        public static List<string> GrabAssemblyReferences(string assemblyPath) {
            var paths = new List<string>();
            using var fs = new FileStream(assemblyPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var peReader = new PEReader(fs);

            MetadataReader mr = peReader.GetMetadataReader();

            foreach (var arh in mr.AssemblyReferences) {
                var assemblyRef = mr.GetAssemblyReference(arh);

                string name = mr.GetString(assemblyRef.Name);
                paths.Add(name);
            }

            return paths;
        }

        public static Dictionary<string, string> GrabPluginMetaData(string assemblyPath) {
            var metadata = new Dictionary<string, string>();
            using var fs = new FileStream(assemblyPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var peReader = new PEReader(fs);

            MetadataReader mr = peReader.GetMetadataReader();

            foreach (var attrh in mr.CustomAttributes) {
                try {
                    var attr = mr.GetCustomAttribute(attrh);
                    if (attr.Constructor.Kind == HandleKind.MemberReference) {
                        var ctor = mr.GetMemberReference((MemberReferenceHandle)attr.Constructor);
                        var attrType = mr.GetTypeReference((TypeReferenceHandle)ctor.Parent);
                        var attrName = mr.GetString(attrType.Name);

                        var attrBlobReader = mr.GetBlobReader(attr.Value);

                        if (attrName == nameof(AssemblyTitleAttribute)
                            || attrName == nameof(AssemblyDescriptionAttribute)
                            || attrName == nameof(AssemblyConfigurationAttribute)
                            || attrName == nameof(AssemblyCompanyAttribute)
                            || attrName == nameof(AssemblyProductAttribute)
                            || attrName == nameof(AssemblyCopyrightAttribute)
                            || attrName == nameof(AssemblyTrademarkAttribute)
                            || attrName == nameof(AssemblyCultureAttribute)
                            || attrName == nameof(GuidAttribute)
                            || attrName == nameof(AssemblyVersionAttribute)
                            || attrName == nameof(AssemblyFileVersionAttribute)) {
                            _ = attrBlobReader.ReadSerializedString(); //Ignore Header
                            var name = attrBlobReader.ReadSerializedString();
                            metadata.Add(attrName, name);
                        }
                        if (attrName == nameof(AssemblyMetadataAttribute)) {
                            _ = attrBlobReader.ReadSerializedString(); //Ignore Header
                            var key = attrBlobReader.ReadSerializedString();
                            var value = attrBlobReader.ReadSerializedString();
                            metadata.Add(key, value);
                        }
                    }
                } catch { }
            }
            return metadata;
        }
    }
}
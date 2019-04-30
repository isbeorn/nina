#region "copyright"

/*
    Copyright © 2016 - 2019 Stefan Berg <isbeorn86+NINA@googlemail.com>

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    N.I.N.A. is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    N.I.N.A. is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with N.I.N.A..  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion "copyright"

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace NINALocaleManager {

    internal class Locale {

        public Locale(string filePath) {
            Entries = new ObservableCollection<LocaleEntry>();

            this.filePath = filePath;

            Name = Path.GetFileNameWithoutExtension(filePath);

            var xml = XElement.Load(filePath);

            XNamespace x = "http://schemas.microsoft.com/winfx/2006/xaml";
            XNamespace xmlNS = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";

            var l = new List<LocaleEntry>();
            foreach (var element in xml.Elements()) {
                var key = element.Attribute(x + "Key").Value;
                var preserveWhiteSpace = element.Attribute(XNamespace.Xml + "space")?.Value ?? "default";
                var value = element.Value;
                l.Add(new LocaleEntry() {
                    Key = key,
                    Value = value,
                    Space = preserveWhiteSpace
                });
            }

            Entries = new ObservableCollection<LocaleEntry>(l.OrderBy(item => item.Key));
        }

        internal void Save() {
            XNamespace name = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
            XNamespace x = "http://schemas.microsoft.com/winfx/2006/xaml";
            XNamespace s = "clr-namespace:System;assembly=mscorlib";

            var elem = new XElement(name + "ResourceDictionary");

            var xAttr = new XAttribute(XNamespace.Xmlns + "x", x);
            var sAttr = new XAttribute(XNamespace.Xmlns + "s", s);
            elem.Add(xAttr);
            elem.Add(sAttr);

            var sorted = Entries.OrderBy(item => item.Key);

            foreach (var entry in sorted) {
                var xmlEntry = new XElement(s + "String",
                        new XAttribute(x + "Key", entry.Key),
                        new XAttribute(XNamespace.Xml + "space", entry.Space),
                        entry.Value
                );

                elem.Add(xmlEntry);
            }

            using (var writer = XmlWriter.Create(filePath, new XmlWriterSettings { OmitXmlDeclaration = true, Indent = true })) {
                elem.Save(writer);
            }
        }

        public string Name { get; private set; }

        public ObservableCollection<LocaleEntry> Entries { get; private set; }

        private string filePath;
    }

    internal class LocaleEntry {
        public string Key { get; set; }
        public string Space { get; set; } = "default";
        public string Value { get; set; }
    }
}
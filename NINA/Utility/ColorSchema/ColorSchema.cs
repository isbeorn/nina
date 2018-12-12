#region "copyright"

/*
    Copyright © 2016 - 2018 Stefan Berg <isbeorn86+NINA@googlemail.com>

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
using System.IO;
using System.Runtime.Serialization;
using System.Windows.Media;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace NINA.Utility {

    [XmlRoot("ColorSchemas")]
    public class ColorSchemas {

        public ColorSchemas() {
            Items = new List<ColorSchema>();
        }

        [XmlElement("ColorSchema")]
        public List<ColorSchema> Items { get; set; }

        public static ColorSchemas ReadColorSchemas() {
            ColorSchemas schemas = null;
            var schemafile = System.AppDomain.CurrentDomain.BaseDirectory + "Utility\\ColorSchema\\ColorSchemas.xml";
            if (File.Exists(schemafile)) {
                try {
                    var schemasxml = XElement.Load(schemafile);

                    System.IO.StringReader reader = new System.IO.StringReader(schemasxml.ToString());
                    XmlSerializer xmlSerializer = new XmlSerializer(typeof(ColorSchemas));

                    schemas = (ColorSchemas)xmlSerializer.Deserialize(reader);
                } catch (Exception e) {
                    schemas = new ColorSchemas();
                    Logger.Error("Could not load color schema xml", e);
                }
            } else {
                schemas = new ColorSchemas();
                Logger.Error("Color schema xml not found!", null);
            }

            return schemas;
        }

        public ColorSchema CreateDefaultAltSchema() {
            return new ColorSchema {
                Name = "Dark",
                PrimaryColor = (Color)ColorConverter.ConvertFromString("#FF550C18"),
                SecondaryColor = (Color)ColorConverter.ConvertFromString("#FF1B2A41"),
                BorderColor = (Color)ColorConverter.ConvertFromString("#FF550C18"),
                BackgroundColor = (Color)ColorConverter.ConvertFromString("#FF02010A"),
                SecondaryBackgroundColor = (Color)ColorConverter.ConvertFromString("#FF2d060d"),
                TertiaryBackgroundColor = (Color)ColorConverter.ConvertFromString("#FF0d3956"),//todo
                ButtonBackgroundColor = (Color)ColorConverter.ConvertFromString("#FF550C18"),
                ButtonBackgroundSelectedColor = (Color)ColorConverter.ConvertFromString("#FF96031A"),
                ButtonForegroundColor = (Color)ColorConverter.ConvertFromString("#FF02010A"),
                ButtonForegroundDisabledColor = (Color)ColorConverter.ConvertFromString("#FF443730"),
                NotificationWarningColor = (Color)ColorConverter.ConvertFromString("#FFF5A300"),
                NotificationErrorColor = (Color)ColorConverter.ConvertFromString("#FFDB0606"),
                NotificationWarningTextColor = (Color)ColorConverter.ConvertFromString("#FF02010A"),
                NotificationErrorTextColor = (Color)ColorConverter.ConvertFromString("#FF02010A")
            };
        }

        public ColorSchema CreateDefaultSchema() {
            return new ColorSchema {
                Name = "Light",
                PrimaryColor = (Color)ColorConverter.ConvertFromString("#FF000000"),
                SecondaryColor = (Color)ColorConverter.ConvertFromString("#FF54748C"),
                BorderColor = (Color)ColorConverter.ConvertFromString("#AABCBCBC"),
                BackgroundColor = (Color)ColorConverter.ConvertFromString("#FFFFFFFF"),
                SecondaryBackgroundColor = (Color)ColorConverter.ConvertFromString("#FF0d3956"),
                TertiaryBackgroundColor = (Color)ColorConverter.ConvertFromString("#FF0d3956"),//todo
                ButtonBackgroundColor = (Color)ColorConverter.ConvertFromString("#FF0B3C5D"),
                ButtonBackgroundSelectedColor = (Color)ColorConverter.ConvertFromString("#FF2190DB"),
                ButtonForegroundColor = (Color)ColorConverter.ConvertFromString("#FFFFFFFF"),
                ButtonForegroundDisabledColor = (Color)ColorConverter.ConvertFromString("#FFc5d2db"),
                NotificationWarningColor = (Color)ColorConverter.ConvertFromString("#FFF5A300"),
                NotificationErrorColor = (Color)ColorConverter.ConvertFromString("#FFDB0606"),
                NotificationWarningTextColor = (Color)ColorConverter.ConvertFromString("#FFFFFFFF"),
                NotificationErrorTextColor = (Color)ColorConverter.ConvertFromString("#FFFFFFFF")
            };
        }
    }

    [Serializable()]
    [XmlRoot(ElementName = "ColorSchema")]
    [DataContract]
    public class ColorSchema {

        [XmlAttribute(nameof(Name))]
        [DataMember]
        public String Name { get; set; }

        [XmlElement(Type = typeof(XmlColor))]
        [DataMember]
        public Color PrimaryColor { get; set; }

        [XmlElement(Type = typeof(XmlColor))]
        [DataMember]
        public Color SecondaryColor { get; set; }

        [XmlElement(Type = typeof(XmlColor))]
        [DataMember]
        public Color BorderColor { get; set; }

        [XmlElement(Type = typeof(XmlColor))]
        [DataMember]
        public Color BackgroundColor { get; set; }

        [XmlElement(Type = typeof(XmlColor))]
        [DataMember]
        public Color SecondaryBackgroundColor { get; set; }

        [XmlElement(Type = typeof(XmlColor))]
        [DataMember]
        public Color TertiaryBackgroundColor { get; set; }

        [XmlElement(Type = typeof(XmlColor))]
        [DataMember]
        public Color ButtonBackgroundColor { get; set; }

        [XmlElement(Type = typeof(XmlColor))]
        [DataMember]
        public Color ButtonBackgroundSelectedColor { get; set; }

        [XmlElement(Type = typeof(XmlColor))]
        [DataMember]
        public Color ButtonForegroundColor { get; set; }

        [XmlElement(Type = typeof(XmlColor))]
        [DataMember]
        public Color ButtonForegroundDisabledColor { get; set; }

        [XmlElement(Type = typeof(XmlColor))]
        [DataMember]
        public Color NotificationWarningColor { get; set; }

        [XmlElement(Type = typeof(XmlColor))]
        [DataMember]
        public Color NotificationErrorColor { get; set; }

        [XmlElement(Type = typeof(XmlColor))]
        [DataMember]
        public Color NotificationWarningTextColor { get; set; }

        [XmlElement(Type = typeof(XmlColor))]
        [DataMember]
        public Color NotificationErrorTextColor { get; set; }

        public ColorSchema() {
        }
    }

    public class XmlColor {
        private Color _color = Colors.Black;

        public XmlColor() {
        }

        public XmlColor(Color c) {
            _color = c;
        }

        public Color ToColor() {
            return _color;
        }

        public void FromColor(Color c) {
            _color = c;
        }

        public static implicit operator Color(XmlColor x) {
            return x.ToColor();
        }

        public static implicit operator XmlColor(Color c) {
            return new XmlColor(c);
        }

        [XmlAttribute]
        public string Color {
            get {
                return _color.ToString();
            }
            set {
                try {
                    _color = (Color)ColorConverter.ConvertFromString(value);
                } catch (Exception) {
                    _color = Colors.Black;
                }
            }
        }
    }
}
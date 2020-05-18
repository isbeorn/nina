#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
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
            var schemafile = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "Utility", "ColorSchema", "ColorSchemas.xml");
            if (File.Exists(schemafile)) {
                try {
                    var schemasxml = XElement.Load(schemafile);

                    using (var reader = new System.IO.StringReader(schemasxml.ToString())) {
                        XmlSerializer xmlSerializer = new XmlSerializer(typeof(ColorSchemas));

                        schemas = (ColorSchemas)xmlSerializer.Deserialize(reader);
                    }
                } catch (Exception e) {
                    schemas = new ColorSchemas();
                    Logger.Error("Could not load color schema xml", e);
                }
            } else {
                schemas = new ColorSchemas();
                Logger.Error("Color schema xml not found!");
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
                SecondaryBackgroundColor = (Color)ColorConverter.ConvertFromString("#FF230409"),
                TertiaryBackgroundColor = (Color)ColorConverter.ConvertFromString("#FF2d060d"),
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
                TertiaryBackgroundColor = (Color)ColorConverter.ConvertFromString("#FF114f77"),
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
    public class ColorSchema : BaseINPC {
        private Color primaryColor;
        private Color secondaryColor;
        private Color notificationErrorTextColor;
        private Color notificationWarningTextColor;
        private Color notificationErrorColor;
        private Color notificationWarningColor;
        private Color buttonForegroundDisabledColor;
        private Color buttonForegroundColor;
        private Color buttonBackgroundSelectedColor;
        private Color buttonBackgroundColor;
        private Color tertiaryBackgroundColor;
        private Color secondaryBackgroundColor;
        private Color backgroundColor;
        private Color borderColor;

        [XmlAttribute(nameof(Name))]
        [DataMember]
        public String Name { get; set; }

        [XmlElement(Type = typeof(XmlColor))]
        [DataMember]
        public Color PrimaryColor {
            get {
                return primaryColor;
            }
            set {
                if (primaryColor != value) {
                    primaryColor = value;
                    RaisePropertyChanged();
                }
            }
        }

        [XmlElement(Type = typeof(XmlColor))]
        [DataMember]
        public Color SecondaryColor {
            get {
                return secondaryColor;
            }
            set {
                if (secondaryColor != value) {
                    secondaryColor = value;
                    RaisePropertyChanged();
                }
            }
        }

        [XmlElement(Type = typeof(XmlColor))]
        [DataMember]
        public Color BorderColor {
            get {
                return borderColor;
            }
            set {
                if (borderColor != value) {
                    borderColor = value;
                    RaisePropertyChanged();
                }
            }
        }

        [XmlElement(Type = typeof(XmlColor))]
        [DataMember]
        public Color BackgroundColor {
            get {
                return backgroundColor;
            }
            set {
                if (backgroundColor != value) {
                    backgroundColor = value;
                    RaisePropertyChanged();
                }
            }
        }

        [XmlElement(Type = typeof(XmlColor))]
        [DataMember]
        public Color SecondaryBackgroundColor {
            get {
                return secondaryBackgroundColor;
            }
            set {
                if (secondaryBackgroundColor != value) {
                    secondaryBackgroundColor = value;
                    RaisePropertyChanged();
                }
            }
        }

        [XmlElement(Type = typeof(XmlColor))]
        [DataMember]
        public Color TertiaryBackgroundColor {
            get {
                return tertiaryBackgroundColor;
            }
            set {
                if (tertiaryBackgroundColor != value) {
                    tertiaryBackgroundColor = value;
                    RaisePropertyChanged();
                }
            }
        }

        [XmlElement(Type = typeof(XmlColor))]
        [DataMember]
        public Color ButtonBackgroundColor {
            get {
                return buttonBackgroundColor;
            }
            set {
                if (buttonBackgroundColor != value) {
                    buttonBackgroundColor = value;
                    RaisePropertyChanged();
                }
            }
        }

        [XmlElement(Type = typeof(XmlColor))]
        [DataMember]
        public Color ButtonBackgroundSelectedColor {
            get {
                return buttonBackgroundSelectedColor;
            }
            set {
                if (buttonBackgroundSelectedColor != value) {
                    buttonBackgroundSelectedColor = value;
                    RaisePropertyChanged();
                }
            }
        }

        [XmlElement(Type = typeof(XmlColor))]
        [DataMember]
        public Color ButtonForegroundColor {
            get {
                return buttonForegroundColor;
            }
            set {
                if (buttonForegroundColor != value) {
                    buttonForegroundColor = value;
                    RaisePropertyChanged();
                }
            }
        }

        [XmlElement(Type = typeof(XmlColor))]
        [DataMember]
        public Color ButtonForegroundDisabledColor {
            get {
                return buttonForegroundDisabledColor;
            }
            set {
                if (buttonForegroundDisabledColor != value) {
                    buttonForegroundDisabledColor = value;
                    RaisePropertyChanged();
                }
            }
        }

        [XmlElement(Type = typeof(XmlColor))]
        [DataMember]
        public Color NotificationWarningColor {
            get {
                return notificationWarningColor;
            }
            set {
                if (notificationWarningColor != value) {
                    notificationWarningColor = value;
                    RaisePropertyChanged();
                }
            }
        }

        [XmlElement(Type = typeof(XmlColor))]
        [DataMember]
        public Color NotificationErrorColor {
            get {
                return notificationErrorColor;
            }
            set {
                if (notificationErrorColor != value) {
                    notificationErrorColor = value;
                    RaisePropertyChanged();
                }
            }
        }

        [XmlElement(Type = typeof(XmlColor))]
        [DataMember]
        public Color NotificationWarningTextColor {
            get {
                return notificationWarningTextColor;
            }
            set {
                if (notificationWarningTextColor != value) {
                    notificationWarningTextColor = value;
                    RaisePropertyChanged();
                }
            }
        }

        [XmlElement(Type = typeof(XmlColor))]
        [DataMember]
        public Color NotificationErrorTextColor {
            get {
                return notificationErrorTextColor;
            }
            set {
                if (notificationErrorTextColor != value) {
                    notificationErrorTextColor = value;
                    RaisePropertyChanged();
                }
            }
        }

        [XmlIgnore]
        [IgnoreDataMember]
        public bool IsEditable {
            get => Name == "Custom" || Name == "Alternative Custom";
        }

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

#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Windows.Media;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace NINA.Core.Utility.ColorSchema {

    [XmlRoot("ColorSchemas")]
    public class ColorSchemas {

        public ColorSchemas() {
            Items = new List<ColorSchema>();
        }

        [XmlElement("ColorSchema")]
        public List<ColorSchema> Items { get; set; }

        public static ColorSchemas ReadColorSchemas() {
            ColorSchemas schemas = new ColorSchemas();

            schemas.Items.Add(new ColorSchema() {
                Name = "Light",
                PrimaryColor = (Color)ColorConverter.ConvertFromString("#FF000000"),
                SecondaryColor = (Color)ColorConverter.ConvertFromString("#FF54748c"),
                BorderColor = (Color)ColorConverter.ConvertFromString("#AABCBCBC"),
                BackgroundColor = (Color)ColorConverter.ConvertFromString("#FFFFFFFF"),
                SecondaryBackgroundColor = (Color)ColorConverter.ConvertFromString("#FF0d3956"),
                TertiaryBackgroundColor = (Color)ColorConverter.ConvertFromString("#FF114f77"),
                ButtonBackgroundColor = (Color)ColorConverter.ConvertFromString("#FF0B3C5D"),
                ButtonBackgroundSelectedColor = (Color)ColorConverter.ConvertFromString("#FF2190DB"),
                ButtonForegroundColor = (Color)ColorConverter.ConvertFromString("#FFFFFFFF"),
                ButtonForegroundDisabledColor = (Color)ColorConverter.ConvertFromString("#FFc5d2db"),
                NotificationWarningColor = (Color)ColorConverter.ConvertFromString("#FF5E330B"),
                NotificationErrorColor = (Color)ColorConverter.ConvertFromString("#FF700000"),
                NotificationWarningTextColor = (Color)ColorConverter.ConvertFromString("#FFFFFFFF"),
                NotificationErrorTextColor = (Color)ColorConverter.ConvertFromString("#FFFFFFFF"),
            });
            schemas.Items.Add(new ColorSchema() {
                Name = "Classic",
                PrimaryColor = (Color)ColorConverter.ConvertFromString("#FF000000"),
                SecondaryColor = (Color)ColorConverter.ConvertFromString("#FF54748c"),
                BorderColor = (Color)ColorConverter.ConvertFromString("#FFADB2B5"),
                BackgroundColor = (Color)ColorConverter.ConvertFromString("#FFFFFFFF"),
                SecondaryBackgroundColor = (Color)ColorConverter.ConvertFromString("#FF4f4f4f"),
                TertiaryBackgroundColor = (Color)ColorConverter.ConvertFromString("#FF707070"),
                ButtonBackgroundColor = (Color)ColorConverter.ConvertFromString("#FFDDDDDD"),
                ButtonBackgroundSelectedColor = (Color)ColorConverter.ConvertFromString("#FFb8e0f3"),
                ButtonForegroundColor = (Color)ColorConverter.ConvertFromString("#FF000000"),
                ButtonForegroundDisabledColor = (Color)ColorConverter.ConvertFromString("#FFF4F4F4"),
                NotificationWarningColor = (Color)ColorConverter.ConvertFromString("#FF5E330B"),
                NotificationErrorColor = (Color)ColorConverter.ConvertFromString("#FF700000"),
                NotificationWarningTextColor = (Color)ColorConverter.ConvertFromString("#FF000000"),
                NotificationErrorTextColor = (Color)ColorConverter.ConvertFromString("#FF000000"),
            });
            schemas.Items.Add(new ColorSchema() {
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
                NotificationWarningColor = (Color)ColorConverter.ConvertFromString("#FF5E330B"),
                NotificationErrorColor = (Color)ColorConverter.ConvertFromString("#FF700000"),
                NotificationWarningTextColor = (Color)ColorConverter.ConvertFromString("#FF02010A"),
                NotificationErrorTextColor = (Color)ColorConverter.ConvertFromString("#FF02010A"),
            });
            schemas.Items.Add(new ColorSchema() {
                Name = "Seance",
                PrimaryColor = (Color)ColorConverter.ConvertFromString("#FF000000"),
                SecondaryColor = (Color)ColorConverter.ConvertFromString("#FFBE90D4"),
                BorderColor = (Color)ColorConverter.ConvertFromString("#AAAEA8D3"),
                BackgroundColor = (Color)ColorConverter.ConvertFromString("#FFFFFFFF"),
                SecondaryBackgroundColor = (Color)ColorConverter.ConvertFromString("#FF450d4f"),
                TertiaryBackgroundColor = (Color)ColorConverter.ConvertFromString("#FF5d116b"),
                ButtonBackgroundColor = (Color)ColorConverter.ConvertFromString("#FF663399"),
                ButtonBackgroundSelectedColor = (Color)ColorConverter.ConvertFromString("#FF9A12B3"),
                ButtonForegroundColor = (Color)ColorConverter.ConvertFromString("#FFFFFFFF"),
                ButtonForegroundDisabledColor = (Color)ColorConverter.ConvertFromString("#FFaa69bc"),
                NotificationWarningColor = (Color)ColorConverter.ConvertFromString("#FF5E330B"),
                NotificationErrorColor = (Color)ColorConverter.ConvertFromString("#FF700000"),
                NotificationWarningTextColor = (Color)ColorConverter.ConvertFromString("#FFFFFFFF"),
                NotificationErrorTextColor = (Color)ColorConverter.ConvertFromString("#FFFFFFFF"),
            });
            schemas.Items.Add(new ColorSchema() {
                Name = "Persian",
                PrimaryColor = (Color)ColorConverter.ConvertFromString("#FFECF0F1"),
                SecondaryColor = (Color)ColorConverter.ConvertFromString("#FF9E9E9E"),
                BorderColor = (Color)ColorConverter.ConvertFromString("#AABCBCBC"),
                BackgroundColor = (Color)ColorConverter.ConvertFromString("#FF263238"),
                SecondaryBackgroundColor = (Color)ColorConverter.ConvertFromString("#FF2a2c31"),
                TertiaryBackgroundColor = (Color)ColorConverter.ConvertFromString("#FF2c3438"),
                ButtonBackgroundColor = (Color)ColorConverter.ConvertFromString("#FF00796B"),
                ButtonBackgroundSelectedColor = (Color)ColorConverter.ConvertFromString("#FF00A592"),
                ButtonForegroundColor = (Color)ColorConverter.ConvertFromString("#FFFFFFFF"),
                ButtonForegroundDisabledColor = (Color)ColorConverter.ConvertFromString("#FF9E9E9E"),
                NotificationWarningColor = (Color)ColorConverter.ConvertFromString("#FF5E330B"),
                NotificationErrorColor = (Color)ColorConverter.ConvertFromString("#FF700000"),
                NotificationWarningTextColor = (Color)ColorConverter.ConvertFromString("#FFECF0F1"),
                NotificationErrorTextColor = (Color)ColorConverter.ConvertFromString("#FFECF0F1"),
            });
            schemas.Items.Add(new ColorSchema() {
                Name = "Persian Faint",
                PrimaryColor = (Color)ColorConverter.ConvertFromString("#FFBDC3C7"),
                SecondaryColor = (Color)ColorConverter.ConvertFromString("#FF1D2731"),
                BorderColor = (Color)ColorConverter.ConvertFromString("#AA3F4141"),
                BackgroundColor = (Color)ColorConverter.ConvertFromString("#FF263238"),
                SecondaryBackgroundColor = (Color)ColorConverter.ConvertFromString("#FF2a2c31"),
                TertiaryBackgroundColor = (Color)ColorConverter.ConvertFromString("#FF2c3438"),
                ButtonBackgroundColor = (Color)ColorConverter.ConvertFromString("#FF007063"),
                ButtonBackgroundSelectedColor = (Color)ColorConverter.ConvertFromString("#FF00BCA6"),
                ButtonForegroundColor = (Color)ColorConverter.ConvertFromString("#FFFFFFFF"),
                ButtonForegroundDisabledColor = (Color)ColorConverter.ConvertFromString("#FF9E9E9E"),
                NotificationWarningColor = (Color)ColorConverter.ConvertFromString("#FF5E330B"),
                NotificationErrorColor = (Color)ColorConverter.ConvertFromString("#FF700000"),
                NotificationWarningTextColor = (Color)ColorConverter.ConvertFromString("#FFBDC3C7"),
                NotificationErrorTextColor = (Color)ColorConverter.ConvertFromString("#FFBDC3C7"),
            });
            schemas.Items.Add(new ColorSchema() {
                Name = "High Contrast",
                PrimaryColor = (Color)ColorConverter.ConvertFromString("#FFFFFFFF"),
                SecondaryColor = (Color)ColorConverter.ConvertFromString("#FF00b7b1"),
                BorderColor = (Color)ColorConverter.ConvertFromString("#FFFF9900"),
                BackgroundColor = (Color)ColorConverter.ConvertFromString("#FF000000"),
                SecondaryBackgroundColor = (Color)ColorConverter.ConvertFromString("#FF2F090D"),
                TertiaryBackgroundColor = (Color)ColorConverter.ConvertFromString("#FF191919"),
                ButtonBackgroundColor = (Color)ColorConverter.ConvertFromString("#FFFF0000"),
                ButtonBackgroundSelectedColor = (Color)ColorConverter.ConvertFromString("#FF00b7b1"),
                ButtonForegroundColor = (Color)ColorConverter.ConvertFromString("#FFFFFFFF"),
                ButtonForegroundDisabledColor = (Color)ColorConverter.ConvertFromString("#FF7f7f7f"),
                NotificationWarningColor = (Color)ColorConverter.ConvertFromString("#FF5E330B"),
                NotificationErrorColor = (Color)ColorConverter.ConvertFromString("#FF700000"),
                NotificationWarningTextColor = (Color)ColorConverter.ConvertFromString("#FFFFFFFF"),
                NotificationErrorTextColor = (Color)ColorConverter.ConvertFromString("#FFFFFFFF"),
            });
            schemas.Items.Add(new ColorSchema() {
                Name = "Black Coral",
                PrimaryColor = (Color)ColorConverter.ConvertFromString("#FFDEDEE8"),
                SecondaryColor = (Color)ColorConverter.ConvertFromString("#FF592941"),
                BorderColor = (Color)ColorConverter.ConvertFromString("#FF656F87"),
                BackgroundColor = (Color)ColorConverter.ConvertFromString("#FF545E75"),
                SecondaryBackgroundColor = (Color)ColorConverter.ConvertFromString("#FF393f4c"),
                TertiaryBackgroundColor = (Color)ColorConverter.ConvertFromString("#FF4a5368"),
                ButtonBackgroundColor = (Color)ColorConverter.ConvertFromString("#FF99261a"),
                ButtonBackgroundSelectedColor = (Color)ColorConverter.ConvertFromString("#FFe5a859"),
                ButtonForegroundColor = (Color)ColorConverter.ConvertFromString("#FFF7F7FF"),
                ButtonForegroundDisabledColor = (Color)ColorConverter.ConvertFromString("#FF9E9E9E"),
                NotificationWarningColor = (Color)ColorConverter.ConvertFromString("#FF5E330B"),
                NotificationErrorColor = (Color)ColorConverter.ConvertFromString("#FF700000"),
                NotificationWarningTextColor = (Color)ColorConverter.ConvertFromString("#FFF7F7FF"),
                NotificationErrorTextColor = (Color)ColorConverter.ConvertFromString("#FFF7F7FF"),
            });
            schemas.Items.Add(new ColorSchema() {
                Name = "Arsenic",
                PrimaryColor = (Color)ColorConverter.ConvertFromString("#FFFFFFFF"),
                SecondaryColor = (Color)ColorConverter.ConvertFromString("#FF82A3A1"),
                BorderColor = (Color)ColorConverter.ConvertFromString("#AA495963"),
                BackgroundColor = (Color)ColorConverter.ConvertFromString("#FF394648"),
                SecondaryBackgroundColor = (Color)ColorConverter.ConvertFromString("#FF2a2c31"),
                TertiaryBackgroundColor = (Color)ColorConverter.ConvertFromString("#FF30393a"),
                ButtonBackgroundColor = (Color)ColorConverter.ConvertFromString("#FF406A79"),
                ButtonBackgroundSelectedColor = (Color)ColorConverter.ConvertFromString("#FF64A6BD"),
                ButtonForegroundColor = (Color)ColorConverter.ConvertFromString("#FFF8E9E9"),
                ButtonForegroundDisabledColor = (Color)ColorConverter.ConvertFromString("#FF696D7D"),
                NotificationWarningColor = (Color)ColorConverter.ConvertFromString("#FF5E330B"),
                NotificationErrorColor = (Color)ColorConverter.ConvertFromString("#FF700000"),
                NotificationWarningTextColor = (Color)ColorConverter.ConvertFromString("#FFF8E9E9"),
                NotificationErrorTextColor = (Color)ColorConverter.ConvertFromString("#FFF8E9E9"),
            });
            schemas.Items.Add(new ColorSchema() {
                Name = "Vivid Malachite",
                PrimaryColor = (Color)ColorConverter.ConvertFromString("#FFECF0F1"),
                SecondaryColor = (Color)ColorConverter.ConvertFromString("#FF1b3325"),
                BorderColor = (Color)ColorConverter.ConvertFromString("#FF285238"),
                BackgroundColor = (Color)ColorConverter.ConvertFromString("#FF34403A"),
                SecondaryBackgroundColor = (Color)ColorConverter.ConvertFromString("#FF2a352f"),
                TertiaryBackgroundColor = (Color)ColorConverter.ConvertFromString("#FF415148"),
                ButtonBackgroundColor = (Color)ColorConverter.ConvertFromString("#FF138A36"),
                ButtonBackgroundSelectedColor = (Color)ColorConverter.ConvertFromString("#FF04E824"),
                ButtonForegroundColor = (Color)ColorConverter.ConvertFromString("#FFFFFFFF"),
                ButtonForegroundDisabledColor = (Color)ColorConverter.ConvertFromString("#FF9E9E9E"),
                NotificationWarningColor = (Color)ColorConverter.ConvertFromString("#FF5E330B"),
                NotificationErrorColor = (Color)ColorConverter.ConvertFromString("#FF700000"),
                NotificationWarningTextColor = (Color)ColorConverter.ConvertFromString("#FFBDC3C7"),
                NotificationErrorTextColor = (Color)ColorConverter.ConvertFromString("#FFBDC3C7"),
            });
            schemas.Items.Add(new ColorSchema() {
                Name = "Shark",
                PrimaryColor = (Color)ColorConverter.ConvertFromString("#FFEDEDED"),
                SecondaryColor = (Color)ColorConverter.ConvertFromString("#FFA9AAAC"),
                BorderColor = (Color)ColorConverter.ConvertFromString("#AA3E4146"),
                BackgroundColor = (Color)ColorConverter.ConvertFromString("#FF36393E"),
                SecondaryBackgroundColor = (Color)ColorConverter.ConvertFromString("#FF202225"),
                TertiaryBackgroundColor = (Color)ColorConverter.ConvertFromString("#FF404144"),
                ButtonBackgroundColor = (Color)ColorConverter.ConvertFromString("#FF2a2c31"),
                ButtonBackgroundSelectedColor = (Color)ColorConverter.ConvertFromString("#FF24252A"),
                ButtonForegroundColor = (Color)ColorConverter.ConvertFromString("#FFFFFFFF"),
                ButtonForegroundDisabledColor = (Color)ColorConverter.ConvertFromString("#FF848484"),
                NotificationWarningColor = (Color)ColorConverter.ConvertFromString("#FF5E330B"),
                NotificationErrorColor = (Color)ColorConverter.ConvertFromString("#FF700000"),
                NotificationWarningTextColor = (Color)ColorConverter.ConvertFromString("#FFA9AAAC"),
                NotificationErrorTextColor = (Color)ColorConverter.ConvertFromString("#FFA9AAAC"),
            });
            schemas.Items.Add(new ColorSchema() {
                Name = "Wisteria",
                PrimaryColor = (Color)ColorConverter.ConvertFromString("#FFECF0F1"),
                SecondaryColor = (Color)ColorConverter.ConvertFromString("#FF6644AD"),
                BorderColor = (Color)ColorConverter.ConvertFromString("#AA3F4141"),
                BackgroundColor = (Color)ColorConverter.ConvertFromString("#FF2D0D25"),
                SecondaryBackgroundColor = (Color)ColorConverter.ConvertFromString("#FF230d1e"),
                TertiaryBackgroundColor = (Color)ColorConverter.ConvertFromString("#FF3d1433"),
                ButtonBackgroundColor = (Color)ColorConverter.ConvertFromString("#FF8E44AD"),
                ButtonBackgroundSelectedColor = (Color)ColorConverter.ConvertFromString("#FF9B59B6"),
                ButtonForegroundColor = (Color)ColorConverter.ConvertFromString("#FFECF0F1"),
                ButtonForegroundDisabledColor = (Color)ColorConverter.ConvertFromString("#FFa866c4"),
                NotificationWarningColor = (Color)ColorConverter.ConvertFromString("#FF5E330B"),
                NotificationErrorColor = (Color)ColorConverter.ConvertFromString("#FF700000"),
                NotificationWarningTextColor = (Color)ColorConverter.ConvertFromString("#FFECF0F1"),
                NotificationErrorTextColor = (Color)ColorConverter.ConvertFromString("#FFECF0F1"),
            });
            schemas.Items.Add(new ColorSchema() {
                Name = "Navy",
                PrimaryColor = (Color)ColorConverter.ConvertFromString("#FF6FC3DF"),
                SecondaryColor = (Color)ColorConverter.ConvertFromString("#FFE93B19"),
                BorderColor = (Color)ColorConverter.ConvertFromString("#AA1F3B53"),
                BackgroundColor = (Color)ColorConverter.ConvertFromString("#FF0C141F"),
                SecondaryBackgroundColor = (Color)ColorConverter.ConvertFromString("#FF10233d"),
                TertiaryBackgroundColor = (Color)ColorConverter.ConvertFromString("#FF0f2138"),
                ButtonBackgroundColor = (Color)ColorConverter.ConvertFromString("#FF1C314F"),
                ButtonBackgroundSelectedColor = (Color)ColorConverter.ConvertFromString("#FF488093"),
                ButtonForegroundColor = (Color)ColorConverter.ConvertFromString("#FFBFEEFF"),
                ButtonForegroundDisabledColor = (Color)ColorConverter.ConvertFromString("#FF3a5168"),
                NotificationWarningColor = (Color)ColorConverter.ConvertFromString("#FFE93B19"),
                NotificationErrorColor = (Color)ColorConverter.ConvertFromString("#FFDB0606"),
                NotificationWarningTextColor = (Color)ColorConverter.ConvertFromString("#FF6FC3DF"),
                NotificationErrorTextColor = (Color)ColorConverter.ConvertFromString("#FF6FC3DF"),
            });
            schemas.Items.Add(new ColorSchema() {
                Name = "Dark Nebula",
                PrimaryColor = (Color)ColorConverter.ConvertFromString("#FFF5F4FA"),
                SecondaryColor = (Color)ColorConverter.ConvertFromString("#68808080"),
                BorderColor = (Color)ColorConverter.ConvertFromString("#AA3E4146"),
                BackgroundColor = (Color)ColorConverter.ConvertFromString("#E1191A1C"),
                SecondaryBackgroundColor = (Color)ColorConverter.ConvertFromString("#FF1E2024"),
                TertiaryBackgroundColor = (Color)ColorConverter.ConvertFromString("#FF404144"),
                ButtonBackgroundColor = (Color)ColorConverter.ConvertFromString("#FF34373D"),
                ButtonBackgroundSelectedColor = (Color)ColorConverter.ConvertFromString("#FF696C70"),
                ButtonForegroundColor = (Color)ColorConverter.ConvertFromString("#FF6495ED"),
                ButtonForegroundDisabledColor = (Color)ColorConverter.ConvertFromString("#FF848484"),
                NotificationWarningColor = (Color)ColorConverter.ConvertFromString("#FFBA5E07"),
                NotificationErrorColor = (Color)ColorConverter.ConvertFromString("#FF700000"),
                NotificationWarningTextColor = (Color)ColorConverter.ConvertFromString("#FFF0F8FF"),
                NotificationErrorTextColor = (Color)ColorConverter.ConvertFromString("#FFF0F8FF"),
            });
            schemas.Items.Add(new ColorSchema() {
                Name = "Dichromacy",
                PrimaryColor = (Color)ColorConverter.ConvertFromString("#FFEAF430"),
                SecondaryColor = (Color)ColorConverter.ConvertFromString("#FF808080"),
                BorderColor = (Color)ColorConverter.ConvertFromString("#FF3E4146"),
                BackgroundColor = (Color)ColorConverter.ConvertFromString("#FF191A1C"),
                SecondaryBackgroundColor = (Color)ColorConverter.ConvertFromString("#FF000000"),
                TertiaryBackgroundColor = (Color)ColorConverter.ConvertFromString("#FF404144"),
                ButtonBackgroundColor = (Color)ColorConverter.ConvertFromString("#FF34373D"),
                ButtonBackgroundSelectedColor = (Color)ColorConverter.ConvertFromString("#FF106CE6"),
                ButtonForegroundColor = (Color)ColorConverter.ConvertFromString("#FFEAF430"),
                ButtonForegroundDisabledColor = (Color)ColorConverter.ConvertFromString("#FF848484"),
                NotificationWarningColor = (Color)ColorConverter.ConvertFromString("#FFBA5E07"),
                NotificationErrorColor = (Color)ColorConverter.ConvertFromString("#FF700000"),
                NotificationWarningTextColor = (Color)ColorConverter.ConvertFromString("#FFEAF430"),
                NotificationErrorTextColor = (Color)ColorConverter.ConvertFromString("#FFEAF430"),
            });


            schemas.Items.Add(new ColorSchema {
                Name = "Custom",
                PrimaryColor = (Color)ColorConverter.ConvertFromString("#FFF5F4FA"),
                SecondaryColor = (Color)ColorConverter.ConvertFromString("#68808080"),
                BorderColor = (Color)ColorConverter.ConvertFromString("#AA3E4146"),
                BackgroundColor = (Color)ColorConverter.ConvertFromString("#E1191A1C"),
                SecondaryBackgroundColor = (Color)ColorConverter.ConvertFromString("#FF1E2024"),
                TertiaryBackgroundColor = (Color)ColorConverter.ConvertFromString("#FF404144"),
                ButtonBackgroundColor = (Color)ColorConverter.ConvertFromString("#FF34373D"),
                ButtonBackgroundSelectedColor = (Color)ColorConverter.ConvertFromString("#FF696C70"),
                ButtonForegroundColor = (Color)ColorConverter.ConvertFromString("#FF6495ED"),
                ButtonForegroundDisabledColor = (Color)ColorConverter.ConvertFromString("#FF848484"),
                NotificationWarningColor = (Color)ColorConverter.ConvertFromString("#FFBA5E07"),
                NotificationErrorColor = (Color)ColorConverter.ConvertFromString("#FF700000"),
                NotificationWarningTextColor = (Color)ColorConverter.ConvertFromString("#FFF0F8FF"),
                NotificationErrorTextColor = (Color)ColorConverter.ConvertFromString("#FFF0F8FF")
            });


            schemas.Items.Add(new ColorSchema {
                Name = "Alternative Custom",
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
            });


            return schemas;
        }
    }

    [Serializable()]
    [DataContract]
    public class ColorSchema : SerializableINPC {
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

        [DataMember]
        public String Name { get; set; }

        [DataMember]
        public Color PrimaryColor {
            get => primaryColor;
            set {
                if (primaryColor != value) {
                    primaryColor = value;
                    RaisePropertyChanged();
                }
            }
        }

        [DataMember]
        public Color SecondaryColor {
            get => secondaryColor;
            set {
                if (secondaryColor != value) {
                    secondaryColor = value;
                    RaisePropertyChanged();
                }
            }
        }

        [DataMember]
        public Color BorderColor {
            get => borderColor;
            set {
                if (borderColor != value) {
                    borderColor = value;
                    RaisePropertyChanged();
                }
            }
        }

        [DataMember]
        public Color BackgroundColor {
            get => backgroundColor;
            set {
                if (backgroundColor != value) {
                    backgroundColor = value;
                    RaisePropertyChanged();
                }
            }
        }

        [DataMember]
        public Color SecondaryBackgroundColor {
            get => secondaryBackgroundColor;
            set {
                if (secondaryBackgroundColor != value) {
                    secondaryBackgroundColor = value;
                    RaisePropertyChanged();
                }
            }
        }

        [DataMember]
        public Color TertiaryBackgroundColor {
            get => tertiaryBackgroundColor;
            set {
                if (tertiaryBackgroundColor != value) {
                    tertiaryBackgroundColor = value;
                    RaisePropertyChanged();
                }
            }
        }

        [DataMember]
        public Color ButtonBackgroundColor {
            get => buttonBackgroundColor;
            set {
                if (buttonBackgroundColor != value) {
                    buttonBackgroundColor = value;
                    RaisePropertyChanged();
                }
            }
        }

        [DataMember]
        public Color ButtonBackgroundSelectedColor {
            get => buttonBackgroundSelectedColor;
            set {
                if (buttonBackgroundSelectedColor != value) {
                    buttonBackgroundSelectedColor = value;
                    RaisePropertyChanged();
                }
            }
        }

        [DataMember]
        public Color ButtonForegroundColor {
            get => buttonForegroundColor;
            set {
                if (buttonForegroundColor != value) {
                    buttonForegroundColor = value;
                    RaisePropertyChanged();
                }
            }
        }

        [DataMember]
        public Color ButtonForegroundDisabledColor {
            get => buttonForegroundDisabledColor;
            set {
                if (buttonForegroundDisabledColor != value) {
                    buttonForegroundDisabledColor = value;
                    RaisePropertyChanged();
                }
            }
        }

        [DataMember]
        public Color NotificationWarningColor {
            get => notificationWarningColor;
            set {
                if (notificationWarningColor != value) {
                    notificationWarningColor = value;
                    RaisePropertyChanged();
                }
            }
        }

        [DataMember]
        public Color NotificationErrorColor {
            get => notificationErrorColor;
            set {
                if (notificationErrorColor != value) {
                    notificationErrorColor = value;
                    RaisePropertyChanged();
                }
            }
        }

        [DataMember]
        public Color NotificationWarningTextColor {
            get => notificationWarningTextColor;
            set {
                if (notificationWarningTextColor != value) {
                    notificationWarningTextColor = value;
                    RaisePropertyChanged();
                }
            }
        }

        [DataMember]
        public Color NotificationErrorTextColor {
            get => notificationErrorTextColor;
            set {
                if (notificationErrorTextColor != value) {
                    notificationErrorTextColor = value;
                    RaisePropertyChanged();
                }
            }
        }

        [XmlIgnore]
        [IgnoreDataMember]
        public bool IsEditable => Name == "Custom" || Name == "Alternative Custom";

        public ColorSchema() {
        }
    }
}
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace NINA.Utility
{

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
                    Logger.Error("Could not load color schema xml" + e.Message, e.StackTrace);                    
                }
            } else {
                schemas = new ColorSchemas();
                Logger.Error("Color schema xml not found!");
            }

            schemas.Items.Add(new ColorSchema {
                Name = "Custom",
                PrimaryColor = Properties.Settings.Default.PrimaryColor,
                SecondaryColor = Properties.Settings.Default.SecondaryColor,
                BorderColor = Properties.Settings.Default.BorderColor,
                BackgroundColor = Properties.Settings.Default.BackgroundColor,
                ButtonBackgroundColor = Properties.Settings.Default.ButtonBackgroundColor,
                ButtonBackgroundSelectedColor = Properties.Settings.Default.ButtonBackgroundSelectedColor,
                ButtonForegroundColor = Properties.Settings.Default.ButtonForegroundColor,
                ButtonForegroundDisabledColor = Properties.Settings.Default.ButtonForegroundDisabledColor
            });

            schemas.Items.Add(new ColorSchema {
                Name = "Alternative Custom",
                PrimaryColor = Properties.Settings.Default.AltPrimaryColor,
                SecondaryColor = Properties.Settings.Default.AltSecondaryColor,
                BorderColor = Properties.Settings.Default.AltBorderColor,
                BackgroundColor = Properties.Settings.Default.AltBackgroundColor,
                ButtonBackgroundColor = Properties.Settings.Default.AltButtonBackgroundColor,
                ButtonBackgroundSelectedColor = Properties.Settings.Default.AltButtonBackgroundSelectedColor,
                ButtonForegroundColor = Properties.Settings.Default.AltButtonForegroundColor,
                ButtonForegroundDisabledColor = Properties.Settings.Default.AltButtonForegroundDisabledColor
            });

            return schemas;
        }

        public ColorSchema CreateDefaultAltSchema() {
            return new ColorSchema {
                PrimaryColor = (Color)ColorConverter.ConvertFromString("#FF550C18"),
                SecondaryColor = (Color)ColorConverter.ConvertFromString("#FF1B2A41"),
                BorderColor = (Color)ColorConverter.ConvertFromString("#FF550C18"),
                BackgroundColor = (Color)ColorConverter.ConvertFromString("#FF02010A"),
                ButtonBackgroundColor = (Color)ColorConverter.ConvertFromString("#FF550C18"),
                ButtonBackgroundSelectedColor = (Color)ColorConverter.ConvertFromString("#FF96031A"),
                ButtonForegroundColor = (Color)ColorConverter.ConvertFromString("#FF02010A"),
                ButtonForegroundDisabledColor = (Color)ColorConverter.ConvertFromString("#FF443730")
            };
        }

        public ColorSchema CreateDefaultSchema() {
            return new ColorSchema {
                PrimaryColor = (Color)ColorConverter.ConvertFromString("#FF000000"),
                SecondaryColor = (Color)ColorConverter.ConvertFromString("#FF1D2731"),
                BorderColor = (Color)ColorConverter.ConvertFromString("#AABCBCBC"),
                BackgroundColor = (Color)ColorConverter.ConvertFromString("#FFFFFFFF"),
                ButtonBackgroundColor = (Color)ColorConverter.ConvertFromString("#FF0B3C5D"),
                ButtonBackgroundSelectedColor = (Color)ColorConverter.ConvertFromString("#FF2190DB"),
                ButtonForegroundColor = (Color)ColorConverter.ConvertFromString("#FFFFFFFF"),
                ButtonForegroundDisabledColor = (Color)ColorConverter.ConvertFromString("#FF1D2731")
            };
        }
    }

    [Serializable()]
    [XmlRoot(ElementName = "ColorSchema")]
    public class ColorSchema {

        [XmlAttribute("Name")]
        public String Name { get; set; }
        
        [XmlElement(Type = typeof(XmlColor))]        
        public Color PrimaryColor { get; set; }
        [XmlElement(Type = typeof(XmlColor))]
        public Color SecondaryColor { get; set; }
        [XmlElement(Type = typeof(XmlColor))]
        public Color BorderColor { get; set; }
        [XmlElement(Type = typeof(XmlColor))]
        public Color BackgroundColor { get; set; }
        [XmlElement(Type = typeof(XmlColor))]
        public Color ButtonBackgroundColor { get; set; }
        [XmlElement(Type = typeof(XmlColor))]
        public Color ButtonBackgroundSelectedColor { get; set; }
        [XmlElement(Type = typeof(XmlColor))]
        public Color ButtonForegroundColor { get; set; }
        [XmlElement(Type = typeof(XmlColor))]
        public Color ButtonForegroundDisabledColor { get; set; }

        public ColorSchema() {

        }
    }

    public class XmlColor {
        private Color _color = Colors.Black;

        public XmlColor() { }
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

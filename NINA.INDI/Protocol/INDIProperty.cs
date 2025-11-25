#region "copyright"

/*
    Copyright © 2025 Nico Trost <nico.trost57@gmail.com> and the PI.N.S. contributors

    This file is part of PI 'N' Stars.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.INDI.Enums;
using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace NINA.INDI.Protocol {

    public abstract class INDIProperty {
        public string DeviceName { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string Group { get; set; } = string.Empty;
        public PropertyState State { get; set; }
        public PropertyPermission Permission { get; set; }
        public string Timestamp { get; set; } = string.Empty;

        public abstract XElement ToXml();
    }

    public class INDINumberProperty : INDIProperty {
        public List<INDINumber> Numbers { get; set; } = new();

        public override XElement ToXml() {
            var element = new XElement("newNumberVector",
                new XAttribute("device", DeviceName),
                new XAttribute("name", Name));

            foreach (var number in Numbers) {
                element.Add(new XElement("oneNumber",
                    new XAttribute("name", number.Name),
                    number.Value));
            }

            return element;
        }
    }

    public class INDINumber {
        public string Name { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public double Value { get; set; }
        public double Min { get; set; }
        public double Max { get; set; }
        public double Step { get; set; }
        public string Format { get; set; } = string.Empty;
    }

    public class INDISwitchProperty : INDIProperty {
        public List<INDISwitch> Switches { get; set; } = new();
        public SwitchRule Rule { get; set; }

        public override XElement ToXml() {
            var element = new XElement("newSwitchVector",
                new XAttribute("device", DeviceName),
                new XAttribute("name", Name));

            foreach (var sw in Switches) {
                element.Add(new XElement("oneSwitch",
                    new XAttribute("name", sw.Name),
                    sw.Value ? "On" : "Off"));
            }

            return element;
        }
    }

    public class INDISwitch {
        public string Name { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public bool Value { get; set; }
    }

    public class INDITextProperty : INDIProperty {
        public List<INDIText> Texts { get; set; } = new();

        public override XElement ToXml() {
            var element = new XElement("newTextVector",
                new XAttribute("device", DeviceName),
                new XAttribute("name", Name));

            foreach (var text in Texts) {
                element.Add(new XElement("oneText",
                    new XAttribute("name", text.Name),
                    text.Value));
            }

            return element;
        }
    }

    public class INDIText {
        public string Name { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }

    public class INDIBlobProperty : INDIProperty {
        public List<INDIBlob> Blobs { get; set; } = [];

        public override XElement ToXml() {
            throw new NotImplementedException("BLOB sending not implemented yet");
        }
    }

    public class INDIBlob {
        public string Name { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public byte[] Data { get; set; } = [];
        public string Format { get; set; } = string.Empty;
    }
}

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
using System.Linq;
using System.Xml.Linq;

namespace NINA.INDI.Protocol {
    public static class INDIProtocolParser {
        public static PropertyState ParseState(string state) {
            return state.ToLower() switch {
                "idle" => PropertyState.Idle,
                "ok" => PropertyState.Ok,
                "busy" => PropertyState.Busy,
                "alert" => PropertyState.Alert,
                _ => PropertyState.Idle
            };
        }

        public static PropertyPermission ParsePermission(string perm) {
            return perm.ToLower() switch {
                "ro" => PropertyPermission.ReadOnly,
                "wo" => PropertyPermission.WriteOnly,
                "rw" => PropertyPermission.ReadWrite,
                _ => PropertyPermission.ReadOnly
            };
        }

        public static SwitchRule ParseRule(string rule) {
            return rule.ToLower() switch {
                "oneofmany" => SwitchRule.OneOfMany,
                "atmosone" => SwitchRule.AtMostOne,
                "anyofmany" => SwitchRule.AnyOfMany,
                _ => SwitchRule.OneOfMany
            };
        }

        public static INDINumberProperty ParseDefNumberVector(XElement element) {
            var prop = new INDINumberProperty {
                DeviceName = element.Attribute("device")?.Value ?? string.Empty,
                Name = element.Attribute("name")?.Value ?? string.Empty,
                Label = element.Attribute("label")?.Value ?? string.Empty,
                Group = element.Attribute("group")?.Value ?? string.Empty,
                State = ParseState(element.Attribute("state")?.Value ?? "Idle"),
                Permission = ParsePermission(element.Attribute("perm")?.Value ?? "ro"),
                Timestamp = element.Attribute("timestamp")?.Value ?? string.Empty
            };

            foreach (var defNum in element.Elements("defNumber")) {
                prop.Numbers.Add(new INDINumber {
                    Name = defNum.Attribute("name")?.Value ?? string.Empty,
                    Label = defNum.Attribute("label")?.Value ?? string.Empty,
                    Format = defNum.Attribute("format")?.Value ?? "%g",
                    Min = double.Parse(defNum.Attribute("min")?.Value ?? "0"),
                    Max = double.Parse(defNum.Attribute("max")?.Value ?? "0"),
                    Step = double.Parse(defNum.Attribute("step")?.Value ?? "0"),
                    Value = double.Parse(defNum.Value)
                });
            }

            return prop;
        }

        public static INDISwitchProperty ParseDefSwitchVector(XElement element) {
            var prop = new INDISwitchProperty {
                DeviceName = element.Attribute("device")?.Value ?? string.Empty,
                Name = element.Attribute("name")?.Value ?? string.Empty,
                Label = element.Attribute("label")?.Value ?? string.Empty,
                Group = element.Attribute("group")?.Value ?? string.Empty,
                State = ParseState(element.Attribute("state")?.Value ?? "Idle"),
                Permission = ParsePermission(element.Attribute("perm")?.Value ?? "ro"),
                Rule = ParseRule(element.Attribute("rule")?.Value ?? "OneOfMany"),
                Timestamp = element.Attribute("timestamp")?.Value ?? string.Empty
            };

            foreach (var defSwitch in element.Elements("defSwitch")) {
                var cleanValue = defSwitch.Value.Replace("\r", "").Replace("\n", "").Trim();
                prop.Switches.Add(new INDISwitch {
                    Name = defSwitch.Attribute("name")?.Value ?? string.Empty,
                    Label = defSwitch.Attribute("label")?.Value ?? string.Empty,
                    Value = cleanValue.ToLower() == "on"
                });
            }

            return prop;
        }

        public static INDITextProperty ParseDefTextVector(XElement element) {
            var prop = new INDITextProperty {
                DeviceName = element.Attribute("device")?.Value ?? string.Empty,
                Name = element.Attribute("name")?.Value ?? string.Empty,
                Label = element.Attribute("label")?.Value ?? string.Empty,
                Group = element.Attribute("group")?.Value ?? string.Empty,
                State = ParseState(element.Attribute("state")?.Value ?? "Idle"),
                Permission = ParsePermission(element.Attribute("perm")?.Value ?? "ro"),
                Timestamp = element.Attribute("timestamp")?.Value ?? string.Empty
            };

            foreach (var defText in element.Elements("defText")) {
                prop.Texts.Add(new INDIText {
                    Name = defText.Attribute("name")?.Value ?? string.Empty,
                    Label = defText.Attribute("label")?.Value ?? string.Empty,
                    Value = defText.Value.Replace("\r", "").Replace("\n", "").Trim()
                });
            }

            return prop;
        }

        public static INDIBlobProperty ParseDefBlobVector(XElement element) {
            var prop = new INDIBlobProperty {
                DeviceName = element.Attribute("device")?.Value ?? string.Empty,
                Name = element.Attribute("name")?.Value ?? string.Empty,
                Label = element.Attribute("label")?.Value ?? string.Empty,
                Group = element.Attribute("group")?.Value ?? string.Empty,
                State = ParseState(element.Attribute("state")?.Value ?? "Idle"),
                Permission = ParsePermission(element.Attribute("perm")?.Value ?? "ro"),
                Timestamp = element.Attribute("timestamp")?.Value ?? string.Empty
            };

            foreach (var defBlob in element.Elements("defBLOB")) {
                prop.Blobs.Add(new INDIBlob {
                    Name = defBlob.Attribute("name")?.Value ?? string.Empty,
                    Label = defBlob.Attribute("label")?.Value ?? string.Empty,
                    Format = string.Empty,
                    Data = []
                });
            }

            return prop;
        }

        public static void UpdateNumberProperty(INDINumberProperty prop, XElement element) {
            prop.State = ParseState(element.Attribute("state")?.Value ?? "Idle");
            prop.Timestamp = element.Attribute("timestamp")?.Value ?? string.Empty;

            foreach (var oneNum in element.Elements("oneNumber")) {
                var name = oneNum.Attribute("name")?.Value ?? string.Empty;
                var number = prop.Numbers.FirstOrDefault(n => n.Name == name);
                if (number != null) {
                    number.Value = double.Parse(oneNum.Value);
                }
            }
        }

        public static void UpdateSwitchProperty(INDISwitchProperty prop, XElement element) {
            prop.State = ParseState(element.Attribute("state")?.Value ?? "Idle");
            prop.Timestamp = element.Attribute("timestamp")?.Value ?? string.Empty;

            foreach (var oneSwitch in element.Elements("oneSwitch")) {
                var name = oneSwitch.Attribute("name")?.Value ?? string.Empty;
                var sw = prop.Switches.FirstOrDefault(s => s.Name == name);
                if (sw != null) {
                    var cleanValue = oneSwitch.Value.Replace("\r", "").Replace("\n", "").Trim();
                    sw.Value = cleanValue.ToLower() == "on";
                }
            }
        }

        public static void UpdateTextProperty(INDITextProperty prop, XElement element) {
            prop.State = ParseState(element.Attribute("state")?.Value ?? "Idle");
            prop.Timestamp = element.Attribute("timestamp")?.Value ?? string.Empty;

            foreach (var oneText in element.Elements("oneText")) {
                var name = oneText.Attribute("name")?.Value ?? string.Empty;
                var text = prop.Texts.FirstOrDefault(t => t.Name == name);
                if (text != null) {
                    text.Value = oneText.Value.Replace("\r", "").Replace("\n", "").Trim();
                }
            }
        }

        public static void UpdateBlobProperty(INDIBlobProperty prop, XElement element) {
            prop.State = ParseState(element.Attribute("state")?.Value ?? "Idle");
            prop.Timestamp = element.Attribute("timestamp")?.Value ?? string.Empty;

            foreach (var oneBlob in element.Elements("oneBLOB")) {
                var name = oneBlob.Attribute("name")?.Value ?? string.Empty;
                var format = oneBlob.Attribute("format")?.Value ?? string.Empty;
                var size = int.Parse(oneBlob.Attribute("size")?.Value ?? "0");

                var blob = prop.Blobs.FirstOrDefault(b => b.Name == name);
                if (blob == null) {
                    blob = new INDIBlob { Name = name };
                    prop.Blobs.Add(blob);
                }

                blob.Format = format;

                // Decode base64 BLOB data
                var base64Data = oneBlob.Value.Replace("\r", "").Replace("\n", "").Trim();
                if (!string.IsNullOrEmpty(base64Data)) {
                    try {
                        blob.Data = Convert.FromBase64String(base64Data);
                    } catch {
                        blob.Data = [];
                    }
                }
            }
        }
    }
}

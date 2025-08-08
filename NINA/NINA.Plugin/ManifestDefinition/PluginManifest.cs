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
using NINA.Plugin.Interfaces;
using System;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace NINA.Plugin.ManifestDefinition {

    [Serializable]
    public class PluginManifest : IPluginManifest {

        public static string Schema => @"{
                '$schema': 'http://json-schema.org/draft-07/schema#',
                'type': 'object',
                'properties': {
                    'Name' : {
                        'type': 'string',
                        'maxLength': 50
                    },
                    'Identifier' : {
                        'type': 'string',
                        'format': 'uuid'
                    },
                    'Version' : {
                        'type': 'object',
                        'properties': {
                            'Major': {
                                'anyOf': [
                                    {
                                        'type': 'string',
                                        'pattern': '^\\d+$'
                                    },
                                    {
                                        'type': 'number'
                                    }
                                ]
                            },
                            'Minor': {
                                'anyOf': [
                                    {
                                        'type': 'string',
                                        'pattern': '^\\d+$'
                                    },
                                    {
                                        'type': 'number'
                                    }
                                ]
                            },
                            'Patch': {
                                'anyOf': [
                                    {
                                        'type': 'string',
                                        'pattern': '^\\d+$'
                                    },
                                    {
                                        'type': 'number'
                                    }
                                ]
                            },
                            'Build': {
                                'anyOf': [
                                    {
                                        'type': 'string',
                                        'pattern': '^\\d+$'
                                    },
                                    {
                                        'type': 'number'
                                    }
                                ]
                            }
                        },
                            'required': ['Major', 'Minor', 'Patch', 'Build']
                    },
                    'Author' : {
                        'type': 'string',
                        'maxLength': 256
                    },
                    'Homepage' : {
                        'type': 'string',
                        'format': 'uri-reference'
                    },
                    'Repository' : {
                        'type': 'string',
                        'format': 'uri-reference'
                    },
                    'License' : {
                        'type': 'string',
                        'maxLength': 512
                    },
                    'LicenseURL' : {
                        'type': 'string',
                        'format': 'uri-reference'
                    },
                    'ChangelogURL' : {
                        'type': 'string',
                        'format': 'uri-reference'
                    },
                    'Tags': {
                        'type': 'array',
                        'items': {
                            'type': 'string',
                            'minLength': 1,
                            'maxLength': 30
                        },
                        'maxItems': 20
                    },
                    'MinimumApplicationVersion': {
                        'type': 'object',
                        'properties': {
                            'Major': {
                                'anyOf': [
                                    {
                                        'type': 'string',
                                        'pattern': '^\\d+$'
                                    },
                                    {
                                        'type': 'number'
                                    }
                                ]
                            },
                            'Minor': {
                                'anyOf': [
                                    {
                                        'type': 'string',
                                        'pattern': '^\\d+$'
                                    },
                                    {
                                        'type': 'number'
                                    }
                                ]
                            },
                            'Patch': {
                                'anyOf': [
                                    {
                                        'type': 'string',
                                        'pattern': '^\\d+$'
                                    },
                                    {
                                        'type': 'number'
                                    }
                                ]
                            },
                            'Build': {
                                'anyOf': [
                                    {
                                        'type': 'string',
                                        'pattern': '^\\d+$'
                                    },
                                    {
                                        'type': 'number'
                                    }
                                ]
                            }
                        },
                            'required': ['Major', 'Minor', 'Patch', 'Build']
                    },
                    'Descriptions': {
                        'type': 'object',
                        'properties': {
                            'ShortDescription': {
                                'type': 'string',
                                'maxLength': 256
                            },
                            'LongDescription': {
                                'type': 'string',
                                'maxLength': 10000
                            },
                            'FeaturedImageURL': {
                                'type': 'string',
                                'format': 'uri-reference'
                            },
                            'ScreenshotURL': {
                                'type': 'string',
                                'format': 'uri-reference'
                            },
                            'AltScreenshotURL': {
                                'type': 'string',
                                'format': 'uri-reference'
                            }
                        },
                            'required': ['ShortDescription']
                    },

                    'Installer': {
                        'type': 'object',
                        'properties': {
                            'URL': {
                                'type': 'string',
                                'format': 'uri-reference'
                            },
                            'Checksum': {
                                'type': 'string',
                                'minLength': 32,
                                'maxLength': 64,
                                'pattern': '^[A-Fa-f0-9]{32,64}$'
                            },
                            'ChecksumType': {
                                'type': 'string',
                                'enum': ['MD5','SHA1','SHA256']
                            },
                            'Type': {
                                'type': 'string',
                                'enum': ['DLL', 'ARCHIVE']
                            }
                        },
                            'required': ['URL', 'Checksum', 'ChecksumType', 'Type']
                    }
                },
                'required': ['Name', 'Identifier', 'Version', 'Author', 'Repository', 'License', 'LicenseURL', 'MinimumApplicationVersion', 'Descriptions', 'Installer']
            }";

        [JsonProperty(Required = Required.Always)]
        public string Name { get; set; }

        [JsonProperty(Required = Required.Always)]
        public string Identifier { get; set; }

        [JsonProperty(Required = Required.Always)]
        [JsonConverter(typeof(ConcreteManifestConverter<PluginVersion>))]
        public IPluginVersion Version { get; set; }

        [JsonProperty(Required = Required.Always)]
        public string Author { get; set; }

        [JsonProperty]
        public string Homepage { get; set; }

        [JsonProperty(Required = Required.Always)]
        public string Repository { get; set; }

        [JsonProperty(Required = Required.Always)]
        public string License { get; set; }

        [JsonProperty(Required = Required.Always)]
        public string LicenseURL { get; set; }

        public string ChangelogURL { get; set; }

        [JsonProperty]
        public string[] Tags { get; set; }

        [JsonProperty(Required = Required.Always)]
        [JsonConverter(typeof(ConcreteManifestConverter<PluginVersion>))]
        public IPluginVersion MinimumApplicationVersion { get; set; }

        [JsonProperty(Required = Required.Always)]
        [JsonConverter(typeof(ConcreteManifestConverter<PluginDescription>))]
        public IPluginDescription Descriptions { get; set; }

        [JsonProperty(Required = Required.Always)]
        [JsonConverter(typeof(ConcreteManifestConverter<PluginInstallerDetails>))]
        public IPluginInstallerDetails Installer { get; set; }

        public Task Initialize() {
            return Task.CompletedTask;
        }

        public Task Teardown() {
            return Task.CompletedTask;
        }
    }
}
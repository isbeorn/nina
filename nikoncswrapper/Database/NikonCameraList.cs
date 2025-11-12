using System;
using System.Collections.Generic;
using System.Text;

namespace Nikon {
    public class NikonCameraModel {
        public SensorSpecs Sensor { get; set; }
        public LiveViewConfig LiveView { get; set; }

        public class SensorSpecs {
            public int ResX { get; set; }
            public int ResY { get; set; }

            public double SizeX { get; set; }
            public double SizeY { get; set; }

            public double PixelSizeX { get; set; }
            public double PixelSizeY { get; set; }
        }

        public class LiveViewConfig {
            public uint OnStatus { get; set; }
            public int ImageHeaderSize { get; set; }
        }
    }

    public static class NikonCameraDatabase {
        public static readonly Dictionary<string, NikonCameraModel> CameraList = new Dictionary<string, NikonCameraModel> {

            ["D3"] = new NikonCameraModel {
                Sensor = new NikonCameraModel.SensorSpecs {
                    ResX = 4256, ResY = 2832,
                    SizeX = 36.0, SizeY = 23.9,
                    PixelSizeX = 8.43, PixelSizeY = 8.43
                },
                LiveView = new NikonCameraModel.LiveViewConfig {
                    OnStatus = 1U,
                    ImageHeaderSize = 64
                }
            },

            ["D300"] = new NikonCameraModel {
                Sensor = new NikonCameraModel.SensorSpecs {
                    ResX = 4288, ResY = 2848,
                    SizeX = 23.6, SizeY = 15.8,
                    PixelSizeX = 5.53, PixelSizeY = 5.53
                },
                LiveView = new NikonCameraModel.LiveViewConfig {
                    OnStatus = 1U,
                    ImageHeaderSize = 64
                }
            },

            ["D300S"] = new NikonCameraModel {
                Sensor = new NikonCameraModel.SensorSpecs {
                    ResX = 4288, ResY = 2848,
                    SizeX = 23.6, SizeY = 15.8,
                    PixelSizeX = 5.53, PixelSizeY = 5.53
                },
                LiveView = new NikonCameraModel.LiveViewConfig {
                    OnStatus = 1U,
                    ImageHeaderSize = 64
                }
            },

            ["D700"] = new NikonCameraModel {
                Sensor = new NikonCameraModel.SensorSpecs {
                    ResX = 4256, ResY = 2832,
                    SizeX = 36.0, SizeY = 23.9,
                    PixelSizeX = 8.43, PixelSizeY = 8.43
                },
                LiveView = new NikonCameraModel.LiveViewConfig {
                    OnStatus = 1U,
                    ImageHeaderSize = 64
                }
            },

            ["D3X"] = new NikonCameraModel {
                Sensor = new NikonCameraModel.SensorSpecs {
                    ResX = 6048, ResY = 4032,
                    SizeX = 35.9, SizeY = 24.0,
                    PixelSizeX = 5.94, PixelSizeY = 5.94
                },
                LiveView = new NikonCameraModel.LiveViewConfig {
                    OnStatus = 1U,
                    ImageHeaderSize = 64
                }
            },

            ["D5000"] = new NikonCameraModel {
                Sensor = new NikonCameraModel.SensorSpecs {
                    ResX = 4288, ResY = 2848,
                    SizeX = 23.6, SizeY = 15.8,
                    PixelSizeX = 5.53, PixelSizeY = 5.53
                },
                LiveView = new NikonCameraModel.LiveViewConfig {
                    OnStatus = 1U,
                    ImageHeaderSize = 128
                }
            },

            ["D90"] = new NikonCameraModel {
                Sensor = new NikonCameraModel.SensorSpecs {
                    ResX = 4288, ResY = 2848,
                    SizeX = 23.6, SizeY = 15.8,
                    PixelSizeX = 5.53, PixelSizeY = 5.53
                },
                LiveView = new NikonCameraModel.LiveViewConfig {
                    OnStatus = 1U,
                    ImageHeaderSize = 128
                }
            },

            ["D7000"] = new NikonCameraModel {
                Sensor = new NikonCameraModel.SensorSpecs {
                    ResX = 4928, ResY = 3264,
                    SizeX = 23.6, SizeY = 15.7,
                    PixelSizeX = 4.80, PixelSizeY = 4.80
                },
                LiveView = new NikonCameraModel.LiveViewConfig {
                    OnStatus = 1U,
                    ImageHeaderSize = 384
                }
            },

            ["D5100"] = new NikonCameraModel {
                Sensor = new NikonCameraModel.SensorSpecs {
                    ResX = 4928, ResY = 3264,
                    SizeX = 23.6, SizeY = 15.7,
                    PixelSizeX = 4.80, PixelSizeY = 4.80
                },
                LiveView = new NikonCameraModel.LiveViewConfig {
                    OnStatus = 1U,
                    ImageHeaderSize = 384
                }
            },

            ["D800"] = new NikonCameraModel {
                Sensor = new NikonCameraModel.SensorSpecs {
                    ResX = 7360, ResY = 4912,
                    SizeX = 35.9, SizeY = 24.0,
                    PixelSizeX = 4.88, PixelSizeY = 4.88
                },
                LiveView = new NikonCameraModel.LiveViewConfig {
                    OnStatus = 1U,
                    ImageHeaderSize = 384
                }
            },

            ["D4"] = new NikonCameraModel {
                Sensor = new NikonCameraModel.SensorSpecs {
                    ResX = 4928, ResY = 3280,
                    SizeX = 36.0, SizeY = 23.9,
                    PixelSizeX = 7.29, PixelSizeY = 7.29
                },
                LiveView = new NikonCameraModel.LiveViewConfig {
                    OnStatus = 1U,
                    ImageHeaderSize = 384
                }
            },

            ["D600"] = new NikonCameraModel {
                Sensor = new NikonCameraModel.SensorSpecs {
                    ResX = 6016, ResY = 4016,
                    SizeX = 35.9, SizeY = 24.0,
                    PixelSizeX = 5.97, PixelSizeY = 5.97
                },
                LiveView = new NikonCameraModel.LiveViewConfig {
                    OnStatus = 1U,
                    ImageHeaderSize = 384
                }
            },

            ["D610"] = new NikonCameraModel {
                Sensor = new NikonCameraModel.SensorSpecs {
                    ResX = 6016, ResY = 4016,
                    SizeX = 35.9, SizeY = 24.0,
                    PixelSizeX = 5.97, PixelSizeY = 5.97
                },
                LiveView = new NikonCameraModel.LiveViewConfig {
                    OnStatus = 1U,
                    ImageHeaderSize = 384
                }
            },

            ["D5200"] = new NikonCameraModel {
                Sensor = new NikonCameraModel.SensorSpecs {
                    ResX = 6000, ResY = 4000,
                    SizeX = 23.5, SizeY = 15.6,
                    PixelSizeX = 3.91, PixelSizeY = 3.91
                },
                LiveView = new NikonCameraModel.LiveViewConfig {
                    OnStatus = 1U,
                    ImageHeaderSize = 384
                }
            },

            ["D7100"] = new NikonCameraModel {
                Sensor = new NikonCameraModel.SensorSpecs {
                    ResX = 6000, ResY = 4000,
                    SizeX = 23.5, SizeY = 15.6,
                    PixelSizeX = 3.91, PixelSizeY = 3.91
                },
                LiveView = new NikonCameraModel.LiveViewConfig {
                    OnStatus = 1U,
                    ImageHeaderSize = 384
                }
            },

            ["D5300"] = new NikonCameraModel {
                Sensor = new NikonCameraModel.SensorSpecs {
                    ResX = 6000, ResY = 4000,
                    SizeX = 23.5, SizeY = 15.6,
                    PixelSizeX = 3.91, PixelSizeY = 3.91
                },
                LiveView = new NikonCameraModel.LiveViewConfig {
                    OnStatus = 1U,
                    ImageHeaderSize = 384
                }
            },

            ["DF"] = new NikonCameraModel {
                Sensor = new NikonCameraModel.SensorSpecs {
                    ResX = 4928, ResY = 3280,
                    SizeX = 36.0, SizeY = 23.9,
                    PixelSizeX = 7.29, PixelSizeY = 7.29
                },
                LiveView = new NikonCameraModel.LiveViewConfig {
                    OnStatus = 1U,
                    ImageHeaderSize = 384
                }
            },

            ["D4S"] = new NikonCameraModel {
                Sensor = new NikonCameraModel.SensorSpecs {
                    ResX = 4928, ResY = 3280,
                    SizeX = 36.0, SizeY = 23.9,
                    PixelSizeX = 7.29, PixelSizeY = 7.29
                },
                LiveView = new NikonCameraModel.LiveViewConfig {
                    OnStatus = 1U,
                    ImageHeaderSize = 384
                }
            },

            ["D810"] = new NikonCameraModel {
                Sensor = new NikonCameraModel.SensorSpecs {
                    ResX = 7360, ResY = 4912,
                    SizeX = 35.9, SizeY = 24.0,
                    PixelSizeX = 4.88, PixelSizeY = 4.88
                },
                LiveView = new NikonCameraModel.LiveViewConfig {
                    OnStatus = 1U,
                    ImageHeaderSize = 384
                }
            },

            ["D810A"] = new NikonCameraModel {
                Sensor = new NikonCameraModel.SensorSpecs {
                    ResX = 7360, ResY = 4912,
                    SizeX = 35.9, SizeY = 24.0,
                    PixelSizeX = 4.88, PixelSizeY = 4.88
                },
                LiveView = new NikonCameraModel.LiveViewConfig {
                    OnStatus = 1U,
                    ImageHeaderSize = 384
                }
            },

            ["D750"] = new NikonCameraModel {
                Sensor = new NikonCameraModel.SensorSpecs {
                    ResX = 6016, ResY = 4016,
                    SizeX = 35.9, SizeY = 24.0,
                    PixelSizeX = 5.97, PixelSizeY = 5.97
                },
                LiveView = new NikonCameraModel.LiveViewConfig {
                    OnStatus = 1U,
                    ImageHeaderSize = 384
                }
            },

            ["D5500"] = new NikonCameraModel {
                Sensor = new NikonCameraModel.SensorSpecs {
                    ResX = 6000, ResY = 4000,
                    SizeX = 23.5, SizeY = 15.6,
                    PixelSizeX = 3.91, PixelSizeY = 3.91
                },
                LiveView = new NikonCameraModel.LiveViewConfig {
                    OnStatus = 1U,
                    ImageHeaderSize = 384
                }
            },

            ["D5600"] = new NikonCameraModel {
                Sensor = new NikonCameraModel.SensorSpecs {
                    ResX = 6000, ResY = 4000,
                    SizeX = 23.5, SizeY = 15.6,
                    PixelSizeX = 3.91, PixelSizeY = 3.91
                },
                LiveView = new NikonCameraModel.LiveViewConfig {
                    OnStatus = 1U,
                    ImageHeaderSize = 384
                }
            },

            ["D5600"] = new NikonCameraModel {
                Sensor = new NikonCameraModel.SensorSpecs {
                    ResX = 6000, ResY = 4000,
                    SizeX = 23.5, SizeY = 15.6,
                    PixelSizeX = 3.91, PixelSizeY = 3.91
                },
                LiveView = new NikonCameraModel.LiveViewConfig {
                    OnStatus = 1U,
                    ImageHeaderSize = 384
                }
            },

            ["D7200"] = new NikonCameraModel {
                Sensor = new NikonCameraModel.SensorSpecs {
                    ResX = 6000, ResY = 4000,
                    SizeX = 23.5, SizeY = 15.6,
                    PixelSizeX = 3.91, PixelSizeY = 3.91
                },
                LiveView = new NikonCameraModel.LiveViewConfig {
                    OnStatus = 1U,
                    ImageHeaderSize = 384
                }
            },

            ["D5"] = new NikonCameraModel {
                Sensor = new NikonCameraModel.SensorSpecs {
                    ResX = 5588, ResY = 3712,
                    SizeX = 35.9, SizeY = 23.9,
                    PixelSizeX = 6.44, PixelSizeY = 6.44
                },
                LiveView = new NikonCameraModel.LiveViewConfig {
                    OnStatus = 1U,
                    ImageHeaderSize = 384
                }
            },

            ["1V3"] = new NikonCameraModel {
                Sensor = new NikonCameraModel.SensorSpecs {
                    ResX = 5588, ResY = 3712,
                    SizeX = 35.9, SizeY = 23.9,
                    PixelSizeX = 6.44, PixelSizeY = 6.44
                },
                LiveView = new NikonCameraModel.LiveViewConfig {
                    OnStatus = 1U,
                    ImageHeaderSize = 384
                }
            },

            ["D500"] = new NikonCameraModel {
                Sensor = new NikonCameraModel.SensorSpecs {
                    ResX = 5568, ResY = 3712,
                    SizeX = 23.5, SizeY = 15.7,
                    PixelSizeX = 4.22, PixelSizeY = 4.22
                },
                LiveView = new NikonCameraModel.LiveViewConfig {
                    OnStatus = 1U,
                    ImageHeaderSize = 384
                }
            },

            ["D7500"] = new NikonCameraModel {
                Sensor = new NikonCameraModel.SensorSpecs {
                    ResX = 5568, ResY = 3712,
                    SizeX = 23.5, SizeY = 15.7,
                    PixelSizeX = 4.22, PixelSizeY = 4.22
                },
                LiveView = new NikonCameraModel.LiveViewConfig {
                    OnStatus = 1U,
                    ImageHeaderSize = 384
                }
            },

            ["D850"] = new NikonCameraModel {
                Sensor = new NikonCameraModel.SensorSpecs {
                    ResX = 8256, ResY = 5504,
                    SizeX = 35.9, SizeY = 23.9,
                    PixelSizeX = 4.35, PixelSizeY = 4.35
                },
                LiveView = new NikonCameraModel.LiveViewConfig {
                    OnStatus = 1U,
                    ImageHeaderSize = 384
                }
            },

            ["Z 7"] = new NikonCameraModel {
                Sensor = new NikonCameraModel.SensorSpecs {
                    ResX = 8256, ResY = 5504,
                    SizeX = 35.9, SizeY = 23.9,
                    PixelSizeX = 4.35, PixelSizeY = 4.35
                },
                LiveView = new NikonCameraModel.LiveViewConfig {
                    OnStatus = 3U,
                    ImageHeaderSize = 512
                }
            },

            ["Z 6"] = new NikonCameraModel {
                Sensor = new NikonCameraModel.SensorSpecs {
                    ResX = 6048, ResY = 4024,
                    SizeX = 35.9, SizeY = 23.9,
                    PixelSizeX = 5.94, PixelSizeY = 5.94
                },
                LiveView = new NikonCameraModel.LiveViewConfig {
                    OnStatus = 3U,
                    ImageHeaderSize = 512
                }
            },

            ["Z 50"] = new NikonCameraModel {
                Sensor = new NikonCameraModel.SensorSpecs {
                    ResX = 5568, ResY = 3712,
                    SizeX = 23.5, SizeY = 15.7,
                    PixelSizeX = 4.22, PixelSizeY = 4.22
                },
                LiveView = new NikonCameraModel.LiveViewConfig {
                    OnStatus = 3U,
                    ImageHeaderSize = 512
                }
            },

            ["D780"] = new NikonCameraModel {
                Sensor = new NikonCameraModel.SensorSpecs {
                    ResX = 6048, ResY = 4024,
                    SizeX = 35.9, SizeY = 23.9,
                    PixelSizeX = 5.92, PixelSizeY = 5.92
                },
                LiveView = new NikonCameraModel.LiveViewConfig {
                    OnStatus = 3U,
                    ImageHeaderSize = 512
                }
            },

            ["D6"] = new NikonCameraModel {
                Sensor = new NikonCameraModel.SensorSpecs {
                    ResX = 5568, ResY = 3712,
                    SizeX = 35.9, SizeY = 23.9,
                    PixelSizeX = 6.44, PixelSizeY = 6.44
                },
                LiveView = new NikonCameraModel.LiveViewConfig {
                    OnStatus = 3U,
                    ImageHeaderSize = 512
                }
            },

            ["Z 5"] = new NikonCameraModel {
                Sensor = new NikonCameraModel.SensorSpecs {
                    ResX = 6016, ResY = 4016,
                    SizeX = 35.9, SizeY = 23.9,
                    PixelSizeX = 5.95, PixelSizeY = 5.95
                },
                LiveView = new NikonCameraModel.LiveViewConfig {
                    OnStatus = 3U,
                    ImageHeaderSize = 512
                }
            },

            ["Z 7II"] = new NikonCameraModel {
                Sensor = new NikonCameraModel.SensorSpecs {
                    ResX = 8256, ResY = 5504,
                    SizeX = 35.9, SizeY = 23.9,
                    PixelSizeX = 4.35, PixelSizeY = 4.35
                },
                LiveView = new NikonCameraModel.LiveViewConfig {
                    OnStatus = 3U,
                    ImageHeaderSize = 512
                }
            },

            ["Z 6II"] = new NikonCameraModel {
                Sensor = new NikonCameraModel.SensorSpecs {
                    ResX = 6048, ResY = 4024,
                    SizeX = 35.9,  SizeY = 23.9,
                    PixelSizeX = 5.94,  PixelSizeY = 5.94
                },
                LiveView = new NikonCameraModel.LiveViewConfig {
                    OnStatus = 3U,
                    ImageHeaderSize = 512
                }
            },

            ["Z 9"] = new NikonCameraModel {
                Sensor = new NikonCameraModel.SensorSpecs {
                    ResX = 8256, ResY = 5504,
                    SizeX = 35.9, SizeY = 23.9,
                    PixelSizeX = 4.35, PixelSizeY = 4.35
                },
                LiveView = new NikonCameraModel.LiveViewConfig {
                    OnStatus = 3U,
                    ImageHeaderSize = 1024
                }
            },

            ["Z 8"] = new NikonCameraModel {
                Sensor = new NikonCameraModel.SensorSpecs {
                    ResX = 8256, ResY = 5504,
                    SizeX = 35.9, SizeY = 23.9,
                    PixelSizeX = 4.35, PixelSizeY = 4.35
                },
                LiveView = new NikonCameraModel.LiveViewConfig {
                    OnStatus = 3U,
                    ImageHeaderSize = 1024
                }
            },

        };

        public static NikonCameraModel GetCameraModel(String key) {
            if (!CameraList.TryGetValue(key, out var model)) {
                throw new KeyNotFoundException($"Camera model '{key}' not found in the database.");
            }

            return model;
        }
        public static NikonCameraModel.SensorSpecs GetSensorSpecs(String key) {
            NikonCameraModel model = GetCameraModel(key);
            if (model.LiveView == null) {
                throw new InvalidOperationException($"Camera model '{key}' does not have his sensor specification configured.");
            }

            return model.Sensor;
        }
        public static NikonCameraModel.LiveViewConfig GetLiveViewConfig(String key) {
            NikonCameraModel model = GetCameraModel(key);
            if (model.LiveView == null) {
                throw new InvalidOperationException($"Camera model '{key}' does not have LiveView configuration.");
            }

            return model.LiveView;
        }
    }
}

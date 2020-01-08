using NINA.Utility;
using System;
using System.Collections.Generic;
using System.IO;

namespace EDSDKLib {

    public static class EDSDKLocal {

        static EDSDKLocal() {
            DllLoader.LoadDll(Path.Combine("Canon", "EDSDK.dll"));
        }

        private static object lockObj = new object();
        private static bool initialized;

        public static void Initialize() {
            lock (lockObj) {
                if (!initialized) {
                    if (!DllLoader.IsX86()) {
                        //When EOS Utility by Canon is open in the background the EDSDK.EdsInitializeSDK throws an uncatchable AccessViolation Exception
                        //Therefore the system processes are scanned for this program and if found the initialization is prevented
                        var eosUtil = System.Diagnostics.Process.GetProcessesByName("EOS Utility");
                        if (eosUtil.Length > 0) {
                            throw new Exception("Cannot initialize Canon SDK. EOS Utiltiy is preventing the DLL to load. Please close EOS Utility first to be able to connect to your Canon Camera!");
                        }
                    }

                    var err = EDSDK.EdsInitializeSDK();
                    if (err > 0) {
                        throw new Exception($"Canon EdsInitializeSDK failed with code {err}");
                    } else {
                        initialized = true;
                    }
                }
            }
        }

        /*
         * Camera ISO speeds
         * This should enumerate all possible ISO speeds that all supported Canon cameras
         * are capable of. See table defined in EDSDK documentation, section 5.2.22 kEdsPropID_ISOSpeed
         */

        public static Dictionary<int, int> ISOSpeeds = new Dictionary<int, int>() {
            {0,         0x00000000 },
            {6,         0x00000028 },
            {12,        0x00000030 },
            {50,        0x00000040 },
            {100,       0x00000048 },
            {125,       0x0000004b },
            {160,       0x0000004d },
            {200,       0x00000050 },
            {250,       0x00000053 },
            {320,       0x00000055 },
            {400,       0x00000058 },
            {500,       0x0000005b },
            {640,       0x0000005d },
            {800,       0x00000060 },
            {1000,      0x00000063 },
            {1250,      0x00000065 },
            {1600,      0x00000068 },
            {2000,      0x0000006b },
            {2500,      0x0000006d },
            {3200,      0x00000070 },
            {4000,      0x00000073 },
            {5000,      0x00000075 },
            {6400,      0x00000078 },
            {8000,      0x0000007b },
            {10000,     0x0000007d },
            {12800,     0x00000080 },
            {16000,     0x00000083 },
            {20000,     0x00000085 },
            {25600,     0x00000088 },
            {32000,     0x0000008b },
            {40000,     0x0000008d },
            {51200,     0x00000090 },
            {64000,     0x00000093 },
            {80000,     0x00000095 },
            {102400,    0x00000098 },
            {204800,    0x000000a0 },
            {409600,    0x000000a8 },
            {819200,    0x000000b0 }
        };

        /*
         * Camera shutter speeds
         * This should enumerate all possible shuttder speeds that all supported Canon cameras
         * are capable of. See table defined in EDSDK documentation, section 5.2.26 kEdsPropID_Tv
         */

        public static Dictionary<int, double> ShutterSpeeds = new Dictionary<int, double> {
            {0x0C, double.MaxValue},
            {0x10, 30      },
            {0x13, 25      },
            {0x14, 20      },
            {0x15, 20      }, /* 1/3 */
            {0x18, 15      },
            {0x1B, 13      },
            {0x1C, 10      },
            {0x1D, 10      }, /* 1/3 */
            {0x20, 8       },
            {0x24, 6       },
            {0x23, 6       }, /* 1/3 */
            {0x25, 5       },
            {0x28, 4       },
            {0x2B, 3.2     },
            {0x2C, 3       },
            {0x2D, 2.5     },
            {0x30, 2       },
            {0x33, 1.6     },
            {0x34, 1.5     },
            {0x35, 1.3     },
            {0x38, 1       },
            {0x3B, 0.8     },
            {0x3C, 0.7     },
            {0x3D, 0.6     },
            {0x40, 0.5     },
            {0x43, 0.4     },
            {0x44, 0.3     },
            {0x45, 0.3     }, /* 1/3 */
            {0x48, 1/4d    },
            {0x4B, 1/5d    },
            {0x4C, 1/6d    },
            {0x4D, 1/6d    }, /* 1/3 */
            {0x50, 1/8d    },
            {0x53, 1/10d   }, /* 1/3 */
            {0x54, 1/10d   },
            {0x55, 1/13d   },
            {0x58, 1/15d   },
            {0x5B, 1/20d   }, /* 1/3 */
            {0x5C, 1/20d   },
            {0x5D, 1/25d   },
            {0x60, 1/30d   },
            {0x63, 1/40d   },
            {0x64, 1/45d   },
            {0x65, 1/50d   },
            {0x68, 1/60d   },
            {0x6B, 1/80d   },
            {0x6C, 1/90d   },
            {0x6D, 1/100d  },
            {0x70, 1/125d  },
            {0x73, 1/160d  },
            {0x74, 1/180d  },
            {0x75, 1/200d  },
            {0x78, 1/250d  },
            {0x7B, 1/320d  },
            {0x7C, 1/350d  },
            {0x7D, 1/400d  },
            {0x80, 1/500d  },
            {0x83, 1/640d  },
            {0x84, 1/750d  },
            {0x85, 1/800d  },
            {0x88, 1/1000d },
            {0x8B, 1/1250d },
            {0x8C, 1/1500d },
            {0x8D, 1/1600d },
            {0x90, 1/2000d },
            {0x93, 1/2500d },
            {0x94, 1/3000d },
            {0x95, 1/3200d },
            {0x98, 1/4000d },
            {0x9B, 1/5000d },
            {0x9C, 1/6000d },
            {0x9D, 1/6400d },
            {0xA0, 1/8000d }
        };

        /*
         * EDSDK error code dictionary
         * This should be kept in sync with the error codes defined by EDSDK.
         * This dictionary exists to map error codes to the EDSDK-provided descriptive name.
         */

        public static Dictionary<uint, string> ErrorCodes = new Dictionary<uint, string> {
            {0x00000000, "EDS_ERR_OK"},
            {0x00000001, "EDS_ERR_UNIMPLEMENTED"},
            {0x00000002, "EDS_ERR_INTERNAL_ERROR"},
            {0x00000003, "EDS_ERR_MEM_ALLOC_FAILED"},
            {0x00000004, "EDS_ERR_MEM_FREE_FAILED"},
            {0x00000005, "EDS_ERR_OPERATION_CANCELLED"},
            {0x00000006, "EDS_ERR_INCOMPATIBLE_VERSION"},
            {0x00000007, "EDS_ERR_NOT_SUPPORTED"},
            {0x00000008, "EDS_ERR_UNEXPECTED_EXCEPTION"},
            {0x00000009, "EDS_ERR_PROTECTION_VIOLATION"},
            {0x0000000A, "EDS_ERR_MISSING_SUBCOMPONENT"},
            {0x0000000B, "EDS_ERR_SELECTION_UNAVAILABLE"},
            {0x00000020, "EDS_ERR_FILE_IO_ERROR"},
            {0x00000021, "EDS_ERR_FILE_TOO_MANY_OPEN"},
            {0x00000022, "EDS_ERR_FILE_NOT_FOUND"},
            {0x00000023, "EDS_ERR_FILE_OPEN_ERROR"},
            {0x00000024, "EDS_ERR_FILE_CLOSE_ERROR"},
            {0x00000025, "EDS_ERR_FILE_SEEK_ERROR"},
            {0x00000026, "EDS_ERR_FILE_TELL_ERROR"},
            {0x00000027, "EDS_ERR_FILE_READ_ERROR"},
            {0x00000028, "EDS_ERR_FILE_WRITE_ERROR"},
            {0x00000029, "EDS_ERR_FILE_PERMISSION_ERROR"},
            {0x0000002A, "EDS_ERR_FILE_DISK_FULL_ERROR"},
            {0x0000002B, "EDS_ERR_FILE_ALREADY_EXISTS"},
            {0x0000002C, "EDS_ERR_FILE_FORMAT_UNRECOGNIZED"},
            {0x0000002D, "EDS_ERR_FILE_DATA_CORRUPT"},
            {0x0000002E, "EDS_ERR_FILE_NAMING_NA"},
            {0x00000040, "EDS_ERR_DIR_NOT_FOUND"},
            {0x00000041, "EDS_ERR_DIR_IO_ERROR"},
            {0x00000042, "EDS_ERR_DIR_ENTRY_NOT_FOUND"},
            {0x00000043, "EDS_ERR_DIR_ENTRY_EXISTS"},
            {0x00000044, "EDS_ERR_DIR_NOT_EMPTY"},
            {0x00000050, "EDS_ERR_PROPERTIES_UNAVAILABLE"},
            {0x00000051, "EDS_ERR_PROPERTIES_MISMATCH"},
            {0x00000053, "EDS_ERR_PROPERTIES_NOT_LOADED"},
            {0x00000060, "EDS_ERR_INVALID_PARAMETER"},
            {0x00000061, "EDS_ERR_INVALID_HANDLE"},
            {0x00000062, "EDS_ERR_INVALID_POINTER"},
            {0x00000063, "EDS_ERR_INVALID_INDEX"},
            {0x00000064, "EDS_ERR_INVALID_LENGTH"},
            {0x00000065, "EDS_ERR_INVALID_FN_POINTER"},
            {0x00000066, "EDS_ERR_INVALID_SORT_FN"},
            {0x00000080, "EDS_ERR_DEVICE_NOT_FOUND"},
            {0x00000081, "EDS_ERR_DEVICE_BUSY"},
            {0x00000082, "EDS_ERR_DEVICE_INVALID"},
            {0x00000083, "EDS_ERR_DEVICE_EMERGENCY"},
            {0x00000084, "EDS_ERR_DEVICE_MEMORY_FULL"},
            {0x00000085, "EDS_ERR_DEVICE_INTERNAL_ERROR"},
            {0x00000086, "EDS_ERR_DEVICE_INVALID_PARAMETER"},
            {0x00000087, "EDS_ERR_DEVICE_NO_DISK"},
            {0x00000088, "EDS_ERR_DEVICE_DISK_ERROR"},
            {0x00000089, "EDS_ERR_DEVICE_CF_GATE_CHANGED"},
            {0x0000008A, "EDS_ERR_DEVICE_DIAL_CHANGED"},
            {0x0000008B, "EDS_ERR_DEVICE_NOT_INSTALLED"},
            {0x0000008C, "EDS_ERR_DEVICE_STAY_AWAKE"},
            {0x0000008D, "EDS_ERR_DEVICE_NOT_RELEASED"},
            {0x000000A0, "EDS_ERR_STREAM_IO_ERROR"},
            {0x000000A1, "EDS_ERR_STREAM_NOT_OPEN"},
            {0x000000A2, "EDS_ERR_STREAM_ALREADY_OPEN"},
            {0x000000A3, "EDS_ERR_STREAM_OPEN_ERROR"},
            {0x000000A4, "EDS_ERR_STREAM_CLOSE_ERROR"},
            {0x000000A5, "EDS_ERR_STREAM_SEEK_ERROR"},
            {0x000000A6, "EDS_ERR_STREAM_TELL_ERROR"},
            {0x000000A7, "EDS_ERR_STREAM_READ_ERROR"},
            {0x000000A8, "EDS_ERR_STREAM_WRITE_ERROR"},
            {0x000000A9, "EDS_ERR_STREAM_PERMISSION_ERROR"},
            {0x000000AA, "EDS_ERR_STREAM_COULDNT_BEGIN_THREAD"},
            {0x000000AB, "EDS_ERR_STREAM_BAD_OPTIONS"},
            {0x000000AC, "EDS_ERR_STREAM_END_OF_STREAM"},
            {0x000000C0, "EDS_ERR_COMM_PORT_IS_IN_USE"},
            {0x000000C1, "EDS_ERR_COMM_DISCONNECTED"},
            {0x000000C2, "EDS_ERR_COMM_DEVICE_INCOMPATIBLE"},
            {0x000000C3, "EDS_ERR_COMM_BUFFER_FULL"},
            {0x000000C4, "EDS_ERR_COMM_USB_BUS_ERR"},
            {0x000000D0, "EDS_ERR_USB_DEVICE_LOCK_ERROR"},
            {0x000000D1, "EDS_ERR_USB_DEVICE_UNLOCK_ERROR"},
            {0x000000E0, "EDS_ERR_STI_UNKNOWN_ERROR"},
            {0x000000E1, "EDS_ERR_STI_INTERNAL_ERROR"},
            {0x000000E2, "EDS_ERR_STI_DEVICE_CREATE_ERROR"},
            {0x000000E3, "EDS_ERR_STI_DEVICE_RELEASE_ERROR"},
            {0x000000E4, "EDS_ERR_DEVICE_NOT_LAUNCHED"},
            {0x000000F0, "EDS_ERR_ENUM_NA"},
            {0x000000F1, "EDS_ERR_INVALID_FN_CALL"},
            {0x000000F2, "EDS_ERR_HANDLE_NOT_FOUND"},
            {0x000000F3, "EDS_ERR_INVALID_ID"},
            {0x000000F4, "EDS_ERR_WAIT_TIMEOUT_ERROR"},
            {0x00002003, "EDS_ERR_SESSION_NOT_OPEN"},
            {0x00002004, "EDS_ERR_INVALID_TRANSACTIONID"},
            {0x00002007, "EDS_ERR_INCOMPLETE_TRANSFER"},
            {0x00002008, "EDS_ERR_INVALID_STRAGEID"},
            {0x0000200A, "EDS_ERR_DEVICEPROP_NOT_SUPPORTED"},
            {0x0000200B, "EDS_ERR_INVALID_OBJECTFORMATCODE"},
            {0x00002011, "EDS_ERR_SELF_TEST_FAILED"},
            {0x00002012, "EDS_ERR_PARTIAL_DELETION"},
            {0x00002014, "EDS_ERR_SPECIFICATION_BY_FORMAT_UNSUPPORTED"},
            {0x00002015, "EDS_ERR_NO_VALID_OBJECTINFO"},
            {0x00002016, "EDS_ERR_INVALID_CODE_FORMAT"},
            {0x00002017, "EDS_ERR_UNKNOWN_VENDER_CODE"},
            {0x00002018, "EDS_ERR_CAPTURE_ALREADY_TERMINATED"},
            {0x0000201A, "EDS_ERR_INVALID_PARENTOBJECT"},
            {0x0000201B, "EDS_ERR_INVALID_DEVICEPROP_FORMAT"},
            {0x0000201C, "EDS_ERR_INVALID_DEVICEPROP_VALUE"},
            {0x0000201E, "EDS_ERR_SESSION_ALREADY_OPEN"},
            {0x0000201F, "EDS_ERR_TRANSACTION_CANCELLED"},
            {0x00002020, "EDS_ERR_SPECIFICATION_OF_DESTINATION_UNSUPPORTED"},
            {0x0000A001, "EDS_ERR_UNKNOWN_COMMAND"},
            {0x0000A005, "EDS_ERR_OPERATION_REFUSED"},
            {0x0000A006, "EDS_ERR_LENS_COVER_CLOSE"},
            {0x0000A101, "EDS_ERR_LOW_BATTERY"},
            {0x0000A102, "EDS_ERR_OBJECT_NOTREADY"},
            {0x00008D01, "EDS_ERR_TAKE_PICTURE_AF_NG"},
            {0x00008D02, "EDS_ERR_TAKE_PICTURE_RESERVED"},
            {0x00008D03, "EDS_ERR_TAKE_PICTURE_MIRROR_UP_NG"},
            {0x00008D04, "EDS_ERR_TAKE_PICTURE_SENSOR_CLEANING_NG"},
            {0x00008D05, "EDS_ERR_TAKE_PICTURE_SILENCE_NG"},
            {0x00008D06, "EDS_ERR_TAKE_PICTURE_NO_CARD_NG"},
            {0x00008D07, "EDS_ERR_TAKE_PICTURE_CARD_NG"},
            {0x00008D08, "EDS_ERR_TAKE_PICTURE_CARD_PROTECT_NG"},
            {0x000000F5, "EDS_ERR_LAST_GENERIC_ERROR_PLUS_ONE"}
      };
    }
}
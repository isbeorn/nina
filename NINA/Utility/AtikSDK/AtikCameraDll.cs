using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Utility.AtikSDK {
    class AtikCameraDll {
        private const string DLLNAME = "ArtemisHSC.dll";

        static AtikCameraDll() {
            DllLoader.LoadDll("Atik/" + DLLNAME);
        }

        /// <summary>
        /// Returns the version of the API. This number comes from the services itself. 
        /// </summary>
        [DllImport(DLLNAME, EntryPoint = "ArtemisAPIVersion", CallingConvention = CallingConvention.Cdecl)]
        private static extern int ArtemisAPIVersion();

        /// <summary>
        /// Returns the version of the DLL. This number is set in the DLL. 
        /// It is important to check that the DLL and API version match. 
        /// If not, there is a strong possibility that things won’t work properly. 
        /// </summary>
        [DllImport(DLLNAME, EntryPoint = "ArtemisDLLVersion", CallingConvention = CallingConvention.Cdecl)]
        private static extern int ArtemisDLLVersion();

        /// <summary>
        /// Returns a bool to indicate whether the given device is present. 
        /// </summary>
        [DllImport(DLLNAME, EntryPoint = "ArtemisDeviceIsPresent", CallingConvention = CallingConvention.Cdecl)]
        private static extern bool ArtemisDeviceIsPresent(int iDevice);

        /// <summary>
        /// Sets the supplied ‘pName’ variable to the name of the given device. Return true if iDevice is found, false otherwise. 
        /// </summary>
        [DllImport(DLLNAME, EntryPoint = "ArtemisDeviceName", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern bool ArtemisDeviceName(int iDevice, [MarshalAs(UnmanagedType.LPStr)] string pName);

        /// <summary>
        /// Sets the supplied ‘pSerial’ variable to the serial number of the given device. Returns true if iDevice is found, false otherwise. 
        /// </summary>
        [DllImport(DLLNAME, EntryPoint = "ArtemisDeviceSerial", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern bool ArtemisDeviceSerial(int iDevice, [MarshalAs(UnmanagedType.LPStr)] string pName);

        /// <summary>
        /// Connects to the given camera. 
        /// The ArtemisHandle is actually a ‘void *’ and will be needed for all camera specific methods. 
        /// It will return 0 if this method fails. 
        /// You can call this method with ‘-1’, in which case, you will received the first camera that is not currently in use (by any application). 
        /// Note: It is possible for different applications to connect to the same camera. 
        /// </summary>
        [DllImport(DLLNAME, EntryPoint = "ArtemisConnect", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern IntPtr ArtemisConnect(int iDevice);

        /// <summary>
        /// This method is used to release the camera when done. It will allow other applications to connect to the camera as listed above.   
        /// </summary>
        [DllImport(DLLNAME, EntryPoint = "ArtemisDisconnect", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern bool ArtemisDisconnect(IntPtr camera);

        /// <summary>
        /// This method is used to release all the cameras when done. 
        /// It will allow other applications to connect to the camera as listed above. 
        /// Although it used to be best to call this function when the application is closing, it is no longer necessary as the DLL itself will remove all connections when shutting down. 
        /// </summary>
        [DllImport(DLLNAME, EntryPoint = "ArtemisDisconnectAll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern bool ArtemisDisconnectAll();

        /// <summary>
        /// This method is used to release all the cameras when done. 
        /// It will allow other applications to connect to the camera as listed above. 
        /// Although it used to be best to call this function when the application is closing, it is no longer necessary as the DLL itself will remove all connections when shutting down. 
        /// </summary>
        [DllImport(DLLNAME, EntryPoint = "ArtemisDisconnectAll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern bool ArtemisDisconnectAll(int iDevice);

        /// <summary>
        /// Used to check that the given camera is still connected. 
        /// The most likely reason for this happening is that the camera has been unplugged. 
        /// </summary>
        [DllImport(DLLNAME, EntryPoint = "ArtemisIsConnected", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern bool ArtemisIsConnected(IntPtr camera);

        /// <summary>
        /// The refresh devices count tells you how many times the camera list has changed on the service. 
        /// The camera list changes every time a USB device is connected or removed. 
        /// Therefore, the purpose of this method is to tell the user that the cameras have changed and that it’s worth checking to make sure their camera(s) are still connected. 
        /// </summary>
        [DllImport(DLLNAME, EntryPoint = "ArtemisRefreshDevicesCount", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern int ArtemisRefreshDevicesCount();

        /// <summary>
        /// Used to check that a device is a camera before connecting to it. 
        /// The only alternative is a test bench, so this function is usually of no use. 
        /// </summary>
        [DllImport(DLLNAME, EntryPoint = "ArtemisDeviceIsCamera", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern bool ArtemisDeviceIsCamera(int iDevice);

        /// <summary>
        /// This method is used to find out the serial number of the camera (as written in the EEPROM). 
        /// Note that this number is likely to be different to the Device Serial which is supplied by the USB device. 
        /// This method returns an ArtemisErrorCode as listed below.
        /// </summary>
        [DllImport(DLLNAME, EntryPoint = "ArtemisCameraSerial", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern ArtemisErrorCode ArtemisCameraSerial(IntPtr camera, ref int flags, ref int serial);

        /// <summary>
        /// Sets the supplied ARTEMISPROPERTIES to the value for the given camera. 
        /// </summary>
        [DllImport(DLLNAME, EntryPoint = "ArtemisProperties", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern ArtemisErrorCode ArtemisProperties(IntPtr camera, ArtemisPropertiesStruct prop);

        /// <summary>
        /// Gives the colour properties of the given camera.  
        /// The offsets(Normal / Preview – X / Y) give you information about the Bayer matrix used.
        /// </summary>
        [DllImport(DLLNAME, EntryPoint = "ArtemisColourProperties", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern ArtemisErrorCode ArtemisColourProperties(IntPtr camera, ref ArtemisColourType colorType, ref int normalOffsetX, ref int normalOffsetY, ref int previewOffsetX, ref int previewOffsetY);

        /// <summary>
        /// As you might expect, this method is used to start an exposure on the camera. 
        /// The function will return an error code as listed below. 
        /// Note: If you have previously stopped the exposure, then you have to wait for the camera state (listed below) to return to Idle before another exposure can be made. 
        /// </summary>
        [DllImport(DLLNAME, EntryPoint = "ArtemisStartExposure", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern ArtemisErrorCode ArtemisStartExposure(IntPtr camera, float seconds);

        /// <summary>
        /// Same as Start Exposure, except the supplied time is in milliseconds instead of seconds. 
        /// </summary>
        [DllImport(DLLNAME, EntryPoint = "ArtemisStartExposureMS", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern ArtemisErrorCode ArtemisStartExposureMS(IntPtr camera, int milliseconds);

        /// <summary>
        /// Used to cancel the current exposure.  
        /// </summary>
        [DllImport(DLLNAME, EntryPoint = "ArtemisStopExposure", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern ArtemisErrorCode ArtemisStopExposure(IntPtr camera);

        /// <summary>
        /// Let’s you know when the image is ready. The value is set to ‘false’ when ‘Start Exposure’ is called and only returns true once the exposure has finished. 
        /// </summary>
        [DllImport(DLLNAME, EntryPoint = "ArtemisImageReady", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern bool ArtemisImageReady(IntPtr camera);

        /// <summary>
        /// Used to cancel the current exposure.  
        /// </summary>
        [DllImport(DLLNAME, EntryPoint = "ArtemisCameraState", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern ArtemisCameraStateEnum ArtemisCameraState(IntPtr camera);

        /// <summary>
        /// How much of the image has been download from the sensor. Note: Some cameras will do the whole thing in one go, so the value will jump from 0% to 100%. Other take longer and will reflect how long is left 
        /// </summary>
        [DllImport(DLLNAME, EntryPoint = "ArtemisDownloadPercent", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern int ArtemisDownloadPercent(IntPtr camera);

        /// <summary>
        /// Will populate the given values with the details of the image
        /// </summary>
        [DllImport(DLLNAME, EntryPoint = "ArtemisGetImageData", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern ArtemisErrorCode ArtemisGetImageData(IntPtr camera, ref int x, ref int y, ref int w, ref int h, ref int binX, ref int binY);

        /// <summary>
        /// Returns a pointer to the image buffer. You should call ArtemisGetImageData first to find out the dimensions of the image. This will return 0 if there is no image.
        /// </summary>
        [DllImport(DLLNAME, EntryPoint = "ArtemisImageBuffer", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern IntPtr ArtemisImageBuffer(IntPtr camera);

        /// <summary>
        /// Used to set the binning values of the exposure. The function will return an error code as listed below. 
        /// </summary>
        [DllImport(DLLNAME, EntryPoint = "ArtemisBin", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern ArtemisErrorCode ArtemisBin(IntPtr camera, int x, int y);

        /// <summary>
        /// This function will populate the given parameters with the current binning values. 
        /// </summary>
        [DllImport(DLLNAME, EntryPoint = "ArtemisGetBin", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern ArtemisErrorCode ArtemisGetBin(IntPtr camera, ref int x, ref int y);

        /// <summary>
        /// This function will populate the given parameters with the maximum binning values for this camera. 
        /// </summary>
        [DllImport(DLLNAME, EntryPoint = "ArtemisGetMaxBin", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern ArtemisErrorCode ArtemisGetMaxBin(IntPtr camera, ref int x, ref int y);

        /// <summary>
        /// This function will set the subframe values
        /// </summary>
        [DllImport(DLLNAME, EntryPoint = "ArtemisSubframe", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern ArtemisErrorCode ArtemisSubframe(IntPtr camera, int x, int y, int w, int h);

        /// <summary>
        /// This function will set the x, y position of the subframe.
        /// </summary>
        [DllImport(DLLNAME, EntryPoint = "ArtemisSubframePos", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern ArtemisErrorCode ArtemisSubframePos(IntPtr camera, int x, int y);

        /// <summary>
        /// This function will set the size of subframe. 
        /// </summary>
        [DllImport(DLLNAME, EntryPoint = "ArtemisSubframSize", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern ArtemisErrorCode ArtemisSubframSize(IntPtr camera, int x, int y);

        /// <summary>
        /// This function will populate the given values with the current subframe values.
        /// </summary>
        [DllImport(DLLNAME, EntryPoint = "ArtemisGetSubframe", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern ArtemisErrorCode ArtemisGetSubframe(IntPtr camera, ref int x, ref int y, ref int w, ref int h);

        /// <summary>
        /// Preview mode will produce images at a faster rate, but at a cost of quality. 
        /// This method is used to set the camera into normal / preview mode. 
        /// Passing bPrev = true will set the camera into preview mode, ‘bPrev = false’ sets the camera into normal mode.
        /// </summary>
        [DllImport(DLLNAME, EntryPoint = "ArtemisSetPreview", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern ArtemisErrorCode ArtemisSetPreview(IntPtr camera, bool prev);

        /// <summary>
        /// This function has two purposes. 
        /// Firstly, if you call this function with ‘sensor = 0’, then temperature will actually be set to the number of sensors. 
        /// Once you know how many sensors there are, you can call this method with a ‘1-based’ sensor index, and temperature will be set to the temperature reading of that sensor. 
        /// The temperature is in 1/100 of a degree (Celcius), so a value of -1000 is actually -10C
        /// </summary>
        [DllImport(DLLNAME, EntryPoint = "ArtemisTemperatureSensorInfo", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern ArtemisErrorCode ArtemisTemperatureSensorInfo(IntPtr camera, int sensor, ref int temperature);

        /// <summary>
        /// This function is used to set the temperature of the camera. 
        /// The setpoint is in 1/100 of a degree (Celcius). So, to set the cooling to -10C, you need to call the function with setpoint = -1000. 
        /// </summary>
        [DllImport(DLLNAME, EntryPoint = "ArtemisSetCooling", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern ArtemisErrorCode ArtemisSetCooling(IntPtr camera, int setPoint);

        /// <summary>
        /// Gives the current state of the cooling. 
        /// </summary>
        [DllImport(DLLNAME, EntryPoint = "ArtemisCoolingInfo", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern ArtemisErrorCode ArtemisCoolingInfo(IntPtr camera, ref int flags, ref int level, ref int minlvl, ref int maxlvl, ref int setPoint);

        /// <summary>
        /// Tells the camera to start warming up. 
        /// Note: It is very important that this function is called at the end of operation. 
        /// Letting the sensor warm up naturally can cause damage to the sensor. 
        /// It’s not unusual for the temperature to go further down before going up. 
        /// </summary>
        [DllImport(DLLNAME, EntryPoint = "ArtemisCoolerWarmUp", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern ArtemisErrorCode ArtemisCoolerWarmUp(IntPtr camera);

        /*
         * Not yet added: Internal and External Filter Wheel and Guiding Methods.
         */

        enum ArtemisCameraStateEnum {
            /// <summary>
            /// The default state of the camera. Indicates that the camera is ready to start and exposure 
            /// </summary>
            CAMERA_IDLE,

            /// <summary>
            /// This is a temporary state between receiving a start exposure command, and the exposure actually starting. 
            /// </summary>
            CAMERA_WAITING,

            /// <summary>
            /// The camera is exposing 
            /// </summary>
            CAMERA_EXPOSING,

            /// <summary>
            /// The service is reading the image off the sensor 
            /// </summary>
            CAMERA_DOWNLOADING,

            /// <summary>
            /// Stop Exposure has been called, and we are waiting for everything to stop. Some cameras will still need to clear the sensor, so this can take a while. 
            /// </summary>
            CAMERA_FLUSHING,

            /// <summary>
            /// Something has gone wrong with the camera 
            /// </summary>
            CAMERA_ERROR = -1
        }

        enum ArtemisColourType {
            /// <summary>
            /// Unknown colour type 
            /// </summary>
            ARTEMIS_COLOUR_UNKNOWN,

            /// <summary>
            /// No colour (Mono camera) 
            /// </summary>
            ARTEMIS_COLOUR_NONE,

            /// <summary>
            /// Colour Camera (Bayer Matrix) 
            /// </summary>
            ARTEMIS_COLOUR_RGGB
        }

        enum ArtemisErrorCode {
            /// <summary>
            /// The function call has been successful 
            /// </summary>
            ARTEMIS_OK,

            /// <summary>
            /// One or more of the parameters supplied are inconsistent with what was expected. One example of this would be calling any camera function with an invalid ArtemisHandle. Another might be to set a subframe to an unreachable area.
            /// </summary>
            ARTEMIS_INVALID_PARAMETER,

            /// <summary>
            /// Returned when calling a camera function on a camera which is no longer connected. 
            /// </summary>
            ARTEMIS_NOT_CONNECTED,

            /// <summary>
            /// Returned for functions that are no longer used. 
            /// </summary>
            ARTEMIS_NOT_IMPLEMENTED,

            /// <summary>
            /// Returned if a function times out for any reason 
            /// </summary>
            ARTEMIS_NO_RESPONSE,

            /// <summary>
            /// Returned when trying to call a camera specific function on a camera which doesn’t have that feature. (Such as the cooling functions on cameras without cooling). 
            /// </summary>
            ARTEMIS_INVALID_FUNCTION,

            /// <summary>
            /// Returned if trying to call a function on something that hasn’t been initialised. The only current example is the lens control 
            /// </summary>
            ARTEMIS_NOT_INITIALISED,

            /// <summary>
            /// Returned if a function couldn’t complete for any other reason
            /// </summary>
            ARTEMIS_OPERATION_FAILED
        }

        struct ArtemisPropertiesStruct {
            /// <summary>
            /// The manufacturer of the camera. Usually Atik Cameras. 
            /// </summary>
            [MarshalAs(UnmanagedType.LPStr)]
            public string Manufacturer;

            /// <summary>
            /// The name of the type of camera 
            /// </summary>
            [MarshalAs(UnmanagedType.LPStr)]
            public string Description;

            /// <summary>
            /// Represents the properties of the camera: 
            /// 1   =  Has FIFO 
            /// 2   = Has External Trigger 
            /// 4   = Can return preview data 
            /// 8   = Camera can subsample 
            /// 16   = Has Mechanical Shutter 
            /// 32     = Has Guide Port 
            /// 64     = Has GPIO capabilities 
            /// 128   = Has Window Heater 
            /// 256   = Can download 8-bit image 
            /// 512   =  Can Overlap exposure 
            /// 1024 = Has Filter Wheel 
            /// </summary>
            public int cameraflags;

            /// <summary>
            /// The value is ‘1’ if the sensor is interlaced. 0 otherwise 
            /// </summary>
            public int ccdflags;

            /// <summary>
            /// The height of each pixel in microns 
            /// </summary>
            public float PixelMicronsY;

            /// <summary>
            /// The width of each pixel in microns 
            /// </summary>
            public float PixelMicronsX;

            /// <summary>
            /// The width of the sensor in pixels  
            /// </summary>
            public int nPixelsX;

            /// <summary>
            /// The height of the sensor in pixels 
            /// </summary>
            public int nPixelsY;

            /// <summary>
            /// The firmware version of the camera 
            /// </summary>
            public int Protocol;
        }

    }
}

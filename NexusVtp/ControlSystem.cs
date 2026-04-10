using Crestron.SimplSharp;                              // For Basic SIMPL# Classes
using Crestron.SimplSharp.CrestronDataStore;
using Crestron.SimplSharp.CrestronIO;
using Crestron.SimplSharpPro;                       	// For Basic SIMPL#Pro classes
using Crestron.SimplSharpPro.CrestronThread;        	// For Threading
using Crestron.SimplSharpPro.UI;
using Forte.SSPro.UI.Helper.Library.UI;
using Nexus.Driver.Architecture.Components;
using Nexus.Driver.Architecture.Enumerations;
using Nexus.Framework.Services;
using Nexus.Qsc.Qsys.Driver;
using Nexus.Qsc.Qsys.Driver.Settings;
using Nexus.Utils;
using Nexus.Vaddio.RoboshotIP.Driver;
using NexusCommon;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Timers;

namespace NexusVtp
{
    /// <summary>
    /// Main control system class that initializes and manages all devices, drivers, and touchpanel interfaces
    /// </summary>
    public class ControlSystem : CrestronControlSystem
    {
        /// <summary>
        /// TS770 touchpanel device
        /// </summary>
        Ts770 _ts770;
        /// <summary>
        /// Touchpanel interface wrapper
        /// </summary>
        Panel _panel;
        /// <summary>
        /// QSC Q-SYS DSP driver for audio routing and volume control
        /// </summary>
        QscQsysDriver _dsp;
        /// <summary>
        /// First Vaddio RoboShot camera for PTZ control
        /// </summary>
        VaddioRoboshotIP _camera01;
        /// <summary>
        /// Second Vaddio RoboShot camera for PTZ control
        /// </summary>
        VaddioRoboshotIP _camera02;
        /// <summary>
        /// Main touchpanel controller for navigation and system functions
        /// </summary>
        TpMain tpMain;
        /// <summary>
        /// DSP volume control handler for touchpanel
        /// </summary>
        TpDsp tpDsp;
        /// <summary>
        /// Phone call DSP control handler for touchpanel
        /// </summary>
        TpDspPhone tpDspPhone;
        /// <summary>
        /// Camera control handler for touchpanel
        /// </summary>
        TpCamera tpCamera;
        /// <summary>
        /// Audio/video matrix routing handler for touchpanel
        /// </summary>
        TpMatrix tpMatrix;
        /// <summary>
        /// Lighting control handler for touchpanel
        /// </summary>
        TpLights tpLights;
        /// <summary>
        /// Progress tracking for long-running operations
        /// </summary>
        Progress _progress;
        /// <summary>
        /// Macro execution engine for system-wide actions
        /// </summary>
        Macro _macro;
        /// <summary>
        /// Timer for periodic time display updates on touchpanel
        /// </summary>
        Timer _timerTimeUpdate;

        /// <summary>
        /// Dictionary to hold cameras
        /// </summary>
        private Dictionary<uint, ICamera> _cameras = new Dictionary<uint, ICamera>();

        //LgDisplayDriver _display;
        /// <summary>
        /// ControlSystem Constructor. Starting point for the SIMPL#Pro program.
        /// Use the constructor to:
        /// * Initialize the maximum number of threads (max = 400)
        /// * Register devices
        /// * Register event handlers
        /// * Add Console Commands
        /// 
        /// Please be aware that the constructor needs to exit quickly; if it doesn't
        /// exit in time, the SIMPL#Pro program will exit.
        /// 
        /// You cannot send / receive data in the constructor
        /// </summary>
        public ControlSystem()
            : base()
        {
            try
            {
                Thread.MaxNumberOfUserThreads = 20;
                this._timerTimeUpdate = new Timer(1000) { AutoReset = true };
            }
            catch (Exception e)
            {
                ErrorLog.Error("Error in the constructor: {0}", e.Message);
            }
        }

        /// <summary>
        /// Initializes the control system after the constructor has completed.
        /// This method orchestrates the initialization sequence for all devices, drivers, and touchpanel interfaces.
        /// The sequence is: cameras → display → DSP → macros → progress tracking → touchpanels → Nexus configuration.
        /// Wires up the Crestron DataStore and starts the periodic time update timer for the touchpanel header.
        /// </summary>
        public override async void InitializeSystem()
        {
            try
            {
                await NexusServiceManager.Initialize(true);
                ConfigCamera();
                ConfigDisplay();
                ConfigDsp();
                ConfigMacro();
                ConfigProgress();
                ConfigTp();
                //want this after the TPs so event handlers in the tp files will fire
                ConfigNexus();
                _progress.RunAction("Initialize");  

                // for the clock so it can wait
                this._timerTimeUpdate.Elapsed += new ElapsedEventHandler(this.OnTimerTimeUpdate);
                this._timerTimeUpdate.Start();

                //TODO is there a way to set program ID tag
                //InitialParametersClass.SystemSettings

                ////datastore - 
                var initResult = CrestronDataStoreStatic.InitCrestronDataStore();

                if (initResult != CrestronDataStore.CDS_ERROR.CDS_SUCCESS)
                {
                    ErrorLog.Error("DataStore init failed: {0}", initResult);
                    NexusServiceManager.System.Log(Nexus.Driver.Architecture.Enumerations.LoggingLevels.Exceptions, $"{MethodBase.GetCurrentMethod().Name}: DataStore init failed: {initResult}");
                    return;
                }
            }
            catch (Exception e)
            {
                ErrorLog.Error($"{MethodBase.GetCurrentMethod().Name}: {0}", e.Message);
                NexusServiceManager.System.Log(Nexus.Driver.Architecture.Enumerations.LoggingLevels.Exceptions, $"{MethodBase.GetCurrentMethod().Name}: {e.Message}");
            }
        }

        /// <summary>
        /// Configures Vaddio RoboShot camera instances and registers them in the camera dictionary.
        /// Creates two PTZ camera instances, registers their connection change events, and populates the camera collection
        /// for use by the camera control touchpanel.
        /// </summary>
        private void ConfigCamera()
        {
            try
            {
                _camera01 = new VaddioRoboshotIP("NachoCamera");
                _camera02 = new VaddioRoboshotIP("OtherCamera");

                //_camera.Initialize("192.168.1.140", 23, "admin", "password");// default
                //_camera.Connect();
                //_display.Initialize("192.168.10.154", 23, "admin", "password");// default
                //_display.Connect();

                //_camera.DebuggingEnable = true;
                //_camera.DebuggingLevels = Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Info;
                //_camera.LoggingEnable = true;
                //_camera.LoggingLevels = Nexus.Driver.Architecture.Enumerations.LoggingLevels.Info;
                //_camera.AutoReconnect = true;
                // key is the output to which it is connected
                _cameras.Add(1, _camera01);
                _cameras.Add(2, _camera02);
                _camera01.OnConnectedChange += (isConnected) => OnDeviceConnectedChange(_camera01.Name, isConnected);
                _camera02.OnConnectedChange += (isConnected) => OnDeviceConnectedChange(_camera02.Name, isConnected);

                foreach (var camera in _cameras.Values)
                {
                    var vaddioCamera = camera as VaddioRoboshotIP;
                    if (vaddioCamera != null)
                    {
                        vaddioCamera.OnConnectedChange += (isConnected) => OnDeviceConnectedChange(vaddioCamera.Name, isConnected);
                    }
                }
            }
            catch (Exception err)
            {
                NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Errors, $"{MethodBase.GetCurrentMethod().Name}: {GetInnerErr.GetInnermostException(err)}");
                NexusServiceManager.System.Log(Nexus.Driver.Architecture.Enumerations.LoggingLevels.Exceptions, $"{MethodBase.GetCurrentMethod().Name}: {GetInnerErr.GetInnermostException(err)}");
            }
        }

        /// <summary>
        /// Configures display driver for video output control.
        /// Currently stubbed for future display driver implementation.
        /// </summary>
        private void ConfigDisplay()
        {
            try
            {
                //_display = new LgDisplayDriver("NachoDisplay");
            }
            catch (Exception err)
            {
                NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Errors, $"{MethodBase.GetCurrentMethod().Name}: {GetInnerErr.GetInnermostException(err)}");
                NexusServiceManager.System.Log(Nexus.Driver.Architecture.Enumerations.LoggingLevels.Exceptions, $"{MethodBase.GetCurrentMethod().Name}: {GetInnerErr.GetInnermostException(err)}");
            }
        }

        /// <summary>
        /// Configures the QSC Q-SYS DSP driver for audio routing and volume control.
        /// Sets up the DSP connection with auto-reconnect enabled, registers connection change events,
        /// and configures four volume gain controls (Master, Program, Phone, Privacy).
        /// </summary>
        private void ConfigDsp()
        {
            // IF EMULATING, ALLOW QSYS DESIGNER THROUGH FIREWALL
            try
            {
                _dsp = new QscQsysDriver("NachoDsp");
                //_dsp.IpAddress = "192.168.10.140";
                //_dsp.Port = 1710;              // default
                //_dsp.Username = "admin";      // if required
                //_dsp.Password = "";           // if required   
                _dsp.AutoReconnect = true;
                _dsp.OnConnectedChange += (isConnected) => OnDeviceConnectedChange(_dsp.Name, isConnected);
                _dsp.AddVolume("Master", new GainSettings { Name = "Master", ScriptName = "gain_master" });
                _dsp.AddVolume("Program", new GainSettings { Name = "Program", ScriptName = "gain_program" });
                _dsp.AddVolume("Phone", new GainSettings { Name = "Phone", ScriptName = "gain_phone" });
                _dsp.AddVolume("Privacy", new GainSettings { Name = "Privacy", ScriptName = "gain_privacy" });
                //_dsp.AddPreset("Defaults", new PresetSettings { Name = "Defaults", ScriptName = "Snapshot_Controller" });           
                //_dsp.AddDialer("PhoneCall", new DialerSettings { Name = "PhoneCall", ScriptName = "phonecall" });
            }
            catch (Exception err)
            {
                NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Errors, $"{MethodBase.GetCurrentMethod().Name}: {GetInnerErr.GetInnermostException(err)}");
                NexusServiceManager.System.Log(Nexus.Driver.Architecture.Enumerations.LoggingLevels.Exceptions, $"{MethodBase.GetCurrentMethod().Name}: {GetInnerErr.GetInnermostException(err)}");
            }
        }

        /// <summary>
        /// Configures system-wide macros for coordinating multi-device actions across rooms modes (Shutdown, Presentation, PhoneCall, VideoCall).
        /// Each macro combines progress tracking, DSP preset recall, and camera positioning operations.
        /// Macros are triggered via the touchpanel and can be extended by touchpanel controllers for panel-specific synchronization.
        /// </summary>
        private void ConfigMacro()
        {
            try
            {
                //Add stuff for drivers and devices here. The _macro object gets passed to the TpMain class, which will run the Actions but TpMain can also add Actions to it as well for syncing the panels.
                _macro = new Macro();
                //TODO - GET SNAPSHOT CONTROLLER SCRIPT NAME FROM DRIVER
                string snapshotScriptName = "Snapshot_Controller";
                _macro.AddAction("Shutdown", () => _progress.RunAction("Shutdown"));
                _macro.AddAction("Shutdown", () => _dsp.PresetRecall(snapshotScriptName, 1));
                _macro.AddAction("Shutdown", () =>
                {
                    foreach (var camera in _cameras.Values)
                    {
                        camera.GoHome();
                    }
                });

                _macro.AddAction("Presentation", () => _progress.RunAction("Presentation"));
                _macro.AddAction("Presentation", () => _dsp.PresetRecall(snapshotScriptName, 2));
                _macro.AddAction("PhoneCall", () => _progress.RunAction("PhoneCall"));
                _macro.AddAction("PhoneCall", () => _dsp.PresetRecall(snapshotScriptName, 3));
                _macro.AddAction("VideoCall", () => _progress.RunAction("VideoCall"));
                _macro.AddAction("VideoCall", () => _dsp.PresetRecall(snapshotScriptName, 4));
            }
            catch (Exception err)
            {
                NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Errors, $"{MethodBase.GetCurrentMethod().Name}: {GetInnerErr.GetInnermostException(err)}");
                NexusServiceManager.System.Log(Nexus.Driver.Architecture.Enumerations.LoggingLevels.Exceptions, $"{MethodBase.GetCurrentMethod().Name}: {GetInnerErr.GetInnermostException(err)}");
            }
        }

        /// <summary>
        /// Registers system configuration settings with the Nexus Service Manager.
        /// Configures room information, source names/icons, video destinations, routing groups, lighting presets,
        /// and dynamic camera preset configurations. These settings are used throughout the system for dynamic UI updates
        /// and device control logic.
        /// </summary>
        private void ConfigNexus()
        {
            try
            {
                //TODO - DEBUGGING - COMMENT THIS FOR RELEASE so that it goes to the default level
                NexusServiceManager.System.DebuggingLevels = DebuggingLevels.Debug;
                NexusServiceManager.System.AddSystemConfig("Room Information", new Settings.RoomInformation());
                NexusServiceManager.System.AddSystemConfig("Source Names", new Settings.SourceNames());
                NexusServiceManager.System.AddSystemConfig("Source Icons", new Settings.SourceIcons());
                NexusServiceManager.System.AddSystemConfig("Source Visible", new Settings.SourceVisible());
                NexusServiceManager.System.AddSystemConfig("Video Destination Names", new Settings.VideoDestinationNames());
                NexusServiceManager.System.AddSystemConfig("Routing Group Names", new Settings.RoutingGroupNames());
                NexusServiceManager.System.AddSystemConfig("Lighting Preset", new Settings.LightingPresets());
                NexusServiceManager.System.AddSystemConfig("Volume Names", new Settings.VolumeNames());
                foreach (var camera in _cameras.Values)
                {
                    var vaddioCamera = camera as VaddioRoboshotIP;
                    if (vaddioCamera != null)
                    {
                        NexusServiceManager.System.AddSystemConfig($"{vaddioCamera.Name}", new Settings.CameraPresetNames());
                    }             
                }
            }
            catch (Exception err)
            {
                NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Errors, $"{MethodBase.GetCurrentMethod().Name}: {GetInnerErr.GetInnermostException(err)}");
                NexusServiceManager.System.Log(Nexus.Driver.Architecture.Enumerations.LoggingLevels.Exceptions, $"{MethodBase.GetCurrentMethod().Name}: {GetInnerErr.GetInnermostException(err)}");
            }
        }

        /// <summary>
        /// Configures progress tracking for long-running operations.
        /// Establishes progress actions (Initialize, Shutdown, Presentation, PhoneCall, VideoCall) with durations and status messages
        /// to provide user feedback during system state transitions displayed on the touchpanel.
        /// </summary>
        private void ConfigProgress()
        {
            try
            {
                _progress = new Progress();
                _progress.AddAction("Initialize", 5);
                _progress.AddMessage("Initialize", 0, "Initializing...");
                _progress.AddMessage("Initialize", 3, "Still Initializing...");
                _progress.AddAction("Shutdown", 10);
                _progress.AddMessage("Shutdown", 0, "Shutting down...");
                _progress.AddMessage("Shutdown", 3, "Doing other shutdown stuff...");
                _progress.AddMessage("Shutdown", 6, "Doing even more shutdown stuff...");
                _progress.AddAction("Presentation", 5);
                _progress.AddMessage("Presentation", 0, "Setting up presentation...");
                _progress.AddAction("PhoneCall", 3);
                _progress.AddMessage("PhoneCall", 0, "Setting up phone call...");
                _progress.AddAction("VideoCall", 5);
                _progress.AddMessage("VideoCall", 0, "Setting up video call...");
            }
            catch (Exception err)
            {
                NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Errors, $"{MethodBase.GetCurrentMethod().Name}: {GetInnerErr.GetInnermostException(err)}");
                NexusServiceManager.System.Log(Nexus.Driver.Architecture.Enumerations.LoggingLevels.Exceptions, $"{MethodBase.GetCurrentMethod().Name}: {GetInnerErr.GetInnermostException(err)}");
            }
        }

        /// <summary>
        /// Initializes the TS770 touchpanel and instantiates all touchpanel controllers.
        /// Loads the SGD (Smart Graphics Design) file, creates touchpanel handler instances for each subsystem
        /// (main navigation, DSP, phone, camera, matrix routing, lighting), and wires inter-TP event handlers
        /// for coordinated functionality (volume popup timeout management, source routing coordination).
        /// </summary>
        private void ConfigTp()
        {
            try
            {              
                string sgdPath = Path.Combine(Directory.GetApplicationDirectory(), "Tp/1xxxxxx-Nexus_TS-770_v0_1.sgd");
                _ts770 = new Ts770(0x03, this);
                _panel = new Panel(_ts770, sgdPath);
                _panel.OnPanelOnlineChange += OnPanelOnlineChange;

                //handles navigation and other non-device-specific functions of the panel
                tpMain = new TpMain(_panel, "Tp Main", _progress, _macro);
                tpDsp = new TpDsp(_panel, "Tp Dsp", _dsp);
                tpDspPhone = new TpDspPhone(_panel, "Tp Dsp Phone", _dsp);
                tpCamera = new TpCamera(_panel, "Tp Camera", _cameras);
                tpMatrix = new TpMatrix(_panel, "Tp Matrix");
                tpLights = new TpLights(_panel, "Tp Lights");

                //ADD INTERACTIONS BETWEEN TPs HERE
                //tpMain handles page navigation and includes a timeout for the volume popup
                //tpDsp raises a UserActivity event when the volume is adjusted so that tpMain can reset the timeout to keep the popup visible
                tpDsp.UserActivity += tpMain.TimerTimeoutVolumeRestart;
                tpMain.OnRoutingSubpage += tpMatrix.ResetSelectedSource;
            }
            catch (Exception err)
            {
                NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Errors, $"{MethodBase.GetCurrentMethod().Name}: {GetInnerErr.GetInnermostException(err)}");
                NexusServiceManager.System.Log(Nexus.Driver.Architecture.Enumerations.LoggingLevels.Exceptions, $"{MethodBase.GetCurrentMethod().Name}: {GetInnerErr.GetInnermostException(err)}");
            }
        }

        /// <summary>
        /// Handles panel connection state changes and logs to debug output
        /// </summary>
        /// <param name="currentDevice">The panel that raised the event</param>
        /// <param name="args">Event arguments containing online/offline state</param>
        private void OnPanelOnlineChange(Panel currentDevice, OnlineOfflineEventArgs args)
        {
            NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Debug, $"Panel ID {currentDevice.ThePanel.ID} is {(args.DeviceOnLine ? "Online" : "Offline")}");
        }

        /// <summary>
        /// Handles device connection state changes and logs to debug output
        /// </summary>
        /// <param name="name">The name of the device</param>
        /// <param name="obj">True if online, false if offline</param>
        private void OnDeviceConnectedChange(string name, bool obj)
        {
            NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Debug, $"{name} is {(obj ? "Online" : "Offline")}");
        }

        /// <summary>
        /// Timer elapsed event handler that updates the date and time displayed in the touchpanel header.
        /// Fires every 1000 milliseconds to refresh the clock display with current system date and time.
        /// </summary>
        /// <param name="source">The Timer object that raised the elapsed event</param>
        /// <param name="e">The ElapsedEventArgs containing timing information</param>
        private void OnTimerTimeUpdate(object source, ElapsedEventArgs e)
        {
            try
            {
                DateTime now = DateTime.Now;
                string newDate = now.ToString("dddd, MMMM d, yyyy");
                string newTime = now.ToString("h:mm tt"); // use "hh:mm tt" for 12-hour with AM/PM

                if (this.tpMain != null)
                {
                    this.tpMain.SetHeaderDate(newDate);
                    this.tpMain.SetHeaderTime(newTime);
                }
            }
            catch (Exception ex)
            {
                ErrorLog.Error("Error in On_Timer_Time_Update: {0}", ex.Message);
            }
        }
    }
}
using Crestron.SimplSharp;                              // For Basic SIMPL# Classes
using Crestron.SimplSharp.CrestronIO;
using Crestron.SimplSharpPro;                       	// For Basic SIMPL#Pro classes
using Crestron.SimplSharpPro.CrestronThread;        	// For Threading
using Crestron.SimplSharpPro.DeviceSupport;         	// For Generic Device Support
using Crestron.SimplSharpPro.Diagnostics;		    	// For System Monitor Access
using Crestron.SimplSharpPro.UI;
using Forte.SSPro.UI;
using Forte.SSPro.UI.Helper.Library.UI;
using Independentsoft.Exchange;
using Nexus.Driver.Architecture;
using Nexus.Driver.Architecture.Components;
using Nexus.Driver.Architecture.Configuration;
using Nexus.Driver.Architecture.Devices;
using Nexus.Driver.Architecture.Enumerations;
using Nexus.Framework.Services;
using Nexus.Framework.Services.Services;
using Nexus.Qsc.Qsys.Driver;
using Nexus.Qsc.Qsys.Driver.Components;
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
    public class ControlSystem : CrestronControlSystem
    {
        Ts770 _ts770;
        Panel _panel;
        QscQsysDriver _dsp;
        VaddioRoboshotIP _camera01;
        VaddioRoboshotIP _camera02;
        TpMain tpMain;
        TpDsp tpDsp;
        TpDspPhone tpDspPhone;
        TpCamera tpCamera;
        TpMatrix tpMatrix;
        TpLights tpLights;
        Progress _progress;
        Macro _macro;
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
        /// InitializeSystem - this method gets called after the constructor 
        /// has finished. 
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

            }
            catch (Exception e)
            {
                ErrorLog.Error($"{MethodBase.GetCurrentMethod().Name}: {0}", e.Message);
                NexusServiceManager.System.Log(Nexus.Driver.Architecture.Enumerations.LoggingLevels.Exceptions, $"{MethodBase.GetCurrentMethod().Name}: {e}");
            }
        }

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
                foreach (var camera in _cameras.Values)
                {
                    var vaddioCamera = camera as VaddioRoboshotIP;
                    if (vaddioCamera != null)
                    {
                        NexusServiceManager.System.AddSystemConfig($"{vaddioCamera.Name} Presets", new Settings.CameraPresetNames());
                    }             
                }
            }
            catch (Exception err)
            {
                NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Errors, $"{MethodBase.GetCurrentMethod().Name}: {GetInnerErr.GetInnermostException(err)}");
                NexusServiceManager.System.Log(Nexus.Driver.Architecture.Enumerations.LoggingLevels.Exceptions, $"{MethodBase.GetCurrentMethod().Name}: {GetInnerErr.GetInnermostException(err)}");
            }
        }

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
                tpDsp.UserActivity += tpMain.TimerTimeoutVolumeRestart;
                tpDspPhone = new TpDspPhone(_panel, "Tp Dsp Phone", _dsp);
                tpCamera = new TpCamera(_panel, "Tp Camera", _cameras);
                tpMatrix = new TpMatrix(_panel, "Tp Matrix");
                tpLights = new TpLights(_panel, "Tp Lights");
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

using Crestron.SimplSharp;
using Crestron.SimplSharpPro;
using Crestron.SimplSharpPro.UI;
using Forte.SSPro.UI.Helper.Library.UI;
using Independentsoft.Exchange;
using Independentsoft.Exchange.Autodiscover;
using Nexus.Driver.Architecture.Configuration;
using Nexus.Driver.Architecture.Enumerations;
using Nexus.Framework.Services;
using Nexus.Utils;
using NexusCommon;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Timers;
using static NexusCommon.Settings;
using static NexusVtp.SmartGraphics;

namespace NexusVtp
{
    /// <summary>
    /// Set joins and setup event handlers.
    /// </summary>
    public class TpMain
    {
        private const int MaxRoutingGroup = 4;
        private string _panelName = string.Empty;
        private Panel _Panel;
        private Progress _Progress;
        private Macro _Macro;
        private ExtendedSmartObject _mode;
        private ExtendedSmartObject _modePresentation;
        private ExtendedSmartObject _modeVtc;
        private ExtendedSmartObject _mainMenu;
        private ExtendedSmartObject _settingsMenu;
        private ExtendedSmartObject _routingGroup;

        private uint _timeSettingsHold = 3;
        private uint _timeTimeoutMode = 10;
        private uint _timeTimeoutVolume = 10;
        private uint _timeCleanReturnHold = 5;
        private bool _cleanReturnHeld = false;
        private bool _settingsHeld = false;
        private bool _visibleInfo = false;
        private bool _visibleLights = false;
        private bool _visibleMixer = false;
        private bool _visibleMode = false;

        private Timer _timerCleanReturnHeld;
        private Timer _timerTimeoutMode;
        private Timer _timerTimeoutVolume;
        private Timer _timerSettingsHeld;

        enum Page
        {
            Start = 21,
            Main = 22,
            Setup = 23,
        }

        enum Btn
        {
            Lights = 19,
            Info = 27,
            Mode = 42,
            ModePresentation = 43,
            ModeVtc = 44,
            VolumeOpen = 26,
            VolumeClose = 28,
            ShutdownCancel = 30,
            ShutdownConfirm = 40,
            CleanReturn = 50,
            VolumeMixer = 60,
            RoutingGroup = 90,
        }

        enum Lvl
        {
            Progress = 5,
        }

        enum Lbl
        {
            HeaderTime = 1,
            HeaderDate = 2,
            HeaderName = 3,
            HeaderMessage = 4,
            Progress = 5,
        }

        enum SubpageStart
        {
            Lights = 19,
            Info = 27,         
        }

        enum SubpageMain
        {
            Power = 30,
            Routing = 31,
            Share = 32,
            Camera = 33,
            Phone = 34,
            Settings = 37,
            Volume = 62,
            VolumeMixer = 63,
        }

        enum SubpageModal
        {
            Progress = 40,
        }

        enum SubpageMode
        {
            Mode = 42,
            Presentation = 43,
            Vtc = 44,
        }

        enum Mode
        {
            Presentation = 11,
            PhoneCall = 12,
            VideoCall = 13,
        }

        enum MainMenu
        {
            Power = 11,
            Mode = 12,
            Routing = 13,
            Share = 14,
            Camera = 15,
            Phone = 16,
            Tuner = 17,
            Usb = 18,
            Settings = 19,
        }

        enum SettingsMenu
        {
            //all other values are generic
            Clean = 11,
        }

        private Dictionary<int, int> subpageMapSettingsMenu = new Dictionary<int, int>
        {
            // the key is the cue from the smart object and the value is the subpage number to show when that cue is active
            { 11, 51 },
            { 12, 52 },
            { 13, 53 },
        };

        private Dictionary<int, int> subpageMapRoutingGroup = new Dictionary<int, int>
        {
            // the key is the cue from the smart object and the value is the subpage number to show when that cue is active
            { 11, 91 },  
            { 12, 92 },  
            { 13, 93 },  
            { 14, 94 },  
        };

        private Dictionary<uint, bool> enableMode = new Dictionary<uint, bool>
        {
            { 1, true }, //Presentation
            { 2, true }, //PhoneCall
            { 3, true }, //VideoCall
        };

        private Dictionary<uint, bool> enableMainMenu = new Dictionary<uint, bool>
        {
            { 1, true }, //Power
            { 2, true }, //Mode
            { 3, true }, //Routing
            { 4, true }, //Vtc
            { 5, true }, //Camera
            { 6, true }, //Phone
            { 7, false }, //Tuner
            { 8, false }, //Usb
            { 9, true }, //Settingsu
        };

        private Dictionary<uint, string> labelModePresentation = new Dictionary<uint, string>
        {
            { 1, "Presentation 1" },
            { 2, "Presentation 2" },
            { 3, "Presentation 3" },
            { 4, "Presentation 4" },
            { 5, "Presentation 5" },
            { 6, "Presentation 6" },
            { 7, "Presentation 7" },
            { 8, "Presentation 8" },
        };

        private Dictionary<uint, string> labelModeVtc = new Dictionary<uint, string>
        {
            { 1, "Vtc 1" },
            { 2, "Vtc 2" },
            { 3, "Vtc 3" },
            { 4, "Vtc 4" },
            { 5, "Vtc 5" },
            { 6, "Vtc 6" },
            { 7, "Vtc 7" },
            { 8, "Vtc 8" },
        };

        private Dictionary<uint, string> _labelRoutingGroup = new Dictionary<uint, string>();


        private Dictionary<uint, bool> visibleMainMenu = new Dictionary<uint, bool>
        {
            { 1, true }, //Power
            { 2, true }, //Mode
            { 3, true }, //Routing
            { 4, true }, //Vtc
            { 5, true }, //Camera
            { 6, true }, //Phone
            { 7, true }, //Tuner
            { 8, true }, //Usb
            { 9, true }, //Settings
        };

        private Dictionary<uint, bool> visibleMode = new Dictionary<uint, bool>
        {
            { 1, true }, //Presentation
            { 2, true }, //PhoneCall
            { 3, true }, //VideoCall
        };

        private Dictionary<uint, bool> visibleModePresentation = new Dictionary<uint, bool>
        {
            { 1, true },
            { 2, true },
            { 3, true },
            { 4, true },
            { 5, true },
            { 6, true },
            { 7, true },
            { 8, true },
        };

        private Dictionary<uint, bool> visibleModeVtc = new Dictionary<uint, bool>
        {
            { 1, true },
            { 2, true },
            { 3, true },
            { 4, true },
            { 5, true },
            { 6, true },
            { 7, true },
            { 8, true },
        };

        private Dictionary<uint, bool> visibleRoutingGroup = new Dictionary<uint, bool>
        {
            { 1, true },
            { 2, true },
            { 3, true },
            { 4, false },
        };

        private Dictionary<uint, bool> visibleSettingsMenu = new Dictionary<uint, bool>
        {
            { 1, true },
            { 2, true },
            { 3, true },
            { 4, false },
            { 5, false },
            { 6, false },
            { 7, false },
            { 8, false },
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="TpMain"/> class.
        /// </summary>
        /// <param name="panel"> The wrapped tp. </param>
        /// <param name="panelName"> Description of the panel for debugging. </param>
        /// <param name="progress"> Progress instance. </param>
        /// <param name="macro"> macro instance. </param>
        public TpMain(Panel panel, string panelName, Progress progress, Macro macro)
        {
            this._Panel = panel;
            this._panelName = panelName;
            this._Progress = progress;
            this._Macro = macro;
            Initialize();
        }

        //public methods
        /// <summary>
        /// Set the time.
        /// </summary>
        /// /// <param name="data"> Formatted date. </param>
        public void SetHeaderTime(string data)
        {
            _Panel.SetSerial((uint)Lbl.HeaderTime, data);
        }

        /// <summary>
        /// Set the date.
        /// </summary>
        /// /// <param name="data"> Formatted time. </param>
        public void SetHeaderDate(string data)
        {
            _Panel.SetSerial((uint)Lbl.HeaderDate, data);
        }

        public void SetHeaderMessage(string data)
        {
            _Panel.SetSerial((uint)Lbl.HeaderMessage, data);
        }

        private void SetHeaderName(string data)
        {
            _Panel.SetSerial((uint)Lbl.HeaderName, data);
        }

        //private methods
        private void Initialize()
        {
            try
            {
                //this.SetHeaderName(this._panelName); //now handled in Nexus Settings
                NexusServiceManager.System.OnSettingChanged += System_OnSettingChanged;
                this.SetHeaderMessage("MESSAGE");

                // subscribe to progress events and map to existing handlers
                this._Progress.GaugeChanged += (s, value) => this.OnProgressGauge(value);
                this._Progress.CountChanged += (s, count) => this.OnProgressCount(count);
                this._Progress.MessageChanged += (s, msg) => this.OnProgressMessage(msg);
                this._Progress.Started += (s, e) => this.OnProgressStart();
                this._Progress.Stopped += (s, e) => this.OnProgressStop();

                // add macro actions
                _Macro.AddAction("Shutdown", this.SetMacroShutdown);
                _Macro.AddAction("Presentation", this.SetMacroPresentation);
                _Macro.AddAction("PhoneCall", this.SetMacroPhoneCall);
                _Macro.AddAction("VideoCall", this.SetMacroVideoCall);

                // timers
                this._timerCleanReturnHeld = new Timer(this._timeCleanReturnHold * 1000);
                this._timerSettingsHeld = new Timer(this._timeSettingsHold * 1000);
                this._timerTimeoutMode = new Timer(this._timeTimeoutMode * 1000);
                this._timerTimeoutVolume = new Timer(this._timeTimeoutVolume * 1000);

                // timer event handlers
                this._timerCleanReturnHeld.Elapsed += new ElapsedEventHandler(this.OnTimerCleanReturnHeld);
                this._timerSettingsHeld.Elapsed += new ElapsedEventHandler(this.OnTimerSettingsHeld);
                this._timerTimeoutMode.Elapsed += new ElapsedEventHandler(this.OnTimerTimeoutMode);
                this._timerTimeoutVolume.Elapsed += new ElapsedEventHandler(this.OnTimerTimeoutVolume);

                // nav buttons - joins - TODO consolidate some joins
                var btnInfo = _Panel.AddButtonGroup("BtnInfo", (uint)Btn.Info);
                btnInfo.OnPanelButtonGroupChange += OnBtnInfo;

                var btnLights = _Panel.AddButtonGroup("BtnLights", (uint)Btn.Lights);
                btnLights.OnPanelButtonGroupChange += OnBtnLights;

                var btnMode = _Panel.AddButtonGroup("BtnMode", (uint)Btn.Mode);
                btnMode.OnPanelButtonGroupChange += OnBtnMode;

                var btnModePresentation = _Panel.AddButtonGroup("BtnModePresentation", (uint)Btn.ModePresentation);
                btnModePresentation.OnPanelButtonGroupChange += OnBtnModePresentation;

                var btnModeVtc = _Panel.AddButtonGroup("BtnModeVtc", (uint)Btn.ModeVtc);
                btnModeVtc.OnPanelButtonGroupChange += OnBtnModeVtc;

                var btnVolumeOpen = _Panel.AddButtonGroup("BtnVolumeOpen", (uint)Btn.VolumeOpen);
                btnVolumeOpen.OnPanelButtonGroupChange += this.OnBtnVolumeOpen;

                var btnVolumeClose = _Panel.AddButtonGroup("BtnVolumeClose", (uint)Btn.VolumeClose);
                btnVolumeClose.OnPanelButtonGroupChange += this.OnBtnVolumeClose;

                var btnVolumeMixer = _Panel.AddButtonGroup("BtnVolumeMixer", (uint)Btn.VolumeMixer);
                btnVolumeMixer.OnPanelButtonGroupChange += this.OnBtnVolumeMixer;

                var btnShutdownConfirm = _Panel.AddButtonGroup("BtnShutdownConfirm", (uint)Btn.ShutdownConfirm);
                btnShutdownConfirm.OnPanelButtonGroupChange += OnBtnShutdownConfirm;

                var btnShutdownCancel = _Panel.AddButtonGroup("BtnShutdownCancel", (uint)Btn.ShutdownCancel);
                btnShutdownCancel.OnPanelButtonGroupChange += OnBtnShutdownCancel;

                var btnCleanReturn = _Panel.AddButtonGroup("BtnCleanReturn", (uint)Btn.CleanReturn);
                btnCleanReturn.OnPanelButtonGroupChange += OnBtnCleanReturn;

                // add smart object 
                _mode = _Panel.AddSmartObject("Mode", _Panel.ThePanel.SmartObjects[(int)SgId.Mode]);
                _mainMenu = _Panel.AddSmartObject("MainMenu", _Panel.ThePanel.SmartObjects[(int)SgId.MainMenu]);
                _settingsMenu = _Panel.AddSmartObject("SettingsMenu", _Panel.ThePanel.SmartObjects[(int)SgId.SettingsMenu]);
                _routingGroup = _Panel.AddSmartObject("RoutingGroup", _Panel.ThePanel.SmartObjects[(int)SgId.RoutingGroup]);
                _modePresentation = _Panel.AddSmartObject("ModePresentation", _Panel.ThePanel.SmartObjects[(int)SgId.ModePresentation]);
                _modeVtc = _Panel.AddSmartObject("ModeVtc", _Panel.ThePanel.SmartObjects[(int)SgId.ModeVtc]);

                // add handler
                _mode.OnSmartObjectSignalChange += OnMode;
                _mainMenu.OnSmartObjectSignalChange += OnMainMenu;
                _settingsMenu.OnSmartObjectSignalChange += OnSettingsMenu;
                _routingGroup.OnSmartObjectSignalChange += OnRoutingGroup;
                _modePresentation.OnSmartObjectSignalChange += OnModePresentation;
                _modeVtc.OnSmartObjectSignalChange += OnModeVtc;


                //set items, if the state does not exist, it can cause problems, so ensure the item numbers match
                // enable
                SetEnableMode();
                SetEnableMainMenu();

                // label
                SetLabelRoutingGroup();
                SetLabelModePresentation();
                SetLabelModeVtc();

                // visible
                SetVisibleMode();
                SetVisibleMainMenu();
                SetVisibleSettingsMenu();
                SetVisibleRoutingGroup();
                SetVisibleModePresentation();
                SetVisibleModeVtc();
       
            }
            catch (Exception err)
            {
                NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Errors, $"{_panelName} {MethodBase.GetCurrentMethod().Name}: {GetInnerErr.GetInnermostException(err)}");
                NexusServiceManager.System.Log(Nexus.Driver.Architecture.Enumerations.LoggingLevels.Exceptions, $"{_panelName} {MethodBase.GetCurrentMethod().Name}: {GetInnerErr.GetInnermostException(err)}");
            }
        }

        //private methods
        // event handlers - button objects
        private void OnBtnCleanReturn(object o, ButtonGroupEventArgs e)
        {
            if (e.Sig.BoolValue)
            {
                _Panel.SetBoolean((int)Btn.CleanReturn, true);
                this._cleanReturnHeld = false;
                RestartTimer(this._timerCleanReturnHeld);
                NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Debug, $"{_panelName}  Clean Return Press");
            }
            else
            {
                _Panel.SetBoolean((int)Btn.CleanReturn, false);
                this._timerCleanReturnHeld.Stop();
                if (!this._cleanReturnHeld)
                {
                    NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Debug, $"{_panelName}  Clean Return tapped");
                }
            }
        }

        private void OnBtnInfo(object o, ButtonGroupEventArgs e)
        {
            if (e.Sig.BoolValue)
            {
                this._visibleInfo = !this._visibleInfo;
                _Panel.SetBoolean((int)SubpageStart.Info, this._visibleInfo);
                NexusServiceManager.System.Debug(DebuggingLevels.Debug, $"devicePlatform: {CrestronEnvironment.DevicePlatform}");
                NexusServiceManager.System.Debug(DebuggingLevels.Debug,$"programIdTag: {InitialParametersClass.ProgramIDTag}");
                NexusServiceManager.System.Debug(DebuggingLevels.Debug, $"appNumber: {InitialParametersClass.ApplicationNumber}");
                NexusServiceManager.System.Debug(DebuggingLevels.Debug, $"controllerPrompt: {InitialParametersClass.ControllerPromptName}");
                NexusServiceManager.System.Debug(DebuggingLevels.Debug, $"programDirectory: {InitialParametersClass.ProgramDirectory}");
                NexusServiceManager.System.Debug(DebuggingLevels.Debug, $"firmwareVersion: {InitialParametersClass.FirmwareVersion}");
                //TODO get cpz name and compile date, add debug statments and send them to the TP
            }
        }

        private void OnBtnLights(object o, ButtonGroupEventArgs e)
        {
            if (e.Sig.BoolValue)
            {
                this._visibleLights = !this._visibleLights;
                _Panel.SetBoolean((int)SubpageStart.Lights, this._visibleLights);
            }
        }

        private void OnBtnShutdownCancel(object o, ButtonGroupEventArgs e)
        {
            if (e.Sig.BoolValue)
            {
                _mainMenu.SetBoolean((uint)MainMenu.Power, false);
                _Panel.SetBoolean((int)SubpageMain.Power, false);
            }
        }

        private void OnBtnShutdownConfirm(object o, ButtonGroupEventArgs e)
        {
            if (e.Sig.BoolValue)
            {
                this._Macro.RunAction("Shutdown");
            }
        }

        private void OnBtnMode(object o, ButtonGroupEventArgs e)
        {
            if (e.Sig.BoolValue)
            {
                NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Debug, $"{_panelName} {MethodBase.GetCurrentMethod().Name}");
                this._visibleMode = !this._visibleMode;
                _Panel.SetBoolean((int)SubpageMode.Mode, this._visibleMode);
                //_mainMenu.SetBoolean((uint)MainMenu.Mode, this._visibleMode);

                TimerTimeoutModeRestart();
            }
        }

        private void OnBtnModePresentation(object o, ButtonGroupEventArgs e)
        {
            if (e.Sig.BoolValue)
            {
                _Panel.SetBoolean((int)SubpageMode.Presentation, false);
                TimerTimeoutModeRestart();
            }
        }

        private void OnBtnModeVtc(object o, ButtonGroupEventArgs e)
        {
            if (e.Sig.BoolValue)
            {
                _Panel.SetBoolean((int)SubpageMode.Vtc, false);
                TimerTimeoutModeRestart();
            }
        }

        private void OnBtnVolumeOpen(object o, ButtonGroupEventArgs e)
        {
            if (e.Sig.BoolValue)
            {
                this.SetVisibleVolume(true);
                this.TimerTimeoutVolumeRestart();
            }
        }

        private void OnBtnVolumeClose(object o, ButtonGroupEventArgs e)
        {
            if (e.Sig.BoolValue)
            {
                this.SetVisibleVolume(false);
                this._timerTimeoutVolume.Stop();
                this.ResetVolumeMixer();
            }
        }

        private void OnBtnVolumeMixer(object o, ButtonGroupEventArgs e)
        {
            if (e.Sig.BoolValue)
            {
                this._visibleMixer = !this._visibleMixer;
                this.SetVisibleVolumeMixer(this._visibleMixer);
                this.TimerTimeoutVolumeRestart();
            }
        }

        // event handlers - Smart objects
        private void OnMainMenu(object o, SmartObjectEventArgs ea)
        {
            NexusDebugSmartObjectEvent(o, ea);
            int num = (int)ea.Sig.Number;
            if (Enum.IsDefined(typeof(MainMenu), num))
            {
                NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Debug, $"{_panelName} {MethodBase.GetCurrentMethod().Name} {num} found in MainMenu");
                // need a press and hold for settings, so have to check boolean on some items
                switch (num)
                {
                    case (int)MainMenu.Power:
                        if (ea.Sig.BoolValue)
                        {
                            NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Debug, $"{_panelName} {MethodBase.GetCurrentMethod().Name} Power");
                            _mainMenu.SetBoolean((uint)MainMenu.Power, true);
                            _Panel.SetBoolean((int)SubpageMain.Power, true);
                        }            
                        break;
                    case (int)MainMenu.Mode:
                    if (ea.Sig.BoolValue)
                        {
                            NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Debug, $"{_panelName} {MethodBase.GetCurrentMethod().Name} Mode");
                            this._visibleMode = true;
                            _mainMenu.SetBoolean((uint)MainMenu.Mode, true);
                            _Panel.SetBoolean((int)SubpageMode.Mode, this._visibleMode);
                            TimerTimeoutModeRestart();
                        }   
                        break; 
                    case (int)MainMenu.Settings:
                        if (ea.Sig.BoolValue)
                        {
                            this._settingsHeld = false;
                            this._timerSettingsHeld.Start();
                        }
                        else
                        {
                            this._timerSettingsHeld.Stop();
                            if (!this._settingsHeld)
                            {
                                this.ResetMenus();
                                _mainMenu.SetBoolean((uint)MainMenu.Settings, true);
                                _Panel.SetBoolean((int)SubpageMain.Settings, true);
                            }
                        }
                        break;
                    //Normal menu items, could have done a default case and handled all there but then have to do some mapping, thought this be easier to modify though more code
                    case (int)MainMenu.Routing:
                        if (ea.Sig.BoolValue)
                        {            
                            this.ResetMenus();
                            _mainMenu.SetBoolean((uint)MainMenu.Routing, true);
                            _Panel.SetBoolean((int)SubpageMain.Routing, true);
                        }
                        break;
                    case (int)MainMenu.Share:
                        if (ea.Sig.BoolValue)
                        {
                            this.ResetMenus();
                            _mainMenu.SetBoolean((uint)MainMenu.Share, true);
                            _Panel.SetBoolean((int)SubpageMain.Share, true);
                        }
                        break;
                    case (int)MainMenu.Camera:
                        if (ea.Sig.BoolValue)
                        {
                            this.ResetMenus();
                            _mainMenu.SetBoolean((uint)MainMenu.Camera, true);
                            _Panel.SetBoolean((int)SubpageMain.Camera, true);
                        }
                        break;
                    case (int)MainMenu.Phone:
                        if (ea.Sig.BoolValue)
                        {
                            this.ResetMenus();
                            _mainMenu.SetBoolean((uint)MainMenu.Phone, true);
                            _Panel.SetBoolean((int)SubpageMain.Phone, true);
                        }
                        break;
                }
            }
        }

        private void OnMode(object o, SmartObjectEventArgs ea)
        {
            NexusDebugSmartObjectEvent(o, ea);
            if (ea.Sig.BoolValue)
            {         
                int num = (int)ea.Sig.Number;
                if (Enum.IsDefined(typeof(Mode), num))
                {
                    NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Debug, $"{_panelName} {MethodBase.GetCurrentMethod().Name} {num} found in Mode");
                    TimerTimeoutModeRestart();
                    switch (num)
                    {
                        case (int)Mode.Presentation:
                        {
                            bool supportsList = true;
                            if (supportsList)
                            {
                                _Panel.SetBoolean((uint)SubpageMode.Presentation, true);
                            }
                            else
                            { 
                                this._Macro.RunAction("Presentation"); 
                            }                     
                            break;
                        }
                        case (int)Mode.PhoneCall:
                        {
                            this._Macro.RunAction("PhoneCall");
                            break;
                        }
                        case (int)Mode.VideoCall:
                        {
                            bool supportsList = true;
                            if (supportsList)
                            {
                                _Panel.SetBoolean((uint)SubpageMode.Vtc, true);
                            }
                            else 
                            {
                                this._Macro.RunAction("VideoCall");
                            }
                            break;
                        }
                    }
                }
            }
        }

        private void OnModePresentation(object o, SmartObjectEventArgs ea)
        {
            NexusDebugSmartObjectEvent(o, ea);
            if (ea.Sig.BoolValue)
            {
                int num = (int)ea.Sig.Number;
                NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Debug, $"{_panelName} {MethodBase.GetCurrentMethod().Name} {num}");
                //TODO setup individual macros
                this._Macro.RunAction("Presentation");
            }
        }

        private void OnModeVtc(object o, SmartObjectEventArgs ea)
        {
            NexusDebugSmartObjectEvent(o, ea);
            if (ea.Sig.BoolValue)
            {
                int num = (int)ea.Sig.Number;
                NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Debug, $"{_panelName} {MethodBase.GetCurrentMethod().Name} {num}");
                //TODO setup individual macros
                this._Macro.RunAction("VideoCall");
            }
        }

        private void OnRoutingGroup(object o, SmartObjectEventArgs ea)
        {
            if (!(ea.Sig.Name.Contains("Pressed")))
            {
                return;
            }
            if (ea.Sig.BoolValue)
            {
                NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Debug, $"{_panelName} {MethodBase.GetCurrentMethod().Name} handle press");
                NexusDebugSmartObjectEvent(o, ea);
                ResetRoutingGroup();               
                ResetRoutingGroupSubpages();
                int num = (int)ea.Sig.Number;
                _routingGroup.SetBoolean((uint)num, true);
                if (this.subpageMapRoutingGroup.TryGetValue(num, out int subpage))
                {
                    _Panel.SetBoolean((uint)subpage, true);
                }
                else
                {
                    NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Errors, $"{_panelName} {MethodBase.GetCurrentMethod().Name} {num} subpage not defined");
                }
            }
        }

        private void OnSettingsMenu(object o, SmartObjectEventArgs ea)
        {
            if (!(ea.Sig.Name.Contains("Pressed")))
            {
                return;
            }

            if (ea.Sig.BoolValue)
            {
                NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Debug, $"{_panelName} {MethodBase.GetCurrentMethod().Name} handle press");
                NexusDebugSmartObjectEvent(o, ea);
                ResetSettingsMenu();
                ResetSettingsSubpages();
                int num = (int)ea.Sig.Number;
                if (num != (int)SettingsMenu.Clean)
                {
                    _settingsMenu.SetBoolean((uint)num, true);
                }
                if (this.subpageMapSettingsMenu.TryGetValue(num, out int subpage))
                {
                    _Panel.SetBoolean((uint)subpage, true);
                }
            }
        }

        // event handlers - Progress
        private void OnProgressGauge(object data)
        {
            NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Info, $"{_panelName} {MethodBase.GetCurrentMethod().Name} {data}");
            // ignore non-ushort payloads
            if (!(data is ushort value))
            {
                NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Errors, $"{_panelName} {MethodBase.GetCurrentMethod().Name} ignored non-ushort payload");
                return;
            }

            // safe to set progress now - USE value NOT data
            _Panel.SetAnalog((uint)Lvl.Progress, (short)value);
        }

        private void OnProgressCount(object data)
        {
            NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Debug, $"{_panelName} {MethodBase.GetCurrentMethod().Name} {data}");
        }

        private void OnProgressMessage(object data)
        {
            NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Debug, $"{_panelName} {MethodBase.GetCurrentMethod().Name} {data}");

            // ignore non-ushort payloads
            if (!(data is string value))
            {
                NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Errors, $"{_panelName} {MethodBase.GetCurrentMethod().Name} ignored non-string payload");
                return;
            }

            // safe to set progress now - USE value NOT data
            _Panel.SetSerial((uint)Lbl.Progress, value);
        }

        private void OnProgressStart()
        {
            NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Debug, $"{_panelName} {MethodBase.GetCurrentMethod().Name}");
            _Panel.SetBoolean((int)SubpageModal.Progress, true);
        }

        private void OnProgressStop()
        {
            NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Debug, $"{_panelName} {MethodBase.GetCurrentMethod().Name}");
            _Panel.SetBoolean((int)SubpageModal.Progress, false);
        }

        // event handlers - Timers
        private void OnTimerCleanReturnHeld(object o, ElapsedEventArgs e)
        {
            NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Debug, $"{_panelName}  Clean Return held");
            ResetSettingsSubpages();
        }

        private void OnTimerSettingsHeld(object o, ElapsedEventArgs e)
        {
            this._settingsHeld = true;
            _Panel.SetBoolean((int)Page.Setup, true);
            _Panel.SetBoolean((int)Page.Setup, false);
        }

        private void OnTimerTimeoutMode(object o, ElapsedEventArgs e)
        {
            this.ResetModeMenu();
            this.ResetModeSubpages();
        }

        private void OnTimerTimeoutVolume(object o, ElapsedEventArgs e)
        {
            this.SetVisibleVolume(false);
            this.ResetVolumeMixer();
        }

        // Functions
        private void ResetMenus()
        {
            ResetModeMenu();
            ResetModeSubpages();
            ResetMainMenu();
            ResetMainSubpages();
            ResetSettingsMenu();
            ResetSettingsSubpages();
            ResetRoutingGroup();
            ResetRoutingGroupSubpages();
        }

        private void ResetMainMenu()
        {
            foreach (MainMenu cue in Enum.GetValues(typeof(MainMenu)))
            {
                _mainMenu.SetBoolean((uint)cue, false);
            }
        }

        private void ResetMainSubpages()
        {
            foreach (SubpageMain join in Enum.GetValues(typeof(SubpageMain)))
            {
                _Panel.SetBoolean((uint)join, false);
            }
        }

        private void ResetModeMenu()
        {
            this._visibleMode = false;
            _mainMenu.SetBoolean((uint)MainMenu.Mode, this._visibleMode);
        }

        private void ResetModeSubpages()
        {
            foreach (SubpageMode join in Enum.GetValues(typeof(SubpageMode)))
            {
                _Panel.SetBoolean((uint)join, false);
            }
        }

        private void ResetRoutingGroup()
        {
            ResetItemSelected(_routingGroup, visibleRoutingGroup);
        }

        private void ResetRoutingGroupSubpages()
        {
            foreach (var subpage in subpageMapRoutingGroup.Values)
            {
                _Panel.SetBoolean((uint)subpage, false);
            }
        }

        private void ResetSettingsMenu()
        {
            ResetItemSelected(_settingsMenu, visibleSettingsMenu);
        }

        private void ResetSettingsSubpages()
        {
            foreach (var subpage in subpageMapSettingsMenu.Values)
            {
                _Panel.SetBoolean((uint)subpage, false);
            }
        }
        private void RestartTimer(Timer timer)
        {
            if (timer.Enabled)
            {
                timer.Stop();
            }
            timer.Start();
        }


        private void ResetVolumeMixer()
        {
            this._visibleMixer = false;
            this.SetVisibleVolumeMixer(this._visibleMixer);
        }

        private void SetEnableMode()
        {
            SetItemEnabledByDictionary(_mode, enableMode);
        }
        private void SetEnableMainMenu()
        {
            SetItemEnabledByDictionary(_mainMenu, enableMainMenu);
        }
        private void SetLabelModePresentation()
        {
            SetItemTextByDictionary(_modePresentation, labelModePresentation);
        }

        private void SetLabelModeVtc()
        {
            SetItemTextByDictionary(_modeVtc, labelModeVtc);
        }

        private void SetLabelRoutingGroup()
        {
            SetItemTextByDictionary(_routingGroup, _labelRoutingGroup);
        }
        private void SetVisibleMode()
        {
            SetItemVisibleByDictionary(_mode, visibleMode);
        }
        private void SetMacroPresentation()
        {
            ResetMenus();
            _Panel.SetBoolean((int)Page.Main, true);
            _Panel.SetBoolean((int)Page.Main, false);
            _mainMenu.SetBoolean((uint)MainMenu.Routing, true);
            _Panel.SetBoolean((int)SubpageMain.Routing, true);
        }

        private void SetMacroShutdown()
        {
            ResetMenus();
            _Panel.SetBoolean((int)Page.Start, true);
            _Panel.SetBoolean((int)Page.Start, false);
            
        }

        private void SetMacroPhoneCall()
        {
            ResetMenus();
            _Panel.SetBoolean((int)Page.Main, true);
            _Panel.SetBoolean((int)Page.Main, false);
            _mainMenu.SetBoolean((uint)MainMenu.Phone, true);
            _Panel.SetBoolean((int)SubpageMain.Phone, true);
        }

        private void SetMacroVideoCall()
        {
            ResetMenus();
            _Panel.SetBoolean((int)Page.Main, true);
            _Panel.SetBoolean((int)Page.Main, false);
            _mainMenu.SetBoolean((uint)MainMenu.Camera, true);
            _Panel.SetBoolean((int)SubpageMain.Camera, true);
        }

        private void SetVisibleMainMenu()
        {
            SetItemVisibleByDictionary(_mainMenu, visibleMainMenu);
        }

        private void SetVisibleRoutingGroup()
        {
            _Panel.SetBoolean((int)Btn.RoutingGroup, true);
            SetItemVisibleByDictionary(_routingGroup, visibleRoutingGroup);
        }

        private void SetVisibleSettingsMenu()
        {
            SetItemVisibleByDictionary(_settingsMenu, visibleSettingsMenu);
        }

        private void SetVisibleVolume(bool state)
        {
            _Panel.SetBoolean((int)SubpageMain.Volume, state);
        }

        private void SetVisibleVolumeMixer(bool state)
        {
            _Panel.SetBoolean((int)SubpageMain.VolumeMixer, state);
        }

        private void SetVisibleModePresentation()
        {
            SetItemVisibleByDictionary(_modePresentation, visibleModePresentation);
        }

        private void SetVisibleModeVtc()
        {
            SetItemVisibleByDictionary(_modeVtc, visibleModeVtc);
        }

        // TODO make public?
        private void TimerTimeoutModeRestart()
        {
            RestartTimer(this._timerTimeoutMode);
        }

        /// <summary>
        /// Restarts the volume timeout timer to keep the volume subpage visible during user interaction
        /// </summary>
        public void TimerTimeoutVolumeRestart()
        {
            RestartTimer(this._timerTimeoutVolume);
        }

        /// <summary>
        /// Handles system settings.
        /// </summary>
        /// <param name="FriendlyName"> The friendly name of the setting. </param>
        /// <param name="Settings"> The settings object containing updated values. </param>
        private void System_OnSettingChanged(string FriendlyName, INexusSettings Settings)
        {
            if (Settings is Settings.RoomInformation roomInformation)
            {
                this.SetHeaderName(roomInformation.RoomName);
            }
            else if (Settings is Settings.RoutingGroupNames routingGroupNames)
            {
                var routingGroupType = routingGroupNames.GetType();
                for (uint routingGroup = 1; routingGroup <= MaxRoutingGroup; routingGroup++)
                {
                    var propName = $"RoutingGroup{routingGroup:D2}";
                    var prop = routingGroupType.GetProperty(propName);
                    if (prop != null)
                    {
                        _labelRoutingGroup[routingGroup] = (string)prop.GetValue(routingGroupNames);
                    }
                }
                SetLabelRoutingGroup();
            }
        }
    }
}

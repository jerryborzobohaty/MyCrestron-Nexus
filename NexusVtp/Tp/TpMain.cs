
using Crestron.SimplSharp;
using Crestron.SimplSharpPro;
using Crestron.SimplSharpPro.DeviceSupport;
using Forte.SSPro.UI.Helper.Library.UI;
using Nexus.Driver.Architecture.Configuration;
using Nexus.Driver.Architecture.Enumerations;
using Nexus.Framework.Services;
using Nexus.Utils;
using NexusCommon;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Timers;
using static NexusVtp.SmartGraphics;

namespace NexusVtp
{
    /// <summary>
    /// Set joins and setup event handlers.
    /// </summary>
    public class TpMain
    {
        /// <summary>
        /// Maximum number of routing groups supported
        /// </summary>
        private const int MaxRoutingGroup = 4;
        /// <summary>
        /// Default routing group
        /// </summary>
        private const int DefaultRoutingGroup = 1;
        /// <summary>
        /// Friendly name of the panel for debugging purposes
        /// </summary>
        private string _panelName = string.Empty;
        /// <summary>
        /// The Crestron touchpanel interface wrapper
        /// </summary>
        private Panel _panel;
        /// <summary>
        /// Progress tracking instance for operations
        /// </summary>
        private Progress _progress;
        /// <summary>
        /// Macro execution engine for system actions
        /// </summary>
        private Macro _macro;
        /// <summary>
        /// Smart object for mode selection
        /// </summary>
        private ExtendedSmartObject _mode;
        /// <summary>
        /// Smart object for presentation mode list
        /// </summary>
        private ExtendedSmartObject _modePresentation;
        /// <summary>
        /// Smart object for VTC mode list
        /// </summary>
        private ExtendedSmartObject _modeVtc;
        /// <summary>
        /// Smart object for main menu
        /// </summary>
        private ExtendedSmartObject _mainMenu;
        /// <summary>
        /// Smart object for settings menu
        /// </summary>
        private ExtendedSmartObject _settingsMenu;
        /// <summary>
        /// Smart object for routing group selection
        /// </summary>
        private ExtendedSmartObject _routingGroup;

        /// <summary>
        /// Time in seconds before settings menu triggers during hold
        /// </summary>
        private uint _timeSettingsHold = 3;
        /// <summary>
        /// Timeout in seconds before mode menu automatically closes
        /// </summary>
        private uint _timeTimeoutMode = 10;
        /// <summary>
        /// Timeout in seconds before volume panel automatically closes
        /// </summary>
        private uint _timeTimeoutVolume = 10;
        /// <summary>
        /// Time in seconds before clean return action triggers during hold
        /// </summary>
        private uint _timeCleanReturnHold = 5;
        /// <summary>
        /// Flag indicating whether clean return button is currently held
        /// </summary>
        private bool _cleanReturnHeld = false;
        /// <summary>
        /// Flag indicating whether settings button is currently held
        /// </summary>
        private bool _settingsHeld = false;
        /// <summary>
        /// Visibility state of the info subpage
        /// </summary>
        private bool _visibleInfo = false;
        /// <summary>
        /// Visibility state of the lights subpage
        /// </summary>
        private bool _visibleLights = false;
        /// <summary>
        /// Visibility state of the mixer subpage
        /// </summary>
        private bool _visibleMixer = false;
        /// <summary>
        /// Visibility state of the mode menu
        /// </summary>
        private bool _visibleMode = false;

        private uint _routingGroupSelected = 0;

        /// <summary>
        /// Timer for detecting clean return button hold duration
        /// </summary>
        private Timer _timerCleanReturnHeld;
        /// <summary>
        /// Timer for auto-closing mode menu after timeout
        /// </summary>
        private Timer _timerTimeoutMode;
        /// <summary>
        /// Timer for auto-closing volume panel after timeout
        /// </summary>
        private Timer _timerTimeoutVolume;
        /// <summary>
        /// Timer for detecting settings button hold duration
        /// </summary>
        private Timer _timerSettingsHeld;

        /// <summary>
        /// Main page join numbers
        /// </summary>
        enum Page
        {
            /// <summary>Start/home page</summary>
            Start = 21,
            /// <summary>Main control page</summary>
            Main = 22,
            /// <summary>Setup/settings page</summary>
            Setup = 23,
        }

        /// <summary>
        /// Button join numbers for UI controls
        /// </summary>
        enum Btn
        {
            /// <summary>Lights control button</summary>
            Lights = 19,
            /// <summary>Info panel button</summary>
            Info = 27,
            /// <summary>Mode menu button</summary>
            Mode = 42,
            /// <summary>Presentation mode button</summary>
            ModePresentation = 43,
            /// <summary>VTC mode button</summary>
            ModeVtc = 44,
            /// <summary>Volume increase button</summary>
            VolumeOpen = 26,
            /// <summary>Volume decrease button</summary>
            VolumeClose = 28,
            /// <summary>Cancel shutdown button</summary>
            ShutdownCancel = 30,
            /// <summary>Confirm shutdown button</summary>
            ShutdownConfirm = 40,
            /// <summary>Clean return/reset button</summary>
            CleanReturn = 50,
            /// <summary>Volume mixer panel button</summary>
            VolumeMixer = 60,
            /// <summary>Routing group selector button</summary>
            RoutingGroup = 90,
        }

        /// <summary>
        /// Analog/level join numbers
        /// </summary>
        enum Lvl
        {
            /// <summary>Progress bar level</summary>
            Progress = 5,
        }

        /// <summary>
        /// Text label join numbers
        /// </summary>
        enum Lbl
        {
            HeaderTime = 1,
            HeaderDate = 2,
            HeaderName = 3,
            HeaderMessage = 4,
            Progress = 5,
        }

        /// <summary>
        /// Start page subpage numbers
        /// </summary>
        enum SubpageStart
        {
            /// <summary>Lights control subpage</summary>
            Lights = 19,
            /// <summary>Info panel subpage</summary>
            Info = 27,         
        }

        /// <summary>
        /// Main page subpage numbers
        /// </summary>
        enum SubpageMain
        {
            /// <summary>Power control subpage</summary>
            Power = 30,
            /// <summary>Routing control subpage</summary>
            Routing = 31,
            /// <summary>Share/presentation subpage</summary>
            Share = 32,
            /// <summary>Camera control subpage</summary>
            Camera = 33,
            /// <summary>Phone control subpage</summary>
            Phone = 34,
            /// <summary>Settings subpage</summary>
            Settings = 37,
            /// <summary>Volume control subpage</summary>
            Volume = 62,
            /// <summary>Volume mixer subpage</summary>
            VolumeMixer = 63,
        }

        /// <summary>
        /// Modal/overlay subpage numbers
        /// </summary>
        enum SubpageModal
        {
            /// <summary>Progress indicator subpage</summary>
            Progress = 40,
        }

        /// <summary>
        /// Mode selection subpage numbers
        /// </summary>
        enum SubpageMode
        {
            /// <summary>Mode menu subpage</summary>
            Mode = 42,
            /// <summary>Presentation mode subpage</summary>
            Presentation = 43,
            /// <summary>VTC mode subpage</summary>
            Vtc = 44,
        }

        /// <summary>
        /// System operating modes
        /// </summary>
        enum Mode
        {
            /// <summary>Presentation mode</summary>
            Presentation = 11,
            /// <summary>Phone call mode</summary>
            PhoneCall = 12,
            /// <summary>Video conference mode</summary>
            VideoCall = 13,
        }

        /// <summary>
        /// Main menu item identifiers
        /// </summary>
        enum MainMenu
        {
            /// <summary>Power menu item</summary>
            Power = 11,
            /// <summary>Mode menu item</summary>
            Mode = 12,
            /// <summary>Routing menu item</summary>
            Routing = 13,
            /// <summary>Share menu item</summary>
            Share = 14,
            /// <summary>Camera menu item</summary>
            Camera = 15,
            /// <summary>Phone menu item</summary>
            Phone = 16,
            /// <summary>Tuner menu item</summary>
            Tuner = 17,
            /// <summary>USB menu item</summary>
            Usb = 18,
            /// <summary>Settings menu item</summary>
            Settings = 19,
        }

        /// <summary>
        /// Settings menu item identifiers
        /// </summary>
        enum SettingsMenu
        {
            /// <summary>Clean/reset settings item</summary>
            Clean = 11,
        }

        private Dictionary<uint, uint> subpageMapSettingsMenu = new Dictionary<uint, uint>
        {
            { 1, 51 },
            { 2, 52 },
            { 3, 53 },
        };

        private Dictionary<uint, uint> subpageMapRoutingGroup = new Dictionary<uint, uint>
        {
            { 1, 91 },  
            { 2, 92 },  
            { 3, 93 },  
            { 4, 94 },  
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
        /// <param name="panel">The touchpanel interface wrapper</param>
        /// <param name="panelName">Description of the panel for debugging</param>
        /// <param name="progress">Progress tracking instance for operations</param>
        /// <param name="macro">Macro execution engine for system actions</param>
        public TpMain(Panel panel, string panelName, Progress progress, Macro macro)
        {
            this._panel = panel;
            this._panelName = panelName;
            this._progress = progress;
            this._macro = macro;
            Initialize();
        }

        /// <summary>
        /// Sets the header time display text
        /// </summary>
        /// <param name="data">Formatted time string</param>
        public void SetHeaderTime(string data)
        {
            _panel.SetSerial((uint)Lbl.HeaderTime, data);
        }

        /// <summary>
        /// Sets the header date display text
        /// </summary>
        /// <param name="data">Formatted date string</param>
        public void SetHeaderDate(string data)
        {
            _panel.SetSerial((uint)Lbl.HeaderDate, data);
        }

        /// <summary>
        /// Sets the header message display text
        /// </summary>
        /// <param name="data">Message string</param>
        public void SetHeaderMessage(string data)
        {
            _panel.SetSerial((uint)Lbl.HeaderMessage, data);
        }

        /// <summary>
        /// Sets the header name (room name) display text
        /// </summary>
        /// <param name="data">Room name string</param>
        private void SetHeaderName(string data)
        {
            _panel.SetSerial((uint)Lbl.HeaderName, data);
        }

        /// <summary>
        /// Initializes UI components, event handlers, and button/smart object mappings
        /// </summary>
        private void Initialize()
        {
            try
            {
                //this.SetHeaderName(this._panelName); //now handled in Nexus Settings
                NexusServiceManager.System.OnSettingChanged += System_OnSettingChanged;
                this.SetHeaderMessage("MESSAGE");

                // subscribe to progress events and map to existing handlers
                this._progress.GaugeChanged += (s, value) => this.OnProgressGauge(value);
                this._progress.CountChanged += (s, count) => this.OnProgressCount(count);
                this._progress.MessageChanged += (s, msg) => this.OnProgressMessage(msg);
                this._progress.Started += (s, e) => this.OnProgressStart();
                this._progress.Stopped += (s, e) => this.OnProgressStop();

                // add macro actions
                _macro.AddAction("Shutdown", this.SetMacroShutdown);
                _macro.AddAction("Presentation", this.SetMacroPresentation);
                _macro.AddAction("PhoneCall", this.SetMacroPhoneCall);
                _macro.AddAction("VideoCall", this.SetMacroVideoCall);

                // timers
                this._timerCleanReturnHeld = new Timer(this._timeCleanReturnHold * 1000);
                this._timerSettingsHeld = new Timer(this._timeSettingsHold * 1000);
                this._timerTimeoutMode = new Timer(this._timeTimeoutMode * 1000);
                this._timerTimeoutVolume = new Timer(this._timeTimeoutVolume * 1000);

                // timer event handlers
                this._timerCleanReturnHeld.Elapsed += OnTimerCleanReturnHeld;
                this._timerSettingsHeld.Elapsed += OnTimerSettingsHeld;
                this._timerTimeoutMode.Elapsed += OnTimerTimeoutMode;
                this._timerTimeoutVolume.Elapsed += OnTimerTimeoutVolume;

                // nav buttons - joins - TODO consolidate some joins
                var btnInfo = _panel.AddButtonGroup("BtnInfo", (uint)Btn.Info);
                btnInfo.OnPanelButtonGroupChange += OnBtnInfo;

                var btnLights = _panel.AddButtonGroup("BtnLights", (uint)Btn.Lights);
                btnLights.OnPanelButtonGroupChange += OnBtnLights;

                var btnMode = _panel.AddButtonGroup("BtnMode", (uint)Btn.Mode);
                btnMode.OnPanelButtonGroupChange += OnBtnMode;

                var btnModePresentation = _panel.AddButtonGroup("BtnModePresentation", (uint)Btn.ModePresentation);
                btnModePresentation.OnPanelButtonGroupChange += OnBtnModePresentation;

                var btnModeVtc = _panel.AddButtonGroup("BtnModeVtc", (uint)Btn.ModeVtc);
                btnModeVtc.OnPanelButtonGroupChange += OnBtnModeVtc;

                var btnVolumeOpen = _panel.AddButtonGroup("BtnVolumeOpen", (uint)Btn.VolumeOpen);
                btnVolumeOpen.OnPanelButtonGroupChange += this.OnBtnVolumeOpen;

                var btnVolumeClose = _panel.AddButtonGroup("BtnVolumeClose", (uint)Btn.VolumeClose);
                btnVolumeClose.OnPanelButtonGroupChange += this.OnBtnVolumeClose;

                var btnVolumeMixer = _panel.AddButtonGroup("BtnVolumeMixer", (uint)Btn.VolumeMixer);
                btnVolumeMixer.OnPanelButtonGroupChange += this.OnBtnVolumeMixer;

                var btnShutdownConfirm = _panel.AddButtonGroup("BtnShutdownConfirm", (uint)Btn.ShutdownConfirm);
                btnShutdownConfirm.OnPanelButtonGroupChange += OnBtnShutdownConfirm;

                var btnShutdownCancel = _panel.AddButtonGroup("BtnShutdownCancel", (uint)Btn.ShutdownCancel);
                btnShutdownCancel.OnPanelButtonGroupChange += OnBtnShutdownCancel;

                var btnCleanReturn = _panel.AddButtonGroup("BtnCleanReturn", (uint)Btn.CleanReturn);
                btnCleanReturn.OnPanelButtonGroupChange += OnBtnCleanReturn;

                // add smart object 
                _mode = _panel.AddSmartObject("Mode", _panel.ThePanel.SmartObjects[(int)SgId.Mode]);
                _mainMenu = _panel.AddSmartObject("MainMenu", _panel.ThePanel.SmartObjects[(int)SgId.MainMenu]);
                _settingsMenu = _panel.AddSmartObject("SettingsMenu", _panel.ThePanel.SmartObjects[(int)SgId.SettingsMenu]);
                _routingGroup = _panel.AddSmartObject("RoutingGroup", _panel.ThePanel.SmartObjects[(int)SgId.RoutingGroup]);
                _modePresentation = _panel.AddSmartObject("ModePresentation", _panel.ThePanel.SmartObjects[(int)SgId.ModePresentation]);
                _modeVtc = _panel.AddSmartObject("ModeVtc", _panel.ThePanel.SmartObjects[(int)SgId.ModeVtc]);

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

        private void OnBtnCleanReturn(object o, ButtonGroupEventArgs e)
        {
            if (e.Sig.BoolValue)
            {
                _panel.SetBoolean((int)Btn.CleanReturn, true);
                this._cleanReturnHeld = false;
                RestartTimer(this._timerCleanReturnHeld);
                NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Debug, $"{_panelName}  Clean Return Press");
            }
            else
            {
                _panel.SetBoolean((int)Btn.CleanReturn, false);
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
                _panel.SetBoolean((int)SubpageStart.Info, this._visibleInfo);
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
                _panel.SetBoolean((int)SubpageStart.Lights, this._visibleLights);
            }
        }

        private void OnBtnShutdownCancel(object o, ButtonGroupEventArgs e)
        {
            if (e.Sig.BoolValue)
            {
                _mainMenu.SetBoolean((uint)MainMenu.Power, false);
                _panel.SetBoolean((int)SubpageMain.Power, false);
            }
        }

        private void OnBtnShutdownConfirm(object o, ButtonGroupEventArgs e)
        {
            if (e.Sig.BoolValue)
            {
                this._macro.RunAction("Shutdown");
            }
        }

        private void OnBtnMode(object o, ButtonGroupEventArgs e)
        {
            if (e.Sig.BoolValue)
            {
                NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Debug, $"{_panelName} {MethodBase.GetCurrentMethod().Name}");
                this._visibleMode = !this._visibleMode;
                _mainMenu.SetBoolean((uint)MainMenu.Mode, this._visibleMode);
                _panel.SetBoolean((int)SubpageMode.Mode, this._visibleMode);
                TimerTimeoutModeRestart();
            }
        }

        private void OnBtnModePresentation(object o, ButtonGroupEventArgs e)
        {
            if (e.Sig.BoolValue)
            {
                _panel.SetBoolean((int)SubpageMode.Presentation, false);
                TimerTimeoutModeRestart();
            }
        }

        private void OnBtnModeVtc(object o, ButtonGroupEventArgs e)
        {
            if (e.Sig.BoolValue)
            {
                _panel.SetBoolean((int)SubpageMode.Vtc, false);
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

        /// <summary>
        /// Handles main menu Smart Object button selections
        /// </summary>
        /// <param name="o">Event sender</param>
        /// <param name="ea">Smart Object event arguments</param>
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
                            _panel.SetBoolean((int)SubpageMain.Power, true);
                        }            
                        break;
                    case (int)MainMenu.Mode:
                    if (ea.Sig.BoolValue)
                        {
                            NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Debug, $"{_panelName} {MethodBase.GetCurrentMethod().Name} Mode");
                            this._visibleMode = true;
                            _mainMenu.SetBoolean((uint)MainMenu.Mode, true);
                            _panel.SetBoolean((int)SubpageMode.Mode, this._visibleMode);
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
                                _panel.SetBoolean((int)SubpageMain.Settings, true);
                            }
                        }
                        break;
                    //Normal menu items, could have done a default case and handled all there but then have to do some mapping, thought this be easier to modify though more code
                    case (int)MainMenu.Routing:
                        if (ea.Sig.BoolValue)
                        {            
                            this.ResetMenus();
                            this.RestoreRoutingGroup();
                            _mainMenu.SetBoolean((uint)MainMenu.Routing, true);
                            _panel.SetBoolean((int)SubpageMain.Routing, true);
                        }
                        break;
                    case (int)MainMenu.Share:
                        if (ea.Sig.BoolValue)
                        {
                            this.ResetMenus();
                            _mainMenu.SetBoolean((uint)MainMenu.Share, true);
                            _panel.SetBoolean((int)SubpageMain.Share, true);
                        }
                        break;
                    case (int)MainMenu.Camera:
                        if (ea.Sig.BoolValue)
                        {
                            this.ResetMenus();
                            _mainMenu.SetBoolean((uint)MainMenu.Camera, true);
                            _panel.SetBoolean((int)SubpageMain.Camera, true);
                        }
                        break;
                    case (int)MainMenu.Phone:
                        if (ea.Sig.BoolValue)
                        {
                            this.ResetMenus();
                            _mainMenu.SetBoolean((uint)MainMenu.Phone, true);
                            _panel.SetBoolean((int)SubpageMain.Phone, true);
                        }
                        break;
                }
            }
        }

        /// <summary>
        /// Handles mode selection Smart Object events
        /// </summary>
        /// <param name="o">Event sender</param>
        /// <param name="ea">Smart Object event arguments</param>
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
                                _panel.SetBoolean((uint)SubpageMode.Presentation, true);
                            }
                            else
                            { 
                                this._macro.RunAction("Presentation"); 
                            }                     
                            break;
                        }
                        case (int)Mode.PhoneCall:
                        {
                            this._macro.RunAction("PhoneCall");
                            break;
                        }
                        case (int)Mode.VideoCall:
                        {
                            bool supportsList = true;
                            if (supportsList)
                            {
                                _panel.SetBoolean((uint)SubpageMode.Vtc, true);
                            }
                            else 
                            {
                                this._macro.RunAction("VideoCall");
                            }
                            break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Handles presentation mode selection from Smart Object list
        /// </summary>
        /// <param name="o">Event sender</param>
        /// <param name="ea">Smart Object event arguments</param>
        private void OnModePresentation(object o, SmartObjectEventArgs ea)
        {
            NexusDebugSmartObjectEvent(o, ea);
            if (ea.Sig.BoolValue)
            {
                int num = (int)ea.Sig.Number;
                NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Debug, $"{_panelName} {MethodBase.GetCurrentMethod().Name} {num}");
                //TODO setup individual macros
                this._macro.RunAction("Presentation");
            }
        }

        /// <summary>
        /// Handles VTC mode selection from Smart Object list
        /// </summary>
        /// <param name="o">Event sender</param>
        /// <param name="ea">Smart Object event arguments</param>
        private void OnModeVtc(object o, SmartObjectEventArgs ea)
        {
            NexusDebugSmartObjectEvent(o, ea);
            if (ea.Sig.BoolValue)
            {
                int num = (int)ea.Sig.Number;
                NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Debug, $"{_panelName} {MethodBase.GetCurrentMethod().Name} {num}");
                //TODO setup individual macros
                this._macro.RunAction("VideoCall");
            }
        }

        /// <summary>
        /// Handles routing group selection from Smart Object list
        /// </summary>
        /// <param name="o">Event sender</param>
        /// <param name="ea">Smart Object event arguments</param>
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
                _routingGroupSelected = GetItemNumberFromSignalName(ea.Sig.Name);
                NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Debug, $"{_panelName} {MethodBase.GetCurrentMethod().Name} _routingGroupSelected {_routingGroupSelected}");
                _routingGroup.SetBoolean(ea.Sig.Number, true);
                if (this.subpageMapRoutingGroup.TryGetValue(_routingGroupSelected, out uint subpage))
                {
                    _panel.SetBoolean(subpage, true);
                }
                else
                {
                    NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Errors, $"{_panelName} {MethodBase.GetCurrentMethod().Name} {_routingGroupSelected} subpage not defined");
                }
            }
        }

        /// <summary>
        /// Handles settings menu selection from Smart Object list
        /// </summary>
        /// <param name="o">Event sender</param>
        /// <param name="ea">Smart Object event arguments</param>
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
                uint item = GetItemNumberFromSignalName(ea.Sig.Name);
                if (item != (uint)SettingsMenu.Clean)
                {
                    _settingsMenu.SetBoolean(ea.Sig.Number, true);
                }
                if (this.subpageMapSettingsMenu.TryGetValue(item, out uint subpage))
                {
                    _panel.SetBoolean(subpage, true);
                }
            }
        }

        /// <summary>
        /// Handles progress gauge value changes
        /// </summary>
        /// <param name="data">The gauge value (must be ushort)</param>
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
            _panel.SetAnalog((uint)Lvl.Progress, (short)value);
        }

        /// <summary>
        /// Handles progress count changes
        /// </summary>
        /// <param name="data">The count value</param>
        private void OnProgressCount(object data)
        {
            NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Debug, $"{_panelName} {MethodBase.GetCurrentMethod().Name} {data}");
        }

        /// <summary>
        /// Handles progress message updates
        /// </summary>
        /// <param name="data">The message text (must be string)</param>
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
            _panel.SetSerial((uint)Lbl.Progress, value);
        }

        /// <summary>
        /// Handles progress operation start event - shows progress modal
        /// </summary>
        private void OnProgressStart()
        {
            NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Debug, $"{_panelName} {MethodBase.GetCurrentMethod().Name}");
            _panel.SetBoolean((int)SubpageModal.Progress, true);
        }

        /// <summary>
        /// Handles progress operation stop event - hides progress modal
        /// </summary>
        private void OnProgressStop()
        {
            NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Debug, $"{_panelName} {MethodBase.GetCurrentMethod().Name}");
            _panel.SetBoolean((int)SubpageModal.Progress, false);
        }

        /// <summary>
        /// Handles clean return button hold timer elapsed event - resets settings subpages
        /// </summary>
        /// <param name="o">Event sender (Timer)</param>
        /// <param name="e">Elapsed event arguments</param>
        private void OnTimerCleanReturnHeld(object o, ElapsedEventArgs e)
        {
            NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Debug, $"{_panelName}  Clean Return held");
            ResetSettingsSubpages();
        }

        /// <summary>
        /// Handles settings button hold timer elapsed event - navigates to setup page
        /// </summary>
        /// <param name="o">Event sender (Timer)</param>
        /// <param name="e">Elapsed event arguments</param>
        private void OnTimerSettingsHeld(object o, ElapsedEventArgs e)
        {
            this._settingsHeld = true;
            _panel.SetBoolean((int)Page.Setup, true);
            _panel.SetBoolean((int)Page.Setup, false);
        }

        /// <summary>
        /// Handles mode menu timeout timer - closes mode menu after timeout
        /// </summary>
        /// <param name="o">Event sender (Timer)</param>
        /// <param name="e">Elapsed event arguments</param>
        private void OnTimerTimeoutMode(object o, ElapsedEventArgs e)
        {
            this.ResetModeMenu();
            this.ResetModeSubpages();
        }

        /// <summary>
        /// Handles volume panel timeout timer - closes volume panel after timeout
        /// </summary>
        /// <param name="o">Event sender (Timer)</param>
        /// <param name="e">Elapsed event arguments</param>
        private void OnTimerTimeoutVolume(object o, ElapsedEventArgs e)
        {
            this.SetVisibleVolume(false);
            this.ResetVolumeMixer();
        }

        /// <summary>
        /// Resets all menus and subpages to closed state
        /// </summary>
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

        /// <summary>
        /// Resets main menu to deselected state
        /// </summary>
        private void ResetMainMenu()
        {
            foreach (MainMenu cue in Enum.GetValues(typeof(MainMenu)))
            {
                _mainMenu.SetBoolean((uint)cue, false);
            }
        }

        /// <summary>
        /// Closes all main page subpages
        /// </summary>
        private void ResetMainSubpages()
        {
            foreach (SubpageMain join in Enum.GetValues(typeof(SubpageMain)))
            {
                _panel.SetBoolean((uint)join, false);
            }
        }

        /// <summary>
        /// Resets mode menu visibility to hidden
        /// </summary>
        private void ResetModeMenu()
        {
            this._visibleMode = false;
            _mainMenu.SetBoolean((uint)MainMenu.Mode, this._visibleMode);
        }

        /// <summary>
        /// Closes all mode page subpages
        /// </summary>
        private void ResetModeSubpages()
        {
            foreach (SubpageMode join in Enum.GetValues(typeof(SubpageMode)))
            {
                _panel.SetBoolean((uint)join, false);
            }
        }

        /// <summary>
        /// Resets routing group menu to deselected state
        /// </summary>
        private void ResetRoutingGroup()
        {
            ResetItemSelected(_routingGroup, visibleRoutingGroup);
        }

        /// <summary>
        /// Closes all routing group subpages
        /// </summary>
        private void ResetRoutingGroupSubpages()
        {
            foreach (var subpage in subpageMapRoutingGroup.Values)
            {
                _panel.SetBoolean((uint)subpage, false);
            }
        }

        /// <summary>
        /// Resets settings menu to deselected state
        /// </summary>
        private void ResetSettingsMenu()
        {
            ResetItemSelected(_settingsMenu, visibleSettingsMenu);
        }

        /// <summary>
        /// Closes all settings subpages
        /// </summary>
        private void ResetSettingsSubpages()
        {
            foreach (var subpage in subpageMapSettingsMenu.Values)
            {
                _panel.SetBoolean((uint)subpage, false);
            }
        }

        /// <summary>
        /// Restarts a timer by stopping and starting it
        /// </summary>
        /// <param name=\"timer\">The timer to restart</param>
        private void RestartTimer(Timer timer)
        {
            if (timer.Enabled)
            {
                timer.Stop();
            }
            timer.Start();
        }

        /// <summary>
        /// Resets volume mixer to hidden state
        /// </summary>
        private void ResetVolumeMixer()
        {
            this._visibleMixer = false;
            this.SetVisibleVolumeMixer(this._visibleMixer);
        }

        /// <summary>
        /// Restores routing group menu to previous state
        /// </summary>
        private void RestoreRoutingGroup()
        {
            if (_routingGroupSelected == 0)
                _routingGroupSelected = DefaultRoutingGroup;
            SetItemSelected(_routingGroup, _routingGroupSelected, true);
            if (this.subpageMapRoutingGroup.TryGetValue(_routingGroupSelected, out uint subpage))
            {
                _panel.SetBoolean(subpage, true);
            }
        }

        /// <summary>
        /// Sets enable state for mode items
        /// </summary>
        private void SetEnableMode()
        {
            SetItemEnabledByDictionary(_mode, enableMode);
        }

        /// <summary>
        /// Sets enable state for main menu items
        /// </summary>
        private void SetEnableMainMenu()
        {
            SetItemEnabledByDictionary(_mainMenu, enableMainMenu);
        }

        /// <summary>
        /// Sets labels for presentation mode list items
        /// </summary>
        private void SetLabelModePresentation()
        {
            SetItemTextByDictionary(_modePresentation, labelModePresentation);
        }

        /// <summary>
        /// Sets labels for VTC mode list items
        /// </summary>
        private void SetLabelModeVtc()
        {
            SetItemTextByDictionary(_modeVtc, labelModeVtc);
        }

        /// <summary>
        /// Sets labels for routing group list items
        /// </summary>
        private void SetLabelRoutingGroup()
        {
            SetItemTextByDictionary(_routingGroup, _labelRoutingGroup);
        }

        /// <summary>
        /// Sets visibility for mode menu items
        /// </summary>
        private void SetVisibleMode()
        {
            SetItemVisibleByDictionary(_mode, visibleMode);
        }

        /// <summary>
        /// Executes macro action for presentation mode - shows routing menu
        /// </summary>
        private void SetMacroPresentation()
        {
            ResetMenus();
            _panel.SetBoolean((int)Page.Main, true);
            _panel.SetBoolean((int)Page.Main, false);
            _mainMenu.SetBoolean((uint)MainMenu.Routing, true);
            _panel.SetBoolean((int)SubpageMain.Routing, true);
        }

        /// <summary>
        /// Executes macro action for shutdown - returns to start page
        /// </summary>
        private void SetMacroShutdown()
        {
            ResetMenus();
            _panel.SetBoolean((int)Page.Start, true);
            _panel.SetBoolean((int)Page.Start, false);
        }

        /// <summary>
        /// Executes macro action for phone call mode - shows phone menu
        /// </summary>
        private void SetMacroPhoneCall()
        {
            ResetMenus();
            _panel.SetBoolean((int)Page.Main, true);
            _panel.SetBoolean((int)Page.Main, false);
            _mainMenu.SetBoolean((uint)MainMenu.Phone, true);
            _panel.SetBoolean((int)SubpageMain.Phone, true);
        }

        /// <summary>
        /// Executes macro action for video call mode - shows camera menu
        /// </summary>
        private void SetMacroVideoCall()
        {
            ResetMenus();
            _panel.SetBoolean((int)Page.Main, true);
            _panel.SetBoolean((int)Page.Main, false);
            _mainMenu.SetBoolean((uint)MainMenu.Camera, true);
            _panel.SetBoolean((int)SubpageMain.Camera, true);
        }

        /// <summary>
        /// Sets visibility for main menu items
        /// </summary>
        private void SetVisibleMainMenu()
        {
            SetItemVisibleByDictionary(_mainMenu, visibleMainMenu);
        }

        /// <summary>
        /// Sets visibility for routing group menu items
        /// </summary>
        private void SetVisibleRoutingGroup()
        {
            _panel.SetBoolean((int)Btn.RoutingGroup, true);
            SetItemVisibleByDictionary(_routingGroup, visibleRoutingGroup);
        }

        /// <summary>
        /// Sets visibility for settings menu items
        /// </summary>
        private void SetVisibleSettingsMenu()
        {
            SetItemVisibleByDictionary(_settingsMenu, visibleSettingsMenu);
        }

        /// <summary>
        /// Sets visibility state of the volume control panel
        /// </summary>
        /// <param name=\"state\">True to show, false to hide the volume panel</param>
        private void SetVisibleVolume(bool state)
        {
            _panel.SetBoolean((int)SubpageMain.Volume, state);
        }

        /// <summary>
        /// Sets visibility state of the volume mixer subpage
        /// </summary>
        /// <param name=\"state\">True to show, false to hide the mixer</param>
        private void SetVisibleVolumeMixer(bool state)
        {
            _panel.SetBoolean((int)SubpageMain.VolumeMixer, state);
        }

        /// <summary>
        /// Sets visibility for presentation mode list items
        /// </summary>
        private void SetVisibleModePresentation()
        {
            SetItemVisibleByDictionary(_modePresentation, visibleModePresentation);
        }

        /// <summary>
        /// Sets visibility for VTC mode list items
        /// </summary>
        private void SetVisibleModeVtc()
        {
            SetItemVisibleByDictionary(_modeVtc, visibleModeVtc);
        }

        /// <summary>
        /// Restarts the mode menu timeout timer
        /// </summary>
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
        /// Handles system settings changes for room information and routing group names
        /// </summary>
        /// <param name="FriendlyName">The friendly name of the setting</param>
        /// <param name="Settings">The settings object containing updated values</param>
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

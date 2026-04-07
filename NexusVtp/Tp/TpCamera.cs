using Crestron.SimplSharp;
using Crestron.SimplSharpPro;
using Crestron.SimplSharpPro.UI;
using Forte.SSPro.UI.Helper.Library.UI;
using Nexus.Driver.Architecture.Components;
using Nexus.Driver.Architecture.Configuration;
using Nexus.Driver.Architecture.Devices;
using Nexus.Framework.Services;
using Nexus.Utils;
using Nexus.Vaddio.RoboshotIP.Driver;
using NexusCommon;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Timers;
using static NexusVtp.SmartGraphics;

namespace NexusVtp
{ 
    /// <summary>
    /// Manages camera control functionality for the touchpanel interface
    /// </summary>
    public class TpCamera
    {
        private const int MaxPreset = 6;
        /// <summary>
        /// Time in seconds until a faster command is sent during hold
        /// </summary>
        const int HoldTime = 2;
        /// <summary>
        /// Time in seconds until save mode automatically exits
        /// </summary>
        const int SaveTimeout = 5;

        private string _panelName = string.Empty;
        private Panel _Panel;
        private ExtendedSmartObject _select;
        private ExtendedSmartObject _dpad;
        private ExtendedSmartObject _preset;
        //private VaddioRoboshotIP _camera;
        private bool _saveMode = false;
        private Dictionary<uint, ICamera> _cameras;

        private Dictionary<uint, (ExtendedSmartObject Id, SgCue Cue)> _cuesVisibleSelect;
        private Dictionary<uint, (ExtendedSmartObject Id, SgCue Cue)> _cuesLabelSelect;
        private Dictionary<int, (ExtendedSmartObject Id, SgCue Cue)> _cuesVisiblePreset;
        private Dictionary<uint, (ExtendedSmartObject Id, SgCue Cue)> _cuesLabelPreset;
        
        private Timer _timerHold;
        private Timer _timerSaveTimeout;
        private readonly object _holdLock = new object();
        private Direction? _activeDirection = null;
        private bool _fastCommandSent = false;
        private bool _autoFocus = false;
        private ICamera _selectedCamera;

        //private Dictionary<int, string> labelPreset = new Dictionary<int, string>
        //{
        //    { 1, "Preset 1" },
        //    { 2, "Preset 2" },
        //    { 3, "Preset 3" },
        //    { 4, "Preset 4" },
        //    { 5, "Preset 5" },
        //    { 6, "Preset 6" },
        //};

        private Dictionary<uint, string> _labelPreset = new Dictionary<uint, string>();

        /// <summary>
        /// Stores preset labels per camera, keyed by camera friendly name
        /// </summary>
        private Dictionary<string, Dictionary<uint, string>> _presetLabelsByCamera = new Dictionary<string, Dictionary<uint, string>>();

        private Dictionary<uint, string> _labelSelect = new Dictionary<uint, string>();

        private Dictionary<int, bool> visiblePreset = new Dictionary<int, bool>
        {
            { 1, true },
            { 2, true },
            { 3, true },
            { 4, true },
            { 5, true },
            { 6, true },
        };

        private Dictionary<uint, bool> _visibleSelect = new Dictionary<uint, bool>();


        /// <summary>
        /// Initializes a new instance of the <see cref="TpCamera"/> class.
        /// </summary>
        /// <param name="panel">The panel instance</param>
        /// <param name="panelName">Description of the panel for debugging</param>
        /// <param name="camera">The camera device</param>
        //public TpCamera(Panel panel, string panelName, VaddioRoboshotIP camera)
        public TpCamera(Panel panel, string panelName, Dictionary<uint, ICamera> cameras)
        {
            this._Panel = panel;
            this._panelName = panelName;
            this._cameras = cameras;
            Initialize();
        }

        /// <summary>
        /// Button join numbers for camera controls
        /// </summary>
        enum Btn
        {
            ZoomIn = 605,
            ZoomOut = 606,
            FocusIn = 607,
            FocusOut = 608,
            FocusAuto = 609,
            Save = 637,
        }

        /// <summary>
        /// Directional movement options for camera pan/tilt
        /// </summary>
        enum Direction
        {
            Up = 1,
            Down = 2,
            Left = 3,
            Right = 4,
        }
        /// <summary>
        /// Camera movement speed values
        /// </summary>
        enum Speed
        {
            Press = 5,
            Held = 15,
        }

        /// <summary>
        /// Initializes UI components, event handlers, and camera feedback subscriptions
        /// </summary>
        private void Initialize()
        {
            try
            {
                NexusServiceManager.System.OnSettingChanged += System_OnSettingChanged;

                _timerHold = new Timer(HoldTime * 1000) { AutoReset = false };
                _timerHold.Elapsed += OnTimerHoldElapsed;

                _timerSaveTimeout = new Timer(SaveTimeout * 1000) { AutoReset = false };
                _timerSaveTimeout.Elapsed += OnTimerSaveTimeoutElapsed;

                // buttons - joins
                var bgZoom = _Panel.AddButtonGroup("Zoom", (uint)Btn.ZoomIn, (uint)Btn.ZoomOut);
                bgZoom.OnPanelButtonGroupChange += OnBgZoom;

                var bgFocus = _Panel.AddButtonGroup("Focus", (uint)Btn.FocusIn, (uint)Btn.FocusAuto);
                bgFocus.OnPanelButtonGroupChange += OnBgFocus;

                var btnSave = _Panel.AddButtonGroup("Save", (uint)Btn.Save);
                btnSave.OnPanelButtonGroupChange += OnBtnSave;

                // smart graphics
                _select = _Panel.AddSmartObject("Select", _Panel.ThePanel.SmartObjects[(int)SgId.CameraSelect]);
                _select.OnSmartObjectSignalChange += OnSelect;
                _cuesVisibleSelect = new Dictionary<uint, (ExtendedSmartObject Id, SgCue Cue)>();
                for (uint i = 1; i <= _cameras.Count; i++)
                {
                    _cuesVisibleSelect[i] = (_select, (SgCue)(VisibleCueItemBase + i));
                    _visibleSelect[i] = true; // default all cameras visible
                };
                SetVisibleSelect();

                _cuesLabelSelect = new Dictionary<uint, (ExtendedSmartObject Id, SgCue Cue)>();
                for (uint i = 1; i <= _cameras.Count; i++)
                {
                    _cuesLabelSelect[i] = (_select, (SgCue)(CueItemBase + i));
                    //_labelSelect[i] = (_cameras[i] as VaddioRoboshotIP)?.Name ?? $"Camera {i}";
                    _labelSelect[i] = (_cameras[i] as VaddioRoboshotIP)?.Name ?? $"Camera {i}";
                };
                SetLabelSelect();
                
                _dpad = _Panel.AddSmartObject("Dpad", _Panel.ThePanel.SmartObjects[(int)SgId.CameraDpad]);
                _dpad.OnSmartObjectSignalChange += OnDpad;

                _preset = _Panel.AddSmartObject("CameraPreset", _Panel.ThePanel.SmartObjects[(int)SgId.CameraPreset]);
                _preset.OnSmartObjectSignalChange += OnPreset;
                _cuesVisiblePreset = new Dictionary<int, (ExtendedSmartObject Id, SgCue Cue)>();
                for (int i = 1; i <= visiblePreset.Count; i++)  
                {
                    _cuesVisiblePreset[i] = (_preset, (SgCue)(VisibleCueItemBase + i));
                }
                SetVisiblePreset();

                _cuesLabelPreset = new Dictionary<uint, (ExtendedSmartObject Id, SgCue Cue)>();
                //for (int i = 1; i <= labelPreset.Count; i++)
                for (uint i = 1; i <= MaxPreset; i++)
                {
                    _cuesLabelPreset[i] = (_preset, (SgCue)(CueItemBase + i));
                }
                SetLabelPreset();
                

                //feedback handler
                //_camera.OnAutoFocusChange += OnAutoFocusChange;
            }
            catch (Exception err)
            {
                NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Errors, $"{_panelName} {MethodBase.GetCurrentMethod().Name}: {GetInnerErr.GetInnermostException(err)}");
                NexusServiceManager.System.Log(Nexus.Driver.Architecture.Enumerations.LoggingLevels.Exceptions, $"{_panelName} {MethodBase.GetCurrentMethod().Name}: {GetInnerErr.GetInnermostException(err)}");
            }
        }

        /// <summary>
        /// Handles zoom button group events (zoom in/out)
        /// </summary>
        /// <param name="o">The source object</param>
        /// <param name="e">Button group event arguments</param>
        private void OnBgZoom(object o, ButtonGroupEventArgs e)
        {
            if (e.Sig.BoolValue)
            {
                NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Debug, $"{_panelName} {MethodBase.GetCurrentMethod().Name} {e.Sig.Number}");
                switch (e.Sig.Number)
                {
                    case (uint)Btn.ZoomIn:
                        NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Debug, $"{_panelName} {MethodBase.GetCurrentMethod().Name} in");
                        //_camera.ZoomIn();
                        break;
                    case (uint)Btn.ZoomOut:
                        NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Debug, $"{_panelName} {MethodBase.GetCurrentMethod().Name} out");
                        //_camera.ZoomOut();
                        break;
                }
            }
            else
            {
                SendCameraStop();
            }
        }

        /// <summary>
        /// Handles focus button group events (focus in/out/auto)
        /// </summary>
        /// <param name="o">The source object</param>
        /// <param name="e">Button group event arguments</param>
        private void OnBgFocus(object o, ButtonGroupEventArgs e)
        {
            if (e.Sig.BoolValue)
            {
                NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Debug, $"{_panelName} {MethodBase.GetCurrentMethod().Name} {e.Sig.Number}");
                switch (e.Sig.Number)
                {
                    case (uint)Btn.FocusIn:
                        NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Debug, $"{_panelName} {MethodBase.GetCurrentMethod().Name} in");
                        //_camera.FocusIn();
                        break;
                    case (uint)Btn.FocusOut:
                        NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Debug, $"{_panelName} {MethodBase.GetCurrentMethod().Name} out");
                        //_camera.FocusOut();
                        break;
                    case (uint)Btn.FocusAuto:
                        _autoFocus = !_autoFocus;
                        SetAutoFocus();
                        NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Debug, $"{_panelName} {MethodBase.GetCurrentMethod().Name} auto {_autoFocus}");
                        //_camera.AutoFocus(_autoFocus);
                        break;
                }
            }
            else
            {
                SendCameraStop();
            }
        }

        /// <summary>
        /// Handles save button press to toggle preset save mode
        /// </summary>
        /// <param name="o">The source object</param>
        /// <param name="e">Button group event arguments</param>
        private void OnBtnSave(object o, ButtonGroupEventArgs e)
        {
            if (e.Sig.BoolValue)
            {
                this._saveMode = !this._saveMode;
                _Panel.SetBoolean((int)Btn.Save, this._saveMode);
            }
        }

        /// <summary>
        /// Handles camera selection Smart Object events
        /// </summary>
        /// <param name="o">The source object</param>
        /// <param name="ea">Smart Object event arguments</param>
        private void OnSelect(object o, SmartObjectEventArgs ea)
        {
            NexusDebugSmartObjectEvent(o, ea);
            int num = (int)ea.Sig.Number;
            if (!Enum.IsDefined(typeof(SgCue), num))
            {
                NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Debug, $"{_panelName} {MethodBase.GetCurrentMethod().Name} {num} not defined");
                return; 
            }       

            if (ea.Sig.BoolValue)
            {
                ResetCueItemBooleans(_select);
                _select.SetBoolean((uint)num, true);
                uint item = GetIndexFromItemCue(num);
                NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Debug,$"{_panelName} item {item}");
                if (_cameras.TryGetValue(item, out var cam))
                {
                    _selectedCamera = cam;             
                    NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Debug,$"{_panelName} Selected item {item}");
                    if (_selectedCamera != null)
                    {
                        UpdatePresetsForSelectedCamera(); 
                        string cameraName = (_selectedCamera as VaddioRoboshotIP)?.Name;
                        NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Debug, $"{_panelName} Selected {cameraName}");
                    }
                }
                else
                {
                    NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Warning,
                        $"{_panelName} Camera {num} not found");
                }
            }
        }
        /// <summary>
        /// Handles directional pad Smart Object events for camera pan/tilt control
        /// </summary>
        /// <param name="o">The source object</param>
        /// <param name="ea">Smart Object event arguments</param>
        private void OnDpad(object o, SmartObjectEventArgs ea)
        {
            NexusDebugSmartObjectEvent(o, ea);
            Direction direction = (Direction)ea.Sig.Number;
            if (ea.Sig.BoolValue)
            {
                // press
                switch (direction)
                {
                    case Direction.Up: HandleDpadPress(Direction.Up); break;
                    case Direction.Down: HandleDpadPress(Direction.Down); break;
                    case Direction.Left: HandleDpadPress(Direction.Left); break;
                    case Direction.Right: HandleDpadPress(Direction.Right); break;
                    default:
                        NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Errors, $"{_panelName} {MethodBase.GetCurrentMethod().Name} Unknown direction: {ea.Sig.Number}");
                        break;
                }
            }
            else
            {
                // release
                switch (direction)
                {
                    case Direction.Up: HandleDpadRelease(Direction.Up); break;
                    case Direction.Down: HandleDpadRelease(Direction.Down); break;
                    case Direction.Left: HandleDpadRelease(Direction.Left); break;
                    case Direction.Right: HandleDpadRelease(Direction.Right); break;
                    default:
                        NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Errors, $"{_panelName} {MethodBase.GetCurrentMethod().Name} Unknown direction: {ea.Sig.Number}");
                        break;
                }
            }
        }

        /// <summary>
        /// Handles preset Smart Object events for recalling or saving camera presets
        /// </summary>
        /// <param name="o">The source object</param>
        /// <param name="ea">Smart Object event arguments</param>
        private void OnPreset(object o, SmartObjectEventArgs ea)
        {
            NexusDebugSmartObjectEvent(o, ea);
            int num = (int)ea.Sig.Number;
            if (!Enum.IsDefined(typeof(SgCue), num))
            {
                NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Debug, $"{_panelName} {MethodBase.GetCurrentMethod().Name} {num} not defined");
                return;
            }

            if (ea.Sig.BoolValue)
            {
                ResetCueItemBooleans(_preset);
                _preset.SetBoolean((uint)num, true);  
                uint preset = GetIndexFromItemCue(num);
                if (_saveMode)
                {
                    // Save current position to preset
                    NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Debug, $"{_panelName} {MethodBase.GetCurrentMethod().Name} Saving preset {preset}");
                    //_camera.SavePreset((int)preset);
                    ResetSaveMode();
                }
                else
                {
                    // Recall preset
                    NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Debug, $"{_panelName} {MethodBase.GetCurrentMethod().Name} Recalling preset {preset}");
                    //_camera.RecallPreset((int)preset);
                }   
            }
        }

        /// <summary>
        /// Handles hold timer elapsed event to send faster camera movement command
        /// </summary>
        /// <param name="sender">The timer that raised the event</param>
        /// <param name="e">Elapsed event arguments</param>
        private void OnTimerHoldElapsed(object sender, ElapsedEventArgs e)
        {
            lock (_holdLock)
            {
                if (_activeDirection.HasValue && !_fastCommandSent)
                {
                    NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Debug, $"{_panelName} {MethodBase.GetCurrentMethod().Name} hold detected, sending fast command for {_activeDirection.Value}");
                    SendCameraDirection(_activeDirection.Value, (int)Speed.Held);
                    _fastCommandSent = true;
                }
            }
        }

        /// <summary>
        /// Handles save timeout timer elapsed event to exit save mode
        /// </summary>
        /// <param name="sender">The timer that raised the event</param>
        /// <param name="e">Elapsed event arguments</param>
        private void OnTimerSaveTimeoutElapsed(object sender, ElapsedEventArgs e)
        {
            _saveMode = false;
            _Panel.SetBoolean((int)Btn.Save, this._saveMode);
        }

        /// <summary>
        /// Handles camera auto focus state change feedback
        /// </summary>
        /// <param name="isAutoFocus">True if auto focus is enabled, false otherwise</param>
        private void OnAutoFocusChange(bool isAutoFocus)
        {
            _autoFocus = isAutoFocus;
            NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Debug, $"{_panelName} {MethodBase.GetCurrentMethod().Name} {(isAutoFocus ? "Enabled" : "Disabled")}");
        }

        /// <summary>
        /// Handles directional pad press event and initiates camera movement with hold detection
        /// </summary>
        /// <param name="dir">The direction to move the camera</param>
        private void HandleDpadPress(Direction dir)
        {
            lock (_holdLock)
            {
                _activeDirection = dir;
                _fastCommandSent = false;
                SendCameraDirection(dir, (int)Speed.Press);
                _timerHold.Stop();
                _timerHold.Start();
            }
        }

        /// <summary>
        /// Handles directional pad release event and stops camera movement
        /// </summary>
        /// <param name="dir">The direction that was released</param>
        private void HandleDpadRelease(Direction dir)
        {
            lock (_holdLock)
            {
                _timerHold.Stop();
                SendCameraStop();
                _activeDirection = null;
                _fastCommandSent = false;
            }
        }

        /// <summary>
        /// Resets save mode to false and stops the save timeout timer
        /// </summary>
        private void ResetSaveMode()
        {
            _saveMode = false;
            _Panel.SetBoolean((int)Btn.Save, this._saveMode);
            _timerSaveTimeout.Stop();
        }   

        /// <summary>
        /// Sends a camera movement command in the specified direction at the specified speed
        /// </summary>
        /// <param name="dir">The direction to move the camera</param>
        /// <param name="speed">The speed of the movement</param>
        private void SendCameraDirection(Direction dir, int speed)
        {
            //if (_camera == null)
            //{
            //    NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Errors, $"{_panelName} Camera is null");
            //    return;
            //}

            try
            {
                switch (dir)
                {
                    case Direction.Up:
                        NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Debug, $"{_panelName} {MethodBase.GetCurrentMethod().Name} up");
                        //_camera.TiltSpeed = speed;
                        //_camera.TiltUp();
                        break;
                    case Direction.Down:
                        NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Debug, $"{_panelName} {MethodBase.GetCurrentMethod().Name} down");
                        //_camera.TiltSpeed = speed;
                        //_camera.TiltDown();
                        break;
                    case Direction.Left:
                        NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Debug, $"{_panelName} {MethodBase.GetCurrentMethod().Name} left");
                        //_camera.PanSpeed = speed;
                        //_camera.PanLeft();
                        break;
                    case Direction.Right:
                        NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Debug, $"{_panelName} {MethodBase.GetCurrentMethod().Name} right");
                        //_camera.PanSpeed = speed;
                        //_camera.PanRight();
                        break;
                }
            }
            catch (Exception ex)
            {
                NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Errors, $"{_panelName} {MethodBase.GetCurrentMethod().Name} Exception: {GetInnerErr.GetInnermostException(ex)}");
            }
        }

        /// <summary>
        /// Sends a stop command to halt all camera movement
        /// </summary>
        private void SendCameraStop()
        {
            //if (_camera == null)
            //{
            //    NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Errors, $"{_panelName} Camera is null");
            //    return;
            //}
            try
            {
                NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Debug, $"{_panelName} {MethodBase.GetCurrentMethod().Name}");
                //_camera.PtzStop();
            }
            catch (Exception ex)
            {

                NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Errors, $"{_panelName} {MethodBase.GetCurrentMethod().Name} Exception: {GetInnerErr.GetInnermostException(ex)}");
            }
        }

        /// <summary>
        /// Updates the auto focus button state on the panel
        /// </summary>
        private void SetAutoFocus()
        {
            _Panel.SetBoolean((uint)Btn.FocusAuto, _autoFocus);
        }
        /// <summary>
        /// Updates preset labels on the panel
        /// </summary>
        private void SetLabelPreset()
        {
            SetCueByKey(_cuesLabelPreset, _labelPreset);
        }

        /// <summary>
        /// Updates camera selection labels on the panel
        /// </summary>
        private void SetLabelSelect()
        {
            SetCueByKey(_cuesLabelSelect, _labelSelect);
        }

        /// <summary>
        /// Updates visibility of camera selection items on the panel
        /// </summary>
        private void SetVisibleSelect()
        {
            SetCueByKey(_cuesVisibleSelect, _visibleSelect);
        }
        /// <summary>
        /// Updates visibility of preset items on the panel
        /// </summary>
        private void SetVisiblePreset()
        {
            SetCueByKey(_cuesVisiblePreset, visiblePreset);
        }

        ///// <summary>
        ///// Handles system setting.
        ///// </summary>
        ///// <param name="FriendlyName"> The friendly name of the setting. </param>
        ///// <param name="Settings"> The settings object containing updated values. </param>
        //private void System_OnSettingChanged(string FriendlyName, INexusSettings Settings)
        //{
        //    if (Settings is Settings.CameraPresetNames cameraPresetNames)
        //    {
        //        var presetLabels = new[]
        //        {
        //            cameraPresetNames.Preset01,
        //            cameraPresetNames.Preset02,
        //            cameraPresetNames.Preset03,
        //            cameraPresetNames.Preset04,
        //            cameraPresetNames.Preset05,
        //            cameraPresetNames.Preset06
        //        };

        //        // Build debug message from array
        //        var debugMessage = $"{MethodBase.GetCurrentMethod().Name}: {FriendlyName}\n";
        //        for (int i = 0; i < presetLabels.Length; i++)
        //        {
        //            debugMessage += $"  Preset{i + 1}: {presetLabels[i]}\n";
        //        }

        //        NexusServiceManager.System.Debug(
        //            Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Debug,
        //            debugMessage.TrimEnd('\n'));

        //        //SetLabels(presetLabels);
        //    }
        //}
        private void System_OnSettingChanged(string FriendlyName, INexusSettings Settings)
        {
            if (Settings is Settings.CameraPresetNames cameraPresetNames)
            {
                var presetLabels = new[]
                {
                    cameraPresetNames.Preset01,
                    cameraPresetNames.Preset02,
                    cameraPresetNames.Preset03,
                    cameraPresetNames.Preset04,
                    cameraPresetNames.Preset05,
                    cameraPresetNames.Preset06
                };

                // Store preset labels for this camera using FriendlyName as key
                var presetDict = new Dictionary<uint, string>();
                for (uint i = 0; i < presetLabels.Length; i++)
                {
                    presetDict[i + 1] = presetLabels[i];  // Presets are 1-indexed
                }
                _presetLabelsByCamera[FriendlyName] = presetDict;

                // Build debug message
                var debugMessage = $"{MethodBase.GetCurrentMethod().Name}: {FriendlyName}\n";
                for (int i = 0; i < presetLabels.Length; i++)
                {
                    debugMessage += $"  Preset{i + 1}: {presetLabels[i]}\n";
                }

                NexusServiceManager.System.Debug(
                    Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Debug,
                    debugMessage.TrimEnd('\n'));

                DebugPrintCameraPresets();

                // Update UI if this is the currently selected camera
                UpdatePresetsForSelectedCamera();
            }
        }

        /// <summary>
        /// Updates preset labels based on the currently selected camera
        /// </summary>
        private void UpdatePresetsForSelectedCamera()
        {
            if (_selectedCamera == null)
            {
                NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Warning, $"{_panelName} {MethodBase.GetCurrentMethod().Name} Selected camera is null");
                return;
            }

            string cameraName = (_selectedCamera as VaddioRoboshotIP)?.Name;
            if (cameraName == null)
            {
                NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Warning, $"{_panelName} {MethodBase.GetCurrentMethod().Name} Selected camera does not have a name");
                return;
            }

            NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Debug, $"{_panelName} {MethodBase.GetCurrentMethod().Name} cameraName: {cameraName}");
            if (_presetLabelsByCamera.TryGetValue(cameraName, out var presetLabels))
            {
                NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Debug, $"{_panelName} {MethodBase.GetCurrentMethod().Name} attempt to update presets for camera: {cameraName}");
                _labelPreset = presetLabels;
                SetLabelPreset();
                DebugPrintCurrentPresets();  // ← Add this
            }
            else
            {
                NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Debug, $"{_panelName} {MethodBase.GetCurrentMethod().Name} cannot write presets for camera: {cameraName}");
            }
        }

        /// <summary>
        /// Prints all preset labels for all cameras to debug output
        /// </summary>
        private void DebugPrintCameraPresets()
        {
            NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Debug, $"{_panelName} {MethodBase.GetCurrentMethod().Name}");
            var debugMessage = $"{_panelName} {MethodBase.GetCurrentMethod().Name}: Camera Presets\n";

            if (_presetLabelsByCamera.Count == 0)
            {
                debugMessage += "  No camera presets loaded\n";
            }
            else
            {
                foreach (var cameraEntry in _presetLabelsByCamera)
                {
                    string cameraName = cameraEntry.Key;
                    var presets = cameraEntry.Value;

                    debugMessage += $"\n  Camera: {cameraName}\n";
                    if (presets.Count == 0)
                    {
                        debugMessage += "    No presets configured\n";
                    }
                    else
                    {
                        foreach (var presetEntry in presets)
                        {
                            debugMessage += $"    Preset {presetEntry.Key}: {presetEntry.Value}\n";
                        }
                    }
                }
            }

            NexusServiceManager.System.Debug(
                Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Debug,
                debugMessage.TrimEnd('\n'));
        }

        private void DebugPrintCurrentPresets()
        {
            NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Debug, $"{_panelName} {MethodBase.GetCurrentMethod().Name}");
            var debugMessage = $"{_panelName} {MethodBase.GetCurrentMethod().Name}: Current Preset Labels\n";

            if (_selectedCamera != null)
            {
                string cameraName = (_selectedCamera as VaddioRoboshotIP)?.Name;
                debugMessage += $"  Selected Camera: {cameraName ?? "Unknown"}\n";
            }
            else
            {
                debugMessage += "  No camera selected\n";
            }

            if (_labelPreset.Count == 0)
            {
                debugMessage += "  No presets currently loaded\n";
            }
            else
            {
                foreach (var presetEntry in _labelPreset)
                {
                    debugMessage += $"  Preset {presetEntry.Key}: {presetEntry.Value}\n";
                }
            }

            NexusServiceManager.System.Debug(
                Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Debug,
                debugMessage.TrimEnd('\n'));
        }
    }
}

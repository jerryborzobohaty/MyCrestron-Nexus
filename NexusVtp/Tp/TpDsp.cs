using Crestron.SimplSharpPro;
using Forte.SSPro.UI.Helper.Library.UI;
using Nexus.Driver.Architecture.Configuration;
using Nexus.Framework.Services;
using Nexus.Qsc.Qsys.Driver;
using Nexus.Utils;
using NexusCommon;
using System;
using System.Collections.Generic;
using System.Reflection;
using static NexusVtp.SmartGraphics;

namespace NexusVtp
{
    /// <summary>
    /// Manages DSP volume control functionality for touchpanels with support for both direct joins and Smart Objects
    /// </summary>
    public class TpDsp
    {
        /// <summary>
        /// Name of the Privacy volume control in the DSP
        /// </summary>
        private const string NamePrivacy = "Privacy";
        /// <summary>
        /// Name of the Master volume control in the DSP
        /// </summary>
        private const string NameMaster = "Master";
        /// <summary>
        /// Name of the Program volume control in the DSP
        /// </summary>
        private const string NameProgram = "Program";
        /// <summary>
        /// Name of the Phone volume control in the DSP
        /// </summary>
        private const string NamePhone = "Phone";
        /// <summary>
        /// Name of a press event that we receive from a subpage reference list, will be appended with a number to indicate which button was pressed (e.g. "press1", "press2", etc.)
        /// </summary>
        private const string SrlPress = "press";
        /// <summary>
        /// Reference to the QSC Q-SYS DSP driver
        /// </summary>
        private QscQsysDriver _dsp;
        /// <summary>
        /// Descriptive name of the panel for debugging purposes
        /// </summary>
        private string _panelName = string.Empty;
        /// <summary>
        /// The touchpanel interface wrapper
        /// </summary>
        private Panel _panel;
        /// <summary>
        /// Smart Object for master volume control
        /// </summary>
        private ExtendedSmartObject _volumeMaster;
        /// <summary>
        /// Smart Object for mixer volume controls (Program and Phone)
        /// </summary>
        private ExtendedSmartObject _volumeMixer;

        /// <summary>
        /// Flag to ensure DSP event subscriptions only occur once across all panel instances
        /// </summary>
        private static bool _subscribed = false;

        /// <summary>
        /// Raised when a mute state changes for any volume control
        /// </summary>
        public static event Action<string, bool> MuteChanged;

        /// <summary>
        /// Raised when a level changes for any volume control
        /// </summary>
        public static event Action<string, int> LevelChanged;

        /// <summary>
        /// Raised when a user interacts with DSP controls on this panel instance (for UI timeout reset)
        /// </summary>
        public event Action UserActivity;

        /// <summary>
        /// Structure to hold feedback join numbers for panel updates
        /// </summary>
        private class FeedbackJoins
        {
            /// <summary>
            /// Gets or sets the join number for mute feedback
            /// </summary>
            public uint Mute { get; set; }
            /// <summary>
            /// Gets or sets the join number for level feedback
            /// </summary>
            public uint Level { get; set; }
        }

        /// <summary>
        /// Structure to hold feedback cue numbers for Smart Object updates
        /// </summary>
        private class FeedbackSrl
        {
            /// <summary>
            /// Gets or sets the Smart Object ID
            /// </summary>
            public uint Id { get; set; }
            /// <summary>
            /// Gets or sets the cue number for mute feedback
            /// </summary>
            public uint Mute { get; set; }
            /// <summary>
            /// Gets or sets the cue number for level feedback
            /// </summary>
            public uint Level { get; set; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TpDsp"/> class.
        /// </summary>
        /// <param name="panel"> The wrapped tp. </param>
        /// <param name="panelName"> Description of the panel for debugging. </param>
        /// <param name="dsp"> The dsp. </param>
        public TpDsp(Panel panel, string panelName, QscQsysDriver dsp)
        {
            this._panel = panel;
            this._panelName = panelName;
            this._dsp = dsp;
            this.Initialize();
        }

        /// <summary>
        /// Maps button join numbers to their corresponding action handlers
        /// </summary>
        private Dictionary<int, Action<ButtonGroupEventArgs>> _mapJoinToAction = new Dictionary<int, Action<ButtonGroupEventArgs>>();

        /// <summary>
        /// Maps Smart Object ID and button index tuples to their corresponding action handlers
        /// </summary>
        private Dictionary<(SgId Id, uint press), Action<SmartObjectEventArgs>> _mapSrlPressNameToAction = new Dictionary<(SgId id, uint press), Action<SmartObjectEventArgs>>();

        /// <summary>
        /// Maps volume control names to their feedback join numbers
        /// </summary>
        private Dictionary<string, FeedbackJoins> _mapNameToFeedbackJoins = new Dictionary<string, FeedbackJoins>();

        /// <summary>
        /// Maps volume control names to their feedback Smart Object
        /// </summary>
        private Dictionary<string, FeedbackSrl> _mapNameToFeedbackSrl = new Dictionary<string, FeedbackSrl>();

        /// <summary>
        /// Button join numbers for DSP controls
        /// </summary>
        enum Btn
        {
            Privacy = 450,
        }

        /// <summary>
        /// Initializes button groups, smart objects, event handlers, and feedback mappings
        /// </summary>
        private void Initialize()
        {
            try
            {
                NexusServiceManager.System.OnSettingChanged += System_OnSettingChanged;
                //THIS CONTAINS BOTH JOIN-BASED AND SMART OBJECT-BASED CONTROL AND FEEDBACK TO DEMONSTRATE BOTH METHODS.
                //IT CAN BE USED AS A REFERENCE FOR EITHER APPROACH OR BOTH.
                //RESOURCES ARE KEPT SEPARATE FOR EITHER APPROACH TO MAKE IT CLEAR WHAT RELATES TO WHAT OR SO THAT WHAT IS NOT NEEDED IN THE PROJECT CAN BE EASILY REMOVED IF DESIRED.
                // buttons - joins
                var btnPrivacy = _panel.AddButtonGroup("Privacy", (uint)Btn.Privacy);
                btnPrivacy.OnPanelButtonGroupChange += OnBgVolumeButton;

                // THIS MAPS A JOIN TO AN ACTION, THE ACTION WILL CONTROL THE DEVICE
                _mapJoinToAction.Add((int)Btn.Privacy, (e) => BgHandleMute(e, NamePrivacy));

                // THIS MAPS NAME THAT THE DEVICE GIVES US TO THE MUTE AND LEVEL JOINS
                _mapNameToFeedbackJoins.Add(NamePrivacy, new FeedbackJoins
                {
                    Mute = (uint)Btn.Privacy,
                    Level = 0  // No level for privacy
                });

                // add smart object 
                // THIS ADDS THE SMART OBJECTS TO THE PANEL AND SUBSCRIBES TO THEIR SIGNAL CHANGES
                _volumeMaster = _panel.AddSmartObject("VolumeMaster", _panel.ThePanel.SmartObjects[(int)SgId.VolumeMaster]);
                _volumeMixer = _panel.AddSmartObject("VolumeMixer", _panel.ThePanel.SmartObjects[(int)SgId.VolumeMixer]);

                // add handler
                _volumeMaster.OnSmartObjectSignalChange += OnSgVolumeButton;
                _volumeMixer.OnSmartObjectSignalChange += OnSgVolumeButton;

                // THIS MAPS AN ID AND BUTTON PRESS TO AN ACTION, THE ACTION WILL CONTROL THE DEVICE
                _mapSrlPressNameToAction.Add((SgId.VolumeMaster, 1), ea => SgHandleUp(ea, NameMaster));
                _mapSrlPressNameToAction.Add((SgId.VolumeMaster, 2), ea => SgHandleDown(ea, NameMaster));
                _mapSrlPressNameToAction.Add((SgId.VolumeMaster, 3), ea => SgHandleMute(ea, NameMaster));

                _mapSrlPressNameToAction.Add((SgId.VolumeMixer, 1), ea => SgHandleUp(ea, NameProgram));
                _mapSrlPressNameToAction.Add((SgId.VolumeMixer, 2), ea => SgHandleDown(ea, NameProgram));
                _mapSrlPressNameToAction.Add((SgId.VolumeMixer, 3), ea => SgHandleMute(ea, NameProgram));
                _mapSrlPressNameToAction.Add((SgId.VolumeMixer, 4), ea => SgHandleUp(ea, NamePhone));
                _mapSrlPressNameToAction.Add((SgId.VolumeMixer, 5), ea => SgHandleDown(ea, NamePhone));
                _mapSrlPressNameToAction.Add((SgId.VolumeMixer, 6), ea => SgHandleMute(ea, NamePhone));

                // THIS MAPS THE NAME THAT THE DEVICE GIVES US TO THE SMART OBJECT ID, MUTE AND LEVEL
                _mapNameToFeedbackSrl.Add(NameMaster, new FeedbackSrl
                {
                    Id = (uint)SgId.VolumeMaster,
                    Mute = 3,
                    Level = 1,
                });

                _mapNameToFeedbackSrl.Add(NameProgram, new FeedbackSrl
                {
                    Id = (uint)SgId.VolumeMixer,
                    Mute = 3,
                    Level = 1,
                });

                _mapNameToFeedbackSrl.Add(NamePhone, new FeedbackSrl
                {
                    Id = (uint)SgId.VolumeMixer,
                    Mute = 6,
                    Level = 2,
                });

                // THIS SUBSCRIBES TO THE DEVICE EVENTS.
                // THE SUBSCRIBE FIELD IS STATIC, SO THAT ONLY A SINGLE SUBSCRIPTION IS MADE FOR ALL INSTANCES OF THIS CLASS.
                if (!_subscribed)
                {
                    SubscribeSmartObjectFeedback();
                    SubscribeJoinFeedback();
                    _subscribed = true;
                }

                // EACH INSTANCE THEN SUBSCRIBES TO THE GLOBAL EVENTS RAISED BY THE DEVICE EVENT HANDLER
                // BOTH HANDLERS WILL TRIGGER, THEY WILL CHECK IF THE EVENT RELATES TO THEIR RESPECTIVE
                // FEEDBACK MAPPING BEFORE UPDATING THE PANEL, SO IT IS SAFE TO SUBSCRIBE TO BOTH
                // REGARDLESS OF WHICH METHOD IS USED IN THE PROJECT
                // For smart object projects:
                MuteChanged += OnMuteChangedSg;
                LevelChanged += OnLevelChangedSg;

                // OR for join-based projects:
                MuteChanged += OnMuteChangedJoins;
                LevelChanged += OnLevelChangedJoins;

            }
            catch (Exception err)
            {
                NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Errors, $"{_panelName} {MethodBase.GetCurrentMethod().Name}: {GetInnerErr.GetInnermostException(err)}");
                NexusServiceManager.System.Log(Nexus.Driver.Architecture.Enumerations.LoggingLevels.Exceptions, $"{_panelName} {MethodBase.GetCurrentMethod().Name}: {GetInnerErr.GetInnermostException(err)}");
            }
        }

        // event handlers - button objects
        /// <summary>
        /// Handles volume and mute button events by routing to appropriate handler methods
        /// </summary>
        /// <param name="o">Event sender</param>
        /// <param name="e">Button group event arguments containing signal information</param>
        private void OnBgVolumeButton(object o, ButtonGroupEventArgs e)
        {
            int num = (int)e.Sig.Number;
            NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Debug, $"{_panelName} {MethodBase.GetCurrentMethod().Name} {num}");

            if (_mapJoinToAction.TryGetValue(num, out Action<ButtonGroupEventArgs> action))
            {
                action(e);
            }
            else
            {
                NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Debug, $"{_panelName} {MethodBase.GetCurrentMethod().Name} No mapping found for button {num}");
            }
        }

        /// <summary>
        /// Handles Smart Object volume button events by routing to appropriate handler methods
        /// </summary>
        /// <param name="o">Event sender</param>
        /// <param name="ea">Smart Object event arguments containing signal information</param>
        private void OnSgVolumeButton(object o, SmartObjectEventArgs ea)
        {
            //THIS IS A SUBPAGE REFERENCE LIST, SO THE NAME IS DIFFERENT THAN OTHER SMART OBJECTS!!!!!
            if (!(ea.Sig.Name.Contains(SrlPress)))
            {
                return;
            }

            //reset the timer to keep the page visible while the user is interacting with the DSP controls
            UserActivity?.Invoke();

            NexusDebugSmartObjectEvent(o, ea);
            var id = (SgId)ea.SmartObjectArgs.ID;

            // Extract the numeric suffix from "press1", "press2", etc.
            if (!uint.TryParse(ea.Sig.Name.Substring(SrlPress.Length), out var press))
            {
                NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Warning,
                    $"{_panelName} {MethodBase.GetCurrentMethod().Name} Failed to parse index from {ea.Sig.Name}");
                return;
            }

            if (_mapSrlPressNameToAction == null)
            {
                NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Warning, $"{_panelName} {MethodBase.GetCurrentMethod().Name} _mapSrlPressNameToAction == null");
                return;
            }

            if (_mapSrlPressNameToAction.TryGetValue((id, press), out var handler))
            {
                handler?.Invoke(ea);
            }
            else
            {
                NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Warning, $"{_panelName} {MethodBase.GetCurrentMethod().Name} Unhandled mapping for id={id} press={press}");
            }
        }

        /// <summary>
        /// Handles volume up button press and release events
        /// </summary>
        /// <param name="e">Button event arguments</param>
        /// <param name="name">Name of the volume control</param>
        private void BgHandleUp(ButtonGroupEventArgs e, string name)
        {
            NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Debug, $"{_panelName} {MethodBase.GetCurrentMethod().Name} {name} {(e.Sig.BoolValue ? "pressed" : "released")}");
            _panel.SetBoolean((uint)e.Sig.Number, e.Sig.BoolValue);
            UserActivity?.Invoke();
            var vol = _dsp.GetVolume(name);
            if (e.Sig.BoolValue) vol?.LevelUp(); else vol?.LevelStop();
        }

        /// <summary>
        /// Handles volume down button press and release events
        /// </summary>
        /// <param name="e">Button event arguments</param>
        /// <param name="name">Name of the volume control</param>
        private void BgHandleDown(ButtonGroupEventArgs e, string name)
        {
            NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Debug, $"{_panelName} {MethodBase.GetCurrentMethod().Name} {name} {(e.Sig.BoolValue ? "pressed" : "released")}");
            _panel.SetBoolean((uint)e.Sig.Number, e.Sig.BoolValue);
            UserActivity?.Invoke();
            var vol = _dsp.GetVolume(name);
            if (e.Sig.BoolValue)vol?.LevelDown(); else vol?.LevelStop();
        }

        /// <summary>
        /// Handles mute toggle button events
        /// </summary>
        /// <param name="e">Button event arguments</param>
        /// <param name="name">Name of the volume control</param>
        private void BgHandleMute(ButtonGroupEventArgs e, string name)
        {
            NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Debug, $"{_panelName} {MethodBase.GetCurrentMethod().Name} {name} {(e.Sig.BoolValue ? "pressed" : "released")}");
            if (e.Sig.BoolValue)
            {
                UserActivity?.Invoke();
                var vol = _dsp.GetVolume(name);
                vol.MuteToggle();
            }
        }

        /// <summary>
        /// Handles volume up button press and release events
        /// </summary>
        /// <param name="e">Button event arguments</param>
        /// <param name="name">Name of the volume control</param>
        private void SgHandleUp(SmartObjectEventArgs ea, string name)
        {
            NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Debug, $"{_panelName} {MethodBase.GetCurrentMethod().Name} {name} {(ea.Sig.BoolValue ? "pressed" : "released")}");
            //if (ea.Sig.BoolValue) UserActivity?.Invoke();
            var vol = _dsp.GetVolume(name);
            if (ea.Sig.BoolValue) vol?.LevelUp(); else vol?.LevelStop();
        }

        /// <summary>
        /// Handles volume down button press and release events
        /// </summary>
        /// <param name="e">Button event arguments</param>
        /// <param name="name">Name of the volume control</param>
        private void SgHandleDown(SmartObjectEventArgs ea, string name)
        {
            NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Debug, $"{_panelName} {MethodBase.GetCurrentMethod().Name} {name} {(ea.Sig.BoolValue ? "pressed" : "released")}");
            //if (ea.Sig.BoolValue) UserActivity?.Invoke();
            var vol = _dsp.GetVolume(name);
            if (ea.Sig.BoolValue) vol?.LevelDown(); else vol?.LevelStop();
        }

        /// <summary>
        /// Handles mute toggle button events
        /// </summary>
        /// <param name="e">Button event arguments</param>
        /// <param name="name">Name of the volume control</param>
        private void SgHandleMute(SmartObjectEventArgs ea, string name)
        {
            NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Debug, $"{_panelName} {MethodBase.GetCurrentMethod().Name} {name} {(ea.Sig.BoolValue ? "pressed" : "released")}");
            if (ea.Sig.BoolValue)
            {
                //UserActivity?.Invoke();
                var vol = _dsp.GetVolume(name);
                vol.MuteToggle();
            }
        }

        /// <summary>
        /// Subscribes to DSP volume control events for Smart Object feedback updates
        /// </summary>
        private void SubscribeSmartObjectFeedback()
        {
            foreach (var mapping in _mapNameToFeedbackSrl)
            {
                var name = mapping.Key;
                var feedback = mapping.Value;
                var vol = _dsp.GetVolume(name);
                if (vol == null)
                {
                    NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Errors,
                        $"Volume control '{name}' not found");
                    continue;
                }

                vol.OnMuteChange += (muted) =>
                {
                    var handler = MuteChanged;
                    handler?.Invoke(name, muted);
                };


                vol.OnLevelChange += (level) =>
                {
                    var handler = LevelChanged;
                    handler?.Invoke(name, level);
                };

                NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Debug,
                    $"Subscribed smart object handlers for '{name}'");
            }
        }

        /// <summary>
        /// Subscribes to DSP volume control events for direct join feedback updates
        /// </summary>
        private void SubscribeJoinFeedback()
        {
            foreach (var mapping in _mapNameToFeedbackJoins)
            {
                var name = mapping.Key;
                var feedback = mapping.Value;
                var vol = _dsp.GetVolume(name);
                if (vol == null)
                {
                    NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Errors,
                        $"Volume control '{name}' not found");
                    continue;
                }

                vol.OnMuteChange += (muted) =>
                {
                    var handler = MuteChanged;
                    handler?.Invoke(name, muted);
                };

                if (feedback.Level > 0)
                {
                    vol.OnLevelChange += (level) =>
                    {
                        var handler = LevelChanged;
                        handler?.Invoke(name, level);
                    };
                }

                NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Debug,
                    $"Subscribed join handlers for '{name}'");
            }
        }


        // event handlers - Feedback
        /// <summary>
        /// Handles global mute change events and updates Smart Object feedback
        /// </summary>
        /// <param name="name">Name of the volume control</param>
        /// <param name="muted">Current mute state</param>
        private void OnMuteChangedSg(string name, bool muted)
        {
            NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Debug, $"{name} muted {muted}");
            if (!_mapNameToFeedbackSrl.TryGetValue(name, out var feedback)) return;

            NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Debug, $"mapped {name}");

            ExtendedSmartObject smartObj = null;
            if (feedback.Id == (uint)SgId.VolumeMaster)
                smartObj = _volumeMaster;
            else if (feedback.Id == (uint)SgId.VolumeMixer)
                smartObj = _volumeMixer;

            SetSrlBooleanByName(smartObj, feedback.Mute, muted);
        }

        /// <summary>
        /// Handles global mute change events and updates direct join feedback
        /// </summary>
        /// <param name="name">Name of the volume control</param>
        /// <param name="muted">Current mute state</param>
        private void OnMuteChangedJoins(string name, bool muted)
        {
            NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Debug, $"{name} muted {muted}");
            if (!_mapNameToFeedbackJoins.TryGetValue(name, out var feedback)) return;

            _panel.SetBoolean(feedback.Mute, muted);
        }

        /// <summary>
        /// Handles global level change events and updates Smart Object feedback
        /// </summary>
        /// <param name="name">Name of the volume control</param>
        /// <param name="level">Current level value</param>
        private void OnLevelChangedSg(string name, int level)
        {
            NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Debug, $"{name} level {level}");
            if (!_mapNameToFeedbackSrl.TryGetValue(name, out var feedback)) return;

            ExtendedSmartObject smartObj = null;
            if (feedback.Id == (uint)SgId.VolumeMaster)
                smartObj = _volumeMaster;
            else if (feedback.Id == (uint)SgId.VolumeMixer)
                smartObj = _volumeMixer;

            SetSrlAnalogByName(smartObj, feedback.Level, (ushort)level);
        }

        /// <summary>
        /// Handles global level change events and updates direct join feedback
        /// </summary>
        /// <param name="name">Name of the volume control</param>
        /// <param name="level">Current level value</param>
        private void OnLevelChangedJoins(string name, int level)
        {
            NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Debug, $"{name} level {level}");
            if (!_mapNameToFeedbackJoins.TryGetValue(name, out var feedback)) return;

            if (feedback.Level > 0)
            {
                _panel.SetAnalog(feedback.Level, (short)level);
            }
        }

        /// <summary>
        /// Handles system settings changes for room information and routing group names
        /// </summary>
        /// <param name="FriendlyName">The friendly name of the setting</param>
        /// <param name="Settings">The settings object containing updated values</param>
        private void System_OnSettingChanged(string FriendlyName, INexusSettings Settings)
        {
            if (Settings is Settings.VolumeNames volumeNames)
            {
                SetSrlTestByName(_volumeMaster, 1, volumeNames.Master);
                SetSrlTestByName(_volumeMixer, 1, volumeNames.Program);
                SetSrlTestByName(_volumeMixer, 2, volumeNames.Phone);
            }
        }
    }
}

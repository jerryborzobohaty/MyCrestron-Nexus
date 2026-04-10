using Crestron.SimplSharp.CrestronDataStore;
using Crestron.SimplSharpPro;
using Forte.SSPro.UI.Helper.Library.UI;
using Nexus.Driver.Architecture.Configuration;
using Nexus.Framework.Services;
using Nexus.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using static NexusCommon.Settings;
using static NexusVtp.SmartGraphics;
using static NexusVtp.TpHelper;


namespace NexusVtp
{
    /// <summary>
    /// Manages matrix routing between audio/video sources and destinations on touchpanels
    /// </summary>
    internal class TpMatrix
    {
        /// <summary>
        /// Maximum number of sources supported
        /// </summary>
        private const int MaxSource = 8;
        /// <summary>
        /// Maximum number of audio destinations supported
        /// </summary>
        private const int MaxDestAudio = 2;
        /// <summary>
        /// Maximum number of video destinations supported
        /// </summary>
        private const int MaxDestVideo = 4;

        /// <summary>
        /// Base join number for video destination labels
        /// </summary>
        private const int LblDestVideoBase = 400;
        /// <summary>
        /// Base join number for video destination buttons
        /// </summary>
        private const int BtnDestVideoBase = 500;
        /// <summary>
        /// Base join number for video destination enable states
        /// </summary>
        private const int EnableDestVideoBase = 540;
        /// <summary>
        /// Base join number for video destination visibility states
        /// </summary>
        private const int VisibleDestVideoBase = 580;

        /// <summary>
        /// Base join number for audio destination buttons
        /// </summary>
        private const int BtnDestAudioBase = 700;
        /// <summary>
        /// Base join number for audio destination enable states
        /// </summary>
        private const int EnableDestAudioBase = 740;
        /// <summary>
        /// Base join number for audio destination visibility states
        /// </summary>
        private const int VisibleDestAudioBase = 780;

        /// <summary>
        /// Descriptive name of the panel for debugging purposes
        /// </summary>
        private string _panelName = string.Empty;
        /// <summary>
        /// The touchpanel interface wrapper
        /// </summary>
        private Panel _panel;
        /// <summary>
        /// Smart Object for source list display
        /// </summary>
        private ExtendedSmartObject _sourceList;
        /// <summary>
        /// Currently selected source physical input number
        /// </summary>
        private uint _selectedSource;

        /// <summary>
        /// Stores audio routing state (destination to source mapping)
        /// </summary>
        private Dictionary<uint, uint> _routesAudio = new Dictionary<uint, uint>();
        /// <summary>
        /// Stores video routing state (destination to source mapping)
        /// </summary>
        private Dictionary<uint, uint> _routesVideo = new Dictionary<uint, uint>();

        /// <summary>
        /// Maps source list UI items to physical input numbers
        /// </summary>
        /// <remarks>
        /// Key is list item (1-9), value is physical input (0-8)
        /// This provides flexibility to reorder the list separate from the physical connections
        /// </remarks>
        private static readonly Dictionary<uint, uint> MapSourceListItemToInput = new Dictionary<uint, uint>
        {
            { 1, 0 },
            { 2, 2 },
            { 3, 1 },
            { 4, 3 },
            { 5, 4 },
            { 6, 5 },
            { 7, 6 },
            { 8, 7 },
            { 9, 8 },
        };

        /// <summary>
        /// Enable state for audio destinations
        /// </summary>
        private static readonly Dictionary<uint, bool> enableDestinationAudio = new Dictionary<uint, bool>
        {
            { 1, true },
            { 2, true },
        };

        /// <summary>
        /// Enable state for video destinations
        /// </summary>
        private static readonly Dictionary<uint, bool> enableDestinationVideo = new Dictionary<uint, bool>
        {
            { 1, true },
            { 2, true },
            { 3, true },
            { 4, true },
        };

        /// <summary>
        /// Enable state for physical inputs
        /// </summary>
        private static readonly Dictionary<uint, bool> enableInput= new Dictionary<uint, bool>
        {
            { 0, true },
            { 1, true },
            { 2, false },
            { 3, true },
            { 4, true },
            { 5, true },
            { 6, true },
            { 7, true },
            { 8, true },
        };

        /// <summary>
        /// Icon/image associations for each source input
        /// </summary>
        private Dictionary<uint, string> _iconSource = new Dictionary<uint, string>();

        /// <summary>
        /// Display labels for video destination outputs
        /// </summary>
        private Dictionary<uint, string> _labelDestinationVideo = new Dictionary<uint, string>();

        /// <summary>
        /// Display labels for source inputs
        /// </summary>
        private Dictionary<uint, string> _labelSource = new Dictionary<uint, string>();

        /// <summary>
        /// Visibility state for each source input
        /// </summary>
        private Dictionary<uint, bool> _visibleSource = new Dictionary<uint, bool>();

        /// <summary>
        /// Visibility state for audio destinations
        /// </summary>
        private static readonly Dictionary<uint, bool> visibleDestinationAudio = new Dictionary<uint, bool>
        {
            { 1, true },
            { 2, true },
        };

        /// <summary>
        /// Visibility state for video destinations
        /// </summary>
        private static readonly Dictionary<uint, bool> visibleDestinationVideo = new Dictionary<uint, bool>
        {
            { 1, true },
            { 2, true },
            { 3, true },
            { 4, true },
        };

        /// <summary>
        /// Button join numbers for preset operations
        /// </summary>
        enum Btn
        {
            /// <summary>Store preset button</summary>
            Store = 598,
            /// <summary>Recall preset button</summary>
            Recall = 599,
        }

        /// <summary>
        /// Route type enumeration for audio and video
        /// </summary>
        enum Type
        {
            /// <summary>Audio routing</summary>
            Audio = 1,
            /// <summary>Video routing</summary>
            Video = 2,
        }

        /// <summary>
        /// Source input identifiers
        /// </summary>
        enum Source
        {
            /// <summary>Test source</summary>
            Test = 3,
        }

        /// <summary>
        /// Audio destination signal identifiers
        /// </summary>
        enum DestinationAudio
        {
            /// <summary>Enable/disable state</summary>
            Enable = 1,
            /// <summary>Visibility state</summary>
            Visible = 2,
        }

        /// <summary>
        /// Video destination signal identifiers
        /// </summary>
        enum DestinationVideo
        {
            /// <summary>Enable/disable state</summary>
            Enable = 2,
            /// <summary>Visibility state</summary>
            Visible = 4,
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TpMatrix"/> class.
        /// </summary>
        /// <param name="panel"> The wrapped tp. </param>
        /// <param name="panelName"> Description of the panel for debugging. </param>
        public TpMatrix(Panel panel, string panelName)
        {
            this._panel = panel;
            this._panelName = panelName;
            Initialize();
        }

        //private methods
        private void Initialize()
        {
            try
            {
                NexusServiceManager.System.OnSettingChanged += System_OnSettingChanged;
                // buttons - joins
                var bgDestAudio = _panel.AddButtonGroup("DestAudio", BtnDestAudioBase, (BtnDestAudioBase + MaxDestAudio));
                bgDestAudio.OnPanelButtonGroupChange += OnBgDestAudio;

                var bgDestVideo = _panel.AddButtonGroup("DestVideo", BtnDestVideoBase, (BtnDestVideoBase + MaxDestVideo));
                bgDestVideo.OnPanelButtonGroupChange += OnBgDestVideo;

                var bgPreset = _panel.AddButtonGroup("Preset", (uint)Btn.Store, (uint)Btn.Recall);
                bgPreset.OnPanelButtonGroupChange += OnBgPreset;

                //labels - joins
                for (uint i = 1; i <= MaxDestVideo; i++)
                {
                    _panel.AddTextField($"LblDestVideo{i}", LblDestVideoBase + i);
                }
                
                // add smart object 
                _sourceList = _panel.AddSmartObject("SourceList", _panel.ThePanel.SmartObjects[(int)SgId.SourceList]);

                // add handler
                _sourceList.OnSmartObjectSignalChange += OnSourceList;

                ResetSelectedSource();

                // this could probably be removed since it will trigger on the Nexus config change
                this.SetEnableSourceList();
                this.SetIconSourceList();
                this.SetLabelDestinationVideo();
                this.SetLabelSourceList();
                this.SetVisibleSourceList();   
            }
            catch (Exception err)
            {
                NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Errors, $"{_panelName} {MethodBase.GetCurrentMethod().Name}: {GetInnerErr.GetInnermostException(err)}");
                NexusServiceManager.System.Log(Nexus.Driver.Architecture.Enumerations.LoggingLevels.Exceptions, $"{_panelName} {MethodBase.GetCurrentMethod().Name}: {GetInnerErr.GetInnermostException(err)}");
            }
        }

        /// <summary>
        /// Handles audio destination button press events and creates audio routes
        /// </summary>
        /// <param name="o">Event sender</param>
        /// <param name="e">Button group event arguments</param>
        private void OnBgDestAudio(object o, ButtonGroupEventArgs e)
        {
            if (e.Sig.BoolValue)
            {
                NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Debug, $"{_panelName} {MethodBase.GetCurrentMethod().Name} {e.Sig.Number}");
                uint destination = (uint)e.Sig.Number - BtnDestAudioBase;
                if (destination > 0)
                {
                    NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Debug, $"{_panelName} {MethodBase.GetCurrentMethod().Name} {_selectedSource} to {destination}");
                    MakeRoute(Type.Audio, destination, _selectedSource);
                    ResetSelectedSource();
                }
                else
                {
                    NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Debug, $"{_panelName} {MethodBase.GetCurrentMethod().Name} {destination} is out of range");
                }
            }
        }
        /// <summary>
        /// Handles video destination button press events and creates video routes
        /// </summary>
        /// <param name="o">Event sender</param>
        /// <param name="e">Button group event arguments</param>
        private void OnBgDestVideo(object o, ButtonGroupEventArgs e)
        {
            if (e.Sig.BoolValue)
            {
                NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Debug, $"{_panelName} {MethodBase.GetCurrentMethod().Name} {e.Sig.Number}");
                uint destination = (uint)e.Sig.Number - BtnDestVideoBase;
                if (destination > 0)
                {
                    NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Debug, $"{_panelName} {MethodBase.GetCurrentMethod().Name} {_selectedSource} to {destination}");
                    MakeRoute(Type.Video, destination, _selectedSource);
                }
                else
                {
                    NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Debug, $"{_panelName} {MethodBase.GetCurrentMethod().Name} {destination} is out of range");
                }
            }
        }

        /// <summary>
        /// Handles preset save and recall button events
        /// </summary>
        /// <param name="o">Event sender</param>
        /// <param name="e">Button group event arguments</param>
        private void OnBgPreset(object o, ButtonGroupEventArgs e)
        {
            if (e.Sig.BoolValue)
            {
                NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Debug, $"{_panelName} {MethodBase.GetCurrentMethod().Name} {e.Sig.Number}");
                switch (e.Sig.Number)
                {
                    case (uint)Btn.Store:
                        NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Debug, $"{_panelName} {MethodBase.GetCurrentMethod().Name} store");
                        PresetStore();
                        break;
                    case (uint)Btn.Recall:
                        NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Debug, $"{_panelName} {MethodBase.GetCurrentMethod().Name} recall");
                        PresetRecall();
                        break;
                }
            }
        }

        /// <summary>
        /// Handles source list Smart Object selection and updates routing state
        /// </summary>
        /// <param name="o">Event sender</param>
        /// <param name="ea">Smart Object event arguments</param>
        private void OnSourceList(object o, SmartObjectEventArgs ea)
        {
            //NexusDebugSmartObjectEvent(o, ea);
            if (!(ea.Sig.Name.Contains("Pressed")))
            {
                return;
            }

            if (ea.Sig.BoolValue)
            {
                NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Debug, $"{_panelName} {MethodBase.GetCurrentMethod().Name} handle press");
                NexusDebugSmartObjectEvent(o, ea);

                uint item = GetItemNumberFromSignalName(ea.Sig.Name);

                //make sure the item was mapped before doing anything
                if (MapSourceListItemToInput.TryGetValue(item, out uint physicalInput))
                {
                    _selectedSource = physicalInput;
                    SetSelectedSource(_selectedSource);
                    NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Debug, $"{_panelName} {MethodBase.GetCurrentMethod().Name} mapped item {item} -> physical input {physicalInput}");
                }
                else
                {
                    NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Debug, $"{_panelName} {MethodBase.GetCurrentMethod().Name} mapping for item {item} is not defined");
                }
                SetDestinations();
            }
        }

        /// <summary>
        /// Creates an audio or video route between source and destination
        /// </summary>
        /// <param name="type">The route type (Audio or Video)</param>
        /// <param name="destination">The destination output number</param>
        /// <param name="source">The source input number</param>
        private void MakeRoute(Type type, uint destination, uint source)
        {
            switch(type)
            {
                case Type.Audio:
                    _routesAudio[destination] = source;
                    SetLabelRoute(Type.Audio, destination);
                    break;
                case Type.Video:
                    _routesVideo[destination] = source;
                    SetLabelRoute(Type.Video, destination);
                    break;
                default:
                    NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Debug, $"{_panelName} {MethodBase.GetCurrentMethod().Name} invalid type");
                    break;
            }
        }

        /// <summary>
        /// Saves all current video routes to persistent data store
        /// </summary>
        private void PresetStore()
        {
            foreach (var kv in _routesVideo)
            {
                uint destination = kv.Key;
                uint source = kv.Value;
                var setLocal = CrestronDataStoreStatic.SetLocalIntValue($"videoout{destination}", (int)source);
                if (setLocal == CrestronDataStore.CDS_ERROR.CDS_SUCCESS)
                {
                    NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Debug,
                        $"{_panelName} {MethodBase.GetCurrentMethod().Name} successfully stored destination {destination} with source {source}");
                }
                else
                {
                    NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Errors,
                        $"{_panelName} {MethodBase.GetCurrentMethod().Name} failed to store destination {destination} with error {setLocal}");
                }
            }
        }

        /// <summary>
        /// Restores all video routes from persistent data store
        /// </summary>
        private void PresetRecall()
        {
            for (uint destination = 1; destination <= MaxDestVideo; destination++)
            {
                var getResult = CrestronDataStoreStatic.GetLocalIntValue($"videoout{destination}", out int source);

                if (getResult == CrestronDataStore.CDS_ERROR.CDS_SUCCESS)
                {
                    _routesVideo[(uint)destination] = (uint)source;
                    SetLabelRoute(Type.Video, destination);
                    NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Debug,
                        $"{_panelName} {MethodBase.GetCurrentMethod().Name} successfully recalled destination {destination} with source {source}");
                }
                else
                {
                    NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Errors,
                        $"{_panelName} {MethodBase.GetCurrentMethod().Name} failed to recall destination {destination} with error {getResult}");
                }
            }
        }



        private void ResetValueEnableDestinationVideo()
        {
            SetAllDictionaryValues(enableDestinationVideo, true);
        }

        /// <summary>
        /// Resets all video destination visibility states to true
        /// </summary>
        private void ResetValueVisibleDestinationVideo()
        {
            SetAllDictionaryValues(visibleDestinationVideo, true);
        }

        /// <summary>
        /// Resets source selection to default state
        /// </summary>
        public void ResetSelectedSource()
        {
            ResetSourceList();
            _selectedSource = 0;
            SetSelectedSource(_selectedSource);
            ScrollToItem(_sourceList, 1);
            SetDestinations();
        }

        /// <summary>
        /// Resets source list selection state
        /// </summary>
        private void ResetSourceList()
        {
            // Reset all items - use MapSourceListItemToInput keys (1-9) not physical inputs (0-8)
            Dictionary<uint, bool> resetMap = new Dictionary<uint, bool>();
            foreach (var kv in MapSourceListItemToInput)
            {
                resetMap[kv.Key] = false;  // kv.Key is list item number (1-9)
            }
            ResetItemSelected(_sourceList, resetMap);
        }

        /// <summary>
        /// Updates destination enable and visibility states based on current source selection
        /// </summary>
        private void SetDestinations()
        {
            //// use this section for setting destination states based on source selection if needed
            //// for example if certain sources should only be routable to certain destinations
            //// you could set the enable state of the destination buttons here based on the selected source
            ResetValueEnableDestinationVideo();
            SetValueEnableDestinationVideo();
            SetEnableDestVideo();
            //// TODO - move somewhere useful, this is just an example of how to also use visibily states but wouldnt use it on source selection
            ResetValueVisibleDestinationVideo();
            SetValueVisibleDestinationVideo();
            SetVisibleDestVideo();
            //// Audio - may want to just call the methods directly without the helper, not sure which will be best
            SetAllDictionaryValues(enableDestinationAudio, true);
            SetAllDictionaryValues(visibleDestinationAudio, true);
            SetValueEnableDestinationAudio();
            SetValueVisibleDestinationAudio();
            SetEnableDestAudio();
            SetVisibleDestAudio();
        }

        /// <summary>
        /// Selects a source input and updates the UI to reflect the selection
        /// </summary>
        /// <param name="source">The physical source input number to select</param>
        private void SetSelectedSource(uint source)
        {
            NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Debug, $"{_panelName} {MethodBase.GetCurrentMethod().Name} source {source}");
            this.ResetSourceList();
            var listItem = MapSourceListItemToInput.FirstOrDefault(kv => kv.Value == source).Key;
            if (listItem > 0)
            {
                SetItemSelected(_sourceList, listItem, true);
            }
            else
            {
                NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Warning, $"{_panelName} {MethodBase.GetCurrentMethod().Name} source {source} not found in mapping");
            }
        }

        /// <summary>
        /// Configures audio destination enable states based on source selection
        /// </summary>
        private void SetValueEnableDestinationAudio()
        {
            switch ((Source)_selectedSource)
            {
                case Source.Test:
                    enableDestinationAudio[(int)DestinationAudio.Enable] = false;
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Configures audio destination visibility states based on source selection
        /// </summary>
        private void SetValueVisibleDestinationAudio()
        {
            switch ((Source)_selectedSource)
            {
                case Source.Test:
                    visibleDestinationAudio[(int)DestinationAudio.Visible] = false;
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Configures video destination enable states based on source selection
        /// </summary>
        private void SetValueEnableDestinationVideo()
        {
            switch((Source)_selectedSource)
            {
                case Source.Test:
                    enableDestinationVideo[(int)DestinationVideo.Enable] = false;
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Configures video destination visibility states based on source selection
        /// </summary>
        private void SetValueVisibleDestinationVideo()
        {
            switch ((Source)_selectedSource)
            {
                case Source.Test:
                    visibleDestinationVideo[(int)DestinationVideo.Visible] = false;
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Sets enable states for all source list items based on physical input configuration
        /// </summary>
        private void SetEnableSourceList()
        {
            foreach (var kv in MapSourceListItemToInput)
            {
                uint listItem = kv.Key;         // 1-9 (UI list position)
                uint physicalInput = kv.Value;  // 0-8 (physical input)

                if (enableInput.TryGetValue(physicalInput, out bool enabled))
                {
                    SetItemEnabled(_sourceList, listItem, enabled);
                }
            }
        }
        /// <summary>
        /// Sets icons/images for all source list items
        /// </summary>
        private void SetIconSourceList()
        {
            foreach (var kv in MapSourceListItemToInput)
            {
                uint listItem = kv.Key;         // 1-9 (UI list position)
                uint physicalInput = kv.Value;  // 0-8 (physical input)

                if (_iconSource.TryGetValue(physicalInput, out string icon))
                {
                    SetItemIcon(_sourceList, listItem, icon);
                }
            }
        }

        /// <summary>
        /// Sets enable states for audio destination buttons
        /// </summary>
        private void SetEnableDestAudio()
        {
            for (uint i = 1; i <= MaxDestAudio; i++)
            {
                if (enableDestinationAudio.TryGetValue(i, out bool state))
                {
                    _panel.SetBoolean(EnableDestAudioBase + i, state);
                }
                else
                {
                    NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Debug, $"{_panelName} {MethodBase.GetCurrentMethod().Name} {i} is not defined");
                }
            }
        }

        /// <summary>
        /// Sets enable states for video destination buttons
        /// </summary>
        private void SetEnableDestVideo()
        {
            for (uint i = 1; i <= MaxDestVideo; i++)
            {
                if (enableDestinationVideo.TryGetValue(i, out bool state))
                {
                    _panel.SetBoolean(EnableDestVideoBase + i, state);
                }
                else
                {
                    NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Debug, $"{_panelName} {MethodBase.GetCurrentMethod().Name} {i} is not defined");
                }
            }
        }

        /// <summary>
        /// Sets display labels for all video destination outputs
        /// </summary>
        private void SetLabelDestinationVideo()
        {
            for(uint i = 1; i <= MaxDestVideo; i++)
            {
                if (_labelDestinationVideo.TryGetValue(i, out string label))
                {
                    _panel.SetSerial(LblDestVideoBase + i, label);
                }
                else
                {
                    NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Debug, $"{_panelName} {MethodBase.GetCurrentMethod().Name} {i} is not defined");
                }
            }
        }

        /// <summary>
        /// Sets visibility states for audio destination buttons
        /// </summary>
        private void SetVisibleDestAudio()
        {
            for (uint i = 1; i <= MaxDestAudio; i++)
            {
                if (visibleDestinationAudio.TryGetValue(i, out bool state))
                {
                    _panel.SetBoolean(VisibleDestAudioBase + i, state);
                }
                else
                {
                    NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Debug, $"{_panelName} {MethodBase.GetCurrentMethod().Name} {i} is not defined");
                }
            }
        }

        /// <summary>
        /// Sets visibility states for video destination buttons
        /// </summary>
        private void SetVisibleDestVideo()
        {
            for (uint i = 1; i <= MaxDestVideo; i++)
            {
                if (visibleDestinationVideo.TryGetValue(i, out bool state))
                {
                    _panel.SetBoolean(VisibleDestVideoBase + i, state);
                }
                else
                {
                    NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Debug, $"{_panelName} {MethodBase.GetCurrentMethod().Name} {i} is not defined");
                }
            }
        }

        /// <summary>
        /// Updates the destination button label to reflect the current route source
        /// </summary>
        /// <param name="type">The route type (Audio or Video)</param>
        /// <param name="destination">The destination output number</param>
        private void SetLabelRoute(Type type, uint destination)
        {

            switch (type)
            {
                case Type.Audio:
                    {
                        if (_routesAudio.TryGetValue(destination, out uint value))
                            if (_labelSource.TryGetValue(value, out string label))
                                _panel.SetSerial((uint)(BtnDestAudioBase + destination), label);
                        break;
                    }
                case Type.Video:
                    {
                        if (_routesVideo.TryGetValue(destination, out uint value))
                            if (_labelSource.TryGetValue(value, out string label))
                                _panel.SetSerial((uint)(BtnDestVideoBase + destination), label);
                        break;
                    }
                default:
                    NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Debug, $"{_panelName} {MethodBase.GetCurrentMethod().Name} invalid type");
                    break;
            }
        }

        /// <summary>
        /// Sets display labels for all source list items
        /// </summary>
        private void SetLabelSourceList()
        {
            foreach (var kv in MapSourceListItemToInput)
            {
                uint listItem = kv.Key;         // 1-9 (UI list position)
                uint physicalInput = kv.Value;  // 0-8 (physical input)

                if (_labelSource.TryGetValue(physicalInput, out string label))
                {
                    SetItemText(_sourceList, listItem, label);
                }
            }
        }
        /// <summary>
        /// Sets visibility states for all source list items
        /// </summary>
        private void SetVisibleSourceList()
        {
            foreach (var kv in MapSourceListItemToInput)
            {
                uint listItem = kv.Key;         // 1-9 (UI list position)
                uint physicalInput = kv.Value;  // 0-8 (physical input)

                if (_visibleSource.TryGetValue(physicalInput, out bool visible))
                {
                    SetItemVisible(_sourceList, listItem, visible);
                }
            }
        }

        /// <summary>
        /// Handles system setting.
        /// </summary>
        /// <param name="FriendlyName"> The friendly name of the setting. </param>
        /// <param name="Settings"> The settings object containing updated values. </param>
        //private void System_OnSettingChanged(string FriendlyName, Nexus.Driver.Architecture.Configuration.INexusSettings Settings)
        private void System_OnSettingChanged(string FriendlyName, INexusSettings Settings)
        {
            // uses reflection to avoid this kind of manual assignment
            //    _labelInput[0] = sourceNames.Source00;
            //    _labelInput[1] = sourceNames.Source01;
            if (Settings is SourceNames sourceNames)
            {
                var sourceType = sourceNames.GetType();
                for (int source = 0; source <= MaxSource; source++)
                {
                    var propName = $"Source{source:D2}";  // D2 formats as "00", "01", etc.
                    var prop = sourceType.GetProperty(propName);
                    if (prop != null)
                    {
                        _labelSource[(uint)source] = (string)prop.GetValue(sourceNames);
                    }
                }

                SetLabelSourceList();
            }

            else if (Settings is SourceIcons sourceIcons)
            {
                var sourceType = sourceIcons.GetType();
                for (int source = 0; source <= MaxSource; source++)
                {
                    var propName = $"Source{source:D2}";  
                    var prop = sourceType.GetProperty(propName);
                    if (prop != null)
                    {
                        _iconSource[(uint)source] = (string)prop.GetValue(sourceIcons);
                    }
                }

                SetIconSourceList();
            }

            else if (Settings is SourceVisible sourceVisible)
            {
                var sourceType = sourceVisible.GetType();
                for (int source = 0; source <= MaxSource; source++)
                {
                    var propName = $"Source{source:D2}";  
                    var prop = sourceType.GetProperty(propName);
                    if (prop != null)
                    {
                        _visibleSource[(uint)source] = (bool)prop.GetValue(sourceVisible);
                    }
                }

                SetVisibleSourceList();
            }
            else if (Settings is VideoDestinationNames videoDestinationNames)
            {
                var destType = videoDestinationNames.GetType();
                for (int destination = 1; destination <= MaxSource; destination++)
                {
                    var propName = $"Destination{destination:D2}";
                    var prop = destType.GetProperty(propName);
                    if (prop != null)
                    {
                        _labelDestinationVideo[(uint)destination] = (string)prop.GetValue(videoDestinationNames);
                    }
                }
                SetLabelDestinationVideo();
            }
        }
    }
}

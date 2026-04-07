using Crestron.SimplSharp;
using Crestron.SimplSharp.CrestronDataStore;
using Crestron.SimplSharpPro;
using Crestron.SimplSharpPro.AudioDistribution;
using Forte.SSPro.UI.Helper.Library.UI;
using Independentsoft.Exchange;
using Nexus.Driver.Architecture.Components;
using Nexus.Driver.Architecture.Configuration;
using Nexus.Framework.Services;
using Nexus.Utils;
using Nexus.Vaddio.RoboshotIP.Driver;
using NexusCommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static Crestron.SimplSharpPro.CrestronConnected.CrestronConnectedDisplayV2.CCVideo;
using static NexusCommon.Settings;
using static NexusVtp.SmartGraphics;
using static NexusVtp.TpHelper;


namespace NexusVtp
{
    internal class TpMatrix
    {
        private const int MaxSource = 8;
        private const int MaxDestAudio = 2;
        private const int MaxDestVideo = 4;

        private const int LblDestVideoBase = 400;
        private const int BtnDestVideoBase = 500;
        private const int EnableDestVideoBase = 540;
        private const int VisibleDestVideoBase = 580;

        private const int BtnDestAudioBase = 700;
        private const int EnableDestAudioBase = 740;
        private const int VisibleDestAudioBase = 780;

        private string _panelName = string.Empty;
        private Panel _Panel;
        private ExtendedSmartObject _sourceList;
        private uint _selectedSource;

        private Dictionary<uint, (ExtendedSmartObject Id, SgCue Cue)> _cuesEnableSourceList;
        private Dictionary<uint, (ExtendedSmartObject Id, SgCue Cue)> _cuesIconSourceList;
        private Dictionary<uint, (ExtendedSmartObject Id, SgCue Cue)> _cuesLabelSourceList;
        private Dictionary<uint, (ExtendedSmartObject Id, SgCue Cue)> _cuesVisibleSourceList;

        private Dictionary<int, int> _routesAudio;
        private Dictionary<int, int> _routesVideo;

        // map the source list to the physical input
        // key is list item, value is physical input
        // this provides flexibility to reorder the list separate from the physical connections
        // so YOU ONLY have to change this one dictionary to reorder the list, and the rest of 
        // the dics are tied tot he physical input, so they dont have to change if you reorder the list
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

        private static readonly Dictionary<uint, bool> enableDestinationAudio = new Dictionary<uint, bool>
        {
            { 1, true },
            { 2, true },
        };

        private static readonly Dictionary<uint, bool> enableDestinationVideo = new Dictionary<uint, bool>
        {
            { 1, true },
            { 2, true },
            { 3, true },
            { 4, true },
        };

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

        //private static readonly Dictionary<int, string> iconInput = new Dictionary<int, string>
        //{
        //    { 0, "Ban" },
        //    { 1, "Video Input" },
        //    { 2, "Keyboard" },
        //    { 3, "USB" },
        //    { 4, "Video Input" },
        //    { 5, "Users" },u
        //    { 6, "Grid" },
        //    { 7, "Camera" },
        //    { 8, "Volume Hi" },
        //};

        private Dictionary<uint, string> _iconSource = new Dictionary<uint, string>();    

        private static readonly Dictionary<uint, string> labelDestination = new Dictionary<uint, string>
        {
            { 1, "Output 1" },
            { 2, "Output 2" },
            { 3, "Output 3" },
            { 4, "Output 4" },
        };

        //private static readonly Dictionary<int, string> labelInput = new Dictionary<int, string>
        //{
        //    { 0, "Input 0" },
        //    { 1, "Input 1" },
        //    { 2, "Input 2" },
        //    { 3, "Input 3" },
        //    { 4, "Input 4" },
        //    { 5, "Input 5" },
        //    { 6, "Input 6" },u
        //    { 7, "Input 7" },
        //    { 8, "Input 8" },
        //};


        private Dictionary<uint, string> _labelSource = new Dictionary<uint, string>();

        //private static readonly Dictionary<int, bool> visibleInput = new Dictionary<int, bool>
        //{
        //    { 0, true },
        //    { 1, true },
        //    { 2, true },
        //    { 3, true },
        //    { 4, true },
        //    { 5, true },
        //    { 6, true },
        //    { 7, true },
        //    { 8, true },
        //};

        private Dictionary<uint, bool> _visibleSource = new Dictionary<uint, bool>();


        private static readonly Dictionary<uint, bool> visibleDestinationAudio = new Dictionary<uint, bool>
        {
            { 1, true },
            { 2, true },
        };

        private static readonly Dictionary<uint, bool> visibleDestinationVideo = new Dictionary<uint, bool>
        {
            { 1, true },
            { 2, true },
            { 3, true },
            { 4, true },
        };

        // for referencing source and destinations to set enable states based on source selection
        enum Btn
        {
            Store = 598,
            Recall = 599,
        }


        enum Type
        {
            Audio = 1,
            Video = 2,
        }

        enum Source
        {
            Test = 3,

        }
        enum DestinationAudio
        {
            Enable = 1,
            Visible = 2,
        }

        enum DestinationVideo
        {
            Enable = 2,
            Visible = 4,
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TpMatrix"/> class.
        /// </summary>
        /// <param name="panel"> The wrapped tp. </param>
        /// <param name="panelName"> Description of the panel for debugging. </param>
        public TpMatrix(Panel panel, string panelName)
        {
            this._Panel = panel;
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
                var bgDestAudio = _Panel.AddButtonGroup("DestAudio", BtnDestAudioBase, (BtnDestAudioBase + MaxDestAudio));
                bgDestAudio.OnPanelButtonGroupChange += OnBgDestAudio;

                var bgDestVideo = _Panel.AddButtonGroup("DestVideo", BtnDestVideoBase, (BtnDestVideoBase + MaxDestVideo));
                bgDestVideo.OnPanelButtonGroupChange += OnBgDestVideo;

                var bgPreset = _Panel.AddButtonGroup("Preset", (uint)Btn.Store, (uint)Btn.Recall);
                bgPreset.OnPanelButtonGroupChange += OnBgPreset;

                //labels - joins
                for (uint i = 1; i <= MaxDestVideo; i++)
                {
                    _Panel.AddTextField($"LblDestVideo{i}", LblDestVideoBase + i);
                }
                this.SetLabelDestVideo();

                // smart graphics
                _sourceList = _Panel.AddSmartObject("SourceList", _Panel.ThePanel.SmartObjects[(int)SgId.SourceList]);
                _sourceList.OnSmartObjectSignalChange += OnSourceList;

                _cuesEnableSourceList = new Dictionary<uint, (ExtendedSmartObject Id, SgCue Cue)>();
                foreach (var kv in MapSourceListItemToInput)
                {
                    uint listItem = kv.Key;         // 1..9 (UI list position)
                    uint physicalInput = kv.Value;  // 0..8 (physical input, 0 = de-route)
                    _cuesEnableSourceList[physicalInput] = (_sourceList, (SgCue)(EnableCueItemBase + listItem));
                }
                this.SetEnableSourceList();

                _cuesIconSourceList = new Dictionary<uint, (ExtendedSmartObject Id, SgCue Cue)>();
                foreach (var kv in MapSourceListItemToInput)
                {
                    uint listItem = kv.Key;
                    uint physicalInput = kv.Value;
                    _cuesIconSourceList[physicalInput] = (_sourceList, (SgCue)(IconCueItemBase + listItem));
                }
                this.SetIconSourceList();

                _cuesLabelSourceList = new Dictionary<uint, (ExtendedSmartObject Id, SgCue Cue)>();
                foreach (var kv in MapSourceListItemToInput)
                {
                    uint listItem = kv.Key;
                    uint physicalInput = kv.Value;
                    _cuesLabelSourceList[physicalInput] = (_sourceList, (SgCue)(CueItemBase + listItem));
                }
                this.SetLabelSourceList();

                _cuesVisibleSourceList = new Dictionary<uint, (ExtendedSmartObject Id, SgCue Cue)>();
                foreach (var kv in MapSourceListItemToInput)
                {
                    uint listItem = kv.Key;
                    uint physicalInput = kv.Value;
                    _cuesVisibleSourceList[physicalInput] = (_sourceList, (SgCue)(VisibleCueItemBase + listItem));
                }
                this.SetVisibleSourceList();

                //initialize dic to store routes - THIS IS TEMP UNTIL WE HAVE REAL FEEDBACK FROM THE MATRIX, THEN THIS WILL BE UPDATED BASED ON FEEDBACK, NOT JUST WHEN WE SEND A ROUTE CHANGE
                _routesAudio = new Dictionary<int, int>();
                _routesVideo = new Dictionary<int, int>();

                //datastore
                var initResult = CrestronDataStoreStatic.InitCrestronDataStore();

                if (initResult != CrestronDataStore.CDS_ERROR.CDS_SUCCESS)
                {
                    ErrorLog.Error("DataStore init failed: {0}", initResult);
                    return;
                }
            }
            catch (Exception err)
            {
                NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Errors, $"{_panelName} {MethodBase.GetCurrentMethod().Name}: {GetInnerErr.GetInnermostException(err)}");
                NexusServiceManager.System.Log(Nexus.Driver.Architecture.Enumerations.LoggingLevels.Exceptions, $"{_panelName} {MethodBase.GetCurrentMethod().Name}: {GetInnerErr.GetInnermostException(err)}");
            }
        }

        // event handlers - button objects
        private void OnBgDestAudio(object o, ButtonGroupEventArgs e)
        {
            if (e.Sig.BoolValue)
            {
                NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Debug, $"{_panelName} {MethodBase.GetCurrentMethod().Name} {e.Sig.Number}");
                int destination = (int)e.Sig.Number - BtnDestAudioBase;
                if (destination < 0)
                {
                    NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Debug, $"{_panelName} {MethodBase.GetCurrentMethod().Name} {destination} is out of range");
                    return;
                }
                else
                {
                    NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Debug, $"{_panelName} {MethodBase.GetCurrentMethod().Name} {_selectedSource} to {destination}");
                    MakeRoute(Type.Audio, destination, (int)_selectedSource);
                }
            }
        }
        private void OnBgDestVideo(object o, ButtonGroupEventArgs e)
        {
            if (e.Sig.BoolValue)
            {
                NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Debug, $"{_panelName} {MethodBase.GetCurrentMethod().Name} {e.Sig.Number}");
                int destination = (int)e.Sig.Number - BtnDestVideoBase;
                if (destination < 0)
                {
                    NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Debug, $"{_panelName} {MethodBase.GetCurrentMethod().Name} {destination} is out of range");
                    return;
                }
                else
                {
                    NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Debug, $"{_panelName} {MethodBase.GetCurrentMethod().Name} {_selectedSource} to {destination}");
                    MakeRoute(Type.Video, destination, (int)_selectedSource);
                }
            }
        }

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

        // event handlers - Smart objects
        private void OnSourceList(object o, SmartObjectEventArgs ea)
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
                ResetCueItemBooleans(_sourceList);
                _sourceList.SetBoolean((uint)num, true);
                uint item = GetIndexFromItemCue(num);         
                NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Debug, $"{_panelName} {MethodBase.GetCurrentMethod().Name} item {item}");
                if (MapSourceListItemToInput.TryGetValue(item, out uint physicalInput))
                { 
                    _selectedSource = physicalInput;
                    NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Debug, $"{_panelName} {MethodBase.GetCurrentMethod().Name} mapped item {item} -> physical input {physicalInput}");
                }
                else
                {
                    NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Debug, $"{_panelName} {MethodBase.GetCurrentMethod().Name} mapping for item {item} is not defined");
                }
                // use this section for setting destination states based on source selection if needed
                // for example if certain sources should only be routable to certain destinations
                // you could set the enable state of the destination buttons here based on the selected source
                ResetValueEnableDestinationVideo();
                SetValueEnableDestinationVideo();     
                SetEnableDestVideo();
                // TODO - move somewhere useful, this is just an example of how to also use visibily states but wouldnt use it on source selection
                ResetValueVisibleDestinationVideo();
                SetValueVisibleDestinationVideo();
                SetVisibleDestVideo();
                // Audio - may want to just call the methods directly without the helper, not sure which will be best
                SetAllDictionaryValues(enableDestinationAudio, true);
                SetAllDictionaryValues(visibleDestinationAudio, true);
                SetValueEnableDestinationAudio();
                SetValueVisibleDestinationAudio();
                SetEnableDestAudio();
                SetVisibleDestAudio();
            }
        }

        // methods
        private void MakeRoute(Type type, int destination, int source)
        {
            switch(type)
            {
                case Type.Audio:
                    _routesAudio[destination] = source;
                    //SetLabelRoute(Type.Audio, destination);
                    break;
                case Type.Video:
                    _routesVideo[destination] = source;
                    //SetLabelRoute(Type.Video, destination);
                    break;
                default:
                    NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Debug, $"{_panelName} {MethodBase.GetCurrentMethod().Name} invalid type");
                    break;
            }
        }

        private void PresetStore()
        {
            foreach (var kv in _routesVideo)
            {
                int destination = kv.Key;
                int source = kv.Value;
                var setLocal = CrestronDataStoreStatic.SetLocalIntValue($"videoout{destination}", source);
                if (setLocal == CrestronDataStore.CDS_ERROR.CDS_SUCCESS)
                {
                    NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Debug,
                        $"{_panelName} {MethodBase.GetCurrentMethod().Name} successfully stored destination {destination} with source {source}");
                }
                else
                {
                    NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Debug,
                        $"{_panelName} {MethodBase.GetCurrentMethod().Name} failed to store destination {destination} with error {setLocal}");
                }
            }
        }

        private void PresetRecall()
        {
            for (int destination = 1; destination <= MaxDestVideo; destination++)
            {
                var getResult = CrestronDataStoreStatic.GetLocalIntValue($"videoout{destination}", out int source);

                if (getResult == CrestronDataStore.CDS_ERROR.CDS_SUCCESS)
                {
                    _routesVideo[destination] = source;
                    //SetLabelRoute(Type.Video, destination);
                    NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Debug,
                        $"{_panelName} {MethodBase.GetCurrentMethod().Name} successfully recalled destination {destination} with source {source}");
                }
                else
                {
                    NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Debug,
                        $"{_panelName} {MethodBase.GetCurrentMethod().Name} failed to recall destination {destination} with error {getResult}");
                }
            }
        }

        private void ResetValueEnableDestinationVideo()
        {
            SetAllDictionaryValues(enableDestinationVideo, true);
        }

        private void ResetValueVisibleDestinationVideo()
        {
            SetAllDictionaryValues(visibleDestinationVideo, true);
        }

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

        private void SetEnableSourceList()
        {
            SetCueByKey(_cuesEnableSourceList, enableInput);
        }
        private void SetIconSourceList()
        {
            //SetCueByKey(_cuesIconSourceList, iconInput);
            SetCueByKey(_cuesIconSourceList, _iconSource);
        }

        private void SetEnableDestAudio()
        {
            for (uint i = 1; i <= MaxDestAudio; i++)
            {
                if (enableDestinationAudio.TryGetValue(i, out bool state))
                {
                    _Panel.SetBoolean(EnableDestAudioBase + i, state);
                }
                else
                {
                    NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Debug, $"{_panelName} {MethodBase.GetCurrentMethod().Name} {i} is not defined");
                }
            }
        }

        private void SetEnableDestVideo()
        {
            for (uint i = 1; i <= MaxDestVideo; i++)
            {
                if (enableDestinationVideo.TryGetValue(i, out bool state))
                {
                    _Panel.SetBoolean(EnableDestVideoBase + i, state);
                }
                else
                {
                    NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Debug, $"{_panelName} {MethodBase.GetCurrentMethod().Name} {i} is not defined");
                }
            }
        }

        private void SetLabelDestVideo()
        {
            for(uint i = 1; i <= MaxDestVideo; i++)
            {
                if (labelDestination.TryGetValue(i, out string label))
                {
                    _Panel.SetSerial(LblDestVideoBase + i, label);
                }
                else
                {
                    NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Debug, $"{_panelName} {MethodBase.GetCurrentMethod().Name} {i} is not defined");
                }
            }
        }

        private void SetVisibleDestAudio()
        {
            for (uint i = 1; i <= MaxDestAudio; i++)
            {
                if (visibleDestinationAudio.TryGetValue(i, out bool state))
                {
                    _Panel.SetBoolean(VisibleDestAudioBase + i, state);
                }
                else
                {
                    NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Debug, $"{_panelName} {MethodBase.GetCurrentMethod().Name} {i} is not defined");
                }
            }
        }

        private void SetVisibleDestVideo()
        {
            for (uint i = 1; i <= MaxDestVideo; i++)
            {
                if (visibleDestinationVideo.TryGetValue(i, out bool state))
                {
                    _Panel.SetBoolean(VisibleDestVideoBase + i, state);
                }
                else
                {
                    NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Debug, $"{_panelName} {MethodBase.GetCurrentMethod().Name} {i} is not defined");
                }
            }
        }

        //private void SetLabelRoute(Type type, int destination)
        //{

        //    switch (type)
        //    {
        //        case Type.Audio:
        //        {
        //            if (_routesAudio.TryGetValue(destination, out int value))
        //                //_Panel.SetSerial((uint)(BtnDestAudioBase + destination), value.ToString());
        //                if (labelInput.TryGetValue(value, out string label))
        //                    _Panel.SetSerial((uint)(BtnDestAudioBase + destination), label);
        //            break;
        //        }
        //        case Type.Video:
        //        {
        //            if (_routesVideo.TryGetValue(destination, out int value))
        //                //_Panel.SetSerial((uint)(BtnDestVideoBase + destination), value.ToString());
        //                if (labelInput.TryGetValue(value, out string label))
        //                    _Panel.SetSerial((uint)(BtnDestVideoBase + destination), label);
        //            break;
        //        }
        //        default:
        //            NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Debug, $"{_panelName} {MethodBase.GetCurrentMethod().Name} invalid type");
        //            break;
        //    }
        //}
   
        private void SetLabelSourceList()
        {
            SetCueByKey(_cuesLabelSourceList, _labelSource);
        }
        private void SetVisibleSourceList()
        {
            SetCueByKey(_cuesVisibleSourceList, _visibleSource);
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
                    var propName = $"Source{source:D2}";  // D2 formats as "00", "01", etc.
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
                    var propName = $"Source{source:D2}";  // D2 formats as "00", "01", etc.
                    var prop = sourceType.GetProperty(propName);
                    if (prop != null)
                    {
                        _visibleSource[(uint)source] = (bool)prop.GetValue(sourceVisible);
                    }
                }

                SetVisibleSourceList();
            }
        }
    }
}

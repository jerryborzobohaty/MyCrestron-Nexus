using Crestron.SimplSharpPro;
using Forte.SSPro.UI.Helper.Library.UI;
using Nexus.Framework.Services;
using Nexus.Driver.Architecture.Configuration;
using Nexus.Utils;
using NexusCommon;
using System;
using System.Reflection;
using System.Collections.Generic;
using static NexusVtp.SmartGraphics;

namespace NexusVtp
{   
    public class TpLights
    {
        /// <summary>
        /// The panel name for debugging purposes.
        /// </summary>
        private string _panelName = string.Empty;

        /// <summary>
        /// The wrapped touch panel.
        /// </summary>
        private Panel _panel;

        /// <summary>
        /// The lighting preset smart object.
        /// </summary>
        private ExtendedSmartObject _preset;

        /// <summary>
        /// Initializes a new instance of the <see cref="TpLights"/> class.
        /// </summary>
        /// <param name="panel"> The wrapped tp. </param>
        /// <param name="panelName"> Description of the panel for debugging. </param>
        public TpLights(Panel panel, string panelName)
        {
            this._panel = panel;
            this._panelName = panelName;
            Initialize();
        }

        /// <summary>
        /// Initializes smart objects and subscribes to events.
        /// </summary>
        private void Initialize()
        {
            try
            { 
                _preset = _panel.AddSmartObject("LightsPreset", _panel.ThePanel.SmartObjects[(int)SgId.LightsPreset]);
                _preset.OnSmartObjectSignalChange += OnPreset;
                NexusServiceManager.System.OnSettingChanged += System_OnSettingChanged;
            }
            catch (Exception err)
            {
                NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Errors, $"{_panelName} {MethodBase.GetCurrentMethod().Name}: {GetInnerErr.GetInnermostException(err)}");
                NexusServiceManager.System.Log(Nexus.Driver.Architecture.Enumerations.LoggingLevels.Exceptions, $"{_panelName} {MethodBase.GetCurrentMethod().Name}: {GetInnerErr.GetInnermostException(err)}");
            }
        }

        /// <summary>
        /// Handles lighting preset selection from the smart object.
        /// </summary>
        /// <param name="o"> The sender object. </param>
        /// <param name="ea"> Smart object event arguments containing the signal data. </param>
        private void OnPreset(object o, SmartObjectEventArgs ea)
        {
            //NexusDebugSmartObjectEvent(o, ea);
            if (!(ea.Sig.Name.Contains("Pressed")))
            {
                return;
            }

            _preset.SetBoolean(ea.Sig.Number, ea.Sig.BoolValue);

            if (ea.Sig.BoolValue)
            {
                NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Debug, $"{_panelName} {MethodBase.GetCurrentMethod().Name} handle press");
                NexusDebugSmartObjectEvent(o, ea);
                uint preset = GetItemNumberFromSignalName(ea.Sig.Name);
                NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Debug, $"{_panelName} {MethodBase.GetCurrentMethod().Name} Recalling light preset {preset}");
            }
        }

        /// <summary>
        /// Handles system setting.
        /// </summary>
        /// <param name="FriendlyName"> The friendly name of the setting. </param>
        /// <param name="Settings"> The settings object containing updated values. </param>
        private void System_OnSettingChanged(string FriendlyName, INexusSettings Settings)
        {
            if (Settings is Settings.LightingPresets lightingPresets)
            {
                var presetLabels = new Dictionary<uint, string>
                {
                    { 1, lightingPresets.Preset1 },
                    { 2, lightingPresets.Preset2 },
                    { 3, lightingPresets.Preset3 },
                    { 4, lightingPresets.Preset4 },
                    { 5, lightingPresets.Preset5 },
                    { 6, lightingPresets.Preset6 }
                };

                // Build debug message from dictionary
                var debugMessage = $"{MethodBase.GetCurrentMethod().Name}: {FriendlyName}\n";
                foreach (var kv in presetLabels)
                {
                    debugMessage += $"  Preset{kv.Key}: {kv.Value}\n";
                }

                NexusServiceManager.System.Debug(
                    Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Debug,
                    debugMessage.TrimEnd('\n'));

                SetLabels(presetLabels);
            }
        }

        /// <summary>
        /// Sets the serial values for lighting preset labels on the smart object.
        /// </summary>
        /// <param name="presetLabels"> Dictionary of preset labels with item numbers as keys. </param>
        private void SetLabels(Dictionary<uint, string> presetLabels)
        {
            SetItemTextByDictionary(_preset, presetLabels);
        }
    }
}

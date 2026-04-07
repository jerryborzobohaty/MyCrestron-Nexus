using Crestron.SimplSharpPro;
using Forte.SSPro.UI.Helper.Library.UI;
using Nexus.Framework.Services;
using Nexus.Driver.Architecture.Configuration;
using Nexus.Utils;
using NexusCommon;
using System;
using System.Reflection;
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

        //private methods
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


        // event handlers - button objects

        // event handlers - Smart objects
        /// <summary>
        /// Handles lighting preset selection from the smart object.
        /// </summary>
        /// <param name="o"> The sender object. </param>
        /// <param name="ea"> Smart object event arguments containing the signal data. </param>
        private void OnPreset(object o, SmartObjectEventArgs ea)
        {
            NexusDebugSmartObjectEvent(o, ea);
            int num = (int)ea.Sig.Number;
            if (!Enum.IsDefined(typeof(SgCue), num))
            {
                NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Debug, $"{_panelName} {MethodBase.GetCurrentMethod().Name} {num} not defined");
                return;
            }

            _preset.SetBoolean((uint)num, ea.Sig.BoolValue);

            if (ea.Sig.BoolValue)
            {
                uint preset = GetIndexFromItemCue(num);
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
                var presetLabels = new[]
                {
                    lightingPresets.Preset1,
                    lightingPresets.Preset2,
                    lightingPresets.Preset3,
                    lightingPresets.Preset4,
                    lightingPresets.Preset5,
                    lightingPresets.Preset6
                };

                // Build debug message from array
                var debugMessage = $"{MethodBase.GetCurrentMethod().Name}: {FriendlyName}\n";
                for (int i = 0; i < presetLabels.Length; i++)
                {
                    debugMessage += $"  Preset{i + 1}: {presetLabels[i]}\n";
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
        /// <param name="presetLabels"> Array of preset label strings to display. </param>
        private void SetLabels(string[] presetLabels)
        {
            for (int i = 0; i < presetLabels.Length && i < ItemCues.Length; i++)
            {
                _preset.SetSerial((uint)ItemCues[i], presetLabels[i]);
            }
        }
    }
}

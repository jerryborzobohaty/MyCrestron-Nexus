using Crestron.SimplSharpPro;
using Forte.SSPro.UI.Helper.Library.UI;
using Nexus.Framework.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

namespace NexusVtp
{
    /// <summary>
    /// Contains Smart Graphics constants, enumerations, and helper methods for touchpanel UI control
    /// </summary>
    public static class SmartGraphics
    {
        /// <summary>
        /// Base join number for item cues when mapping using a loop
        /// </summary>
        public const int CueItemBase = 10;
        /// <summary>
        /// Base join number for enable item cues when mapping using a loop
        /// </summary>
        public const int EnableCueItemBase = 2010;
        /// <summary>
        /// Base join number for icon item cues when mapping using a loop
        /// </summary>
        public const int IconCueItemBase = 2010;
        /// <summary>
        /// Base join number for visible item cues when mapping using a loop
        /// </summary>
        public const int VisibleCueItemBase = 4010;

        /// <summary>
        /// Smart Graphics IDs for various UI components and controls
        /// </summary>
        public enum SgId
        {
            Mode = 1,
            ModePresentation = 2,
            ModeVtc = 3,
            MainMenu = 4,
            SourceList = 5,
            RoutingGroup = 6,
            SettingsMenu = 7,
            VolumeMaster = 9,
            PhoneKeypad = 12,
            VolumeMixer = 13,
            CameraSelect = 14,
            CameraDpad = 15,
            CameraPreset = 16,
            LightsPreset = 17,
        }

        /// <summary>
        /// Smart Graphics cue join numbers for list items (use these when mapping with a string for a key)
        /// </summary>
        public enum SgCue
        {
            Item1 = 11,
            Item2 = 12,
            Item3 = 13,
            Item4 = 14,
            Item5 = 15,
            Item6 = 16,
            Item7 = 17,
            Item8 = 18,
            Item9 = 19,

            EnableItem1 = 2011,
            EnableItem2 = 2012,
            EnableItem3 = 2013,
            EnableItem4 = 2014,
            EnableItem5 = 2015,
            EnableItem6 = 2016,
            EnableItem7 = 2017,
            EnableItem8 = 2018,
            EnableItem9 = 2019,

            VisibleItem1 = 4011,
            VisibleItem2 = 4012,
            VisibleItem3 = 4013,
            VisibleItem4 = 4014,
            VisibleItem5 = 4015,
            VisibleItem6 = 4016,
            VisibleItem7 = 4017,
            VisibleItem8 = 4018,
            VisibleItem9 = 4019,
        }

        /// <summary>
        /// Smart Graphics cue join numbers for volume controls (subpage reference list)
        /// </summary>
        public enum SgCueVolume
        {

            MasterUp = 4011,
            MasterDown = 4012,
            MasterMute = 4013,

            ProgramUp = 4011,     // program uses same cues as master but its cool becasue they are on different smart objects
            ProgramDown = 4012,
            ProgramMute = 4013,

            PhoneUp = 4014,
            PhoneDown = 4015,
            PhoneMute = 4016,

            MasterLevel = 11,
            ProgramLevel = 11, // program uses same cues as master but its cool becasue they are on different smart objects
            PhoneLevel = 12,
        }

        /// <summary>
        /// Array of all item cue enumerations for iteration
        /// </summary>
        public static readonly SgCue[] ItemCues = new[]
{
            SgCue.Item1, SgCue.Item2, SgCue.Item3, SgCue.Item4, SgCue.Item5,
            SgCue.Item6, SgCue.Item7, SgCue.Item8, SgCue.Item9
        };

        /// <summary>
        /// Logs detailed Smart Object event information for debugging purposes
        /// </summary>
        /// <param name="o">The source object that raised the event</param>
        /// <param name="ea">Smart Object event arguments containing signal data</param>
        public static void NexusDebugSmartObjectEvent(object o, SmartObjectEventArgs ea)
        {
            NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Debug, $"o {o}");
            NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Debug, $"id {ea.SmartObjectArgs.ID}");
            NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Debug, $"bool {ea.Sig.BoolValue}");
            NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Debug, $"number {ea.Sig.Number}");
            NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Debug, $"name {ea.Sig.Name}");
            NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Debug, $"ushort {ea.Sig.UShortValue}");
        }

        /// <summary>
        /// Converts a cue item join number to its array index
        /// </summary>
        /// <param name="num">The cue item join number</param>
        /// <returns>The zero-based index for the item</returns>
        public static uint GetIndexFromItemCue(int num)
        {
            return (uint)(num - CueItemBase);
        }

        /// <summary>
        /// Resets all item cue booleans to false for the specified Smart Object
        /// </summary>
        /// <param name="smartObject">The Smart Object whose item cue booleans will be reset</param>
        public static void ResetCueItemBooleans(ExtendedSmartObject smartObject)
        {
            for (int i = 0; i < ItemCues.Length; i++)
            {
                var cue = (uint)ItemCues[i];
                NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Debug, $"{MethodBase.GetCurrentMethod().Name} cue {cue}");
                smartObject.SetBoolean((uint)cue, false);
            }
        }

        /// <summary>
        /// Sets Smart Object cue values based on key-value mappings
        /// </summary>
        /// <typeparam name="TKey">The type of the key used in the dictionaries</typeparam>
        /// <typeparam name="TValue">The type of the value (bool or string)</typeparam>
        /// <param name="cueMap">Dictionary mapping keys to Smart Objects and their cue joins</param>
        /// <param name="valueMap">Dictionary mapping keys to values to be set</param>
        /// <param name="contextName">Optional context name for debug logging</param>
        public static void SetCueByKey<TKey, TValue>(
            Dictionary<TKey, (ExtendedSmartObject Id, SgCue Cue)> cueMap,
            Dictionary<TKey, TValue> valueMap,
            string contextName = "")
        {
            foreach (var kv in cueMap)
            {
                var key = kv.Key;
                var smart = kv.Value.Id;
                var cue = kv.Value.Cue;

                if ((int)cue == 0)
                    return;
                if (valueMap.ContainsKey(key))
                {
                    var value = valueMap[key];
                    if (value is bool boolValue)
                    {
                        smart.SetBoolean((uint)cue, boolValue);
                    }
                    else if (value is string stringValue)
                    {
                        smart.SetSerial((uint)cue, stringValue);
                    }
                    else
                    {
                        NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Errors,
                            $"{contextName} {nameof(SetCueByKey)} Unsupported value type: {typeof(TValue).Name}");
                    }
                }
                else
                {
                    NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Errors,
                        $"{contextName} {nameof(SetCueByKey)} {key} not found");
                }
            }
        }
    }
}

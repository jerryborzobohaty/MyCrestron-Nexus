using Crestron.SimplSharpPro;
using Forte.SSPro.UI.Helper.Library.UI;
using Nexus.Framework.Services;
using Org.BouncyCastle.Utilities.Collections;
using Quartz.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

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

            ProgramUp = 4011,
            ProgramDown = 4012,
            ProgramMute = 4013,

            PhoneUp = 4014,
            PhoneDown = 4015,
            PhoneMute = 4016,

            MasterLevel = 11,
            ProgramLevel = 11,
            PhoneLevel = 12,
        }

        /// <summary>
        /// Logs detailed Smart Object event information for debugging purposes
        /// </summary>
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
        /// Extracts the first number found in a signal name string using regex
        /// </summary>
        /// <param name="input">The signal name string to parse</param>
        /// <returns>The extracted number as a uint</returns>
        /// <exception cref="FormatException">Thrown when no number is found or conversion fails</exception>
        public static uint GetItemNumberFromSignalName(string input)
        {
            // Regular expression to find the number -From ChatGPT
            Regex regex = new Regex(@"\d+");

            // Match the first number in the input string
            Match match = regex.Match(input);

            if (match.Success)
            {
                // Try parsing the matched number to ushort
                if (uint.TryParse(match.Value, out uint number))
                {
                    return number;
                }
                else
                {
                    // Handle conversion failure
                    throw new FormatException("Failed to convert number to int.");
                }
            }
            else
            {
                // Handle no number found in the input
                throw new FormatException("No number found in the input string.");
            }
        }

        /// <summary>
        /// Resets the selected state for all items in the provided state map
        /// </summary>
        public static void ResetItemSelected(ExtendedSmartObject smartObject, Dictionary<uint, bool> stateMap)
        {
            foreach (var kv in stateMap)
            {
                SetItemSelected(smartObject, kv.Key, false);
            }
        }

        /// <summary>
        /// Sets the enabled state for a single Smart Object list item.
        /// </summary>
        public static void SetItemEnabled(ExtendedSmartObject smartObject, uint itemNumber, bool value)
        {
            var signalName = $"Item {itemNumber} Enabled";
            smartObject.SmartObject.BooleanInput[signalName].BoolValue = value;
        }

        /// <summary>
        /// Sets the icon for a single Smart Object list item.
        /// </summary>
        public static void SetItemIcon(ExtendedSmartObject smartObject, uint itemNumber, string value)
        {
            var signalName = $"Set Item {itemNumber} Icon Serial";
            smartObject.SmartObject.StringInput[signalName].StringValue = value;
        }

        /// <summary>
        /// Sets the selected state for a single Smart Object list item.
        /// </summary>
        public static void SetItemSelected(ExtendedSmartObject smartObject, uint itemNumber, bool value)
        {
            var signalName = $"Item {itemNumber} Selected";
            smartObject.SmartObject.BooleanInput[signalName].BoolValue = value;
        }

        /// <summary>
        /// Sets the text for a single Smart Object list item.
        /// </summary>
        public static void SetItemText(ExtendedSmartObject smartObject, uint itemNumber, string value)
        {
            var signalName = $"Set Item {itemNumber} Text";
            smartObject.SmartObject.StringInput[signalName].StringValue = value;
        }

        /// <summary>
        /// Sets the visible state for a single Smart Object list item.
        /// </summary>
        public static void SetItemVisible(ExtendedSmartObject smartObject, uint itemNumber, bool value)
        {
            var signalName = $"Item {itemNumber} Visible";
            smartObject.SmartObject.BooleanInput[signalName].BoolValue = value;
        }

        /// <summary>
        /// Sets enabled state for multiple Smart Object list items from a dictionary.
        /// </summary>
        public static void SetItemEnabledByDictionary(ExtendedSmartObject smartObject, Dictionary<uint, bool> valueMap)
        {
            foreach (var kv in valueMap)
            {
                SetItemEnabled(smartObject, kv.Key, kv.Value);
            }
        }

        /// <summary>
        /// Sets icons for multiple Smart Object list items from a dictionary.
        /// </summary>
        public static void SetItemIconByDictionary(ExtendedSmartObject smartObject, Dictionary<uint, string> valueMap)
        {
            foreach (var kv in valueMap)
            {
                SetItemIcon(smartObject, kv.Key, kv.Value);
            }
        }

        /// <summary>
        /// Sets text for multiple Smart Object list items from a dictionary.
        /// </summary>
        public static void SetItemTextByDictionary(ExtendedSmartObject smartObject, Dictionary<uint, string> valueMap)
        {
            foreach (var kv in valueMap)
            {
                SetItemText(smartObject, kv.Key, kv.Value);
            }
        }

        /// <summary>
        /// Sets visible state for multiple Smart Object list items from a dictionary.
        /// </summary>
        public static void SetItemVisibleByDictionary(ExtendedSmartObject smartObject, Dictionary<uint, bool> valueMap)
        {
            foreach (var kv in valueMap)
            {
                SetItemVisible(smartObject, kv.Key, kv.Value);
            }
        }
    }
}

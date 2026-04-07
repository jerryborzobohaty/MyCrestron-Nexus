using Nexus.Driver.Architecture.Configuration;
using Nexus.Framework.Services.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NexusCommon
{
    // The Nexus config webpage drop down orders the settings based on the order they are defined in this file
    // so sort alpahbetically by the name of the class to make it easier to find settings in the web interface
    // or reorder based on how commonly used they are
    public class Settings
    {
        [NexusComponentSettings("Room Information", typeof(NexusSystemDiagnostics), "AddSystemConfig")]
        public class RoomInformation : INexusSettings
        {
            public string Name { get; set; }

            [NexusStringFieldAttribute("Room Name", DefaultValue = "My Nexus")]
            public string RoomName { get; set; } = "Room Name";
        }

        [NexusComponentSettings("Camera Preset Names", typeof(NexusSystemDiagnostics), "AddSystemConfig")]
        public class CameraPresetNames : INexusSettings
        {
            public string Name { get; set; }

            [NexusStringFieldAttribute("Preset 01")]
            public string Preset01 { get; set; } = "Preset 01";

            [NexusStringFieldAttribute("Preset 02")]
            public string Preset02 { get; set; } = "Preset 02";
            [NexusStringFieldAttribute("Preset 03")]
            public string Preset03 { get; set; } = "Preset 03";

            [NexusStringFieldAttribute("Preset 04")]
            public string Preset04 { get; set; } = "Preset 04";

            [NexusStringFieldAttribute("Preset 05")]
            public string Preset05 { get; set; } = "Preset 05";

            [NexusStringFieldAttribute("Preset 06")]
            public string Preset06 { get; set; } = "Preset 06";
        }

        [NexusComponentSettings("Routing Group Names", typeof(NexusSystemDiagnostics), "AddSystemConfig")]
        public class RoutingGroupNames : INexusSettings
        {
            public string Name { get; set; }

            [NexusStringFieldAttribute("Routing Group 01")]
            public string RoutingGroup01 { get; set; } = "Routing Group 01";

            [NexusStringFieldAttribute("Routing Group 02")]
            public string RoutingGroup02 { get; set; } = "Routing Group 02";
            [NexusStringFieldAttribute("Routing Group 03")]
            public string RoutingGroup03 { get; set; } = "Routing Group 03";

            [NexusStringFieldAttribute("Routing Group 04")]
            public string RoutingGroup04 { get; set; } = "Routing Group 04";
        }

        // I couldn't find where Description and HelpText showed up in the web interface, so I omitted them from the below settings
        // Also, it doesnt seem like it defaults to the default value
        [NexusComponentSettings("Source Names", typeof(NexusSystemDiagnostics), "AddSystemConfig")]
        public class SourceNames : INexusSettings
        {
            public string Name { get; set; }

            [NexusStringFieldAttribute("Source 00")]
            public string Source00 { get; set; } = "Source 00";

            [NexusStringFieldAttribute("Source 01")]
            public string Source01 { get; set; } = "Source 01";

            [NexusStringFieldAttribute("Source 02")]
            public string Source02 { get; set; } = "Source 02";

            [NexusStringFieldAttribute("Source 03")]
            public string Source03 { get; set; } = "Source 03";

            [NexusStringFieldAttribute("Source 04")]
            public string Source04 { get; set; } = "Source 04";

            [NexusStringFieldAttribute("Source 05")]
            public string Source05 { get; set; } = "Source 05";

            [NexusStringFieldAttribute("Source 06")]
            public string Source06 { get; set; } = "Source 06";

            [NexusStringFieldAttribute("Source 07")]
            public string Source07 { get; set; } = "Source 07";

            [NexusStringFieldAttribute("Source 08")]
            public string Source08 { get; set; }
        }

        [NexusComponentSettings("Source Icons", typeof(NexusSystemDiagnostics), "AddSystemConfig")]
        public class SourceIcons : INexusSettings
        {
            public string Name { get; set; }

            [NexusStringFieldAttribute("Source 00")]
            public string Source00 { get; set; } = "Source 00";

            [NexusStringFieldAttribute("Source 01")]
            public string Source01 { get; set; } = "Source 01";

            [NexusStringFieldAttribute("Source 02")]
            public string Source02 { get; set; } = "Source 02";

            [NexusStringFieldAttribute("Source 03")]
            public string Source03 { get; set; } = "Source 03";

            [NexusStringFieldAttribute("Source 04")]
            public string Source04 { get; set; } = "Source 04";

            [NexusStringFieldAttribute("Source 05")]
            public string Source05 { get; set; } = "Source 05";

            [NexusStringFieldAttribute("Source 06")]
            public string Source06 { get; set; } = "Source 06";

            [NexusStringFieldAttribute("Source 07")]
            public string Source07 { get; set; } = "Source 07";

            [NexusStringFieldAttribute("Source 08")]
            public string Source08 { get; set; }
        }

        [NexusComponentSettings("Source Visible", typeof(NexusSystemDiagnostics), "AddSystemConfig")]
        public class SourceVisible : INexusSettings
        {
            public string Name { get; set; }

            [NexusStringFieldAttribute("Source 00")]
            public bool Source00 { get; set; } = true;

            [NexusStringFieldAttribute("Source 01")]
            public bool Source01 { get; set; } = true;

            [NexusStringFieldAttribute("Source 02")]
            public bool Source02 { get; set; } = true;

            [NexusStringFieldAttribute("Source 03")]
            public bool Source03 { get; set; } = true;

            [NexusStringFieldAttribute("Source 04")]
            public bool Source04 { get; set; } = true;

            [NexusStringFieldAttribute("Source 05")]
            public bool Source05 { get; set; } = true;

            [NexusStringFieldAttribute("Source 06")]
            public bool Source06 { get; set; } = true;

            [NexusStringFieldAttribute("Source 07")]
            public bool Source07 { get; set; } = true;

            [NexusStringFieldAttribute("Source 08")]
            public bool Source08 { get; set; } = true;
        }

        [NexusComponentSettings("Video Destination Names", typeof(NexusSystemDiagnostics), "AddSystemConfig")]
        public class VideoDestinationNames : INexusSettings
        {
            public string Name { get; set; }

            [NexusStringFieldAttribute("Destination 01")]
            public string Destination01 { get; set; } = "Destination 01";

            [NexusStringFieldAttribute("Destination 02")]
            public string Destination02 { get; set; } = "Destination 02";

            [NexusStringFieldAttribute("Destination 03")]
            public string Destination03 { get; set; } = "Destination 03";

            [NexusStringFieldAttribute("Destination 04")]
            public string Destination04 { get; set; } = "Destination 04";
        }

        //This example has all the attributes
        [NexusComponentSettings("Lighting Presets", typeof(NexusSystemDiagnostics), "AddSystemConfig")]
        public class LightingPresets : INexusSettings
        {
            public string Name { get; set; }

            [NexusStringFieldAttribute("Preset 1", DefaultValue = "Preset 1", Description = "Preset 1 Name", HelpText = "This is the help text")]
            public string Preset1 { get; set; } = "Preset 1";

            [NexusStringFieldAttribute("Preset 2", DefaultValue = "Preset 2", Description = "Preset 2 Name", HelpText = "This is the help text")]
            public string Preset2 { get; set; } = "Preset 2";

            [NexusStringFieldAttribute("Preset 3", DefaultValue = "Preset 3", Description = "Preset 3 Name", HelpText = "This is the help text")]
            public string Preset3 { get; set; } = "Preset 3";

            [NexusStringFieldAttribute("Preset 4", DefaultValue = "Preset 4", Description = "Preset 4 Name", HelpText = "This is the help text")]
            public string Preset4 { get; set; } = "Preset 4";

            [NexusStringFieldAttribute("Preset 5", DefaultValue = "Preset 5", Description = "Preset 5 Name", HelpText = "This is the help text")]
            public string Preset5 { get; set; } = "Preset 5";

            [NexusStringFieldAttribute("Preset Off", DefaultValue = "Off", Description = "Preset off Name", HelpText = "This is the help text")]
            public string Preset6 { get; set; } = "Off";

        }


    }
}

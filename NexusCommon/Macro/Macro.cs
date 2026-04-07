using Nexus.Framework.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting.Lifetime;
using System.Text;
using System.Threading.Tasks;
using static System.Collections.Specialized.BitVector32;

namespace NexusVtp
{
    public class Macro
    {
        private readonly Dictionary<string, List<Action>> actions = new Dictionary<string, List<Action>>();

        /// <summary>
        /// Initializes a new instance of the <see cref="Macro"/> class.
        /// </summary>
        public Macro()
        {

        }


        public void AddAction(string name, params Action[] actionsToAdd)
        {
            NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Debug, $"{MethodBase.GetCurrentMethod().Name} {name}");
            if (string.IsNullOrWhiteSpace(name))
            {
                NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Errors, $"{MethodBase.GetCurrentMethod().Name} Action name must not be empty");
            }

            if (actionsToAdd == null || actionsToAdd.Length == 0)
            {
                NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Errors, $"{MethodBase.GetCurrentMethod().Name} At least one action must be provided");
            }

            if (!actions.ContainsKey(name))
            {
                actions[name] = new List<Action>();
            }

            actions[name].AddRange(actionsToAdd);
        }

        public void RunAction(string name)
        {
            NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Debug, $"{MethodBase.GetCurrentMethod().Name} {name}");
            if (string.IsNullOrWhiteSpace(name))
            {
                NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Errors, $"{MethodBase.GetCurrentMethod().Name} Action name must not be empty");
            }

            if (!actions.TryGetValue(name, out var actionList))
            {
                NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Errors, $"{MethodBase.GetCurrentMethod().Name} Macro '{name}' not found");
            }

            foreach (var action in actionList)
            {
                action?.Invoke();
            }
        }
    }
}

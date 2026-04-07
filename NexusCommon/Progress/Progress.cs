using Crestron.SimplSharp;
using Nexus.Framework.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Timer = System.Timers.Timer;
using System.Reflection;

namespace NexusVtp
{
    public class Progress
    {
        private const ushort MaxValue = 65535;
        private readonly object sync = new object();
        private readonly Dictionary<string, uint> actions = new Dictionary<string, uint>();
        private readonly Dictionary<string, Dictionary<uint, string>> messages = new Dictionary<string, Dictionary<uint, string>>();
        private Timer stepTimer;
        private Timer countTimer;
        private ushort currentValue;
        private uint currentCount;
        private string currentActionName;
        private uint rampTimeSeconds = 5;
        private uint steps = 1000;
        private double increment;
        private bool isRunning;

        /// <summary>
        /// Initializes a new instance of the <see cref="Progress"/> class.
        /// </summary>
        public Progress()
        {
        }

        /// <summary>
        /// Gauge changed.
        /// </summary>
        public event EventHandler<ushort> GaugeChanged;

        /// <summary>
        /// Count changed.
        /// </summary>
        public event EventHandler<uint> CountChanged;

        /// <summary>
        /// Message changed.
        /// </summary>
        public event EventHandler<string> MessageChanged;

        /// <summary>
        /// Started.
        /// </summary>
        public event EventHandler Started;

        /// <summary>
        /// Stopped.
        /// </summary>
        public event EventHandler Stopped;

        public void AddAction(string name, uint time)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Errors, $"{MethodBase.GetCurrentMethod().Name} name must not be empty");
            }

            lock (this.sync)
            {
                actions[name] = time;
            }
        }

        public void AddMessage(string name, uint time, string message)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Errors, $"{MethodBase.GetCurrentMethod().Name} name must not be empty");
            }

            if (message == null)
            {
                NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Errors, $"{MethodBase.GetCurrentMethod().Name} message is null");
            }

            lock (this.sync)
            {
                if (!messages.ContainsKey(name))
                {
                    messages[name] = new Dictionary<uint, string>();
                }
                messages[name][time] = message;
            }
        }

        public void Dispose()
        {
            this.Stop();
            this.StopTimers();
        }

        private void CountTimerElapsed(object sender, ElapsedEventArgs e)
        {
            uint countValue;
            lock (this.sync)
            {
                if (!this.isRunning)
                {
                    return;
                }

                this.currentCount++;
                countValue = this.currentCount;
            }

            this.CountChanged?.Invoke(this, countValue);
            this.TriggerMessageIfDefined(countValue);
        }

        /// <summary>
        /// Start the ramp.
        /// </summary>
        public void RunAction(string name)
        {
            uint actionTime;
            lock (this.sync)
            {
                if (this.isRunning)
                {
                    return;
                }

                if (!actions.TryGetValue(name, out actionTime))
                {
                    NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Errors, $"{MethodBase.GetCurrentMethod().Name} '{name}' not found");
                }

                this.isRunning = true;

                this.currentValue = 0;
                this.currentCount = 0;
                this.currentActionName = name;

                this.rampTimeSeconds = actionTime;
                this.steps = 1000;
                this.increment = (double)MaxValue / this.steps;
            }

            this.Started?.Invoke(this, EventArgs.Empty);

            // initial notifications
            this.CountChanged?.Invoke(this, 0U);
            this.TriggerMessageIfDefined(0);

            // count timer (1 second)
            this.countTimer = new Timer(1000) { AutoReset = true };
            this.countTimer.Elapsed += this.CountTimerElapsed;
            this.countTimer.Start();

            // step timer: compute interval from rampTimeSeconds
            double stepDurationMs;
            lock (this.sync)
            {
                stepDurationMs = (this.rampTimeSeconds * 1000.0) / this.steps;
            }

            var interval = Math.Max(1.0, stepDurationMs);
            this.stepTimer = new Timer(interval) { AutoReset = true };
            this.stepTimer.Elapsed += this.StepTimerElapsed;
            this.stepTimer.Start();
        }

        private void TriggerMessageIfDefined(uint count)
        {
            lock (this.sync)
            {
                if (string.IsNullOrEmpty(this.currentActionName))
                {
                    return;
                }

                if (messages.TryGetValue(this.currentActionName, out var actionMessages))
                {
                    if (actionMessages.TryGetValue(count, out var message))
                    {
                        this.MessageChanged?.Invoke(this, message);
                    }
                }
            }
        }

        private void StepTimerElapsed(object sender, ElapsedEventArgs e)
        {
            ushort toSend;
            var finished = false;

            lock (this.sync)
            {
                if (!this.isRunning)
                {
                    return;
                }

                double next = this.currentValue + this.increment;
                if (next >= MaxValue)
                {
                    this.currentValue = MaxValue;
                    finished = true;
                }
                else
                {
                    this.currentValue = (ushort)next;
                }

                toSend = this.currentValue;
            }

            this.GaugeChanged?.Invoke(this, toSend);

            if (finished)
            {
                this.Stop();
            }
        }

        /// <summary>
        /// Stop the ramp.
        /// </summary>
        public void Stop()
        {
            lock (this.sync)
            {
                if (!this.isRunning)
                {
                    return;
                }

                this.isRunning = false;
            }

            this.StopTimers();

            lock (this.sync)
            {
                this.currentValue = MaxValue;
            }

            this.GaugeChanged?.Invoke(this, MaxValue);
            this.Stopped?.Invoke(this, EventArgs.Empty);
        }

        private void StopTimers()
        {
            try
            {
                if (this.stepTimer != null)
                {
                    this.stepTimer.Stop();
                    this.stepTimer.Elapsed -= this.StepTimerElapsed;
                    this.stepTimer.Dispose();
                    this.stepTimer = null;
                }
            }
            catch
            {
            }

            try
            {
                if (this.countTimer != null)
                {
                    this.countTimer.Stop();
                    this.countTimer.Elapsed -= this.CountTimerElapsed;
                    this.countTimer.Dispose();
                    this.countTimer = null;
                }
            }
            catch
            {
            }
        }
    }
}

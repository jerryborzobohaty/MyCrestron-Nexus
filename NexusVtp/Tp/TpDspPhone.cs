using Crestron.SimplSharpPro;
using Forte.SSPro.UI.Helper.Library.UI;
using Nexus.Framework.Services;
using Nexus.Qsc.Qsys.Driver;
using Nexus.Utils;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Timers;
using static NexusVtp.SmartGraphics;

namespace NexusVtp
{
    /// <summary>
    /// Manages DSP phone control functionality for touchpanels 
    /// </summary>
    public class TpDspPhone
    {
        /// <summary>
        /// Time in seconds before delete action triggers during hold
        /// </summary>
        private const uint TimeDeleteHold = 1;

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
        /// Smart Object for keypad
        /// </summary>
        private ExtendedSmartObject _keypad;

        /// <summary>
        /// Flag to ensure DSP event subscriptions only occur once across all panel instances
        /// </summary>
        private static bool _subscribed = false;

        /// <summary>
        /// Timer for detecting delete button hold duration
        /// </summary>
        private Timer _timerDeleteHeld;

        /// <summary>
        /// Maps keypad button numbers to their character representations
        /// </summary>
        private static readonly Dictionary<uint, string> _mapKeypad = new Dictionary<uint, string>
        {
            [1] = "1",
            [2] = "2",
            [3] = "3",
            [4] = "4",
            [5] = "5",
            [6] = "6",
            [7] = "7",
            [8] = "8",
            [9] = "9",
            [10] = "0",
            [11] = "*",
            [12] = "#"
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="TpDspPhone"/> class.
        /// </summary>
        /// <param name="panel">The touchpanel interface wrapper</param>
        /// <param name="panelName">Description of the panel for debugging</param>
        /// <param name="dsp">The QSC Q-SYS DSP driver instance</param>
        public TpDspPhone(Panel panel, string panelName, QscQsysDriver dsp)
        {
            this._panel = panel;
            this._panelName = panelName;
            this._dsp = dsp;
            this.Initialize();
        }

        /// <summary>
        /// Button join numbers for phone call control
        /// </summary>
        enum Btn
        {
            /// <summary>Disconnect call button</summary>
            Disconnect = 800,
            /// <summary>Connect call button</summary>
            Connect = 801,
            /// <summary>Delete/backspace button</summary>
            Delete = 802,
        }

        /// <summary>
        /// Initializes button groups, smart objects, event handlers, and feedback mappings
        /// </summary>
        private void Initialize()
        {
            try
            {
                // timers
                this._timerDeleteHeld = new Timer(TimeDeleteHold * 1000);

                // timer event handlers
                this._timerDeleteHeld.Elapsed += OnTimerDeleteHeld;

                // buttons - joins
                var bgCallControls = _panel.AddButtonGroup("PhoneCallControls", (uint)Btn.Disconnect, (uint)Btn.Delete);
                bgCallControls.OnPanelButtonGroupChange += OnBgCallControls;

                // smart objects
                _keypad = _panel.AddSmartObject("PhoneKeypad", _panel.ThePanel.SmartObjects[(int)SgId.PhoneKeypad]);
                _keypad.OnSmartObjectSignalChange += OnKeypad;

            }
            catch (Exception err)
            {
                NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Errors, $"{_panelName} {MethodBase.GetCurrentMethod().Name}: {GetInnerErr.GetInnermostException(err)}");
                NexusServiceManager.System.Log(Nexus.Driver.Architecture.Enumerations.LoggingLevels.Exceptions, $"{_panelName} {MethodBase.GetCurrentMethod().Name}: {GetInnerErr.GetInnermostException(err)}");
            }
        }

        // event handlers - button objects
        /// <summary>
        /// Handles phone call control button events (disconnect, connect, delete)
        /// </summary>
        /// <param name="o">Event sender</param>
        /// <param name="ea">Button group event arguments containing signal information</param>
        private void OnBgCallControls(object o, ButtonGroupEventArgs ea)
        {
            int num = (int)ea.Sig.Number;
            NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Debug, $"{_panelName} {MethodBase.GetCurrentMethod().Name} {num}");
            // need a press and hold for delete, so have to check boolean on some items
            switch ((Btn)num)
            {
                case Btn.Disconnect:
                    if (ea.Sig.BoolValue)
                    {
                        NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Debug, $"{_panelName} {MethodBase.GetCurrentMethod().Name} Disconnect");
                    }
                    break;
                case Btn.Connect:
                    if (ea.Sig.BoolValue)
                    {
                        NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Debug, $"{_panelName} {MethodBase.GetCurrentMethod().Name} Connect");
                    }
                    break;
                case Btn.Delete:
                    if (ea.Sig.BoolValue)
                    {
                        NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Debug, $"{_panelName} {MethodBase.GetCurrentMethod().Name} Delete");
                        this._timerDeleteHeld.Start();
                    }
                    else
                    {
                        this._timerDeleteHeld.Stop();
                    }
                    break;
            }
        }

        /// <summary>
        /// Handles keypad button presses and maps them to character input
        /// </summary>
        /// <param name="o">Event sender</param>
        /// <param name="ea">Smart Object event arguments containing signal information</param>
        private void OnKeypad(object o, SmartObjectEventArgs ea)
        {
            NexusDebugSmartObjectEvent(o, ea);
            if (ea.Sig.BoolValue)
            {
                var num = ea.Sig.Number;
                if (_mapKeypad.TryGetValue(num, out string character))
                {
                    NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Debug,
                        $"{_panelName} {MethodBase.GetCurrentMethod().Name} num={num} char={character}");

                    // TODO: Send character to DSP dialer
                    // _dsp.GetDialer("PhoneCall")?.DialDigit(character);
                }
                else
                {
                    NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Warning,
                        $"{_panelName} {MethodBase.GetCurrentMethod().Name} Unmapped keypad button {num}");
                }
            }
        }

        /// <summary>
        /// Handles delete button hold timer elapsed event
        /// </summary>
        /// <param name="o">Event sender (Timer object)</param>
        /// <param name="e">Elapsed event arguments</param>
        private void OnTimerDeleteHeld(object o, ElapsedEventArgs e)
        {
            NexusServiceManager.System.Debug(Nexus.Driver.Architecture.Enumerations.DebuggingLevels.Debug, $"{_panelName} {MethodBase.GetCurrentMethod().Name} Delete held");
        }
    }
}

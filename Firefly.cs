using ASCOM;
using ASCOM.Utilities;
using ASCOM.photonSeletek.Dome;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Policy;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Net.Mime.MediaTypeNames;

namespace ASCOM.LocalServer
{
    public class Firefly : IDisposable
    {
        public enum State { Open, Opening, Closing, Closed, Unknown }
        public static State currentState = State.Unknown;
        public static bool isSlewing = false;
        public static bool abort = false;
        private Thread statusThread;
        internal static FireflyEXP.Help firefly = null;
        internal static TraceLogger tl;

        internal const string identifier = "Seletek.Firefly.ROR";
        internal static int seletekRelayNo = 1;
        internal static int sensorOpenId = 2;
        internal static int sensorClosedId = 1;
        internal static int relayPauseMs = 1000;       //Delay between multiple relay activations in milliseconds
        internal static int sensorPollingMs = 100;       //Delay between sensor checks in milliseconds
        internal static int noMotionTimeout = 10;      //Timeout for sensor checks in seconds
        internal static int totalTimeout = 300;      //Total timeout for roof movement in seconds  
        internal static int timeoutCalibration = 30; //Timeout for the roof to change to state

        internal static DateTime lastRetryTime;

        public static ControlForm UserForm;

        /// <summary>
        /// Constructor
        /// </summary>
        public Firefly(TraceLogger tlM, int SensorPolling, int RelayPause, int TotalTimeout, int NoMotionTimeout, int RelayNo, int SensorOpen, int SensorClosed, int TimeoutCalibration)
        {
            firefly = new FireflyEXP.Help();
            tl = tlM;
            tl.LogMessageCrLf(identifier, "Seletek Firefly ROR started");

            relayPauseMs = RelayPause;
            sensorPollingMs = SensorPolling;
            totalTimeout = TotalTimeout;
            noMotionTimeout = NoMotionTimeout;
            seletekRelayNo = RelayNo;
            sensorOpenId = SensorOpen;
            sensorClosedId = SensorClosed;
            timeoutCalibration = TimeoutCalibration;

            tl.LogMessageCrLf(identifier, "Starting UI");
            //timoutRoofCycleCompletion = TotalTimeout;

            UserForm = new ControlForm();

            // Start a new thread to show the status window
            statusThread = new Thread(() =>
            {
                System.Windows.Forms.Application.Run(UserForm);
            });
            statusThread.Start();

            UserForm.SetText($"Sensors: open={isRoofOpen}, closed={isRoofClosed}");

            SetStateFromSensor();
            UserForm.UpdateStatus(GetStateString());
        }

        /// <summary>
        /// Destructor
        /// </summary>
        public void Dispose()
        {
            CloseStatusWindow();
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Close the status window
        /// </summary>
        public void CloseStatusWindow()
        {
            if (UserForm != null && !UserForm.IsDisposed)
            {
                UserForm.Invoke(new Action(() => { UserForm.Close(); }));
            }
        }

        /// <summary>
        /// Transition the state of the roof
        /// </summary>
        private static void TransitNextState()
        {
            switch (currentState)
            {
                case State.Open: currentState = State.Closing; break;
                case State.Closing: currentState = State.Closed; break;
                case State.Closed: currentState = State.Opening; break;
                case State.Opening: currentState = State.Open; break;
            }
        }

        public static State GetState()
        {
            return currentState;
        }

        public static string GetStateString()
        {
            return currentState.ToString();
        }

        public static void UpdateStatusUI()
        {
            UserForm.UpdateStatus(GetStateString());
        }

        
        /// <summary>
        /// Set the state of the roof from the sensor
        /// </summary>
        public static void SetStateFromSensor()
        {
            if (isRoofOpen && !isRoofClosed) { currentState = State.Open; }
            if (!isRoofOpen && isRoofClosed) { currentState = State.Closed; }
            Thread.Sleep(sensorPollingMs);
        }

        /// <summary>
        /// Check if the roof is open
        /// </summary>
        public static bool isRoofOpen
        {
            get
            {
                return (!firefly.SensorDigRead[sensorOpenId]);
            }
        }

        /// <summary>
        /// Check if the roof is closed
        /// </summary>
        public static bool isRoofClosed
        {
            get
            {
                return (!firefly.SensorDigRead[sensorClosedId]);
            }
        }


        /// <summary>
        /// Activate the relay
        /// </summary>
        public static void ActivateRelay(bool reverse=false)
        {

            UserForm.SetText("Activating relay");
            firefly.RelayChange(seletekRelayNo);
            Thread.Sleep(relayPauseMs);
            if (reverse)
            {
                Thread.Sleep(relayPauseMs * 2);
                UserForm.SetText("Reversing direction (1)");
                firefly.RelayChange(seletekRelayNo);
                Thread.Sleep(relayPauseMs*5);
                UserForm.SetText("Reversing direction (2)");
                firefly.RelayChange(seletekRelayNo);
                Thread.Sleep(relayPauseMs);
            }
        }


        /// <summary>
        /// Stop any movement
        /// </summary>
        public static void Stop()
        {
            ActivateRelay();
            abort = true;
            currentState = State.Unknown;
            UserForm.SetText("Roof movement aborted");
        }


       /// <summary>
       /// Check if a timeout has been reached
       /// </summary>
       /// <param name="startTime"></param>
       /// <param name="timeoutSeconds"></param>
       /// <param name="message"></param>
       /// <param name="throwException"></param>
       /// <returns></returns>
       /// <exception cref="DriverException"></exception>
        private static bool CheckTimeout(DateTime startTime, double timeoutSeconds, string message, bool throwException=false)
        {
            if (DateTime.Now.Subtract(startTime).TotalSeconds > timeoutSeconds)
            {
                UserForm.SetText(message);
          
                if (throwException)
                {
                    throw new DriverException(message);
                }
                return true; // Timeout reached
            }
            return false; // Timeout not reached
        }
        

        public static void CheckRoof(DateTime startTime, int timeout, bool reverse)
        {
            SetStateFromSensor();
            CheckTimeout(startTime, totalTimeout, "Roof is not moving, Timeout reached", true);

            if (CheckTimeout(lastRetryTime, timeout, "Retrying to move roof."))
            {
                bool _reverse = false;
                if (currentState == State.Opening) _reverse = true;
                ActivateRelay( _reverse);
                lastRetryTime = DateTime.Now;
            }
           
        }

        /// <summary>
        /// Open or close the roof
        /// </summary>
        /// <param name="newState"></param>
        public static void MoveRoof(State newState)
        {

            if (currentState == newState)
            {
                UserForm.SetText($"Roof is already {GetStateString()}");
                return;
            }
            UserForm.SetText($"Moving roof from {GetStateString()} to {newState}", true);
            DateTime startTime = DateTime.Now;
            lastRetryTime = startTime;

            ActivateRelay();
            isSlewing = true;

            UserForm.SetText("Waiting for intial movement");

            abort = false;

            // First segment - Check for initial movement
            while (((newState == State.Open && isRoofClosed) || (newState == State.Closed && isRoofOpen)) && !abort)
            {
                CheckRoof(startTime, noMotionTimeout, reverse: false);
            }

            if (abort) return;

            // Roof is now moving
            TransitNextState();
            UpdateStatusUI();

            // Second segment - Wait for completion
            UserForm.SetText($"Waiting for roof to {newState}");
            lastRetryTime = DateTime.Now;
            while (newState != currentState && !abort)
            {
                CheckRoof(startTime, timeoutCalibration, reverse: true);
            }
                        
            isSlewing = false;

            SetStateFromSensor();
            UserForm.SetText($"Roof is now {GetStateString()}");
            UpdateStatusUI();
        }

        /// <summary>
        /// Open the roof
        /// </summary>
        public static void Open()
        {
            MoveRoof(State.Open);
        }

        /// <summary>
        /// Close the roof
        /// </summary>
        public static void Close()
        {
            MoveRoof(State.Closed);
        }
    }
}
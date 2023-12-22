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
        internal static int timoutRoofCycleCompletion = 30; //Timeout for the roof to change to state

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
            timoutRoofCycleCompletion = TimeoutCalibration;

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
        public static void ActivateRelay()
        {

            UserForm.SetText("Activating relay");
            firefly.RelayChange(seletekRelayNo);
            Thread.Sleep(relayPauseMs);
        }


        /// <summary>
        /// Stop any movement
        /// </summary>
        public static void Stop()
        {
            ActivateRelay();
            abort = true;
            currentState = State.Unknown;
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
            var Elapsed = DateTime.Now.Subtract(startTime).TotalSeconds;
          

            if (Elapsed > timeoutSeconds)
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
        
        private static void SetElapsedUI(DateTime startTime)
        {
            var Elapsed = DateTime.Now.Subtract(startTime).TotalSeconds;
            
            string el = $"{Elapsed / 60:00}:{Elapsed % 60:00.0}";
            UserForm.SetElapsed($"Elapsed: {el}");            
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

            DateTime startTime = DateTime.Now;
            DateTime lastRetryTime = startTime;

            int retries = 0;
            ActivateRelay();
            isSlewing = true;

            UserForm.SetText("Checking intial movement");

            abort = false;

            // First segment - Check for initial movement
            while ((newState == State.Open && isRoofClosed) ||
                   (newState == State.Closed && isRoofOpen))
            {
                if (abort) break;

                CheckTimeout(startTime, totalTimeout, "Roof is not moving, Timout reached", true);
                
                SetElapsedUI(startTime);

                if (CheckTimeout(lastRetryTime, noMotionTimeout, "Roof did not move"))
                {
                    ActivateRelay();
                    lastRetryTime = DateTime.Now;
                }
                Thread.Sleep(sensorPollingMs);
            }

            if (abort) return;

            // Roof is now moving
            TransitNextState();
            UpdateStatusUI();

            // Second segment - Wait for completion
            UserForm.SetText("Waiting for roof...");
            lastRetryTime = DateTime.Now;
            while (newState != currentState)
            {
                if (abort) break;

                SetStateFromSensor();               
                SetElapsedUI(startTime);
                CheckTimeout(startTime, totalTimeout, "Roof is not moving, Timeout reached", true);

                if (CheckTimeout(lastRetryTime, timoutRoofCycleCompletion, $"Retrying to move roof. Retry #{retries}"))
                {
                    ActivateRelay();
                    ActivateRelay();
                    retries++;
                    lastRetryTime = DateTime.Now;
                }
                Thread.Sleep(sensorPollingMs);
            }
                        
            isSlewing = false;

            SetStateFromSensor();
            UpdateStatusUI();
        }

        /// <summary>
        /// Open the roof
        /// </summary>
        public static void Open()
        {
            UserForm.SetText("Opening roof");

            MoveRoof(State.Open);
        }

        /// <summary>
        /// Close the roof
        /// </summary>
        public static void Close()
        {
            UserForm.SetText("Closing roof");

            MoveRoof(State.Closed);
        }
    }
}
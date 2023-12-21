using ASCOM;
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
        internal const int seletekRelayNo = 1;
        internal const int seletekSensorRoofOpen = 2;
        internal const int seletekSensorRoofClosed = 1;
        internal const int delayRelay = 1000;       //Delay between multiple relay activations in milliseconds
        internal const int delaySensor = 100;       //Delay between sensor checks in milliseconds
        internal const int timoutSensor = 10;      //Timeout for sensor checks in seconds
        internal const int timoutTotal = 300;      //Total timeout for roof movement in seconds
        internal const int timoutRoofCycleCompletion = 30; //Timeout for the roof to change to state

        public static ControlForm UserForm;

        /// <summary>
        /// Constructor
        /// </summary>
        public Firefly()
        {
            firefly = new FireflyEXP.Help();
            UserForm = new ControlForm();

            // Start a new thread to show the status window
            statusThread = new Thread(() =>
            {
                System.Windows.Forms.Application.Run(UserForm);
            });
            statusThread.Start();

            UserForm.SetText($"Sensors: open={isRoofOpen}, closed={isRoofClosed}");

            SetStateFromSensor();
            UserForm.SetStatus(GetStateString());
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

        public static void UpdateStateUI()
        {
            UserForm.SetStatus(GetStateString());
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
                return (!firefly.SensorDigRead[seletekSensorRoofOpen]);
            }
        }

        /// <summary>
        /// Check if the roof is closed
        /// </summary>
        public static bool isRoofClosed
        {
            get
            {
                return (!firefly.SensorDigRead[seletekSensorRoofClosed]);
            }
        }


        /// <summary>
        /// Activate the relay
        /// </summary>
        public static void ActivateRelay()
        {

            UserForm.SetText("Activating relay");
            firefly.RelayChange(seletekRelayNo);
            Thread.Sleep(delayRelay);
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
        /// <returns></returns>
        private static bool CheckTimeout(DateTime startTime, double timeoutSeconds, string message)
        {
            if (DateTime.Now.Subtract(startTime).TotalSeconds > timeoutSeconds)
            {
                UserForm.SetText(message);
          
                return true; // Timeout reached
            }
            return false; // Timeout not reached
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

            UserForm.SetText("Checking movement");

            abort = false;

            // First segment - Check for initial movement
            while ((newState == State.Open && isRoofClosed) ||
                   (newState == State.Closed && isRoofOpen))
            {
                if (abort) break;

                if (CheckTimeout(startTime, timoutTotal, "Roof is not moving, Timout reached"))
                {
                    throw new DriverException("Roof is not moving, Timout reached");
                }

                if (CheckTimeout(lastRetryTime, timoutSensor, "Roof is not moving"))
                {
                    ActivateRelay();
                    lastRetryTime = DateTime.Now;
                }
                Thread.Sleep(delaySensor);
            }

            // Roof is now moving
            TransitNextState();
            UpdateStateUI();

            // Second segment - Wait for completion
            lastRetryTime = DateTime.Now;
            while (newState != currentState)
            {
                SetStateFromSensor();
                if (abort) break;

                if (CheckTimeout(startTime, timoutTotal, "Roof is not moving, Timeout reached"))
                {
                    throw new DriverException("Roof is not moving, Timout reached");
                }

                if (CheckTimeout(lastRetryTime, timoutRoofCycleCompletion, $"Retrying to move roof. Retry #{retries}"))
                {
                    ActivateRelay();
                    ActivateRelay();
                    retries++;
                    lastRetryTime = DateTime.Now;
                }
                Thread.Sleep(delaySensor);
            }
            UpdateStateUI();

            abort = false;
            isSlewing = false;
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
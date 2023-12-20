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

        public static ControlForm UserForm;

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


            //Console.WriteLine($"Sensors: open = {isRoofOpen} close = {isRoofClosed}");
            UserForm.SetText($"Sensors: open = {isRoofOpen}, close = {isRoofClosed}");


            SetStateFromSensor();
            UserForm.SetStatus(GetState());
        }

        public void CloseStatusWindow()
        {
            if (UserForm != null && !UserForm.IsDisposed)
            {
                UserForm.Invoke(new Action(() => { UserForm.Close(); }));
            }
        }


     

        public void Dispose()
        {
            // Dispose of any unmanaged resources here
            // Implement cleanup logic

            // If you have any disposable fields, you can call their Dispose() methods too

            // Example:
            // if (myDisposableField != null)
            // {
            //     myDisposableField.Dispose();
            // }

            CloseStatusWindow();
            // Suppress finalization (if you have a finalizer/destructor)
            GC.SuppressFinalize(this);
        }

        private static void StateTransition()
        {
            switch (currentState)
            {
                case State.Open: currentState = State.Closing; break;
                case State.Closing: currentState = State.Closed; break;
                case State.Closed: currentState = State.Opening; break;
                case State.Opening: currentState = State.Open; break;
            }
        }

        public static State GetFFState()
        {
            return currentState;
        }

        public static string GetState()
        {
            return currentState.ToString();
        }

        public static void PrintState()
        {
            //Console.WriteLine($"Roof state is {GetState()}");
            UserForm.SetStatus(GetState());
        }

        public static void SetStateFromSensor()
        {
            if (isRoofOpen && !isRoofClosed) { currentState = State.Open; }
            if (!isRoofOpen && isRoofClosed) { currentState = State.Closed; }
        }

        public static bool isRoofOpen
        {
            get
            {
                return (!firefly.SensorDigRead[seletekSensorRoofOpen]);
            }
        }

        public static bool isRoofClosed
        {
            get
            {
                return (!firefly.SensorDigRead[seletekSensorRoofClosed]);
            }
        }

        

        public static void ActivateRelais()
        {
            
            UserForm.SetText("Activating relay");
            firefly.RelayChange(seletekRelayNo);
        }

        public static void Stop()
        {
            ActivateRelais();
            abort = true;
            currentState = State.Unknown;
        }



        public static void Toggle(State newState)
        {
            PrintState();

            DateTime startTime = DateTime.Now;
            int retries = 0;
            ActivateRelais();
            isSlewing = true;

            //checkMovementOpen();
            UserForm.SetText("Checking movement");

            abort = false;
         
                Thread.Sleep(1000);

                // if current sensor does not change state
                while ((newState == State.Open && isRoofClosed) ||
                       (newState == State.Closed && isRoofOpen))
                {
                if (abort) break;
                    if (DateTime.Now.Subtract(startTime).TotalSeconds > 10)
                    {
                    UserForm.SetText("Roof is not moving");
                        ActivateRelais();
                        Thread.Sleep(1000);
                        startTime = DateTime.Now;
                    }
                    Thread.Sleep(100);
                }

                StateTransition();
                PrintState();

                startTime = DateTime.Now;
                while (newState != currentState)
                {
                    SetStateFromSensor();
                if (abort) break;
                    if (DateTime.Now.Subtract(startTime).TotalSeconds > 30)
                    {
                    UserForm.SetText("Retrying to move roof");
                        ActivateRelais();
                        Thread.Sleep(1000);
                        ActivateRelais();
                        retries++;
                        startTime = DateTime.Now;
                    }
                    Thread.Sleep(1000);
                    Console.Write(".");
                }
                PrintState();
            
            abort = false;
            isSlewing = false;
        }

        public static void Open()
        {
            UserForm.SetText("Opening roof");
            if (currentState == State.Open)
            {
                UserForm.SetText($"Roof is already open");
                return;
            }

            Toggle(State.Open);
        }

        public static void Close()
        {
            UserForm.SetText("Closing roof");

            if (currentState == State.Closed)
            {
                UserForm.SetText($"Roof is already closed");
                return;
            }

            Toggle(State.Closed);
        }
    }
}
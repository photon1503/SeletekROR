using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace ASCOM.LocalServer
{
    public class Firefly
    {

        public enum State { Open, Opening, Closing, Closed, Unknown }
        static State currentState = State.Unknown;

        internal static FireflyEXP.Help firefly = null;

        internal const int seletekRelayNo = 1;
        internal const int seletekSensorRoofOpen = 2;
        internal const int seletekSensorRoofClosed = 1;

        public Firefly()
        {
            firefly = new FireflyEXP.Help();

            Console.WriteLine($"Sensors: open = {isRoofOpen} close = {isRoofClosed}");


            SetStateFromSensor();
        }

        
        ~Firefly()
        {
            firefly = null;
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

        public static string GetState()
        {
            return currentState.ToString();
        }

        public static void PrintState()
        {
            Console.WriteLine($"Roof state is {GetState()}");
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
            Console.WriteLine("Activating relais");
            firefly.RelayChange(seletekRelayNo);
        }

        public static void Stop()
        {
            ActivateRelais();
            currentState = State.Unknown;
        }



        public static void Toggle(State newState)
        {
            PrintState();

            DateTime startTime = DateTime.Now;
            int retries = 0;
            ActivateRelais();

            //checkMovementOpen();
            Console.WriteLine("Checking movement");

            Thread.Sleep(1000);

            // if current sensor does not change state
            while ((newState == State.Open && isRoofClosed) ||
                   (newState == State.Closed && isRoofOpen))
            {
                if (DateTime.Now.Subtract(startTime).TotalSeconds > 10)
                {
                    Console.WriteLine("Roof is not moving");
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

                if (DateTime.Now.Subtract(startTime).TotalSeconds > 30)
                {
                    Console.WriteLine("Retrying to move roof");
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
        }

        public static void Open()
        {
            Console.WriteLine("Opening roof");
            if (currentState == State.Open)
            {
                Console.WriteLine($"Roof is already open");
                return;
            }

            Toggle(State.Open);
        }

        public static void Close()
        {
            Console.WriteLine("Closing roof");

            if (currentState == State.Closed)
            {
                Console.WriteLine($"Roof is already closed");
                return;
            }

            Toggle(State.Closed);
        }
    }
}
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASCOM.LocalServer.Server
{
    internal class Firefly
    {
        internal static FireflyEXP.Help firefly;

        internal const int seletekRelayNo = 1;
        internal const int seletekSensorRoofOpen = 2;
        internal const int seletekSensorRoofClosed = 1;

        internal Firefly()
        {
            firefly = new FireflyEXP.Help();
        }

        private static bool isRoofOpen
        {
            get
            {
                return (!firefly.SensorDigRead[seletekSensorRoofOpen]);
            }
        }

        private static bool isRoofClosed
        {
            get
            {
                return (!firefly.SensorDigRead[seletekSensorRoofClosed]);
            }
        }

        private static void ActivateRelais()
        {
            Console.WriteLine("Activating relais");                
            firefly.RelayChange(seletekRelayNo);
        }

        public static void checkMovementOpen()
        {
            console.WriteLine("Checking movement open");
            DateTime startTime = DateTime.Now;

            while (isRoofClosed){
                if (DateTime.Now.Subtract(startTime).TotalSeconds > 10) {
                    Console.WriteLine("Roof is not opening");
                    ActivateRelais();
                }
            }
            console.WriteLine("Roof is opening");
        }

     public static void checkMovementClose()
        {
            console.WriteLine("Checking movement closing");
            DateTime startTime = DateTime.Now;

            while (isRoofOpen){
                if (DateTime.Now.Subtract(startTime).TotalSeconds > 10) {
                    Console.WriteLine("Roof is not closing");
                    ActivateRelais();
                }
            }
            console.WriteLine("Roof is closing");
        }

        public static void Stop()
        {
            ActivateRelais();
        }

        public static void Retry()
        {
            Console.WriteLine("Retrying to move roof");
            ActivateRelais();         
            Sleep(1000);
            ActivateRelais();         
        }

        public static void Open()
        {
            Console.WriteLine("Opening roof");
            if (isRoofOpen) {
                Console.WriteLine("Roof is already open");
                return;
            }

            DateTime startTime = DateTime.Now;
            int retries = 0;
            while (!isRoofOpen && retries < 12)
            {                    
                ActivateRelais();
                checkMovementOpen();                 

                if (DateTime.Now.Subtract(startTime).TotalSeconds > 30) {
                    Console.WriteLine("Roof is not opening");   
                    Retry();   
                    retries++;           
                     startTime = DateTime.Now;
                }
                Sleep(1000);
            }
            Console.WriteLine("Roof is open");                
        }

        public static void Close()
        {
            Console.WriteLine("Closing roof");
            if (isRoofClosed) {
                Console.WriteLine("Roof is already closed");
                return;
            }

            DateTime startTime = DateTime.Now;
            int retries = 0;
            while (!isRoofClosed && retries < 12)
            {                    
                ActivateRelais();
                checkMovementClose();                 

                if (DateTime.Now.Subtract(startTime).TotalSeconds > 30) {
                    Console.WriteLine("Roof is not closing");   
                    Retry();   
                    retries++;      
                     startTime = DateTime.Now;     
                }
                Sleep(1000);
            }
            console.WriteLine("Roof is closed");
        }
    }

    //main
    class Program
    {
        static void Main(string[] args)
        {
            Firefly.Open();
            Firefly.Close();
        }
    }
}

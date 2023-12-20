using System;
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
            firefly.RelayChange(seletekRelayNo);
        }

        public static void Open()
        {
            if (isRoofOpen) {
                return;
            }

            while (true)
            {
                ActivateRelais();
                // check if isRoofClosed Sensor becomes false within a few seconds

            }
        }
    }
}

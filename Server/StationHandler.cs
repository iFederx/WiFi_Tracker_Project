using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Text;
using System.Threading.Tasks;

namespace Panopticon
{
    class StationHandler
    {
        internal Socket socket;
        internal bool isBlinking = false;
        internal bool isOn = true;
        internal StationHandler(Socket socket)
        {
            this.socket = socket;
        }

        public bool SetSocket(Socket socket)
        {
            this.socket = socket;
            return true;
        }

        public Socket GetSocket()
        {
            return this.socket;
        }
        //TODO_FEDE: this class is used to send commands to a specific board. It will then have probably a constructor reuqesting the IP address of the remote board
        internal void switchLedBlink(bool blink)
        {
            if (!isBlinking)
                Protocol.ESP_BlinkStart(socket);
            else
                Protocol.ESP_BlinkStop(socket);
        }

        internal void reboot()
        {
            Protocol.ESP_Reboot(socket);
        }

        internal void shutdown()
        {
            if (!isOn)
                Protocol.ESP_StandBy(socket);
            else
                Protocol.ESP_Resume(socket);
        }
    }
}

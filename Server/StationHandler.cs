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
		internal string macAddress = null;
        internal bool isBlinking = false;
        internal bool isOn = true;
        internal StationHandler(Socket _socket, string _macAddress)
        {
            socket = _socket;
			macAddress = _macAddress;
        }

        public bool SetSocket(Socket _socket)
        {
            socket = _socket;
            return true;
        }

        public Socket GetSocket()
        {
            return socket;
        }
        //TODO_FEDE: this class is used to send commands to a specific board. It will then have probably a constructor reuqesting the IP address of the remote board
        internal void switchLedBlink(bool blink)
        {
			if (blink == false)
			{
				Protocol.ESP_BlinkStop(socket);
				isBlinking = false;
			}
			else
			{
				Protocol.ESP_BlinkStart(socket);
				isBlinking = true;
			}    
        }

        internal void reboot()
        {
            Protocol.ESP_Reboot(socket);
        }

        internal void shutdown()
        {
			//TODO: da rivedere
            if (!isOn)
                Protocol.ESP_StandBy(socket);
            else
                Protocol.ESP_Resume(socket);
        }
    }
}

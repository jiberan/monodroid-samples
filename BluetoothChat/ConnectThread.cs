/*
* Copyright (C) 2009 The Android Open Source Project
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
* http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*/

using Android.Bluetooth;
using Android.Util;
using Java.Lang;

namespace com.xamarin.samples.bluetooth.bluetoothchat
{
    partial class BluetoothChatService
    {
        /// <summary>
        /// This thread runs while attempting to make an outgoing connection
        /// with a device. It runs straight through; the connection either
        /// succeeds or fails.
        /// </summary>
        protected class ConnectThread : Thread
        {
            BluetoothSocket socket;
            BluetoothDevice device;
            BluetoothChatService service;
            string socketType;

            public ConnectThread(BluetoothDevice device, BluetoothChatService service)
            {
                this.device = device;
                this.service = service;
                BluetoothSocket tmp = null;

                try
                {
                    tmp = device.CreateRfcommSocketToServiceRecord(MY_UUID_SECURE);
                }
                catch (Java.IO.IOException e)
                {
                    Log.Error(TAG, "create() failed", e);
                }
                socket = tmp;
                service.state = STATE_CONNECTING;
            }

            public override void Run()
            {
                Name = $"ConnectThread_{socketType}";

                // Always cancel discovery because it will slow down connection
                service.btAdapter.CancelDiscovery();

                // Make a connection to the BluetoothSocket
                try
                {
                    // This is a blocking call and will only return on a
                    // successful connection or an exception
                    socket.Connect();
                }
                catch (Java.IO.IOException e)
                {
                    // Close the socket
                    try
                    {
                        socket.Close();
                    }
                    catch (Java.IO.IOException e2)
                    {
                        Log.Error(TAG, $"unable to close() {socketType} socket during connection failure.", e2);
                    }

                    // Start the service over to restart listening mode
                    service.ConnectionFailed();
                    return;
                }

                // Reset the ConnectThread because we're done
                lock (this)
                {
                    service.connectThread = null;
                }

                // Start the connected thread
                service.Connected(socket, device, socketType);
            }

            public void Cancel()
            {
                try
                {
                    socket.Close();
                }
                catch (Java.IO.IOException e)
                {
                    Log.Error(TAG, "close() of connect socket failed", e);
                }
            }
        }
    }
}


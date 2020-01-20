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
        /// This thread runs while listening for incoming connections. It behaves
        /// like a server-side client. It runs until a connection is accepted
        /// (or until cancelled).
        /// </summary>
        class AcceptThread : Thread
        {
            // The local server socket
            BluetoothServerSocket serverSocket;
            string socketType;
            BluetoothChatService service;
            private BluetoothChatFragment _bluetoothChatFragment;

            public AcceptThread(BluetoothChatService service, BluetoothChatFragment bluetoothChatFragment)
            {
                _bluetoothChatFragment = bluetoothChatFragment;
                BluetoothServerSocket tmp = null;
                this.service = service;

                try
                {
                    tmp = service.btAdapter.ListenUsingRfcommWithServiceRecord(NAME_SECURE, MY_UUID_SECURE);
                }
                catch (Java.IO.IOException e)
                {
                    Log.Error(TAG, "listen() failed", e);
                }
                serverSocket = tmp;
                service.state = STATE_LISTEN;
            }

            public override void Run()
            {
                Name = $"AcceptThread_{socketType}";
                BluetoothSocket socket = null;

                while (service.GetState() != STATE_CONNECTED)
                {
                    try
                    {
                        socket = serverSocket.Accept();

                        if (socket.OutputStream.CanRead)
                        {
                            byte[] buffer = new byte[1024];
                            socket.OutputStream.Read(buffer, 0, buffer.Length);

                            _ = _bluetoothChatFragment.SendMessage(buffer[2], buffer[3], buffer[4]);
                        }

                    }
                    catch (Java.IO.IOException e)
                    {
                        Log.Error(TAG, "accept() failed", e);
                        break;
                    }

                    if (socket != null)
                    {
                        lock (this)
                        {
                            switch (service.GetState())
                            {
                                case STATE_LISTEN:
                                case STATE_CONNECTING:
                                    // Situation normal. Start the connected thread.
                                    service.Connected(socket, socket.RemoteDevice, socketType);
                                    break;
                                case STATE_NONE:
                                case STATE_CONNECTED:
                                    try
                                    {
                                        socket.Close();
                                    }
                                    catch (Java.IO.IOException e)
                                    {
                                        Log.Error(TAG, "Could not close unwanted socket", e);
                                    }
                                    break;
                            }
                        }
                    }
                }
            }

            public void Cancel()
            {
                try
                {
                    serverSocket.Close();
                }
                catch (Java.IO.IOException e)
                {
                    Log.Error(TAG, "close() of server failed", e);
                }
            }
        }
    }
}


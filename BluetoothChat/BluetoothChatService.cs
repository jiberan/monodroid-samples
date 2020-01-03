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

using System.Runtime.CompilerServices;
using Android.Bluetooth;
using Android.OS;
using Java.Util;

namespace com.xamarin.samples.bluetooth.bluetoothchat
{
    /// <summary>
    /// <para>This class does all the work for setting up and managing Bluetooth
    /// connections with other devices. It has a thread that listens for
    /// incoming connections, a thread for connecting with a device, and a
    /// thread for performing data transmissions when connected.</para>
    /// <para>Note that this isn't a real Android service class; this is
    /// a wrapper which manages the various threads used to connect, send, and
    /// receive messages via BT.</para>
    /// </summary>
    partial class BluetoothChatService
    {
        const string TAG = "BluetoothChatService";

        const string NAME_SECURE = "BluetoothChatSecure";

        static UUID MY_UUID_SECURE = UUID.FromString("00001101-0000-1000-8000-00805f9b34fb");


        BluetoothAdapter btAdapter;
        Handler handler;
        AcceptThread secureAcceptThread;      
        ConnectThread connectThread;
        ConnectedThread connectedThread;
        int state;
        int newState;
        private BluetoothChatFragment _bluetoothChatFragment;

        public const int STATE_NONE = 0;       // we're doing nothing
        public const int STATE_LISTEN = 1;     // now listening for incoming connections
        public const int STATE_CONNECTING = 2; // now initiating an outgoing connection
        public const int STATE_CONNECTED = 3;  // now connected to a remote device

        /// <summary>
        /// Constructor. Prepares a new BluetoothChat session.
        /// </summary>
        /// <param name='handler'>
        /// A Handler to send messages back to the UI Activity.
        /// </param>
        public BluetoothChatService(Handler handler, BluetoothChatFragment bluetoothChatFragment)
        {
            _bluetoothChatFragment = bluetoothChatFragment;
            btAdapter = BluetoothAdapter.DefaultAdapter;
            state = STATE_NONE;
            newState = state;
            this.handler = handler;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        void UpdateUserInterfaceTitle()
        {
            state = GetState();
            newState = state;
            handler.ObtainMessage(Constants.MESSAGE_STATE_CHANGE, newState, -1).SendToTarget();
        }

        /// <summary>
        /// Return the current connection state.
        /// </summary>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public int GetState()
        {
            return state;
        }

        // Start the chat service. Specifically start AcceptThread to begin a
        // session in listening (server) mode. Called by the Activity onResume()
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Start()
        {
            if (connectThread != null)
            {
                connectThread.Cancel();
                connectThread = null;
            }

            if (connectedThread != null)
            {
                connectedThread.Cancel();
                connectedThread = null;
            }

            if (secureAcceptThread == null)
            {
                secureAcceptThread = new AcceptThread(this, _bluetoothChatFragment);
                secureAcceptThread.Start();
            }
            UpdateUserInterfaceTitle();
        }



        /// <summary>
        /// Start the ConnectThread to initiate a connection to a remote device.
        /// </summary>
        /// <param name='device'>
        /// The BluetoothDevice to connect.
        /// </param>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Connect(BluetoothDevice device, bool secure)
        {
            if (state == STATE_CONNECTING)
            {
                if (connectThread != null)
                {
                    connectThread.Cancel();
                    connectThread = null;
                }
            }

            // Cancel any thread currently running a connection
            if (connectedThread != null)
            {
                connectedThread.Cancel();
                connectedThread = null;
            }

            // Start the thread to connect with the given device
            connectThread = new ConnectThread(device, this);
            connectThread.Start();

            UpdateUserInterfaceTitle();
        }

        /// <summary>
        /// Start the ConnectedThread to begin managing a Bluetooth connection
        /// </summary>
        /// <param name='socket'>
        /// The BluetoothSocket on which the connection was made.
        /// </param>
        /// <param name='device'>
        /// The BluetoothDevice that has been connected.
        /// </param>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Connected(BluetoothSocket socket, BluetoothDevice device, string socketType)
        {
            // Cancel the thread that completed the connection
            if (connectThread != null)
            {
                connectThread.Cancel();
                connectThread = null;
            }

            // Cancel any thread currently running a connection
            if (connectedThread != null)
            {
                connectedThread.Cancel();
                connectedThread = null;
            }
            if (secureAcceptThread != null)
            {
                secureAcceptThread.Cancel();
                secureAcceptThread = null;
            }
            // Start the thread to manage the connection and perform transmissions
            connectedThread = new ConnectedThread(socket, this, socketType);
            connectedThread.Start();

            // Send the name of the connected device back to the UI Activity
            var msg = handler.ObtainMessage(Constants.MESSAGE_DEVICE_NAME);
            Bundle bundle = new Bundle();
            bundle.PutString(Constants.DEVICE_NAME, device.Name);
            msg.Data = bundle;
            handler.SendMessage(msg);

            UpdateUserInterfaceTitle();
        }

        /// <summary>
        /// Stop all threads.
        /// </summary>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Stop()
        {
            if (connectThread != null)
            {
                connectThread.Cancel();
                connectThread = null;
            }

            if (connectedThread != null)
            {
                connectedThread.Cancel();
                connectedThread = null;
            }

            if (secureAcceptThread != null)
            {
                secureAcceptThread.Cancel();
                secureAcceptThread = null;
            }

            state = STATE_NONE;
            UpdateUserInterfaceTitle();
        }

        /// <summary>
        /// Write to the ConnectedThread in an unsynchronized manner
        /// </summary>
        /// <param name='out'>
        /// The bytes to write.
        /// </param>
        public void Write(byte[] @out)
        {
            // Create temporary object
            ConnectedThread r;
            // Synchronize a copy of the ConnectedThread
            lock (this)
            {
                if (state != STATE_CONNECTED)
                {
                    return;
                }
                r = connectedThread;
            }
            // Perform the write unsynchronized
            r.Write(@out);
        }

        /// <summary>
        /// Indicate that the connection attempt failed and notify the UI Activity.
        /// </summary>
        void ConnectionFailed()
        {
            state = STATE_LISTEN;

            var msg = handler.ObtainMessage(Constants.MESSAGE_TOAST);
            var bundle = new Bundle();
            bundle.PutString(Constants.TOAST, "Unable to connect device");
            msg.Data = bundle;
            handler.SendMessage(msg);
            Start();
        }

        /// <summary>
        /// Indicate that the connection was lost and notify the UI Activity.
        /// </summary>
        public void ConnectionLost()
        {
            var msg = handler.ObtainMessage(Constants.MESSAGE_TOAST);
            var bundle = new Bundle();
            bundle.PutString(Constants.TOAST, "Unable to connect device.");
            msg.Data = bundle;
            handler.SendMessage(msg);

            state = STATE_NONE;
            UpdateUserInterfaceTitle();
            this.Start();
        }
    }
}


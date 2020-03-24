﻿using System;
using System.Linq;
using System.Text;
using System.Threading;
using Android.OS;
using Android.Views;
using Android.Widget;

namespace com.xamarin.samples.bluetooth.bluetoothchat
{
    [System.Obsolete]
    public partial class BluetoothChatFragment
    {
        /// <summary>
        /// Handles messages that come back from the ChatService.
        /// </summary>
        class ChatHandler : Handler
        {
            BluetoothChatFragment chatFrag;
            SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1);
            public ChatHandler(BluetoothChatFragment frag)
            {
                chatFrag = frag;

            }
            public async override void HandleMessage(Message msg)
            {
                switch (msg.What)
                {
                    case Constants.MESSAGE_STATE_CHANGE:
                        switch (msg.What)
                        {
                            case BluetoothChatService.STATE_CONNECTED:
                                chatFrag.SetStatus(chatFrag.GetString(Resource.String.title_connected_to, chatFrag.connectedDeviceName));
                                chatFrag.conversationArrayAdapter.Clear();
                                break;
                            case BluetoothChatService.STATE_CONNECTING:
                                chatFrag.SetStatus(Resource.String.title_connecting);
                                break;
                            case BluetoothChatService.STATE_LISTEN:
                                chatFrag.SetStatus(Resource.String.not_connected);
                                break;
                            case BluetoothChatService.STATE_NONE:
                                chatFrag.SetStatus(Resource.String.not_connected);
                                break;
                        }
                        break;
                    case Constants.MESSAGE_WRITE:
                        var writeBuffer = (byte[])msg.Obj;
                        var writeMessage = Encoding.ASCII.GetString(writeBuffer);
                        break;
                    case Constants.MESSAGE_READ:
                        var readBuffer = (byte[])msg.Obj;
                        await _semaphoreSlim.WaitAsync();
                        ShowTextOutput(readBuffer);
                        await chatFrag.SendMessage(GetIdPaketu(readBuffer), GetIdTransakce(readBuffer), GetIdVeliciny(readBuffer));
                        _semaphoreSlim.Release();
                        break;
                    case Constants.MESSAGE_DEVICE_NAME:
                        chatFrag.connectedDeviceName = msg.Data.GetString(Constants.DEVICE_NAME);
                        if (chatFrag.Activity != null)
                        {
                            Toast.MakeText(chatFrag.Activity, $"Connected to {chatFrag.connectedDeviceName}.", ToastLength.Short).Show();
                        }
                        break;
                    case Constants.MESSAGE_TOAST:
                        break;
                }
            }

            private void ShowTextOutput(byte[] bytes)
            {
                foreach (var b in bytes)
                {
                    Console.Write(b+" ");
                }
                Console.WriteLine();
            }

            private byte GetIdTransakce(byte[] readBuffer)
            {
                return readBuffer[3];
            }

            private short GetIdVeliciny(byte[] readBuffer)
            {
                var res = readBuffer.Skip(4).Take(2).ToArray();

                if (BitConverter.IsLittleEndian)
                    Array.Reverse(res);

                return BitConverter.ToInt16(res);
            }

            private byte GetIdPaketu(byte[] readBuffer)
            {
                return readBuffer[2];
            }
        }
    }
}


using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using Android.Bluetooth;
using Android.Content;
using Android.Icu.Text;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Views.InputMethods;
using Android.Widget;
using Xamarin.Essentials;

namespace com.xamarin.samples.bluetooth.bluetoothchat
{
    public partial class BluetoothChatFragment : Fragment
    {
        const string TAG = "BluetoothChatFragment";

        const int REQUEST_CONNECT_DEVICE_SECURE = 1;
        const int REQUEST_CONNECT_DEVICE_INSECURE = 2;
        const int REQUEST_ENABLE_BT = 3;

        ListView conversationView;
        EditText outEditText;
        Button sendButton;

        String connectedDeviceName = "";
        ArrayAdapter<String> conversationArrayAdapter;
        StringBuilder outStringBuffer;
        BluetoothAdapter bluetoothAdapter = null;
        BluetoothChatService chatService = null;

        bool requestingPermissionsSecure, requestingPermissionsInsecure;

        DiscoverableModeReceiver receiver;
        ChatHandler handler;
        WriteListener writeListener;

        private Dictionary<int, int> stavDict = new Dictionary<int, int>();


        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetHasOptionsMenu(true);
            bluetoothAdapter = BluetoothAdapter.DefaultAdapter;


            receiver = new DiscoverableModeReceiver();
            receiver.BluetoothDiscoveryModeChanged += (sender, e) =>
            {
                Activity.InvalidateOptionsMenu();
            };

            if (bluetoothAdapter == null)
            {
                Toast.MakeText(Activity, "Bluetooth is not available.", ToastLength.Long).Show();
                Activity.FinishAndRemoveTask();
            }

            writeListener = new WriteListener(this);
            handler = new ChatHandler(this);

            stavDict.Add(0, 0);
            stavDict.Add(1, 8);
            stavDict.Add(2, 9);
            stavDict.Add(3, 10);
            stavDict.Add(4, 11);


        }

        public override void OnStart()
        {
            base.OnStart();
            if (!bluetoothAdapter.IsEnabled)
            {
                var enableIntent = new Intent(BluetoothAdapter.ActionRequestEnable);
                StartActivityForResult(enableIntent, REQUEST_ENABLE_BT);
            }
            else if (chatService == null)
            {
                chatService = new BluetoothChatService(handler, this);
            }

            // Register for when the scan mode changes
            var filter = new IntentFilter(BluetoothAdapter.ActionScanModeChanged);
            Activity.RegisterReceiver(receiver, filter);
        }

        public override void OnResume()
        {
            base.OnResume();
            if (chatService != null)
            {
                if (chatService.GetState() == BluetoothChatService.STATE_NONE)
                {
                    chatService.Start();
                }
            }
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            return inflater.Inflate(Resource.Layout.fragment_bluetooth_chat, container, false);
        }

        public override async void OnViewCreated(View view, Bundle savedInstanceState)
        {
            await GetGps();
            FillSpinner(view);
        }

        private void FillSpinner(View view)
        {

            Spinner spinner = view.FindViewById<Spinner>(Resource.Id.driverCard1);

            spinner.ItemSelected += new EventHandler<AdapterView.ItemSelectedEventArgs>(spinner1_ItemSelected);
            var adapter = ArrayAdapter.CreateFromResource(
                    view.Context, Resource.Array.driver_card_states_array, Android.Resource.Layout.SimpleSpinnerItem);

            adapter.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);
            spinner.Adapter = adapter;



            Spinner spinner2 = view.FindViewById<Spinner>(Resource.Id.driverCard2);

            spinner2.ItemSelected += new EventHandler<AdapterView.ItemSelectedEventArgs>(spinner2_ItemSelected);
            var adapter2 = ArrayAdapter.CreateFromResource(
                    view.Context, Resource.Array.driver_card_states_array, Android.Resource.Layout.SimpleSpinnerItem);

            adapter2.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);
            spinner2.Adapter = adapter2;

        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Android.Content.PM.Permission[] grantResults)
        {
            var allGranted = grantResults.AllPermissionsGranted();
            if (requestCode == PermissionUtils.RC_LOCATION_PERMISSIONS)
            {
                if (requestingPermissionsSecure)
                {
                    PairWithBlueToothDevice(true);
                }
                if (requestingPermissionsInsecure)
                {
                    PairWithBlueToothDevice(false);
                }

                requestingPermissionsSecure = false;
                requestingPermissionsInsecure = false;
            }
        }

        public override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            switch (requestCode)
            {
                case REQUEST_CONNECT_DEVICE_SECURE:
                    if (Result.Ok == resultCode)
                    {
                        ConnectDevice(data, true);
                    }
                    break;
                case REQUEST_ENABLE_BT:
                    if (Result.Ok == resultCode)
                    {
                        Toast.MakeText(Activity, Resource.String.bt_not_enabled_leaving, ToastLength.Short).Show();
                        Activity.FinishAndRemoveTask();
                    }
                    break;
            }
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Resource.Id.secure_connect_scan:
                    PairWithBlueToothDevice(true);
                    return true;
                case Resource.Id.discoverable:
                    EnsureDiscoverable();
                    return true;
            }
            return false;
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            Activity.UnregisterReceiver(receiver);
            if (chatService != null)
            {
                chatService.Stop();
            }
        }

        void PairWithBlueToothDevice(bool secure)
        {
            requestingPermissionsSecure = false;
            requestingPermissionsInsecure = false;

            // Bluetooth is automatically granted by Android. Location, OTOH,
            // is considered a "dangerous permission" and as such has to 
            // be explicitly granted by the user.
            if (!Activity.HasLocationPermissions())
            {
                requestingPermissionsSecure = secure;
                requestingPermissionsInsecure = !secure;
                this.RequestPermissionsForApp();
                return;
            }

            var intent = new Intent(Activity, typeof(DeviceListActivity));
            if (secure)
            {
                StartActivityForResult(intent, REQUEST_CONNECT_DEVICE_SECURE);
            }
            else
            {
                StartActivityForResult(intent, REQUEST_CONNECT_DEVICE_INSECURE);
            }
        }

        public async Task SendMessage(byte idPaketu, byte idTransakce, short idVeliciny)
        {
            chatService.Write(await GetMessage(idPaketu, idTransakce, idVeliciny));
        }

        public async Task<byte[]> GetMessage(byte idPaketu, byte idTransakce, int idVeliciny)
        {
            var data = new List<byte>();
            data.Add(179); //0xB3


            if (idPaketu == 3)
            { // dotaz na velicinu
                if (idVeliciny == 8)
                {
                    //tachograf

                    //karta1
                    data.Add(53); //delka paketu
                    data.Add(4); //id paketu
                    data.Add(idTransakce); //id transakce
                    data.Add((byte)0); //error
                    data.AddRange(GetBytes((ushort)8)); //id velicina

                    data.Add((GetVelicina(Resource.Id.driverCard1))); //datove pole (velicina)
                    data.AddRange(GetTextInfo(Resource.Id.driverCard1Id)); //datove pole (velicina)

                    //karta2
                    data.Add((GetVelicina(Resource.Id.driverCard2))); //datove pole (velicina)
                    data.AddRange(GetTextInfo(Resource.Id.driverCard2Id)); //datove pole (velicina)

                }
                else if (idVeliciny == 2)
                {
                    //datum cas
                    var datetime = DateTime.Now;

                    data.Add(15); //delka paketu
                    data.Add(4); //id paketu
                    data.Add(idTransakce); //id transakce
                    data.Add((byte)0); //error
                    data.AddRange(GetBytes((ushort)2)); //id velicina
                    
                    data.Add((byte)datetime.Day);
                    data.Add((byte)datetime.Month);
                    data.Add((byte)Convert.ToInt32(datetime.ToString("yy")));
                    data.Add((byte)datetime.Hour);
                    data.Add((byte)datetime.Minute);
                    data.Add((byte)datetime.Second);
                }

                else if (idVeliciny == 3)
                {
                    //GPS
                    data.Add(18); //delka paketu
                    data.Add(4); //id paketu
                    data.Add(idTransakce); //id transakce
                    data.Add((byte)0); //error
                    data.AddRange((GetBytes((ushort)3))); //id velicina

                    data.Add(160);  //status

                    var gps = await GetGps();

                    var x = (gps.Item1 * 60) / 0.0001;
                    var y = (gps.Item2 * 60) / 0.0001;

                    data.AddRange(GetBytes(Convert.ToInt32(x)));
                    data.AddRange(GetBytes(Convert.ToInt32(y)));
                    /* data.Add(1);  //platna pozice
                     data.Add(0);  //2D
                     data.Add(0);  //3D
                     data.Add(0);  //sirka
                     data.Add(0);  //delka
                     data.Add(0); //rezerva
                     data.Add(0); //rezerva


                     var gps = await GetGps();

                     var x = (gps.Item1 * 60) / 0.0001;
                     data.AddRange(BitConverter.GetBytes(Convert.ToInt32(x)));


                     var y = (gps.Item2 * 60) / 0.0001;
                     data.AddRange(BitConverter.GetBytes(Convert.ToInt32(y)));*/
                }

                else if (idVeliciny == 7)
                {
                    //tachometr
                    data.Add(13); //delka paketu
                    data.Add(4); //id paketu
                    data.Add(idTransakce); //id transakce
                    data.Add((byte)0); //error
                    data.AddRange((GetBytes((ushort)7))); //id velicina

                    var tach = new Random().Next(10000000, 90000000); //4byte long dle pdf? nevim jak
                    data.AddRange(GetBytes(tach));
                }
            }
            else if (idPaketu == 1)
            {
                //dotaz na identifikaci

                data.Add(27); //delka paketu
                data.Add(2); //id paketu
                data.Add(idTransakce); //id transakce
                //data.Add((byte)0); //id transakce

                var tach = new Random().Next(10000000, 90000000); //4byte long dle pdf? nevim jak
                data.AddRange(GetBytes(104935));

                data.Add((byte)1); //verze protokolu

                data.AddRange(/*new List<byte>(16)*/GetVerzeFW("test"));
            }

            var crc = GetCRC(data.ToArray());
            data.AddRange(GetBytes(crc)); //crc

            var bytes = data.ToArray();
            return bytes;
        }


        public static ushort GetCRC(byte[] buff)
        {
            ushort d = 62000;
            ushort crc;
            byte i, k;
            ushort pomW;
            crc = 0xFFFF;
            for (i = 0; i < buff.Length; i++)
            {
                int pom_i = buff[i] << 8;
                pomW = (ushort)(pom_i);
                for (k = 0; k < 8; k++)
                {
                    var val = (crc ^ pomW) & 0x8000;
                    if (val > 0)
                    {
                        int crci = (crc << 1) ^ 0x1021;
                        crc = (ushort)crci;
                    }
                    else
                        crc <<= 1;
                    pomW <<= 1;
                }
            }
            return (crc);
        }

        byte GetVelicina(int spinnerId)
        {
            Spinner spinner = View.FindViewById<Spinner>(spinnerId);

            var val = stavDict[spinner.SelectedItemPosition];
            return Convert.ToByte(val);
        }

        byte[] GetTextInfo(int txtId)
        {
            var txt = View.FindViewById<TextView>(txtId);
            byte[] bytes = Encoding.ASCII.GetBytes(txt.Text);
            List<byte> newBytes = new List<byte>();
            newBytes.AddRange(bytes);
            newBytes.AddRange(new byte[21 - bytes.Length]);
            return newBytes.ToArray();
        }
        byte[] GetVerzeFW(string text)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(text);
            List<byte> newBytes = new List<byte>();
            newBytes.AddRange(bytes);
            newBytes.AddRange(new byte[16 - bytes.Length]);
            return newBytes.ToArray();
        }

        private byte[] GetBytes(ushort val)
        {
            byte[] intBytes = BitConverter.GetBytes(val);
            Array.Reverse(intBytes);

            return intBytes;
        }
        private byte[] GetBytes(int val)
        {
            byte[] intBytes = BitConverter.GetBytes(val);
            Array.Reverse(intBytes);

            return intBytes;
        }
        private byte[] GetBytes(byte val)
        {
            byte[] intBytes = BitConverter.GetBytes(val);
            Array.Reverse(intBytes);

            return intBytes;
        }

        bool HasActionBar()
        {
            if (Activity == null)
            {
                return false;
            }
            if (Activity.ActionBar == null)
            {
                return false;
            }
            return true;
        }

        void SetStatus(int resId)
        {
            if (HasActionBar())
            {
                Activity.ActionBar.SetSubtitle(resId);
            }
        }

        void SetStatus(string subTitle)
        {
            if (HasActionBar())
            {
                Activity.ActionBar.Subtitle = subTitle;
            }
        }

        void ConnectDevice(Intent data, bool secure)
        {
            var address = data.Extras.GetString(DeviceListActivity.EXTRA_DEVICE_ADDRESS);
            var device = bluetoothAdapter.GetRemoteDevice(address);
            chatService.Connect(device, secure);
        }

        public override void OnPrepareOptionsMenu(IMenu menu)
        {
            var menuItem = menu.FindItem(Resource.Id.discoverable);
            if (menuItem != null)
            {
                menuItem.SetEnabled(bluetoothAdapter.ScanMode == ScanMode.ConnectableDiscoverable);
            }

        }
        public override void OnCreateOptionsMenu(IMenu menu, MenuInflater inflater)
        {
            inflater.Inflate(Resource.Menu.bluetooth_chat, menu);
        }

        /// <summary>
        /// Listen for return key being pressed.
        /// </summary>
        class WriteListener : Java.Lang.Object, TextView.IOnEditorActionListener
        {
            BluetoothChatFragment host;
            public WriteListener(BluetoothChatFragment frag)
            {
                host = frag;
            }
            public bool OnEditorAction(TextView v, [GeneratedEnum] ImeAction actionId, KeyEvent e)
            {
                if (actionId == ImeAction.ImeNull && e.Action == KeyEventActions.Up)
                {
                    //  host.SendMessage(v.Text);
                }
                return true;
            }
        }

        void EnsureDiscoverable()
        {
            if (bluetoothAdapter.ScanMode != ScanMode.ConnectableDiscoverable)
            {
                var discoverableIntent = new Intent(BluetoothAdapter.ActionRequestDiscoverable);
                discoverableIntent.PutExtra(BluetoothAdapter.ExtraDiscoverableDuration, 300);
                StartActivity(discoverableIntent);
            }
        }

        private void spinner1_ItemSelected(object sender, AdapterView.ItemSelectedEventArgs e)
        {
            Spinner spinner = (Spinner)sender;
            string toast = string.Format("Stav karty 1 je {0}", spinner.GetItemAtPosition(e.Position));
            Toast.MakeText(View.Context, toast, ToastLength.Long).Show();
        }

        private void spinner2_ItemSelected(object sender, AdapterView.ItemSelectedEventArgs e)
        {
            Spinner spinner = (Spinner)sender;
            string toast = string.Format("Stav karty 1 je {0}", spinner.GetItemAtPosition(e.Position));
            Toast.MakeText(View.Context, toast, ToastLength.Long).Show();
        }

        async Task<Tuple<double, double>> GetGps()
        {
            try
            {
                var request = new GeolocationRequest(GeolocationAccuracy.Lowest);
                var location = await Geolocation.GetLastKnownLocationAsync();
             
                if (location != null)
                {
                    Toast.MakeText(View.Context, $"Latitude: {location.Latitude}, Longitude: {location.Longitude}, Altitude: {location.Altitude}", ToastLength.Long).Show();

                    return new Tuple<double, double>(location.Latitude, location.Longitude);
                    //   Console.WriteLine($"Latitude: {location.Latitude}, Longitude: {location.Longitude}, Altitude: {location.Altitude}");
                }
            }
            catch (Exception ex)
            {
                return new Tuple<double, double>(0, 0);
            }

            return new Tuple<double, double>(0, 0);
        }
    }
}
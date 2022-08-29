using System;
using Android.App;
using Android.Bluetooth;
using Android.OS;
using Android.Runtime;
using Android.Views;
using AndroidX.AppCompat.App;
using Google.Android.Material.FloatingActionButton;
using System.Threading.Tasks;
using System.IO;
using Android.Widget;
using Toolbar = AndroidX.AppCompat.Widget.Toolbar;
using Android.Graphics;
using Android.Util;
using System.Linq;
using System.Collections.Generic;

namespace Momir_IRL
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        private const string ScryfallUrl = "https://api.scryfall.com/cards/random?q=type:creature+cmc:{0}";
        private ImageView imageView;
        private Spinner cmcDropdown;
        private BluetoothSocket socket;
        private TextView statusLabel;
        private volatile bool printing = false;
        private readonly object syncRoot = new object();

        protected async override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.activity_main);

            statusLabel = FindViewById<TextView>(Resource.Id.status_label);
            statusLabel.Text = "Connecting to Bluetooth...";
            await ConnectToBluetooth();

            var toolbar = FindViewById<Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);

            var cmcs = Enumerable.Range(1, 16).Where(i => Assets.List($"monochrome/{i}").Any());

            imageView = FindViewById<ImageView>(Resource.Id.card);
            cmcDropdown = FindViewById<Spinner>(Resource.Id.cmc);
            cmcDropdown.Adapter = new ArrayAdapter(this, Android.Resource.Layout.SimpleSpinnerDropDownItem, cmcs.ToArray());

            var fab = FindViewById<FloatingActionButton>(Resource.Id.fab);
            fab.Click += FabOnClick;

            statusLabel.Text = "Select CMC and hit send!";
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.menu_main, menu);
            return true;
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            var id = item.ItemId;
            if (id == Resource.Id.action_settings)
            {
                return true;
            }

            return base.OnOptionsItemSelected(item);
        }

        private async void FabOnClick(object sender, EventArgs eventArgs)
        {
            await PopulateImage();
        }

        private async Task PopulateImage(int? cmc = null)
        {
            var success = false;
            for (var i = 0; !success && i < 5; i++)
            {
                try
                {
                    statusLabel.Text = "Fetching card from Scryfall...";
                    var (bmp, monoBmp) = await GetImages(cmc ?? (int)cmcDropdown.SelectedItem);
                    imageView.SetImageBitmap(bmp);
                    imageView.Invalidate();

                    SendToPrinter(monoBmp).ConfigureAwait(false);
                    success = true;
                }
                catch (Exception e)
                {
                    Log.Error("Printer", e.ToString());
                }
            }
            if (!success)
            {
                Toast.MakeText(this, "Error getting image after 5 retries", ToastLength.Short);
            }
        }

        private async Task<(Bitmap, Bitmap)> GetImages(int cmc = 1)
        {
            var cards = await Assets.ListAsync($"original/{cmc}");
            var card = cards[new Random().Next(0, cards.Length - 1)];

            var origialStream = Assets.Open($"original/{cmc}/{card}");
            var monoStream = Assets.Open($"monochrome/{cmc}/{card}");

            var origialBmpTask = BitmapFactory.DecodeStreamAsync(origialStream);
            var monoBmpTask = BitmapFactory.DecodeStreamAsync(monoStream);
            await Task.WhenAll(origialBmpTask, monoBmpTask);
            return (await origialBmpTask, await monoBmpTask);
        }

        const int arduinoBufferSize = 384 * 34 / 8;
        private async Task SendToPrinter(Bitmap bmp)
        {
            lock (syncRoot)
            {
                if (printing)
                {
                    //return;
                }
                printing = true;
            }
            try
            {
                statusLabel.Text = "Sending to printer...";

                var byteArray = GetBytesFromImage(bmp);

                for (var i = 0; i < byteArray.Length / arduinoBufferSize; i++)
                {
                    var compressedBytes = GetCompressedChunk(byteArray, i);
                    SendChunkToPrinter(compressedBytes, i);
                }
            }
            finally
            {
                lock (syncRoot)
                {
                    printing = false;
                }
                statusLabel.Text = "Select CMC and hit send!";
            }
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        private async Task ConnectToBluetooth()
        {
            try
            {
                var device = BluetoothAdapter.DefaultAdapter.BondedDevices.First(d => d.Name.Contains("HC-05"));
                socket = device.CreateInsecureRfcommSocketToServiceRecord(Java.Util.UUID.FromString("00001101-0000-1000-8000-00805f9b34fb"));
                await socket.ConnectAsync();
            }
            catch (Exception e)
            {
                Log.Error("Bluetooth Connect", e.ToString());
                statusLabel.Text = "Could not connect to Bluetooth";
            }
        }

        private byte[] GetBytesFromImage(Bitmap bmp)
        {
            var pixels = new int[bmp.Width * bmp.Height];
            bmp.GetPixels(pixels, 0, bmp.Width, 0, 0, bmp.Width, bmp.Height);

            //white = -16711936
            //black = -65536
            //(p >> 16) & 0xff
            var boolPixels = pixels.Select(p => p == -65536).ToArray();
            var byteArray = new byte[pixels.Length / 8];
            var j = -1;
            for (var i = 0; i < boolPixels.Length; i += 1)
            {
                if (i % 8 == 0)
                    j += 1;
                if (boolPixels[i])
                    byteArray[j] |= (byte)(1 << 7 - (i % 8));
                //byteArray[j] |= (byte)(1 << (i % 8));
            }
            return byteArray;
        }

        private byte[] GetCompressedChunk(byte[] byteArray, int chunk)
        {
            var compressedBytes = new List<byte>();

            for (var idx = 0; idx < arduinoBufferSize; idx += 1)
            {
                var runValue = byteArray[chunk * arduinoBufferSize + idx];
                if (runValue != 0 && runValue != 255)
                {
                    compressedBytes.Add(runValue);
                    continue;
                }

                // only compress 0x00 and 0xFF
                var runLength = (byte)0;
                while (idx < arduinoBufferSize && runLength < 255 && byteArray[chunk * arduinoBufferSize + idx] == runValue)
                {
                    idx += 1;
                    runLength += 1;
                }
                idx -= 1;
                compressedBytes.Add(runValue);
                compressedBytes.Add(runLength);
            }
            return compressedBytes.ToArray();
        }
        
        private bool SendChunkToPrinter(byte[] compressedBytes, int chunk)
        {
            Log.Info("Printer", $"Chunk {chunk} compressed size: {compressedBytes.Length}. Compression ratio: {100.0 * compressedBytes.Count() / arduinoBufferSize * 8.0}");
            socket.OutputStream.Write(compressedBytes, 0, compressedBytes.Length);

            var startWait = DateTime.UtcNow;
            var timeout = TimeSpan.FromSeconds(3);
            while (!socket.InputStream.IsDataAvailable() && DateTime.UtcNow - startWait < timeout)
            { } // wait for printer to print the row
            if (DateTime.UtcNow - startWait >= timeout)
            {
                throw new IOException("Arduino not responding!");
            }
            var b = socket.InputStream.ReadByte();
            if (b == 5)
            {
                Log.Info("Printer", $"Chunk {chunk} printed");
            }

            return b == 6; // pritner says a full image was just printed
        }
	}
}

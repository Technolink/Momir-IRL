using System;
using Android.App;
using Android.Bluetooth;
using Android.OS;
using Android.Runtime;
using Android.Views;
using AndroidX.AppCompat.App;
using Google.Android.Material.FloatingActionButton;
using System.Net.Http;
using System.Threading.Tasks;
using System.IO;
using Android.Widget;
using Toolbar = AndroidX.AppCompat.Widget.Toolbar;
using ScryfallApi.Client.Models;
using Android.Graphics;
using Android.Util;
using System.Text.Json;
using System.Linq;
using System.Collections;
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

        protected async override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.activity_main);

            statusLabel = FindViewById<TextView>(Resource.Id.status_label);

            statusLabel.Text = "Connecting to Bluetooth...";

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
                    imageView.SetImageBitmap(monoBmp);

                    SendToPrinter(monoBmp).ConfigureAwait(false);
                    success = true;
                }
                catch (Exception e)
                {
                    Log.Error("FetchImage", e.ToString());
                }
            }
            if (!success)
            {
                Toast.MakeText(this, "Error getting image after 5 retries", ToastLength.Short);
            }
        }

        private async Task<(Bitmap, Bitmap)> GetImages(int cmc = 1)
        {

            using (var httpClient = new HttpClient())
            {
                var response = await httpClient.GetAsync(string.Format(ScryfallUrl, cmc));
                response.EnsureSuccessStatusCode();
                var responseString = await response.Content.ReadAsStringAsync();
                var card = JsonSerializer.Deserialize<Card>(responseString);
                var imageUrl = card.ImageUris["border_crop"];

                //name = "Zuberi, Golden Feather";
                var monoStream = Assets.Open($"monochrome/{(int)card.ConvertedManaCost}/{card.Id}.bmp");

                var imageResponse = await httpClient.GetAsync(imageUrl);
                imageResponse.EnsureSuccessStatusCode();
                using (var stream = await imageResponse.Content.ReadAsStreamAsync())
                {
                    var bmpTask = BitmapFactory.DecodeStreamAsync(stream);
                    var monoBmpTask = BitmapFactory.DecodeStreamAsync(monoStream);
                    await Task.WhenAll(bmpTask, monoBmpTask);
                    return (await bmpTask, await monoBmpTask);
                }
            }
        }

        const int arduinoBufferSize = 384 * 32 / 8;
        private async Task SendToPrinter(Bitmap bmp)
        {
            statusLabel.Text = "Sending to printer...";

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
                    byteArray[j] |= (byte)(1 << 7-(i % 8));
                    //byteArray[j] |= (byte)(1 << (i % 8));
            }

            // compress byte array?
            var compressedBytes = new List<byte>();

            for (var i = 0; i < byteArray.Length; i += 1)
            {
                var runValue = byteArray[i];
                if (runValue != 0 && runValue != 255)
                {
                    compressedBytes.Add(runValue);
                    continue;
                }

                // only compress 0x00 and 0xFF
                var runStart = i;
                var runLength = (byte)0;
                while (i < byteArray.Length && runLength < 255 && byteArray[i] == runValue)
                {
                    i += 1;
                    runLength += 1;
                }
                i -= 1;
                compressedBytes.Add(runValue);
                compressedBytes.Add(runLength);
            }
            
            Log.Info("Printer", $"Compressed size: {compressedBytes.Count()}. Compression ratio: {100.0 * compressedBytes.Count() / (double)byteArray.Length}");

            for (var i = 0; i < byteArray.Length / arduinoBufferSize; i++)
            {
                socket.OutputStream.Write(byteArray, i*arduinoBufferSize, arduinoBufferSize);
                socket.OutputStream.Flush();
                var startWait = DateTime.UtcNow;
                var timeout = TimeSpan.FromSeconds(3);
                while (!socket.InputStream.IsDataAvailable() && DateTime.UtcNow - startWait < timeout)
                { } // wait for printer to print the row
                if (DateTime.UtcNow - startWait >= timeout)
                {
                    Log.Error("Printer", "Arudino not responding!");
                    statusLabel.Text = "Select CMC and hit send!";
                    throw new IOException("Arduino not responding!");
                }
                var b = socket.InputStream.ReadByte();
                if (b == 6)
                {
                    break;
                }
            }

            statusLabel.Text = "Select CMC and hit send!";
            return;
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
	}
}

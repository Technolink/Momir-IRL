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

namespace Momir_IRL
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        private const string ScryfallUrl = "https://api.scryfall.com/cards/random?q=type:creature+cmc:{0}";
        private ImageView imageView;
        private Spinner cmcDropdown;
        private BluetoothSocket socket;

        protected async override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.activity_main);

            Task btConnectTask = Task.CompletedTask;
            try
            {
                var device = BluetoothAdapter.DefaultAdapter.BondedDevices.First(d => d.Name.Contains("HC-05"));
                socket = device.CreateInsecureRfcommSocketToServiceRecord(Java.Util.UUID.FromString("00001101-0000-1000-8000-00805f9b34fb"));
                btConnectTask = socket.ConnectAsync();
            }
            catch (Exception e)
            {
                Log.Error("Bluetooth Connect", e.ToString());
                Toast.MakeText(this, "Could not connect to HC-05 Bluetooth. Connect before starting the app", ToastLength.Long);
            }

            var toolbar = FindViewById<Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);


            var cmcs = Enumerable.Range(1, 16).Where(i => Assets.List(i.ToString()).Any());

            imageView = FindViewById<ImageView>(Resource.Id.card);
            cmcDropdown = FindViewById<Spinner>(Resource.Id.cmc);
            cmcDropdown.Adapter = new ArrayAdapter(this, Android.Resource.Layout.SimpleSpinnerDropDownItem, cmcs.ToArray());

            var fab = FindViewById<FloatingActionButton>(Resource.Id.fab);
            fab.Click += FabOnClick;

            await btConnectTask;
            PopulateImage(new Random().Next(1, 8));
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
                    var (bmp, monoBmp) = await GetImages(cmc ?? (int)cmcDropdown.SelectedItem);
                    imageView.SetImageBitmap(bmp);
                    //imageView.SetImageBitmap(monoBmp);
                    Task.Run(async () =>
                    {
                        await SendToPrinter(monoBmp);
                    });
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

                var name = card.Name.Split(" // ").First();
                name = string.Join("", name.Split(System.IO.Path.GetInvalidFileNameChars()));

                var monoStream = Assets.Open($"{(int)card.ConvertedManaCost}/{name}.bmp");

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

        private async Task SendToPrinter(Bitmap bmp)
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
                    byteArray[j] |= (byte)(1 << 7-(i % 8));
                    //byteArray[j] |= (byte)(1 << (i % 8));
            }

            for (var i = 0; i < bmp.Height; i += 1) 
            {
                socket.OutputStream.Write(byteArray, i++ * bmp.Width / 8, bmp.Width / 8);
                socket.OutputStream.Write(byteArray, i++ * bmp.Width / 8, bmp.Width / 8);
                socket.OutputStream.Write(byteArray, i++ * bmp.Width / 8, bmp.Width / 8);
                socket.OutputStream.Write(byteArray, i * bmp.Width / 8, bmp.Width / 8);

                while (!socket.InputStream.IsDataAvailable())
                { } // wait for printer to print the row
                var b = socket.InputStream.ReadByte();
            }
            return;
        }

        private async Task<Bitmap> ConvertToMonochrome(Bitmap bmp)
        {
            const int textboxHeight = 307;
            // Run on a background thread. TODO: Replace with ImageMagick + bindings
            return await Task.Run(() =>
            {
                //var bitmap = bmp.Copy(Bitmap.Config.Argb8888, true);
                var bitmap = Bitmap.CreateScaledBitmap(bmp, 384, 544, false);
                var canvas = new Canvas(bitmap);
                var ma = new ColorMatrix();
                ma.SetSaturation(0);
                var paint = new Paint();
                paint.SetColorFilter(new ColorMatrixColorFilter(ma));
                canvas.DrawBitmap(bitmap, 0, 0, paint);

                // Adapted from https://stackoverflow.com/questions/29078142/convert-bitmap-to-1bit-bitmap
                var pixels = new int[bitmap.Width * bitmap.Height];
                bitmap.GetPixels(pixels, 0, bitmap.Width, 0, 0, bitmap.Width, bitmap.Height);

                for (var y = 0; y < bitmap.Height; y++)
                {
                    for (int x = 0; x < bitmap.Width; x++)
                    {
                        int pixel = bitmap.GetPixel(x, y);
                        int lowestBit = pixel & 0xff;
                        if (y < textboxHeight && lowestBit < 64 || y >= textboxHeight && lowestBit < 128)
                        {
                            bitmap.SetPixel(x, y, Color.Black);
                        }
                        else
                        {
                            bitmap.SetPixel(x, y, Color.White);
                        }
                    }
                }

                return bitmap;
            });
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
	}
}

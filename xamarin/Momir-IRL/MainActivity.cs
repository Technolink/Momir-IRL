using System;
using Android.App;
using Android.Bluetooth;
using Android.OS;
using Android.Runtime;
using Android.Views;
using AndroidX.AppCompat.App;
using System.Threading.Tasks;
using System.IO;
using Android.Widget;
using Android.Graphics;
using Android.Util;
using System.Linq;
using System.Collections.Generic;

namespace Momir_IRL
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        private ImageView _imageView;
        private List<ImageButton> _buttons = new List<ImageButton>();
        private BluetoothSocket _socket;
        private volatile bool _printing = false;
        private readonly object _syncRoot = new object();
        private const int ArduinoBufferSize = 384 * 34 / 8;
#if DEBUG
        private const string Folder = "Debug";
#else
        private const string Folder = "Release";
#endif

        protected async override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.activity_main);

            await ConnectToBluetooth();

            //var toolbar = FindViewById<Toolbar>(Resource.Id.toolbar);
            //SetSupportActionBar(toolbar);

            var cmcs = Enumerable.Range(1, 20).Where(i => Assets.List($"{Folder}/monochrome/{i}").Any());

            _imageView = FindViewById<ImageView>(Resource.Id.card);

            var button1 = FindViewById<ImageButton>(Resource.Id.button_1);
            button1.Click += ButtonOnClick;
            button1.SetImageBitmap(await BitmapFactory.DecodeStreamAsync(Assets.Open($"{Folder}/mana/1.png")));
            _buttons.Add(new ImageButton(this)); // never a button for mana value 0
            _buttons.Add(button1);
            
            var lastButton = button1;
            foreach (var cmc in cmcs.Skip(1))
            {
                var layoutParams = new RelativeLayout.LayoutParams(button1.LayoutParameters.Width, button1.LayoutParameters.Height);
                layoutParams.AddRule(LayoutRules.AlignLeft, lastButton.Id);
                var button = new ImageButton(this)
                {
                    Id = View.GenerateViewId(),
                    Background = null,
                    LayoutParameters = layoutParams,
                    ContentDescription = cmc.ToString(),
                };
                button.SetScaleType(ImageView.ScaleType.CenterCrop);
                button.SetImageBitmap(await BitmapFactory.DecodeStreamAsync(Assets.Open($"{Folder}/mana/{cmc}.png")));
                button.Click += ButtonOnClick;

                (button1.Parent as LinearLayout).AddView(button);
                _buttons.Add(button);
                lastButton = button;
            }

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

        private async void ButtonOnClick(object sender, EventArgs eventArgs)
        {
            var button = sender as ImageButton;
            var cmc = _buttons.FindIndex(b => b.Id == button.Id);
            await PopulateImage(cmc);
        }

        private async Task PopulateImage(int cmc )
        {
            try
            {
                var (bmp, monoBmp) = await GetImages(cmc);
                _imageView.SetImageBitmap(bmp);
                _imageView.Invalidate();

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                SendToPrinter(monoBmp).ConfigureAwait(false);
#pragma warning restore CS4014
            }
            catch (Exception e)
            {
                Log.Error("Printer", e.ToString());
            }
        }

        private async Task SendToPrinter(Bitmap bmp)
        {
            lock (_syncRoot)
            {
                if (_printing)
                {
                    return;
                }
                _printing = true;
                _buttons.ForEach(b => b.Enabled = false);
            }
            try
            {
                var byteArray = GetBytesFromImage(bmp);

                for (var i = 0; i < byteArray.Length / ArduinoBufferSize; i++)
                {
                    var compressedBytes = GetCompressedChunk(byteArray, i);
                    await SendChunkToPrinterAsync(compressedBytes, i);
                }
            }
            finally
            {
                lock (_syncRoot)
                {
                    _printing = false;
                    _buttons.ForEach(b => b.Enabled = true);
                }
            }
        }

        private async Task<bool> SendChunkToPrinterAsync(byte[] compressedBytes, int chunk)
        {
            Log.Info("Printer", $"Chunk {chunk} compressed size: {compressedBytes.Length}. Compression ratio: {100.0 * compressedBytes.Count() / ArduinoBufferSize * 8.0}");
            for (var retry = 0; retry < 3; retry++)
            {
                await _socket.OutputStream.WriteAsync(compressedBytes, 0, compressedBytes.Length);

                var startWait = DateTime.UtcNow;
                var timeout = TimeSpan.FromSeconds(3);
                while (!_socket.InputStream.IsDataAvailable() && DateTime.UtcNow - startWait < timeout)
                { await Task.Delay(1); } // wait for printer to print the row

                if (_socket.InputStream.IsDataAvailable())
                {
                    break;
                }
                else
                {
                    Log.Error("Printer", $"Chunk {chunk} failed to send, Arduino not responding");
                    if (retry == 2)
                    {
                        throw new IOException("Arduino not responding!");
                    }
                    else
                    {
                        //await ConnectToBluetooth();
                    }
                }
            }

            var b = _socket.InputStream.ReadByte();
            if (b == 5)
            {
                Log.Info("Printer", $"Chunk {chunk} printed");
            }

            return b == 6; // pritner says a full image was just printed
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
                _socket = device.CreateInsecureRfcommSocketToServiceRecord(Java.Util.UUID.FromString("00001101-0000-1000-8000-00805f9b34fb"));
                await _socket.ConnectAsync();
            }
            catch (Exception e)
            {
                Log.Error("Printer", "Could not connect to Bluetooth " + e.ToString());
            }
        }

        private async Task<(Bitmap, Bitmap)> GetImages(int cmc = 1)
        {
            var cards = await Assets.ListAsync($"{Folder}/original/{cmc}");
            var card = cards[new Random().Next(0, cards.Length - 1)];

            var origialStream = Assets.Open($"{Folder}/original/{cmc}/{card}");
            var monoStream = Assets.Open($"{Folder}/monochrome/{cmc}/{card}");

            var origialBmpTask = BitmapFactory.DecodeStreamAsync(origialStream);
            var monoBmpTask = BitmapFactory.DecodeStreamAsync(monoStream);
            await Task.WhenAll(origialBmpTask, monoBmpTask);
            return (await origialBmpTask, await monoBmpTask);
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

            for (var idx = 0; idx < ArduinoBufferSize; idx += 1)
            {
                var runValue = byteArray[chunk * ArduinoBufferSize + idx];
                if (runValue != 0 && runValue != 255)
                {
                    compressedBytes.Add(runValue);
                    continue;
                }

                // only compress 0x00 and 0xFF
                var runLength = (byte)0;
                while (idx < ArduinoBufferSize && runLength < 255 && byteArray[chunk * ArduinoBufferSize + idx] == runValue)
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
	}
}

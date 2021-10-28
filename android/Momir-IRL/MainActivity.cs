using System;
using Android.App;
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

namespace Momir_IRL
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        private const string ScryfallUrl = "https://api.scryfall.com/cards/random?q=type:creature+cmc:{0}";
        private ImageView imageView;
        private Spinner cmcDropdown;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.activity_main);

            var toolbar = FindViewById<Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);

            imageView = FindViewById<ImageView>(Resource.Id.card);
            cmcDropdown = FindViewById<Spinner>(Resource.Id.cmc);
            cmcDropdown.Adapter = new ArrayAdapter(this, Android.Resource.Layout.SimpleSpinnerDropDownItem, Enumerable.Range(1, 16).ToArray());

            var fab = FindViewById<FloatingActionButton>(Resource.Id.fab);
            fab.Click += FabOnClick;

            PopulateImage();
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

        private async Task PopulateImage()
        {
            try
            {
                var bmp = await GetImage((int)cmcDropdown.SelectedItem);
                imageView.SetImageBitmap(bmp);

                var monoBmp = await ConvertToMonochrome(bmp);
                imageView.SetImageBitmap(monoBmp);
            }
            catch (Exception e)
            {
                Log.Error("FetchImage", e.ToString());
            }
        }

        private async Task<Bitmap> GetImage(int cmc = 1)
        {

            using (var httpClient = new HttpClient())
            {
                var response = await httpClient.GetAsync(string.Format(ScryfallUrl, cmc));
                response.EnsureSuccessStatusCode();
                var responseString = await response.Content.ReadAsStringAsync();
                var card = JsonSerializer.Deserialize<Card>(responseString);
                var imageUrl = card.ImageUris["border_crop"];

                // test scarab god
                imageUrl = new Uri("https://c1.scryfall.com/file/scryfall-cards/border_crop/front/d/7/d79ee141-0ea6-45d6-a682-96a37d703394.jpg?1599708320");

                var imageResponse = await httpClient.GetAsync(imageUrl);
                imageResponse.EnsureSuccessStatusCode();
                using (var stream = await imageResponse.Content.ReadAsStreamAsync())
                {
                    return await BitmapFactory.DecodeStreamAsync(stream);
                }
            }
        }

        private async Task<Bitmap> ConvertToMonochrome(Bitmap bmp)
        {
            // Run on a background thread. TODO: Replace with ImageMagick + bindings
            return await Task.Run(() =>
            {
                var bitmap = bmp.Copy(Bitmap.Config.Argb8888, true);
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
                        if (lowestBit < 64)
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

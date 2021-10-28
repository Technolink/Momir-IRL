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
            try
            {
                var bitmap = await GetImage((int)cmcDropdown.SelectedItem);
                imageView.SetImageBitmap(bitmap);
            }
            catch (Exception e)
            {
                Log.Error("FetchImage", e.ToString());
            }
        }

        private async Task<Stream> GetImageStream(int cmc = 1)
        {
            using (var httpClient = new HttpClient())
            {
                var response = await httpClient.GetAsync(string.Format(ScryfallUrl, cmc));
                response.EnsureSuccessStatusCode();
                var responseString = await response.Content.ReadAsStringAsync();
                var card = JsonSerializer.Deserialize<Card>(responseString);
                var imageUrl = card.ImageUris["border_crop"];

                var imageResponse = await httpClient.GetAsync(imageUrl);
                imageResponse.EnsureSuccessStatusCode();
                return await imageResponse.Content.ReadAsStreamAsync();
            }
        }

        private async Task<Bitmap> GetImage(int cmc = 1)
        {
            var imageStream = await GetImageStream(cmc);
            var bitmap = await BitmapFactory.DecodeStreamAsync(imageStream);
            return bitmap;
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
	}
}

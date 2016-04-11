using System;
using System.Threading.Tasks;

using Android.App;
using Android.Views;
using Android.Widget;
using Android.OS;
using Android.Graphics;
using Android.Animation;
using Runnable = Java.Lang.Runnable;

using Android.Support.V4.View;
using Android.Support.V4.Animation;
using Android.Support.V7.App;
using Android.Support.Design.Widget;
using Android.Support.V7.Widget;

namespace Buzzeroid
{
	[Activity (Label = "Buzzeroid", MainLauncher = true, Icon = "@mipmap/icon")]
	public class MainActivity : AppCompatActivity
	{
		BuzzerApi buzzerApi;

		CoordinatorLayout mainCoordinator;
		FloatingActionButton fab;
		RecyclerView recycler;

		BuzzHistoryAdapter adapter;

		FrameLayout notificationFrame;

		bool chked;

		protected override void OnCreate (Bundle savedInstanceState)
		{
			base.OnCreate (savedInstanceState);

			SetContentView (Resource.Layout.Main);

			var toolbar = FindViewById<Android.Support.V7.Widget.Toolbar> (Resource.Id.toolbar);
			SetSupportActionBar (toolbar);

			mainCoordinator = FindViewById<CoordinatorLayout> (Resource.Id.coordinatorLayout);

			fab = FindViewById<FloatingActionButton> (Resource.Id.fabBuzz);
			fab.Click += OnFabBuzzClick;
			var lp = (CoordinatorLayout.LayoutParams)fab.LayoutParameters;
			lp.Behavior = new FabMoveBehavior ();
			fab.LayoutParameters = lp;

			InitializeNotificationFrame ();

			recycler = FindViewById<RecyclerView> (Resource.Id.recycler);
			recycler.HasFixedSize = true;
			adapter = new BuzzHistoryAdapter (this);
			recycler.SetLayoutManager (new LinearLayoutManager (this));
			recycler.SetItemAnimator (new DefaultItemAnimator ());
			recycler.SetAdapter (adapter);

			InitializeAdapter ();
		}

		public override bool OnCreateOptionsMenu (Android.Views.IMenu menu)
		{
			MenuInflater.Inflate (Resource.Menu.menu, menu);
			return true;
		}

		public override bool OnOptionsItemSelected (Android.Views.IMenuItem item)
		{
			if (item.ItemId == Resource.Id.action_notification) {
				notificationFrame.Visibility = ViewStates.Visible;
				float initialX = -(notificationFrame.Left + notificationFrame.Width + notificationFrame.PaddingLeft);
				notificationFrame.TranslationX = initialX;
				ViewCompat.Animate (notificationFrame)
						  .TranslationX (0)
						  .SetDuration (600)
						  .SetStartDelay (100)
						  .SetInterpolator (new Android.Support.V4.View.Animation.LinearOutSlowInInterpolator ())
						  .Start ();

				return true;
			}
			return base.OnOptionsItemSelected (item);
		}

		void InitializeNotificationFrame ()
		{
			notificationFrame = FindViewById<FrameLayout> (Resource.Id.notifFrame);

			var title = notificationFrame.FindViewById<TextView> (Resource.Id.notifTitle);
			title.Typeface = Typeface.CreateFromAsset (Resources.Assets, "DancingScript.ttf");

			var lp = (CoordinatorLayout.LayoutParams)notificationFrame.LayoutParameters;
			lp.Behavior = new NotificationBehavior ();
			notificationFrame.LayoutParameters = lp;

			notificationFrame.Visibility = ViewStates.Invisible;
		}

		async void InitializeAdapter ()
		{
			try {
				await adapter.PopulateDatabaseWithStuff ();
				await adapter.FillUpFromDatabaseAsync ();
			} catch (Exception e) {
				Android.Util.Log.Error ("AdapterInitialize", e.ToString ());
			}
		}

		async Task<BuzzerApi> EnsureApi ()
		{
			if (buzzerApi != null)
				return buzzerApi;
			buzzerApi = await BuzzerApi.GetBuzzerApiAsync ();
			//ProcessFutureErrorStates (buzzerApi);
			return buzzerApi;
		}

		async void OnFabBuzzClick (object sender, System.EventArgs e)
		{
			var api = await EnsureApi ();
			await api.SetBuzzerStateAsync (chked = !chked);
		}

		async void ProcessFutureErrorStates (BuzzerApi api)
		{
			while (true) {
				var result = await api.GetNextStateStatusAsync ();
				if (!result)
					Snackbar.Make (mainCoordinator, "Failed to send buzz", Snackbar.LengthLong)
							.Show ();
			}
		}
	}
}



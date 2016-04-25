using System;
using System.Threading.Tasks;
using System.Diagnostics;

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
		Stopwatch openedTime = new Stopwatch ();

		CoordinatorLayout mainCoordinator;
		CheckableFab fab;
		RecyclerView recycler;

		BuzzHistoryAdapter adapter;

		FrameLayout notificationFrame;

		protected override void OnCreate (Bundle savedInstanceState)
		{
			base.OnCreate (savedInstanceState);

			SetContentView (Resource.Layout.Main);

			var toolbar = FindViewById<Android.Support.V7.Widget.Toolbar> (Resource.Id.toolbar);
			SetSupportActionBar (toolbar);

			mainCoordinator = FindViewById<CoordinatorLayout> (Resource.Id.coordinatorLayout);

			// SETUP NOTIFICATION FRAME

			notificationFrame = FindViewById<FrameLayout> (Resource.Id.notifFrame);
			notificationFrame.Visibility = ViewStates.Invisible;

			var title = notificationFrame.FindViewById<TextView> (Resource.Id.notifTitle);
			title.Typeface = Typeface.CreateFromAsset (Resources.Assets, "DancingScript.ttf");

			/* Assign notification behavior (aka swipe-to-dismiss)
			 *
			var lp = (CoordinatorLayout.LayoutParams)notificationFrame.LayoutParameters;
			var nb = new NotificationBehavior ();
			nb.Dismissed += (sender, e) => AddNewBuzzEntry (wasOpened: false);
			lp.Behavior = nb;
			notificationFrame.LayoutParameters = lp;*/

			// SETUP FLOATING ACTION BUTTON

			fab = FindViewById<CheckableFab> (Resource.Id.fabBuzz);
			fab.Click += OnFabBuzzClick;

			/* Craft curved motion into FAB
			 *
			lp = (CoordinatorLayout.LayoutParams)fab.LayoutParameters;
			lp.Behavior = new FabMoveBehavior ();
			fab.LayoutParameters = lp;*/

			/* Spice up the FAB icon story
			 *
			fab.SetImageResource (Resource.Drawable.ic_fancy_fab_icon);*/

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
			if (fab.Checked)
				openedTime.Restart ();
			var api = await EnsureApi ();
			await api.SetBuzzerStateAsync (fab.Checked);
			if (!fab.Checked && openedTime.IsRunning) {
				openedTime.Stop ();
				AddNewBuzzEntry (wasOpened: true, duration: openedTime.Elapsed);
				if (notificationFrame.Visibility == ViewStates.Visible) {
					notificationFrame.Visibility = ViewStates.Invisible;
					notificationFrame.TranslationX = 1;
				}
			}
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

		async void AddNewBuzzEntry (bool wasOpened, TimeSpan? duration = null)
		{
			try {
				await adapter.AddNewEntryAsync (new HistoryEntry {
					DidOpen = wasOpened,
					EventDate = DateTime.UtcNow,
					DoorOpenedTime = duration ?? TimeSpan.Zero
				});
			} catch (Exception e) {
				Android.Util.Log.Error ("NewBuzzEntry", e.ToString ());
			}
		}
	}
}



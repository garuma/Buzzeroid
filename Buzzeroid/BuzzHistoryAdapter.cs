
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using Android.Text;
using Android.Graphics;

using Android.Support.V7.Widget;

using SQLite;

namespace Buzzeroid
{
	[Table ("history_entry")]
	public class HistoryEntry {
		[AutoIncrement, PrimaryKey]
		public int Id { get; set; }
		[Column ("did_open")]
		public bool DidOpen { get; set; }
		[Column ("opened_time")]
		public TimeSpan DoorOpenedTime { get; set; }
		[Column ("date"), Indexed]
		public DateTime EventDate { get; set; }
	}

	public class BuzzHistoryAdapter : RecyclerView.Adapter
	{
		Context context;
		List<HistoryEntry> entriesReversed = new List<HistoryEntry> ();

		public BuzzHistoryAdapter (Context context)
		{
			this.context = context;
			this.HasStableIds = true;
		}

		string DbPath {
			get {
				var db = context.OpenOrCreateDatabase ("buzz_history", FileCreationMode.Private, null);
				var path = db.Path;
				db.Close ();
				return path;
			}
		}

		int GetReversedPosition (int position)
		{
			return entriesReversed.Count - 1 - position;
		}

		public async Task PopulateDatabaseWithStuff ()
		{
			var dbPath = DbPath;
			var connection = new SQLiteAsyncConnection (dbPath);
			await connection.CreateTableAsync<HistoryEntry> ();
			await connection.InsertAllAsync (new HistoryEntry [] {
				new HistoryEntry { DidOpen = false, DoorOpenedTime = TimeSpan.Zero, EventDate = new DateTime (2016, 01, 04) },
				new HistoryEntry { DidOpen = true, DoorOpenedTime = TimeSpan.FromMinutes (2), EventDate = new DateTime (2016, 01, 03) },
				new HistoryEntry { DidOpen = true, DoorOpenedTime = TimeSpan.FromSeconds (28), EventDate = new DateTime (2016, 01, 02) },
			});
		}

		public async Task FillUpFromDatabaseAsync ()
		{
			var dbPath = DbPath;
			var connection = new SQLiteAsyncConnection (dbPath);
			var oldCount = entriesReversed.Count;
			await connection.CreateTableAsync<HistoryEntry> ();
			entriesReversed.AddRange (await connection.Table<HistoryEntry> ().OrderBy (e => e.EventDate).ToListAsync ());
			this.NotifyItemRangeInserted (oldCount, entriesReversed.Count - oldCount);
		}

		public async Task AddNewEntryAsync (HistoryEntry entry)
		{
			var dbPath = DbPath;
			var connection = new SQLiteAsyncConnection (dbPath);
			await connection.CreateTableAsync<HistoryEntry> ();
			await connection.InsertAsync (entry);
			entriesReversed.Add (entry);
			this.NotifyItemInserted (GetReversedPosition (0));
		}

		public override RecyclerView.ViewHolder OnCreateViewHolder (ViewGroup parent, int viewType)
		{
			var inflater = LayoutInflater.From (parent.Context);
			return new ViewHolder (inflater.Inflate (Resource.Layout.CardEntry, parent, false));
		}

		public override void OnBindViewHolder (RecyclerView.ViewHolder holder, int position)
		{
			((ViewHolder)holder).SetData (entriesReversed [GetReversedPosition (position)]);
		}

		public override int ItemCount {
			get {
				return entriesReversed.Count;
			}
		}

		public override long GetItemId (int position)
		{
			return entriesReversed [GetReversedPosition (position)].Id;
		}

		class ViewHolder : RecyclerView.ViewHolder
		{
			TextView dateEntry;
			TextView descEntry;
			Button apologizeButton;
			ImageView lockIcon;

			static EventHandler clickHandler;
			static DeveloperExcuseApi api;

			public ViewHolder (View view) : base (view)
			{
				dateEntry = view.FindViewById<TextView> (Resource.Id.dateEntry);
				descEntry = view.FindViewById<TextView> (Resource.Id.descEntry);
				apologizeButton = view.FindViewById<Button> (Resource.Id.apologizeButton);
				lockIcon = view.FindViewById<ImageView> (Resource.Id.lockIcon);
				if (clickHandler == null)
					clickHandler = HandleApologizeClicked;
			}

			public void SetData (HistoryEntry entry)
			{
				dateEntry.Text = entry.EventDate.ToLongDateString ();

				if (!entry.DidOpen)
					descEntry.SetText (Resource.String.refused_buzz);
				else {
					const string Prefix = "Door was opened for ";
					var ts = entry.DoorOpenedTime;
					var timeStr = ts.TotalMinutes > 1 ? ((int)ts.TotalMinutes) + " minutes" : ((int)ts.TotalSeconds) + " seconds";
					var spannable = new SpannableString (Prefix + timeStr);
					spannable.SetSpan (new Android.Text.Style.StyleSpan (TypefaceStyle.Bold), Prefix.Length, spannable.Length (), (SpanTypes)0);
					descEntry.SetText (spannable, TextView.BufferType.Spannable);
				}

				lockIcon.SetImageResource (entry.DidOpen ? Resource.Drawable.ic_lock_open : Resource.Drawable.ic_lock);
				lockIcon.Enabled = entry.DidOpen;

				apologizeButton.Visibility = entry.DidOpen ? ViewStates.Invisible : ViewStates.Visible;
				apologizeButton.Click += clickHandler;
			}

			async void HandleApologizeClicked (object sender, EventArgs e)
			{
				try {
					if (api == null)
						api = new DeveloperExcuseApi ();
					var excuse = await api.GetNextExcuseAsync ();
					var intent = new Intent (Intent.ActionSend)
						.SetType ("text/plain")
						.PutExtra (Intent.ExtraText, excuse);
					var chooser = Intent.CreateChooser (intent, "Send excuse to");
					ItemView.Context.StartActivity (chooser);
				} catch (Exception ex) {
					Android.Util.Log.Error ("Apologize", ex.ToString ());
				}
			}
		}
	}
}


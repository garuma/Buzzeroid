
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using Android.Graphics;
using Java.Lang;

using Android.Support.Design.Widget;
using Android.Support.V7.Widget;
using Android.Support.V4.View;
using IOnDismissListener = Android.Support.Design.Widget.SwipeDismissBehavior.IOnDismissListener;

namespace Buzzeroid
{
	public class NotificationBehavior : SwipeDismissBehavior, IOnDismissListener
	{
		public NotificationBehavior ()
		{
			this.SetSwipeDirection (SwipeDismissBehavior.SwipeDirectionStartToEnd);
			this.SetDragDismissDistance (.98f);
			this.SetStartAlphaSwipeDistance (.1f);
			this.SetListener (this);
		}

		void IOnDismissListener.OnDismiss (View view)
		{
			view.Alpha = 1;
			view.Visibility = ViewStates.Invisible;
			view.RequestLayout ();
		}

		void IOnDismissListener.OnDragStateChanged (int state)
		{
		}

		public override bool CanSwipeDismissView (View view)
		{
			return view.Visibility == ViewStates.Visible && view.TranslationX == 0;
		}

		public override bool BlocksInteractionBelow (CoordinatorLayout parent, Java.Lang.Object child)
		{
			return child.JavaCast<View> ().Visibility == ViewStates.Visible;
		}

		public override int GetScrimColor (CoordinatorLayout parent, Java.Lang.Object child)
		{
			return new Color (0, 0, 0, 128).ToArgb ();
		}

		public override float GetScrimOpacity (CoordinatorLayout parent, Java.Lang.Object child)
		{
			return child.JavaCast<View> ().Visibility == ViewStates.Visible ? 1f : 0f;
		}
	}
}


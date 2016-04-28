
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
using Android.Views.Animations;

using Android.Support.Design.Widget;
using Android.Support.V4.View.Animation;

namespace Buzzeroid
{
	public class FabMoveBehavior : CoordinatorLayout.Behavior
	{
		bool previousVisibility;
		float minX;
		float originalX;
		int distanceX, distanceY;
		float scale;

		IInterpolator interpolator;

		public override bool LayoutDependsOn (CoordinatorLayout parent, Java.Lang.Object child, View dependency)
		{
			if (dependency.Id == Resource.Id.notifFrame)
				return true;
			return base.LayoutDependsOn (parent, child, dependency);
		}

		public override bool OnDependentViewChanged (CoordinatorLayout parent, Java.Lang.Object child, View dependency)
		{
			if (dependency.Id == Resource.Id.notifFrame) {
				var fab = child.JavaCast<FloatingActionButton> ();
				bool isNowVisibility = dependency.Visibility == ViewStates.Visible;

				// If the notification is still invisible, the changes are simply positioning.
				// Initialize everything by recalculating distances
				if (!previousVisibility && !isNowVisibility) {
					var placeholder = dependency.FindViewById (Resource.Id.fabPlaceholder);
					var pos = new int [2];
					placeholder.GetLocationOnScreen (pos);
					var targetX = pos [0] + placeholder.Width / 2;
					var targetY = pos [1] + placeholder.Height / 2;
					fab.GetLocationOnScreen (pos);
					distanceX = targetX - (pos [0] + fab.Width / 2);
					distanceY = targetY - (pos [1] + fab.Height / 2);
					scale = placeholder.Width / (float)fab.Width;
					originalX = dependency.GetX ();
					return false;
				}

				// The notification frame is now gone, erase all changes done to FAB
				if (previousVisibility && !isNowVisibility) {
					previousVisibility = false;
					fab.TranslationY = fab.TranslationX = 0;
					fab.ScaleY = fab.ScaleX = 1;
					fab.Alpha = 1;
					fab.Visibility = ViewStates.Invisible;
					fab.Show ();
					return true;
				}

				// We start the moving process
				if (isNowVisibility ^ previousVisibility) {
					previousVisibility = isNowVisibility;
					minX = Math.Abs (dependency.TranslationX);
				}

				// Notification is being dragged out to the right, FAB should follow suite
				if (dependency.GetX () > originalX) {
					fab.TranslationX = distanceX + dependency.GetX () - originalX;
					fab.Alpha = dependency.Alpha;
					return true;
				}

				// Carry out the initial curved motion in
				/* HACK: since path-based object animators are not yet available
				 * in support, we use the fact that PathInterpolator is to craft
				 * something similar. Think of it as creating the following cubic
				 * bezier (http://cubic-bezier.com/#0,.47,.47,1) and rotating the
				 * graph 90° to the left. X axis becomes the graph and Y axis is
				 * simply a vertical line.
				 */
				if (interpolator == null)
					interpolator = PathInterpolatorCompat.Create (0, .47f, .47f, 1);

				var currentTranslation = Math.Abs (dependency.TranslationX);
				var ratio = (minX - currentTranslation) / (float)minX;
				fab.TranslationY = distanceY * ratio;
				fab.TranslationX = distanceX * interpolator.GetInterpolation (ratio);
				fab.ScaleX = fab.ScaleY = 1 + (scale - 1) * ratio;

				return true;
			}
			return base.OnDependentViewChanged (parent, child, dependency);
		}
	}
}


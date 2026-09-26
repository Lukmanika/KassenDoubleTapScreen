using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Android.Widget;

namespace KassenDoubleTapScreen
{
    [Activity(
        Label = "Standby",
        Theme = "@android:style/Theme.NoTitleBar.Fullscreen",
        LaunchMode = LaunchMode.SingleTask,
        ExcludeFromRecents = true,
        ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.KeyboardHidden,
        ScreenOrientation = ScreenOrientation.Portrait
    )]
    public class StandbyActivity : Activity, GestureDetector.IOnGestureListener, GestureDetector.IOnDoubleTapListener
    {
        private GestureDetector? _gestureDetector;
        private TextView? _tvHint;
        private Vibrator? _vibrator;

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Pastikan layar bisa tampil di atas lock screen dan tetap aktif saat standby
            if (Build.VERSION.SdkInt >= BuildVersionCodes.OMr1)
            {
                SetShowWhenLocked(true);
                SetTurnScreenOn(true);
            }
            else
            {
#pragma warning disable CS0618
                Window?.AddFlags(
                    WindowManagerFlags.ShowWhenLocked |
                    WindowManagerFlags.TurnScreenOn |
                    WindowManagerFlags.DismissKeyguard
                );
#pragma warning restore CS0618
            }

            Window?.AddFlags(WindowManagerFlags.KeepScreenOn | WindowManagerFlags.Fullscreen);

            // Redupkan kecerahan layar ke tingkat terendah (0.001f = hampir padam/hitam total)
            if (Window?.Attributes != null)
            {
                var lp = Window.Attributes;
                lp.ScreenBrightness = 0.001f;
                Window.Attributes = lp;
            }

            SetContentView(Resource.Layout.activity_standby);

            _tvHint = FindViewById<TextView>(Resource.Id.tvStandbyHint);
            _vibrator = (Vibrator?)GetSystemService(VibratorService);
            _gestureDetector = new GestureDetector(this, this);
            _gestureDetector.SetOnDoubleTapListener(this);

            var root = FindViewById<FrameLayout>(Resource.Id.layoutStandbyRoot);
            if (root != null)
            {
                root.Touch += (s, e) =>
                {
                    if (e.Event != null)
                    {
                        _gestureDetector.OnTouchEvent(e.Event);
                    }
                };
            }

            HideSystemUI();
        }

        protected override void OnResume()
        {
            base.OnResume();
            HideSystemUI();
        }

        private void HideSystemUI()
        {
            if (Window?.DecorView != null)
            {
#pragma warning disable CS0618
                Window.DecorView.SystemUiVisibility = (StatusBarVisibility)(
                    SystemUiFlags.ImmersiveSticky |
                    SystemUiFlags.HideNavigation |
                    SystemUiFlags.Fullscreen |
                    SystemUiFlags.LayoutHideNavigation |
                    SystemUiFlags.LayoutFullscreen |
                    SystemUiFlags.LayoutStable
                );
#pragma warning restore CS0618
            }
        }

        private void VibrateBriefly()
        {
            try
            {
                if (AppSettings.IsVibrateEnabled(this) && _vibrator != null && _vibrator.HasVibrator)
                {
                    if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
                    {
                        _vibrator.Vibrate(VibrationEffect.CreateOneShot(50, VibrationEffect.DefaultAmplitude));
                    }
                    else
                    {
#pragma warning disable CS0618
                        _vibrator.Vibrate(50);
#pragma warning restore CS0618
                    }
                }
            }
            catch
            {
                // Abaikan error vibrator
            }
        }

        // GestureDetector.IOnDoubleTapListener
        public bool OnDoubleTap(MotionEvent e)
        {
            VibrateBriefly();
            Finish();
            OverridePendingTransition(0, 0);
            return true;
        }

        public bool OnDoubleTapEvent(MotionEvent e) => false;

        public bool OnSingleTapConfirmed(MotionEvent e)
        {
            if (_tvHint != null)
            {
                _tvHint.Alpha = 1.0f;
                _tvHint.Animate()?.Alpha(0.0f)?.SetDuration(1200)?.Start();
            }
            return true;
        }

        // GestureDetector.IOnGestureListener
        public bool OnDown(MotionEvent e) => true;
        public bool OnFling(MotionEvent? e1, MotionEvent e2, float velocityX, float velocityY) => false;
        public void OnLongPress(MotionEvent e) { }
        public bool OnScroll(MotionEvent? e1, MotionEvent e2, float distanceX, float distanceY) => false;
        public void OnShowPress(MotionEvent e) { }
        public bool OnSingleTapUp(MotionEvent e) => false;

        public override bool OnKeyDown(Keycode keyCode, KeyEvent? e)
        {
            // Jika tombol fisik power, volume, atau back ditekan, segera keluar dari standby
            if (keyCode == Keycode.Back || keyCode == Keycode.VolumeDown || keyCode == Keycode.VolumeUp)
            {
                Finish();
                OverridePendingTransition(0, 0);
                return true;
            }
            return base.OnKeyDown(keyCode, e);
        }
    }
}

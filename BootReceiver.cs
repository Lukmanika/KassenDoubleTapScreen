using Android.App;
using Android.Content;
using Android.OS;

namespace KassenDoubleTapScreen
{
    [BroadcastReceiver(Enabled = true, Exported = true)]
    [IntentFilter(new[] { Intent.ActionBootCompleted })]
    public class BootReceiver : BroadcastReceiver
    {
        public override void OnReceive(Context? context, Intent? intent)
        {
            if (context == null || intent?.Action != Intent.ActionBootCompleted)
                return;

            if (AppSettings.IsServiceEnabled(context))
            {
                if (AppSettings.IsFloatingEnabled(context))
                {
                    var overlayIntent = new Intent(context, typeof(FloatingOverlayService));
                    if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
                    {
                        context.StartForegroundService(overlayIntent);
                    }
                    else
                    {
                        context.StartService(overlayIntent);
                    }
                }

                if (AppSettings.IsSensorWakeEnabled(context))
                {
                    var sensorIntent = new Intent(context, typeof(SensorWakeService));
                    if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
                    {
                        context.StartForegroundService(sensorIntent);
                    }
                    else
                    {
                        context.StartService(sensorIntent);
                    }
                }
            }
        }
    }
}

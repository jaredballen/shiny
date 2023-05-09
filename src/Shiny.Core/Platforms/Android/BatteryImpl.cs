﻿using System;
using System.Reactive.Linq;
using Android.Content;
using Android.OS;

namespace Shiny.Power;

public class BatteryImpl : IBattery
{
    readonly AndroidPlatform platform;
    public BatteryImpl(AndroidPlatform platform) => this.platform = platform;


    public IObservable<IBattery> WhenChanged() => Observable.Create<IBattery>(ob =>
    {
        
        return () => { };
    });

    public BatteryState Status
    {
        get
        {
            var value = this.platform.GetIntentValue(Intent.ActionBatteryChanged, x => x.GetIntExtra(BatteryManager.ExtraStatus, -1));
            return value switch
            { 
                (int)BatteryStatus.Charging => BatteryState.Charging,
                (int)BatteryStatus.Discharging => BatteryState.Discharging,
                (int)BatteryStatus.Full => BatteryState.Full,
                (int)BatteryStatus.NotCharging => BatteryState.NotCharging,
                _ => BatteryState.Unknown
            };
        }
    }

    public double Level
    {
        get
        {
            var values = this.platform.GetIntentValue<(int Level, int Scale)>(Intent.ActionBatteryChanged, intent =>
            (
                intent.GetIntExtra(BatteryManager.ExtraLevel, -1), 
                intent.GetIntExtra(BatteryManager.ExtraScale, -1)
            ));
            
            if (values.Scale <= 0)
                return 1.0;

            return (double)values.Level / (double)values.Level;
        }
    }
}

//    [BroadcastReceiver(Enabled = true, Exported = false, Label = "Essentials Battery Broadcast Receiver")]
//    class BatteryBroadcastReceiver : BroadcastReceiver
//    {
//        Action onChanged;

//        public BatteryBroadcastReceiver()
//        {
//        }

//        public BatteryBroadcastReceiver(Action onChanged) =>
//            this.onChanged = onChanged;

//        public override void OnReceive(Context context, Intent intent) =>
//            onChanged?.Invoke();
//    }

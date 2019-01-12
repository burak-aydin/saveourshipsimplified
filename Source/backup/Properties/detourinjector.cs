using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

using RimWorld;
using Verse;

namespace saveourship
{
    [StaticConstructorOnStartup]
    internal static class DetourInjector
    {
        private static Assembly Assembly => Assembly.GetAssembly(typeof(DetourInjector));

        private static string AssemblyName => Assembly.FullName.Split(',').First();

        static DetourInjector()
        {
            LongEventHandler.QueueLongEvent(Inject, "Initializing", true, null);
        }

        private static void Inject()
        {
            if (DoInject())
                Log.Message(AssemblyName + " injected.");
            else
                Log.Error(AssemblyName + " failed to get injected properly.");
        }

        private const BindingFlags UniversalBindingFlags = BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static bool DoInject()
        {
            // Detour the DropBloodFilth method
            // First MethodInfo is source method to detour
            // Second MethodInfo is our method taking its place
            MethodInfo Verse_PawnHealthTracker_DropBloodFilth = typeof(RimWorld.ShipCountdown).GetMethod("CountdownEnded", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo MyRimworldMod_DropBloodOverride_DropBloodFilth = typeof(MyDetours).GetMethod("countdownend");
            if (!Detours.TryDetourFromTo(Verse_PawnHealthTracker_DropBloodFilth, MyRimworldMod_DropBloodOverride_DropBloodFilth))
            {
                ErrorDetouring("ShowCredits");
                return false;
            }

            // You can do as many detours as you like.

            Log.Message("Injection succeed");
            // All our detours must have succeeded. Hooray!
            return true;
        }

        // Just saves some writing for throwing errors on failed detours
        internal static void ErrorDetouring(string classmethod)
        {
            Log.Error("Failed to inject " + classmethod + " detour!");
        }
    }
}
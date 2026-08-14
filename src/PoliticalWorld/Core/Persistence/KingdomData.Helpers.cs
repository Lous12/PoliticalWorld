using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using NeoModLoader.api;
using NeoModLoader.General;
using NeoModLoader.General.UI.Tab;
using NeoModLoader.General.UI.Window;
using NeoModLoader.General.UI.Prefabs;
using strings;
using UnityEngine;
using UnityEngine.UI;

namespace Lous12.PoliticalWorld
{
    public partial class Main
    {
        private static int GetKingdomIntData(
            Kingdom kingdom,
            string key,
            int fallback
        )
        {
            if (kingdom == null || kingdom.data == null)
            {
                return fallback;
            }

            int value = fallback;
            try
            {
                kingdom.data.get(key, out value, fallback);
            }
            catch
            {
                value = fallback;
            }
            return value;
        }

        private static void SetKingdomIntData(
            Kingdom kingdom,
            string key,
            int value
        )
        {
            if (kingdom == null || kingdom.data == null)
            {
                return;
            }

            try
            {
                kingdom.data.set(key, value);
            }
            catch
            {
            }
        }

        private static string GetKingdomStringData(
            Kingdom kingdom,
            string key,
            string fallback
        )
        {
            if (kingdom == null || kingdom.data == null)
            {
                return fallback;
            }

            string value = fallback;
            try
            {
                kingdom.data.get(key, out value, fallback);
            }
            catch
            {
                value = fallback;
            }
            return value;
        }

        private static void SetKingdomStringData(
            Kingdom kingdom,
            string key,
            string value
        )
        {
            if (kingdom == null || kingdom.data == null)
            {
                return;
            }

            try
            {
                kingdom.data.set(key, value ?? "");
            }
            catch
            {
            }
        }

    }
}

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
        // Step 9D FAST: ideology framework models moved unchanged.
        private sealed class IdeologyBehaviorProfile
        {
            public int Market;
            public int Welfare;
            public int Centralization;
            public int Pluralism;
            public int Militarism;
            public bool Pacifist;
            public bool Stateless;
            public bool Primitivist;
        }

        private sealed class IdeologyNode
        {
            public string Id;
            public string ParentId;
            public string RootIdeologyId;
            public int Tier;
            public string NameKey;
            public int HighSupportStability;
            public int StabilitySupportThreshold;
            public int LowSupportStability;
            public float DiffusionMultiplier;
            public string[] Tags;
        }

    }
}

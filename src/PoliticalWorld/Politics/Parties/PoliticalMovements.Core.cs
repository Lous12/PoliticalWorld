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
        private static void UpdatePoliticalMovements()
        {
            List<Kingdom> kingdoms = GetKingdomsSafe();

            for (int k = 0; k < kingdoms.Count; k++)
            {
                Kingdom kingdom = kingdoms[k];

                if (kingdom == null || kingdom.data == null)
                {
                    continue;
                }

                string stateIdeology = GetStateIdeology(kingdom);

                for (int i = 0; i < IdeologyIds.Length; i++)
                {
                    string ideology = IdeologyIds[i];
                    string suffix = GetMovementKeySuffix(ideology);
                    string initializedKey = MovementInitializedPrefix + suffix;
                    string activeKey = MovementActivePrefix + suffix;
                    string radicalismKey = MovementRadicalismPrefix + suffix;
                    string radicalizedKey = MovementRadicalizedPrefix + suffix;
                    string leaderKey = MovementLeaderNamePrefix + suffix;

                    if (ideology == stateIdeology)
                    {
                        SetKingdomIntData(kingdom, activeKey, 0);
                        SetKingdomIntData(kingdom, radicalizedKey, 0);
                        continue;
                    }

                    int support = GetKingdomIdeologySupport(kingdom, ideology);
                    int initialized = GetKingdomIntData(kingdom, initializedKey, 0);
                    int active = GetKingdomIntData(kingdom, activeKey, 0);

                    if (initialized == 0)
                    {
                        SetKingdomIntData(kingdom, initializedKey, 1);

                        if (support >= MovementFormationThreshold)
                        {
                            active = 1;
                            SetKingdomIntData(kingdom, activeKey, 1);
                            int initialRadicalism = CalculateMovementRadicalismTarget(
                                kingdom,
                                ideology,
                                support
                            );
                            SetKingdomIntData(kingdom, radicalismKey, initialRadicalism);
                            EnsureMovementLeaderName(kingdom, ideology, leaderKey);
                            SetKingdomIntData(
                                kingdom,
                                radicalizedKey,
                                initialRadicalism >= MovementRadicalThreshold ? 1 : 0
                            );
                        }

                        // Первый проход является базовой линией. Это не даёт
                        // десяткам уже существующих государств заспамить лог
                        // сразу после загрузки сохранения.
                        continue;
                    }

                    if (active == 0)
                    {
                        if (support < MovementFormationThreshold)
                        {
                            continue;
                        }

                        active = 1;
                        SetKingdomIntData(kingdom, activeKey, 1);
                        SetKingdomIntData(kingdom, radicalizedKey, 0);
                        EnsureMovementLeaderName(kingdom, ideology, leaderKey);

                        int target = CalculateMovementRadicalismTarget(
                            kingdom,
                            ideology,
                            support
                        );
                        SetKingdomIntData(kingdom, radicalismKey, target);

                        PublishPoliticalEvent(
                            string.Format(
                                LM.Get("ukiol_event_movement_formed"),
                                GetWorldObjectDisplayName(kingdom),
                                GetIdeologyName(ideology),
                                support
                            ),
                            kingdom,
                            null,
                            null,
                            GetIdeologyIconPath(ideology),
                            "movement_formed_" + suffix,
                            25f
                        );
                    }
                    else if (support <= MovementDissolutionThreshold)
                    {
                        SetKingdomIntData(kingdom, activeKey, 0);
                        SetKingdomIntData(kingdom, radicalizedKey, 0);
                        continue;
                    }

                    if (active == 0)
                    {
                        continue;
                    }

                    int currentRadicalism = GetKingdomIntData(
                        kingdom,
                        radicalismKey,
                        20
                    );
                    int radicalismTarget = CalculateMovementRadicalismTarget(
                        kingdom,
                        ideology,
                        support
                    );
                    int nextRadicalism = MoveTowardsInt(
                        currentRadicalism,
                        radicalismTarget,
                        5
                    );
                    SetKingdomIntData(kingdom, radicalismKey, nextRadicalism);

                    if (string.IsNullOrEmpty(GetKingdomStringData(kingdom, leaderKey, "")))
                    {
                        EnsureMovementLeaderName(kingdom, ideology, leaderKey);
                    }

                    int wasRadicalized = GetKingdomIntData(
                        kingdom,
                        radicalizedKey,
                        0
                    );

                    if (
                        wasRadicalized == 0 &&
                        nextRadicalism >= MovementRadicalThreshold
                    )
                    {
                        SetKingdomIntData(kingdom, radicalizedKey, 1);

                        PublishPoliticalEvent(
                            string.Format(
                                LM.Get("ukiol_event_movement_radicalized"),
                                GetWorldObjectDisplayName(kingdom),
                                GetIdeologyName(ideology),
                                support
                            ),
                            kingdom,
                            null,
                            null,
                            GetIdeologyIconPath(ideology),
                            "movement_radical_" + suffix,
                            45f
                        );
                    }
                    else if (
                        wasRadicalized != 0 &&
                        nextRadicalism <= MovementDeradicalizeThreshold
                    )
                    {
                        SetKingdomIntData(kingdom, radicalizedKey, 0);
                    }
                }
            }
        }


    }
}

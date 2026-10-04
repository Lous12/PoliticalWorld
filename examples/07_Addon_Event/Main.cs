using System.Collections.Generic;
using NeoModLoader.api;
using Lous12.PoliticalWorld;

namespace Lous12.PWSDK.Example07
{
    public class Main : BasicMod<Main>
    {
        private const string AddonId = "Lous12.PWSDK.Example07";
        private const string EventId = AddonId + ".example_ping";

        protected override void OnModLoad()
        {
            if (!PoliticalWorldAPI.IsCompatible(1, 10))
            {
                LogError("Political World API 1.10+ is required by this example.");
                return;
            }

            if (!PoliticalWorldAPI.RegisterAddon(new PoliticalWorldAPI.AddonDefinition
            {
                Id = AddonId,
                Name = "PW SDK Example 07",
                Version = "1.0.0",
                Author = "Lous12",
                Description = "Addon-owned custom event example",
                RequiredApiMajor = 1,
                RequiredApiMinor = 10
            })) return;

            if (!PoliticalWorldAPI.RegisterAddonEvent(AddonId, EventId))
            {
                LogError("Could not register custom event " + EventId);
                return;
            }

            PoliticalWorldAPI.Subscribe(AddonId, EventId, OnExampleEvent);

            // Publishing is explicit. Real addons would normally call this from
            // an action, event handler or another meaningful gameplay trigger.
            PoliticalWorldAPI.PublishAddonEvent(
                AddonId,
                EventId,
                new Dictionary<string, string>
                {
                    { "message", "hello from Example 07" },
                    { "api", PoliticalWorldAPI.ApiVersion }
                },
                category: "sdk-example"
            );
        }

        private static void OnExampleEvent(PoliticalWorldAPI.PoliticalEventData data)
        {
            if (data == null) return;
            NeoModLoader.services.LogService.LogInfo(
                "[PW SDK Example 07] received " + data.EventId + "."
            );
        }
    }
}

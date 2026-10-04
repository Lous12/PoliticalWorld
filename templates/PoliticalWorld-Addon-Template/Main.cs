using NeoModLoader.api;
using Lous12.PoliticalWorld;

namespace YourName.MyPoliticalAddon
{
    public class Main : BasicMod<Main>
    {
        private const string AddonId = "YourName.MyPoliticalAddon";

        protected override void OnModLoad()
        {
            // This template intentionally targets the current framework release.
            // If your finished addon only uses older contracts, you can lower the
            // minimum later. Require what you actually use, not whatever number
            // happened to be current when you copied the template.
            PoliticalWorldAPI.AddonDefinition definition =
                new PoliticalWorldAPI.AddonDefinition
                {
                    Id = AddonId,
                    Name = "My Political World Addon",
                    Version = "0.1.0",
                    Author = "YourName",
                    Description = "Started from the Political World API 1.19 template",
                    RequiredApiMajor = 1,
                    RequiredApiMinor = 19,
                    RequiredCapabilities = new[]
                    {
                        "framework.requirements-check",
                        "diagnostics.report"
                    }
                };

            PoliticalWorldAPI.AddonRequirementCheck requirements =
                PoliticalWorldAPI.Framework.CheckRequirements(definition);

            if (!requirements.Compatible)
            {
                LogError(requirements.Summary);
                return;
            }

            if (!PoliticalWorldAPI.RegisterAddon(definition))
            {
                LogError("Political World rejected addon registration.");
                return;
            }

            // Register ideologies/governments/actions/events/UI here.
            // If you need a core feature that is not public, request an API
            // capability instead of reflecting into Main/ScenarioBridge.
            PoliticalWorldAPI.LogDiagnosticsReport(AddonId);
            LogInfo("Loaded with PoliticalWorldAPI " + PoliticalWorldAPI.ApiVersion + ".");
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace Lous12.PoliticalWorld
{
    /// <summary>
    /// API 1.19: release helpers and ecosystem capability discovery.
    ///
    /// This layer intentionally does not add simulation or world scanning. It
    /// gives addon authors one place to ask "what framework am I running on?",
    /// check requirements before registration and build a useful support report.
    /// </summary>
    public static partial class PoliticalWorldAPI
    {
        public sealed class FrameworkReleaseInfo
        {
            public string ApiVersion;
            public int ApiMajor;
            public int ApiMinor;
            public string Channel;
            public string CoreModId;
            public string DisplayName;
            public bool StableContract;
            public string[] Capabilities;
        }

        public sealed class AddonRequirementCheck
        {
            public bool ApiCompatible;
            public bool CapabilitiesReady;
            public bool Compatible;
            public string RequiredApi;
            public string ProvidedApi;
            public string[] MissingCapabilities;
            public string Summary;
        }

        public static class Framework
        {
            public static FrameworkReleaseInfo GetReleaseInfo()
            {
                return new FrameworkReleaseInfo
                {
                    ApiVersion = ApiVersion,
                    ApiMajor = ApiMajor,
                    ApiMinor = ApiMinor,
                    // API 1.19 ships as part of the public PW 1.11 release.
                    // Keep this in sync with the release metadata instead of
                    // leaving development defaults in support reports.
                    Channel = "stable",
                    CoreModId = CoreModId,
                    DisplayName = "PoliticalWorldAPI General Framework",
                    StableContract = true,
                    Capabilities = GetCapabilities()
                };
            }

            /// <summary>
            /// Checks only API/capability requirements. It does not register the
            /// addon and therefore can safely be called before RegisterAddon.
            /// </summary>
            public static AddonRequirementCheck CheckRequirements(AddonDefinition definition)
            {
                AddonRequirementCheck result = new AddonRequirementCheck
                {
                    ApiCompatible = true,
                    CapabilitiesReady = true,
                    Compatible = true,
                    RequiredApi = "none",
                    ProvidedApi = ApiVersion,
                    MissingCapabilities = new string[0],
                    Summary = "OK"
                };

                if (definition == null)
                {
                    result.ApiCompatible = false;
                    result.CapabilitiesReady = false;
                    result.Compatible = false;
                    result.Summary = "Addon definition is null.";
                    return result;
                }

                int reqMajor = definition.RequiredApiMajor;
                int reqMinor = definition.RequiredApiMinor;
                if (reqMajor > 0)
                {
                    result.RequiredApi = reqMajor + "." + reqMinor + "+";
                    result.ApiCompatible = reqMajor == ApiMajor && reqMinor <= ApiMinor;
                }

                List<string> missing = new List<string>();
                string[] required = definition.RequiredCapabilities;
                if (required != null)
                {
                    for (int i = 0; i < required.Length; i++)
                    {
                        string capability = Trim(required[i]);
                        if (string.IsNullOrEmpty(capability)) continue;
                        if (!Ecosystem.IsCapabilityAvailable(capability) && !missing.Contains(capability))
                        {
                            missing.Add(capability);
                        }
                    }
                }

                missing.Sort(StringComparer.Ordinal);
                result.MissingCapabilities = missing.ToArray();
                result.CapabilitiesReady = result.MissingCapabilities.Length == 0;
                result.Compatible = result.ApiCompatible && result.CapabilitiesReady;

                if (!result.ApiCompatible)
                {
                    result.Summary = "Requires PoliticalWorldAPI " + result.RequiredApi +
                        ", but this build provides " + ApiVersion + ".";
                }
                else if (!result.CapabilitiesReady)
                {
                    result.Summary = "Waiting for capabilities: " +
                        string.Join(", ", result.MissingCapabilities);
                }
                else
                {
                    result.Summary = "OK";
                }

                return result;
            }

            public static AddonRequirementCheck CheckRegisteredAddon(string addonId)
            {
                AddonInfo addon = GetAddon(addonId);
                if (addon == null)
                {
                    return new AddonRequirementCheck
                    {
                        ApiCompatible = false,
                        CapabilitiesReady = false,
                        Compatible = false,
                        RequiredApi = "unknown",
                        ProvidedApi = ApiVersion,
                        MissingCapabilities = new string[0],
                        Summary = "Addon is not registered: " + Trim(addonId)
                    };
                }

                return CheckRequirements(new AddonDefinition
                {
                    Id = addon.Id,
                    Name = addon.Name,
                    Version = addon.Version,
                    Description = addon.Description,
                    Author = addon.Author,
                    RequiredApiMajor = addon.RequiredApiMajor,
                    RequiredApiMinor = addon.RequiredApiMinor,
                    RequiredCapabilities = CloneStringArray(addon.RequiredCapabilities)
                });
            }

            /// <summary>
            /// Produces a copy-paste-friendly support report for bug reports.
            /// </summary>
            public static string GetSupportReport(string addonId = "")
            {
                StringBuilder builder = new StringBuilder();
                FrameworkReleaseInfo release = GetReleaseInfo();

                builder.AppendLine("[Political World Framework Support Report]");
                builder.AppendLine("Framework: " + release.DisplayName);
                builder.AppendLine("API: " + release.ApiVersion);
                builder.AppendLine("Channel: " + release.Channel);
                builder.AppendLine("Stable contract: " + (release.StableContract ? "yes" : "no"));
                builder.AppendLine("Core mod: " + release.CoreModId);
                builder.AppendLine();

                if (!string.IsNullOrWhiteSpace(addonId))
                {
                    builder.AppendLine(Ecosystem.GetAddonReport(addonId));
                }
                else
                {
                    builder.AppendLine(Ecosystem.GetFrameworkReport());
                }

                return builder.ToString().TrimEnd();
            }

            /// <summary>
            /// Allows addon authors to surface use of an old API path in the
            /// framework diagnostics without breaking the addon.
            /// </summary>
            public static void ReportDeprecatedUse(
                string addonId,
                string deprecatedFeature,
                string replacement = ""
            )
            {
                string feature = Trim(deprecatedFeature);
                if (string.IsNullOrEmpty(feature)) feature = "unknown";
                string message = "Deprecated API path used: " + feature + ".";
                if (!string.IsNullOrWhiteSpace(replacement))
                {
                    message += " Prefer: " + replacement.Trim() + ".";
                }

                InternalRecordFrameworkIssue(
                    addonId,
                    "WARN",
                    "PWREL001",
                    message
                );
            }
        }
    }
}

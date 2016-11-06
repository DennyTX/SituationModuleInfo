using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ScienceSituationInfo
{
    internal static class SituationHelper
    {
        private const string AllowedMark = " <b><color=green>V</color></b>";
        private const string DisallowedMark = " <b><color=red>X</color></b>";

        static SituationHelper()
        {
            SituationTemplates = new Dictionary<ExperimentSituations, Func<bool, string>>();
            UsageMaskTemplates = new Dictionary<int, string>
            {
                {-1, "<i><color=red>Experiment CAN'T be run at all</color></i>"},
                {0, "<i><color=green>Experiment can always be run</color></i>"},
                {1, "<i><color=magenta>Experiment can be run if vessel is UNDER CONTROL</color></i>"},
                {2, "<i><b><color=green>Experiment can be run if vessel is CREWED</color></b></i>"},
                {4, "<i><b><color=green>Experiment can be run if part contains crew</color><b></i>"},
                {5, "<i><b><color=green>Experiment can be run if part contains crew</color><b></i>"},
                {8, "<i><color=purple>Experiment can be run if CREW HAVE SCIENTIST</color></i>"}
            };

            Initialize();
        }

        internal static Dictionary<ExperimentSituations, Func<bool, string>> SituationTemplates { get; private set; }

        internal static Dictionary<int, string> UsageMaskTemplates { get; private set; }

        static void Initialize()
        {
            var situations = Enum.GetValues(typeof(ExperimentSituations));
            foreach (var situation in situations)
            {
                SituationTemplates.Add((ExperimentSituations) situation,
                    b => string.Format("{0}{1}", ParseSituation((ExperimentSituations) situation), 
                        b ? AllowedMark : DisallowedMark));
            }
        }

        static string ParseSituation(ExperimentSituations situation)
        {
            var rgx = new Regex("(?<!^)([A-Z])");
            return rgx.Replace(situation.ToString(), " $1");
        }

    }
}

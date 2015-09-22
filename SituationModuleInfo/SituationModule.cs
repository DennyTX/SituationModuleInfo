using System;
using System.Collections.Generic;
using System.Linq;
using KspHelper.Behavior;
using UnityEngine;
using System.Reflection;
using System.IO;


namespace SituationModuleInfo
{
    class Item
    {
        public Item()
        {
            ExperimentTitles = new string[1] { string.Empty };
        }
        public string ModuleName { get; set; }

        public string Biome { get; set; }

        public string Situation { get; set; }

        public string[] ExperimentTitles { get; set; }
    }

    [KSPAddon(KSPAddon.Startup.EditorAny, true)]
    public class SituationModule : KspBehavior
    {
        private readonly string[] _etalonSituationMask = new string[6]  
        {
            "Flying High: <b><color=red>X</color></b>",
            "Flying Low: <b><color=red>X</color></b>",
            "In Space High: <b><color=red>X</color></b>",
            "In Space Low: <b><color=red>X</color></b>",
            "Landed: <b><color=red>X</color></b>",
            "Splashed: <b><color=red>X</color></b>"
        };

        private string usageMaskInt = "";
        private ConfigNode _config; 
        private List<AvailablePart> _partsWithScience = new List<AvailablePart>();
        private List<Item> _items = new List<Item>();
        private List<string> _allExperimentTitles = new List<string>();

        protected override void Start()
        {
            var assemblyPath = Assembly.GetExecutingAssembly().Location;
            var directoryPath = Path.GetDirectoryName(assemblyPath);
            _config = ConfigNode.Load(Path.Combine(directoryPath, "config.cfg"));
            LoadConfig();
            SituationMaskAnalys();
        }

        private void LoadConfig()  
        {
            var nodes = _config.GetNodes("ITEM");
            foreach (var node in nodes)
            {
                var item = new Item
                {
                    ModuleName = node.GetValue("moduleName"),
                    Biome = node.GetValue("biome"),
                    Situation = node.GetValue("situation")
                };
                var experimentTitles = node.GetValue("experimentTitles");
                Debug.LogWarning(experimentTitles);
                item.ExperimentTitles = experimentTitles.Split(',').Select(x => x.Trim().ToLower()).ToArray();
                _items.Add(item);
                _allExperimentTitles.AddRange(item.ExperimentTitles);
            }
        }

        private void SituationMaskAnalys()
        {
            _partsWithScience = PartLoader.LoadedPartsList.Where(p => p.partPrefab.Modules.GetModules<ModuleScienceExperiment>().Any()).ToList();

            foreach (var part in _partsWithScience)
            {
                var modules = part.partPrefab.Modules.GetModules<ModuleScienceExperiment>();

                foreach (var moduleScienceExperiment in modules)
                {
                    ScienceExperiment experiment = ResearchAndDevelopment.GetExperiment(moduleScienceExperiment.experimentID) ?? new ScienceExperiment();
                    var moduleNode = _items.FirstOrDefault(x => x.ModuleName.Equals(moduleScienceExperiment.name, StringComparison.InvariantCultureIgnoreCase));
                    if (moduleNode != null)
                    {
                        Type t = moduleScienceExperiment.GetType();
                        var field = t.GetField(moduleNode.Biome);
                        if (field != null)
                        {
                            var value = field.GetValue(moduleScienceExperiment);
                            experiment.biomeMask = (uint)value;
                        }
                        else
                        {
                            var property = t.GetProperty(moduleNode.Biome);
                            if (property != null)
                            {
                                var value = property.GetValue(moduleScienceExperiment, null);
                                experiment.biomeMask = (uint)value;
                            }
                        }

                        field = t.GetField(moduleNode.Situation);
                        if (field != null)
                        {
                            var value = field.GetValue(moduleScienceExperiment);
                            experiment.situationMask = (uint)value;
                        }
                        else
                        {
                            var property = t.GetProperty(moduleNode.Situation);
                            if (property != null)
                            {
                                var value = property.GetValue(moduleScienceExperiment, null);
                                experiment.situationMask = (uint)value;
                            }
                        }
                    }

                    var itemInfo = new string[6];
                    Array.Copy(_etalonSituationMask, itemInfo, 6);

                    if ((experiment.situationMask & (uint)ExperimentSituations.FlyingHigh) ==
                        (uint)ExperimentSituations.FlyingHigh)
                    {
                        itemInfo[0] = "Flying High: <b><color=green>V</color></b>";
                    }

                    if ((experiment.biomeMask & (uint)ExperimentSituations.FlyingHigh) ==
                    (uint)ExperimentSituations.FlyingHigh)
                    {
                        itemInfo[0] = "Flying High: <b><color=green>V</color> Biome Depending</b>";
                    }

                    if ((experiment.situationMask & (uint)ExperimentSituations.FlyingLow) ==
                        (uint)ExperimentSituations.FlyingLow)
                    {
                        itemInfo[1] = "Flying Low: <b><color=green>V</color></b>";
                    }

                    if ((experiment.biomeMask & (uint)ExperimentSituations.FlyingLow) ==
                    (uint)ExperimentSituations.FlyingLow)
                    {
                        itemInfo[1] = "Flying Low: <b><color=green>V</color> Biome Depending</b>";
                    }

                    if ((experiment.situationMask & (uint)ExperimentSituations.InSpaceHigh) ==
                        (uint)ExperimentSituations.InSpaceHigh)
                    {
                        itemInfo[2] = "Space High: <b><color=green>V</color></b>";
                    }

                    if ((experiment.biomeMask & (uint)ExperimentSituations.InSpaceHigh) ==
                    (uint)ExperimentSituations.InSpaceHigh)
                    {
                        itemInfo[2] = "Space High: <b><color=green>V</color> Biome Depending</b>";
                    }

                    if ((experiment.situationMask & (uint)ExperimentSituations.InSpaceLow) ==
                        (uint)ExperimentSituations.InSpaceLow)
                    {
                        itemInfo[3] = "Space Low: <b><color=green>V</color></b>";
                    }

                    if ((experiment.biomeMask & (uint)ExperimentSituations.InSpaceLow) ==
                    (uint)ExperimentSituations.InSpaceLow)
                    {
                        itemInfo[3] = "Space Low: <b><color=green>V</color> Biome Depending</b>";
                    }

                    if ((experiment.situationMask & (uint)ExperimentSituations.SrfLanded) ==
                        (uint)ExperimentSituations.SrfLanded)
                    {
                        itemInfo[4] = "Landed: <b><color=green>V</color></b>";
                    }

                    if ((experiment.biomeMask & (uint)ExperimentSituations.SrfLanded) ==
                    (uint)ExperimentSituations.SrfLanded)
                    {
                        itemInfo[4] = "Landed: <b><color=green>V</color> Biome Depending</b>";
                    }

                    if ((experiment.situationMask & (uint)ExperimentSituations.SrfSplashed) ==
                        (uint)ExperimentSituations.SrfSplashed)
                    {
                        itemInfo[5] = "Splashed: <b><color=green>V</color></b>";
                    }

                    if ((experiment.biomeMask & (uint)ExperimentSituations.SrfSplashed) ==
                    (uint)ExperimentSituations.SrfSplashed)
                    {
                        itemInfo[5] = "Splashed: <b><color=green>V</color> Biome Depending</b>";
                    }

                    switch (moduleScienceExperiment.usageReqMaskInternal)
                    {
                        case -1:
                            usageMaskInt = "<i><color=red>Experiment can't be used at all.</color></i>";
                            break;
                        case 0:
                            usageMaskInt = "<i><color=maroon>Experiment can always be used.</color></i>";
                            break;
                        case 1:
                            usageMaskInt = "<i><color=green>Experiment can be used if vessel is under control.</color></i>";
                            break;
                        case 2:
                            usageMaskInt = "<i><color=navy>Experiment can only be used if vessel is crewed.</color></i>";
                            break;
                        case 4:
                            usageMaskInt = "<i><color=teal>Experiment can only be used if part contains crew.</color></i>";
                            break;
                        case 8:
                            usageMaskInt = "<i><color=purple>Experiment can only be used if crew is scientist.</color></i>";
                            break;
                    }

                    try
                    {
                        List<AvailablePart.ModuleInfo> infos = new List<AvailablePart.ModuleInfo>();
                        foreach (var data in part.moduleInfos)
                        {
                            if (_allExperimentTitles.Contains(data.moduleName.ToLower()))
                            {
                                infos.Add(data);
                            }
                        }
                        if (!infos.Any()) continue;
                        var d = infos.FirstOrDefault(x => x.info.Contains(string.IsNullOrEmpty(moduleScienceExperiment.experimentActionName) ? moduleScienceExperiment.experimentID : moduleScienceExperiment.experimentActionName));
                        if (d == null) continue;
                        d.info = string.Concat(d.info, "\n", GetInfo(itemInfo, usageMaskInt));
                    }
                    catch (Exception ex)
                    {
                        Debug.LogException(ex);
                    }
                }
            }
        }

        private string GetInfo(string[] moduleInfos, string usageMaskInt)
        {
            string data = "--------------------------------\n";
            foreach (string moduleInfo in moduleInfos)
            {
                data = string.Concat(data, moduleInfo + "\n");
            }
            data = string.Concat(data, "\n" + usageMaskInt.ToUpper() + "\n");
            return data;
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using KspHelper.Behavior;
using UnityEngine;

namespace ScienceSituationInfo
{
    class Item
    {
        public string ModuleName { get; set; }

        public string Biome { get; set; }

        public string Situation { get; set; }

        public string ExperimentTitle { get; set; }
    }

 [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    public class SituationModule : KspBehavior
    {
        private string _biomeDepending = "Biome Depending";
        private string _usageMaskInt = "";
        private ConfigNode _config;
        private List<AvailablePart> _partsWithScience = new List<AvailablePart>();
        private readonly List<Item> _items = new List<Item>();
        private ApplicationLauncherButton _button;

        protected override void Start()
        {
            var assemblyPath = Assembly.GetExecutingAssembly().Location;
            var directoryPath = Path.GetDirectoryName(assemblyPath);
            _config = ConfigNode.Load(Path.Combine(directoryPath, "config.cfg"));
            LoadConfig();

            if (_button == null)
            {
                var texture = GameDatabase.Instance.GetTexture("OLDD/ScienceSituationInfo/ScienceInfoOFF", false);
                //var texture = GameDatabase.Instance.GetTexture("OLDD/ScienceSituationInfo/ScienceInfoOFF", false);
                _button = ApplicationLauncher.Instance.AddModApplication(ButtonTrue, ButtonFalse, () => { }, () => { },
                    () => { }, () => { }, ApplicationLauncher.AppScenes.VAB | ApplicationLauncher.AppScenes.SPH, texture);
            }
        }

        private void ButtonTrue()
        {
            SituationMaskAnalys();
            _button.SetTexture(GameDatabase.Instance.GetTexture("OLDD/ScienceSituationInfo/ScienceInfoON", false));
        }

        private void ButtonFalse()
        {
            RemoveModuleAdditionalInfo();
            _button.SetTexture(GameDatabase.Instance.GetTexture("OLDD/ScienceSituationInfo/ScienceInfoOFF", false));
        }

        private void RemoveModuleAdditionalInfo()
        {
            _partsWithScience = PartLoader.LoadedPartsList.Where(p => p.partPrefab.Modules.GetModules<ModuleScienceExperiment>().Any()).ToList();

            foreach (var part in _partsWithScience)
            {
                var moduleInfos = part.partPrefab.partInfo.moduleInfos;
                foreach (var moduleInfo in moduleInfos)
                {
                    var startIndex = moduleInfo.info.IndexOf("--------------------------------", StringComparison.Ordinal);
                    if (startIndex < 0) continue;

                    moduleInfo.info = moduleInfo.info.Remove(startIndex - 1);
                }
            }
        }

        private void LoadConfig()
        {
            var nodes = _config.GetNodes("ITEM");
            foreach (var node in nodes)
            {
                //var experimentTitle = node.GetValue("experimentTitle"); 
                var item = new Item
                {
                    ModuleName = node.GetValue("moduleName"),
                    Biome = node.GetValue("biome") ?? string.Empty,
                    Situation = node.GetValue("situation") ?? string.Empty,
                    ExperimentTitle = node.GetValue("experimentTitle").ToLower()               
                };
                _items.Add(item);
            }
        }

        private void SituationMaskAnalys()
        {
            _partsWithScience = PartLoader.LoadedPartsList.Where(p => p.partPrefab.Modules.GetModules<ModuleScienceExperiment>().Any()).ToList();

            foreach (var part in _partsWithScience)
            {
                var modules = part.partPrefab.Modules.GetModules<ModuleScienceExperiment>();

                foreach (ModuleScienceExperiment moduleScienceExperiment in modules)
                {
                    ScienceExperiment experiment = ResearchAndDevelopment.GetExperiment(moduleScienceExperiment.experimentID) ?? new ScienceExperiment();

                    var moduleNode = _items.FirstOrDefault(x => x.ModuleName.Equals(moduleScienceExperiment.moduleName, StringComparison.InvariantCultureIgnoreCase));

                    PrepareExternalModules(moduleNode, moduleScienceExperiment, experiment);

                    var itemInfo = new string[6];

                    PrepareSituationAndBiomes(experiment, itemInfo);

                    PrepareUsage(moduleScienceExperiment);

                    PrepareInfoDescription(part, moduleScienceExperiment, itemInfo);
                }
            }
        }

        private static void PrepareExternalModules(Item moduleNode, ModuleScienceExperiment moduleScienceExperiment,
            ScienceExperiment experiment)
        {
            if (moduleNode != null)
            {
                Type t = moduleScienceExperiment.GetType();
                //var meth = t.GetMethod("");

                //meth.Invoke(moduleScienceExperiment, new object[]{12, 2, 6});
                var field = t.GetField(moduleNode.Biome);
                if (field != null)
                {
                    var value = field.GetValue(moduleScienceExperiment);
                    experiment.biomeMask = Convert.ToUInt32(value);
                }
                else
                {
                    var property = t.GetProperty(moduleNode.Biome);
                    if (property != null)
                    {
                        var value = property.GetValue(moduleScienceExperiment, null);
                        experiment.biomeMask = Convert.ToUInt32(value);
                    }
                }

                field = t.GetField(moduleNode.Situation);
                if (field != null)
                {
                    var value = field.GetValue(moduleScienceExperiment);
                    experiment.situationMask = Convert.ToUInt32(value);
                }
                else
                {
                    var property = t.GetProperty(moduleNode.Situation);
                    if (property != null)
                    {
                        var value = property.GetValue(moduleScienceExperiment, null);
                        experiment.situationMask = Convert.ToUInt32(value);
                    }
                }
            }
        }

        private void PrepareInfoDescription(AvailablePart part, ModuleScienceExperiment moduleScienceExperiment,
            string[] itemInfo)
        {
            try
            {
                foreach (var item in _items)
                {
                    var infos = part.moduleInfos.Where(
                        x => item.ExperimentTitle.Contains(x.moduleName.ToLower()));

                    if (!infos.Any()) continue;
                    var d =
                        infos.FirstOrDefault(
                            x =>
                                x.info.Contains(
                                    string.IsNullOrEmpty(moduleScienceExperiment.experimentActionName)
                                        ? moduleScienceExperiment.experimentID
                                        : moduleScienceExperiment.experimentActionName));
                    if (d == null) continue;
                    d.info = string.Concat(d.info, "\n", GetInfo(itemInfo, _usageMaskInt));
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        private void PrepareSituationAndBiomes(ScienceExperiment experiment, string[] itemInfo)
        {
            var arr = Enum.GetValues(typeof (ExperimentSituations));
            var arrIndexed = new List<ExperimentSituations>(); 

            foreach (var type in arr)
            {
                arrIndexed.Add((ExperimentSituations)type);  // пробежимся по всем элементам, возьмем его и преобразуем в объект ExperimentSituations
            }
            for (int i = 0; i < 6; i++)
            {
                var enumMember = FormatString(arrIndexed, i);
                if ((experiment.situationMask & (uint) (arrIndexed[i])) == (uint) (arrIndexed[i]))
                {
                    itemInfo[i] = string.Concat(enumMember, "<b><color=green>V</color></b>");
                    if ((experiment.biomeMask & (uint) (arrIndexed[i])) == (uint) (arrIndexed[i]))
                    {
                        itemInfo[i] = string.Concat(itemInfo[i], "  ", _biomeDepending);
                    }
                }
                else
                {
                    itemInfo[i] = string.Concat(enumMember, "<b><color=red>X</color></b>");
                }
            }
        }

        private void PrepareUsage(ModuleScienceExperiment moduleScienceExperiment)
        {
            switch (moduleScienceExperiment.usageReqMaskInternal)
            {
                case -1:
                    _usageMaskInt = "<i><color=red>Experiment can't be used at all.</color></i>";
                    break;
                case 0:
                    _usageMaskInt = "<i><color=maroon>Experiment can always be used.</color></i>";
                    break;
                case 1:
                    _usageMaskInt = "<i><color=green>Experiment can be used if vessel is under control.</color></i>";
                    break;
                case 2:
                    _usageMaskInt = "<i><color=lime>Experiment can only be used if vessel is crewed.</color></i>";
                    break;
                case 4:
                    _usageMaskInt = "<i><color=teal>Experiment can only be used if part contains crew.</color></i>";
                    break;
                case 8:
                    _usageMaskInt = "<i><color=purple>Experiment can only be used if crew is scientist.</color></i>";
                    break;
            }
        }
        private static string FormatString(List<ExperimentSituations> arrIndexed, int i)
        {
            var enumMember = Enum.GetName(typeof (ExperimentSituations), arrIndexed[i]);
            int j = 1;
            while (enumMember != null && j < enumMember.Length)
            {
                if (!enumMember[j].ToString().Equals(enumMember[j].ToString().ToLower()))
                {
                    enumMember = enumMember.Insert(j, " ");
                    j++;
                }
                j++;
            }
            if (enumMember.StartsWith("In "))
            {
                enumMember = enumMember.Substring(3);
            }
            else
            if (enumMember.StartsWith("Srf "))
            {
                enumMember = enumMember.Substring(4);              
            }
            enumMember = string.Concat(enumMember, ": ");
            return enumMember;
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

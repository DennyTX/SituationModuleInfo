using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace ScienceSituationInfo
{
    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    public class SituationModule : MonoBehaviour
    {
        private readonly string _biomeDepending = " Biome Depending";
        private string _usageMaskInt = "";
        private ConfigNode _config;
        private List<AvailablePart> _partsWithScience = new List<AvailablePart>();
        private readonly List<Item> _items = new List<Item>();
  
        private void Start()
        {
            
            var assemblyPath = Assembly.GetExecutingAssembly().Location;
            var directoryPath = Path.GetDirectoryName(assemblyPath);
            System.Diagnostics.Debug.Assert(directoryPath != null, "directoryPath != null"); 
            _config = ConfigNode.Load(Path.Combine(directoryPath, "SituationModuleInfo.cfg"));
            LoadConfig();
            AddScienceSituationInfo();
        }

        private void OnDestroy()
        {
            RemoveScienceSituationInfo();
        }

        private void LoadConfig()
        {
            var nodes = _config.GetNodes("ITEM");
            foreach (var node in nodes)
            {
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

        private void AddScienceSituationInfo()
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
                    var itemInfo = PrepareSituationAndBiomes(experiment);
                    _usageMaskInt = SituationHelper.UsageMaskTemplates[moduleScienceExperiment.usageReqMaskInternal];
                    PrepareInfoDescription(part, moduleScienceExperiment, itemInfo);
                }
            }
        }

        private void RemoveScienceSituationInfo()
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

        private static void PrepareExternalModules(Item moduleNode, ModuleScienceExperiment moduleScienceExperiment, ScienceExperiment experiment)
        {
            if (moduleNode != null)
            {
                Type t = moduleScienceExperiment.GetType();
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

        private List<string> PrepareSituationAndBiomes(ScienceExperiment experiment)
        {
            var result = new List<string>();

            foreach (var situationTemplate in SituationHelper.SituationTemplates)
            {
                var flag = (experiment.situationMask & (uint)(situationTemplate.Key)) == (uint)(situationTemplate.Key); //получаем чтото если текущая ситуация совпадает с заявленной
                var dictValue = situationTemplate.Value; // получили делегат
                var preparedInfo = dictValue(flag); // вызвали делегат
                if (flag && (experiment.biomeMask & (uint)(situationTemplate.Key)) == (uint)(situationTemplate.Key))
                {
                    preparedInfo += _biomeDepending;
                }
                result.Add(preparedInfo);
            }
            return result;
        }

        private void PrepareInfoDescription(AvailablePart part, ModuleScienceExperiment moduleScienceExperiment,
            List<string> itemInfo)
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

        private string GetInfo(List<string> moduleInfos, string usageMaskInt)
        {
            string data = "--------------------------------\n";
            foreach (string moduleInfo in moduleInfos)
            {
                data = string.Concat(data, moduleInfo + "\n");
            }
            data = string.Concat(data, "\n" + usageMaskInt + "\n");
            return data;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using DMagic.Part_Modules;
using SEPScience;
//using kerbal_impact;
using UnityEngine;

namespace ScienceSituationInfo
{
    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    public class SituationModule : MonoBehaviour
    {
        private readonly string _biomeDepending = " Biome Dpnd";
        private string _usageMaskInt = "";

        private List<AvailablePart> _partsWithScience = new List<AvailablePart>();

        private List<AvailablePart> _partsWithScienceSEP = new List<AvailablePart>();
        private List<AvailablePart> _partsWithScienceDM = new List<AvailablePart>();
        private List<AvailablePart> _partsWithScienceIMP = new List<AvailablePart>();
        private List<AvailablePart> _partsWithScienceROV = new List<AvailablePart>();

        protected static bool SEP_Present = false;
        protected static bool DMagic_Present = false;
        protected static bool Impact_Present = false;
        protected static bool Impact_Present_X = false;
        protected static bool ROV_Present = false;
        protected static bool ROV_Present_X = false;


        private void Start()
        {
            SEP_Present = AssemblyLoader.loadedAssemblies.Any(a => a.assembly.GetName().Name == "SEPScience");
            DMagic_Present = AssemblyLoader.loadedAssemblies.Any(a => a.assembly.GetName().Name == "DMagic");
            Impact_Present = AssemblyLoader.loadedAssemblies.Any(a => a.assembly.GetName().Name == "kerbal-impact");
            ROV_Present = AssemblyLoader.loadedAssemblies.Any(a => a.assembly.GetName().Name == "RoverScience");

            if (ROV_Present && !ROV_Present_X)
            {
                _partsWithScienceROV = PartLoader.LoadedPartsList.Where(p => p.name.Contains("roverBrain")).ToList();
                foreach (var part in _partsWithScienceROV)
                {
                    var experimentROV = ResearchAndDevelopment.GetExperiment("RoverScienceExperiment");
                    var itemInfo = PrepareSituationAndBiomes(experimentROV);
                    _usageMaskInt = "<b><color=red>SURFACE ONLY, find a place</color></b>";
                    var d = new AvailablePart.ModuleInfo
                    {
                        moduleDisplayName = experimentROV.experimentTitle,
                        info = GetInfo(itemInfo, _usageMaskInt)
                    };
                    part.moduleInfos.Add(d);
                }
                ROV_Present_X = true;
            }

            if (Impact_Present && !Impact_Present_X)
            {
                _partsWithScienceIMP = PartLoader.LoadedPartsList.Where (p => p.name.Contains("Impact")).ToList();
                foreach (var part in _partsWithScienceIMP)
                {
                    var experimentIMP = new ScienceExperiment();
                    switch (part.name)
                    {
                        case "Impact Seismometer":
                            experimentIMP = ResearchAndDevelopment.GetExperiment("ImpactSeismometer");
                            _usageMaskInt = "<b><color=red>SURFACE ONLY, automated</color></b>";
                            break;
                        case "Impact Spectrometer":
                            experimentIMP = ResearchAndDevelopment.GetExperiment("ImpactSpectrometer");
                            _usageMaskInt = "<b><color=red>ORBIT ONLY, automated</color></b>";
                            break;
                    }
                    var itemInfo = PrepareSituationAndBiomes(experimentIMP);
                    var d = new AvailablePart.ModuleInfo
                    {
                        moduleDisplayName = experimentIMP.experimentTitle, 
                        info = GetInfo(itemInfo, _usageMaskInt)
                    };
                    part.moduleInfos.Add(d);
                }
                Impact_Present_X = true;
            }

            if (SEP_Present)
            {
                _partsWithScienceSEP = PartLoader.LoadedPartsList.Where
                    (p => p.partPrefab.Modules.GetModules<ModuleSEPScienceExperiment>().Any()).ToList();
                foreach (var part in _partsWithScienceSEP)
                {
                    var modulesSEP = part.partPrefab.Modules.GetModules<ModuleSEPScienceExperiment>();
                    foreach (var moduleScienceExperimentSEP in modulesSEP)
                    {
                        var SEP_ID = moduleScienceExperimentSEP.experimentID + "_Basic";
                        ScienceExperiment experimentSEP = ResearchAndDevelopment.GetExperiment(SEP_ID) ?? new ScienceExperiment();
                        var itemInfo = PrepareSituationAndBiomes(experimentSEP);
                        _usageMaskInt = "<b><color=red>SURFACE ONLY, handled by EVA</color></b>";
                        PrepareInfoDescriptionSEP(part, moduleScienceExperimentSEP, itemInfo);
                    }
                }
            }

            if (DMagic_Present)
            {
                _partsWithScienceDM = PartLoader.LoadedPartsList.Where
                    (p => p.partPrefab.Modules.GetModules<DMBasicScienceModule>().Any()).ToList();
                foreach (var part in _partsWithScienceDM)
                {
                    var modulesDM = part.partPrefab.Modules.GetModules<DMBasicScienceModule>();
                    foreach (var moduleScienceExperimentDM in modulesDM)
                    {
                        ScienceExperiment experimentDM = ResearchAndDevelopment.GetExperiment(moduleScienceExperimentDM.experimentID) ?? new ScienceExperiment();
                        var itemInfo = PrepareSituationAndBiomes(experimentDM);
                        _usageMaskInt = "<b><color=red>SURFACE ONLY, handled by EVA</color></b>";
                        PrepareInfoDescriptionDM(part, moduleScienceExperimentDM, itemInfo);
                    }
                }
            }

            _partsWithScience = PartLoader.LoadedPartsList.Where
                (p => p.partPrefab.Modules.GetModules<ModuleScienceExperiment>().Any()).ToList();
            foreach (var part in _partsWithScience)
            {
                var modules = part.partPrefab.Modules.GetModules<ModuleScienceExperiment>();
                foreach (var moduleScienceExperiment in modules)
                {
                    ScienceExperiment experiment = ResearchAndDevelopment.GetExperiment(moduleScienceExperiment.experimentID) ?? new ScienceExperiment();
                    var itemInfo = PrepareSituationAndBiomes(experiment);
                    _usageMaskInt = SituationHelper.UsageMaskTemplates[moduleScienceExperiment.usageReqMaskInternal];
                    PrepareInfoDescription(part, moduleScienceExperiment, itemInfo);
                }
            }
        }

        private List<string> PrepareSituationAndBiomes(ScienceExperiment experiment)
        {
            var result = new List<string>();

            foreach (var situationTemplate in SituationHelper.SituationTemplates)
            {
                uint situationMask = experiment.situationMask;
                if (ROV_Present && experiment.situationMask==64)
                {
                    situationMask = 1;
                }
                var flag = (situationMask & (uint)situationTemplate.Key) == (uint)situationTemplate.Key; //получаем чтото если текущая ситуация совпадает с заявленной
                var dictValue = situationTemplate.Value; // получили делегат
                var preparedInfo = dictValue(flag); // вызвали делегат
                if (flag && (experiment.biomeMask & (uint)situationTemplate.Key) == (uint)situationTemplate.Key)
                {
                    preparedInfo += _biomeDepending;
                }
                result.Add(preparedInfo);
            }
            return result;
        }

        private void PrepareInfoDescription(AvailablePart part, ModuleScienceExperiment moduleScienceExperiment, List<string> itemInfo)
        {
            try
            {
                var infos = part.moduleInfos;
                AvailablePart.ModuleInfo d = null;
                foreach (var x in infos)
                {
                    if (!x.info.Contains(moduleScienceExperiment.experimentActionName)) continue;
                    d = x;
                    break;
                }
                if (d == null) return;
                d.info = string.Concat(d.info, "\n", GetInfo(itemInfo, _usageMaskInt));
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        private void PrepareInfoDescriptionSEP(AvailablePart part, ModuleSEPScienceExperiment moduleScienceExperiment, List<string> itemInfo)
        {
            try
            {
                var infos = part.moduleInfos;
                AvailablePart.ModuleInfo d = null;
                foreach (var x in infos)
                {
                    if (!x.info.Contains(moduleScienceExperiment.experimentActionName)) continue;
                    d = x;
                    break;
                }
                if (d == null) return;
                d.info = string.Concat(d.info, "\n", GetInfo(itemInfo, _usageMaskInt));
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        private void PrepareInfoDescriptionDM(AvailablePart part, DMBasicScienceModule moduleScienceExperiment, List<string> itemInfo)
        {
            try
            {
                var infos = part.moduleInfos;
                AvailablePart.ModuleInfo d = null;
                foreach (var x in infos)
                {
                    if (!x.moduleDisplayName.Contains(moduleScienceExperiment.GUIName)) continue;
                    //if (!x.info.Contains(moduleScienceExperiment.experimentActionName)) continue;
                    d = x;
                    break;
                }
                if (d == null) return;
                d.info = string.Concat(d.info, "\n", GetInfo(itemInfo, _usageMaskInt));
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

        private void OnDestroy()
        {
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

            if (SEP_Present)
            {
                foreach (var part in _partsWithScienceSEP)
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
            if (DMagic_Present)
            {
                foreach (var part in _partsWithScienceDM)
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
            //if (Impact_Present)
            //{
            //    foreach (var part in _partsWithScienceIMP)
            //    {
            //        var moduleInfos = part.moduleInfos;
            //        foreach (var moduleInfo in moduleInfos)
            //        {
            //            var startIndex = moduleInfo.info.IndexOf("--------------------------------", StringComparison.Ordinal);
            //            if (startIndex < 0) continue;
            //            moduleInfo.info = moduleInfo.info.Remove(startIndex - 1);
            //        }
            //    }
            //}
        }
    }
}

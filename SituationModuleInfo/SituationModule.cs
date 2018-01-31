using System;
using System.Collections.Generic;
using System.Linq;
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
        protected static bool DMagic_Present_X = false;
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

            RoverScience();

            Impact();

            if (DMagic_Present && !DMagic_Present_X)
            {
                _partsWithScienceDM = PartLoader.LoadedPartsList.Where(p => p.name.Contains("dmSeismic")).ToList();
                foreach (AvailablePart part in _partsWithScienceDM)
                {
                    ScienceExperiment experimentDMS = new ScienceExperiment();
                    switch (part.name)
                    {
                        case "dmSeismicPod":
                            experimentDMS = ResearchAndDevelopment.GetExperiment("dmseismicHammer");
                            _usageMaskInt = "<b><color=red>SURFACE ONLY, handled by EVA</color></b>";
                            break;
                        case "dmSeismicHammer":
                            experimentDMS = ResearchAndDevelopment.GetExperiment("dmseismicHammer");
                            _usageMaskInt = "<b><color=red>SURFACE ONLY, handled by EVA</color></b>";
                            break;
                    }

                    List<string> itemInfo = PrepareSituationAndBiomes(experimentDMS);
                    if (PrepareInfoDescriptionSE(part, "DMSeismic", itemInfo)) return;
            }
                DMagic_Present_X = true;
            }

            if (SEP_Present)
            {
                _partsWithScienceSEP = PartLoader.LoadedPartsList.Where(p => p.name.Contains("SEP.")).ToList();
                foreach (AvailablePart part in _partsWithScienceSEP)
                {
                    foreach (PartModule tmpPM in part.partPrefab.Modules)
                    {
                        string moduleName = tmpPM.moduleName;
                        if (moduleName == "ModuleSEPScienceExperiment")
                        {
                            string SEP_ID = tmpPM.Fields.GetValue("experimentID") + "_Basic";
                            ScienceExperiment experimentSEP = ResearchAndDevelopment.GetExperiment(SEP_ID) ?? new ScienceExperiment();
                            List<string> itemInfo = PrepareSituationAndBiomes(experimentSEP);
                            _usageMaskInt = "<b><color=red>SURFACE ONLY, handled by EVA</color></b>";
                            if (PrepareInfoDescriptionSE(part, "SEPScience Experiment", itemInfo)) return;
                        }
                    }
                }
            }

            _partsWithScience = PartLoader.LoadedPartsList.Where
                (p => p.partPrefab.Modules.GetModules<ModuleScienceExperiment>().Any()).ToList();
            foreach (AvailablePart part in _partsWithScience)
            {
                List<ModuleScienceExperiment> modules = part.partPrefab.Modules.GetModules<ModuleScienceExperiment>();
                foreach (ModuleScienceExperiment moduleScienceExperiment in modules)
                {
                    ScienceExperiment experiment = ResearchAndDevelopment.GetExperiment(moduleScienceExperiment.experimentID) ?? new ScienceExperiment();
                    List<string> itemInfo = PrepareSituationAndBiomes(experiment);
                    _usageMaskInt = SituationHelper.UsageMaskTemplates[moduleScienceExperiment.usageReqMaskInternal];
                    PrepareInfoDescription(part, moduleScienceExperiment, itemInfo);
                }
            }
        }

        private void Impact()
        {
            if (Impact_Present && !Impact_Present_X)
            {
                _partsWithScienceIMP = PartLoader.LoadedPartsList.Where(p => p.name.Contains("Impact")).ToList();
                foreach (AvailablePart part in _partsWithScienceIMP)
                {
                    ScienceExperiment experimentIMP = new ScienceExperiment();
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
                    List<string> itemInfo = PrepareSituationAndBiomes(experimentIMP);
                    AvailablePart.ModuleInfo d = new AvailablePart.ModuleInfo
                    {
                        moduleDisplayName = experimentIMP.experimentTitle,
                        info = GetInfo(itemInfo, _usageMaskInt)
                    };
                    part.moduleInfos.Add(d);
                }
                Impact_Present_X = true;
            }
        }

        private void RoverScience()
        {
            if (!ROV_Present || ROV_Present_X) return;
            _partsWithScienceROV = PartLoader.LoadedPartsList.Where(p => p.name.Contains("roverBrain")).ToList();
            foreach (AvailablePart part in _partsWithScienceROV)
            {
                ScienceExperiment experimentROV = ResearchAndDevelopment.GetExperiment("RoverScienceExperiment");
                List<string> itemInfo = PrepareSituationAndBiomes(experimentROV);
                _usageMaskInt = "<b><color=red>SURFACE ONLY, find a place</color></b>";
                AvailablePart.ModuleInfo d = new AvailablePart.ModuleInfo
                {
                    moduleDisplayName = experimentROV.experimentTitle,
                    info = GetInfo(itemInfo, _usageMaskInt)
                };
                part.moduleInfos.Add(d);
            }
            ROV_Present_X = true;
        }

        private List<string> PrepareSituationAndBiomes(ScienceExperiment experiment)
        {
            List<string> result = new List<string>();

            foreach (KeyValuePair<ExperimentSituations, Func<bool, string>> situationTemplate in SituationHelper.SituationTemplates)
            {
                uint situationMask = experiment.situationMask;
                if (ROV_Present && experiment.situationMask==64)
                {
                    situationMask = 1;
                }
                bool flag = (situationMask & (uint)situationTemplate.Key) == (uint)situationTemplate.Key; //получаем чтото если текущая ситуация совпадает с заявленной
                Func<bool, string> dictValue = situationTemplate.Value; // получили делегат
                string preparedInfo = dictValue(flag); // вызвали делегат
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
                List<AvailablePart.ModuleInfo> infos = part.moduleInfos;
                AvailablePart.ModuleInfo d = null;
                foreach (AvailablePart.ModuleInfo x in infos)
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

        private bool PrepareInfoDescriptionSE(AvailablePart part, string ExperimentDisplayName, List<string> itemInfo)
        {
            try
            {
                List<AvailablePart.ModuleInfo> infos = part.moduleInfos;
                AvailablePart.ModuleInfo d = null;
                foreach (AvailablePart.ModuleInfo i in infos)
                {
                    if (!i.moduleDisplayName.Contains(ExperimentDisplayName)) continue;
                    d = i;
                    break;
                }
                if (d == null) return true;
                d.info = string.Concat(d.info, "\n", GetInfo(itemInfo, _usageMaskInt));
            }
            catch (Exception ex)
            {
                Debug.Log("DennyTX: " + ex);
            }
            return false;
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
            foreach (AvailablePart part in _partsWithScience)
            {
                List<AvailablePart.ModuleInfo> moduleInfos = part.partPrefab.partInfo.moduleInfos;
                foreach (AvailablePart.ModuleInfo moduleInfo in moduleInfos)
                {
                    int startIndex = moduleInfo.info.IndexOf("--------------------------------", StringComparison.Ordinal);
                    if (startIndex < 0) continue;
                    moduleInfo.info = moduleInfo.info.Remove(startIndex - 1);
                }
            }

            if (SEP_Present)
            {
                foreach (AvailablePart part in _partsWithScienceSEP)
                {
                    List<AvailablePart.ModuleInfo> moduleInfos = part.partPrefab.partInfo.moduleInfos;
                    foreach (AvailablePart.ModuleInfo moduleInfo in moduleInfos)
                    {
                        int startIndex = moduleInfo.info.IndexOf("--------------------------------", StringComparison.Ordinal);
                        if (startIndex < 0) continue;
                        moduleInfo.info = moduleInfo.info.Remove(startIndex - 1);
                    }
                }
            }
        }
    }
}

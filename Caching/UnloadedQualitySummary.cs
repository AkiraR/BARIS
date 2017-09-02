﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;
using FinePrint;
#if !KSP122
using KSP.Localization;
#endif

/*
Source code copyrighgt 2017, by Michael Billard (Angel-125)
License: GNU General Public License Version 3
License URL: http://www.gnu.org/licenses/
If you want to use this code, give me a shout on the KSP forums! :)
Wild Blue Industries is trademarked by Michael Billard and may be used for non-commercial purposes. All other rights reserved.
Note that Wild Blue Industries is a ficticious entity 
created for entertainment purposes. It is in no way meant to represent a real entity.
Any similarity to a real entity is purely coincidental.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
namespace WildBlueIndustries
{
    public class UnloadedQualitySummary
    {
        public Vessel vessel;
        public ProtoPartModuleSnapshot[] qualityModuleSnapshots;
        public ProtoCrewMember rankingAstronaut;
        public int highestRank;
        public int reliability;

        protected void debugLog(string message)
        {
            if (BARISScenario.showDebug == true)
                Debug.Log("[UnloadedQualitySummary] - " + message);
        }

        public ProtoPartModuleSnapshot[] UpdateAndGetFailureCandidates(float mtbfDecrement)
        {
            int currentQuality = 0;
            int totalQuality = 0;
            double currentMTBF = 0;
            bool isActivated = false;
            int breakableModuleCount = 0;
            List<ProtoPartModuleSnapshot> failureCandidates = new List<ProtoPartModuleSnapshot>();
            ProtoPartModuleSnapshot qualityModule;
            float mtbfHibernationFactor;

            debugLog("UpdateAndGetFailureCandidates called for " + vessel.vesselName);
            debugLog("qualityModuleSnapshots count: " + qualityModuleSnapshots.Length);
            for (int index = 0; index < qualityModuleSnapshots.Length; index++)
            {
                qualityModule = qualityModuleSnapshots[index];

                //Get current mtbf, isActivated state, and number of part modules available to break;
                currentMTBF = double.Parse(qualityModule.moduleValues.GetValue("currentMTBF"));
                isActivated = bool.Parse(qualityModule.moduleValues.GetValue("isActivated"));
                mtbfHibernationFactor = float.Parse(qualityModule.moduleValues.GetValue("mtbfHibernationFactor"));
                breakableModuleCount = int.Parse(qualityModule.moduleValues.GetValue("breakableModuleCount"));

                if (currentMTBF > 0 && mtbfDecrement > 0)
                {
                    //Decrement the MTBF
                    if (isActivated)
                        currentMTBF -= mtbfDecrement;
                    else
                        currentMTBF -= (mtbfDecrement * mtbfHibernationFactor);
                    if (currentMTBF < 0)
                        currentMTBF = 0;

                    //Set adjusted MTBF
                    qualityModule.moduleValues.SetValue("currentMTBF", currentMTBF);
                }

                //Add to the list of failure candidates
                //It has to be activated, its breakable modules > 0, and it is out of MTBF.
                if (isActivated && breakableModuleCount > 0 && currentMTBF <= 0)
                    failureCandidates.Add(qualityModule);

                //Now get the quality
                currentQuality = int.Parse(qualityModule.moduleValues.GetValue("currentQuality"));
                totalQuality += currentQuality;
            }

            //Update reliability
            int maxQuality = 100 * qualityModuleSnapshots.Length;
            reliability = Mathf.RoundToInt(((float)totalQuality / (float)maxQuality) * 100.0f);
            debugLog("reliability: " + reliability);

            //Return the failure candidates
            if (failureCandidates.Count > 0)
                return failureCandidates.ToArray();
            else
                return null;
        }

        public int UpdateSafetyRating()
        {
            int currentQuality = 0;
            int totalQuality = 0;

            for (int index = 0; index < qualityModuleSnapshots.Length; index++)
            {
                currentQuality = int.Parse(qualityModuleSnapshots[index].moduleValues.GetValue("currentQuality"));
                totalQuality += currentQuality;
            }

            reliability = (int)Mathf.Round(totalQuality / (100 * qualityModuleSnapshots.Length));
            return reliability;
        }

        public int UpdateHighestRank()
        {
            string qualityCheckSkill = string.Empty;
            int skillRank = 0;
            ProtoCrewMember astronaut = null;

            for (int index = 0; index < qualityModuleSnapshots.Length; index++)
            {
                qualityCheckSkill = qualityModuleSnapshots[index].moduleValues.GetValue("qualityCheckSkill");
                skillRank = BARISScenario.Instance.GetHighestRank(vessel, qualityCheckSkill, out astronaut);
                if (skillRank > highestRank)
                {
                    highestRank = skillRank;
                    rankingAstronaut = astronaut;
                }
            }

            return highestRank;
        }
    }
}

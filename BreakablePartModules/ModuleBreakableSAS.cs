﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;
using FinePrint;
using KSP.UI.Screens;
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
    /// <summary>
    /// ModuleBreakableSAS is designed to interface with ModuleSAS, ModuleReactionWheel, and ModuleQualityControl. When a part is declared broken, it disables the reaction wheel and sas, and when the part is declared fixed,
    /// it re-enables them again. Almost all of the functionality is internal; all the part config designer needs to do is make sure that ModuleBreakableSAS appears after ModuleSAS and ModuleReactionWheel, and ModuleQualityControl
    /// appears after ModuleBreakableSAS.
    /// In addition to the MTBF checks done over time, a quality check occurs whenever SAS is activate.
    /// </summary>
    public class ModuleBreakableSAS : PartModule, ICanBreak
    {
        ModuleSAS moduleSAS;
        ModuleReactionWheel reactionWheel;
        BaseQualityControl qualityControl;

        /// <summary>
        /// What skill to use when performing the quality check. This is not always the same skill required to repair or maintain the part.
        /// </summary>
        [KSPField()]
        public string qualityCheckSkill = "RepairSkill";

        /// <summary>
        /// Flag to indicate whether or not the part is active. It is based upon an event fired by BARISScenario. If set to true, then the part will be included in vessel reliability checks.
        /// </summary>
        [KSPField(isPersistant = true)]
        public bool IsActive;

        /// <summary>
        /// Field to indicate that the part is broken. Used to disable the module after loading a saved game.
        /// </summary>
        [KSPField(isPersistant = true)]
        public bool isBroken;

        protected void debugLog(string message)
        {
            if (BARISScenario.showDebug == true)
                Debug.Log("[" + this.ClassName + "] - " + message);
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            moduleSAS = this.part.FindModuleImplementing<ModuleSAS>();
            reactionWheel = this.part.FindModuleImplementing<ModuleReactionWheel>();
        }

        public void Destroy()
        {
            BARISScenario.Instance.onSasUpdate -= onSASUpdate;
            qualityControl.onUpdateSettings -= onUpdateSettings;
            qualityControl.onPartBroken -= OnPartBroken;
            qualityControl.onPartFixed -= OnPartFixed;
        }

        protected void onSASUpdate(bool sasIsActive)
        {
            if (isBroken)
            {
                IsActive = false;
                return;
            }

            IsActive = sasIsActive;

            qualityControl.UpdateActivationState();
            qualityControl.PerformQualityCheck();
        }

        #region ICanBreak
        public string GetCheckSkill()
        {
            return qualityCheckSkill;
        }

        public bool ModuleIsActivated()
        {
            if (!BARISBreakableParts.CrewedPartsCanFail && this.part.CrewCapacity > 0)
                return false;
            if (!BARISBreakableParts.CommandPodsCanFail && this.part.FindModuleImplementing<ModuleCommand>() != null)
                return false;

            if (!BARISBreakableParts.SASCanFail)
                return false;

            return IsActive;
        }

        public void SubscribeToEvents(BaseQualityControl moduleQualityControl)
        {
            debugLog("SubscribeToEvents");
            qualityControl = moduleQualityControl;
            qualityControl.onUpdateSettings += onUpdateSettings;
            qualityControl.onPartBroken += OnPartBroken;
            qualityControl.onPartFixed += OnPartFixed;

            //Handle persistence case for broken part.
            if (isBroken)
                OnPartBroken(qualityControl);
        }

        protected void onUpdateSettings(BaseQualityControl moduleQualityControl)
        {
            //Quality check events
            if (BARISSettings.PartsCanBreak && BARISBreakableParts.SASCanFail)
            {
                BARISScenario.Instance.onSasUpdate += onSASUpdate;
            }

            else
            {
                BARISScenario.Instance.onSasUpdate -= onSASUpdate;
            }
        }

        public void OnPartBroken(BaseQualityControl moduleQualityControl)
        {
            if (!BARISSettings.PartsCanBreak || !BARISBreakableParts.SASCanFail)
                return;

            isBroken = true;
            if (moduleSAS != null)
            {
                moduleSAS.enabled = false;
                moduleSAS.isEnabled = false;
            }

            if (reactionWheel != null)
            {
                reactionWheel.enabled = false;
                reactionWheel.isEnabled = false;
            }

            if (this.part.vessel == FlightGlobals.ActiveVessel)
            {
                string message = Localizer.Format(this.part.partInfo.title + BARISScenario.SASBroken);
                BARISScenario.Instance.LogPlayerMessage(message);
            }
            qualityControl.UpdateQualityDisplay(qualityControl.qualityDisplay + Localizer.Format(BARISScenario.SASLabel));
        }

        public void OnPartFixed(BaseQualityControl moduleQualityControl)
        {
            isBroken = false;
            if (moduleSAS != null)
            {
                moduleSAS.enabled = true;
                moduleSAS.isEnabled = true;
            }

            if (reactionWheel != null)
            {
                reactionWheel.enabled = true;
                reactionWheel.isEnabled = true;
            }
        }
        #endregion
    }
}

﻿/* Copyright (c) Citrix Systems, Inc. 
 * All rights reserved. 
 * 
 * Redistribution and use in source and binary forms, 
 * with or without modification, are permitted provided 
 * that the following conditions are met: 
 * 
 * *   Redistributions of source code must retain the above 
 *     copyright notice, this list of conditions and the 
 *     following disclaimer. 
 * *   Redistributions in binary form must reproduce the above 
 *     copyright notice, this list of conditions and the 
 *     following disclaimer in the documentation and/or other 
 *     materials provided with the distribution. 
 * 
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND 
 * CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, 
 * INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF 
 * MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE 
 * DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR 
 * CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, 
 * SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, 
 * BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR 
 * SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS 
 * INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, 
 * WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING 
 * NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE 
 * OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF 
 * SUCH DAMAGE.
 */

using System;
using XenAdmin.Core;
using XenAdmin.Network;
using XenAPI;

namespace XenAdmin.Alerts.Types
{
    public class HotfixEligibilityAlert: Alert
    {
        private readonly Pool pool;
        public readonly XenServerVersion Version;

        public HotfixEligibilityAlert(IXenConnection connection, XenServerVersion version)
        {
            Connection = connection;
            this.Version = version;
            pool = Helpers.GetPoolOfOne(connection);
            _timestamp = DateTime.Now;
         }

        #region Overrides of Alert

        public override string Title
        {
            get
            {
                if (pool == null || Version == null)
                    return string.Empty;

                var productVersionText = string.Format(Messages.STRING_SPACE_STRING,
                    Helpers.NaplesOrGreater(Connection) ? Messages.XENSERVER : Messages.XENSERVER_LEGACY,
                    Helpers.GetMaster(Connection)?.ProductVersionText());
                var unlicensed = pool.IsFreeLicenseOrExpired();

                switch (Version.HotfixEligibility)
                {
                    case hotfix_eligibility.all when Version.EolDate != DateTime.MinValue:
                        return string.Format(Messages.HOTFIX_ELIGIBILITY_ALERT_TITLE_APPROACHING_EOL, productVersionText, HelpersGUI.DateTimeToString(Version.EolDate.ToLocalTime(), Messages.DATEFORMAT_DMY, true));

                    case hotfix_eligibility.premium when unlicensed:
                        return string.Format(Messages.HOTFIX_ELIGIBILITY_ALERT_TITLE_FREE, productVersionText);
                    case hotfix_eligibility.premium when Version.EolDate != DateTime.MinValue:
                        return string.Format(Messages.HOTFIX_ELIGIBILITY_ALERT_TITLE_APPROACHING_EOL, productVersionText, HelpersGUI.DateTimeToString(Version.EolDate.ToLocalTime(), Messages.DATEFORMAT_DMY, true));

                    case hotfix_eligibility.cu when pool.IsFreeLicenseOrExpired():
                        return string.Format(Messages.HOTFIX_ELIGIBILITY_ALERT_TITLE_FREE, productVersionText);
                    case hotfix_eligibility.cu:
                        return Messages.HOTFIX_ELIGIBILITY_ALERT_TITLE_CU;

                    case hotfix_eligibility.none:
                        return string.Format(Messages.HOTFIX_ELIGIBILITY_ALERT_TITLE_EOL, productVersionText);
                    default:
                        return string.Empty;
                }
            }
        }

        public override string Description
        {
            get
            {
                if (pool == null)
                    return string.Empty;

                var versionText = Helpers.GetMaster(Connection)?.ProductVersionText();
                var productVersionText = string.Format(Messages.STRING_SPACE_STRING, 
                    Helpers.NaplesOrGreater(Connection) ? Messages.XENSERVER : Messages.XENSERVER_LEGACY,
                    versionText);
                var unlicensed = pool.IsFreeLicenseOrExpired();

                switch (Version.HotfixEligibility)
                {
                    //all
                    case hotfix_eligibility.all when unlicensed && Version.EolDate != DateTime.MinValue:
                        return string.Format(Messages.HOTFIX_ELIGIBILITY_ALERT_DESCRIPTION_APPROACHING_EOL_FREE, productVersionText, HelpersGUI.DateTimeToString(Version.EolDate.ToLocalTime(), Messages.DATEFORMAT_DMY, true), versionText);
                    case hotfix_eligibility.all when Version.EolDate != DateTime.MinValue:
                        return string.Format(Messages.HOTFIX_ELIGIBILITY_ALERT_DESCRIPTION_APPROACHING_EOL, productVersionText, HelpersGUI.DateTimeToString(Version.EolDate.ToLocalTime(), Messages.DATEFORMAT_DMY, true), versionText);

                    // premium
                    case hotfix_eligibility.premium when unlicensed && Version.HotfixEligibilityPremiumDate != DateTime.MinValue: 
                        return string.Format(Messages.HOTFIX_ELIGIBILITY_ALERT_DESCRIPTION_FREE, productVersionText, HelpersGUI.DateTimeToString(Version.HotfixEligibilityPremiumDate.ToLocalTime(), Messages.DATEFORMAT_DMY, true));
                    case hotfix_eligibility.premium when !unlicensed && Version.EolDate != DateTime.MinValue:
                        return string.Format(Messages.HOTFIX_ELIGIBILITY_ALERT_DESCRIPTION_APPROACHING_EOL, productVersionText, HelpersGUI.DateTimeToString(Version.EolDate.ToLocalTime(), Messages.DATEFORMAT_DMY, true), versionText);

                    // cu
                    case hotfix_eligibility.cu when unlicensed && Version.HotfixEligibilityPremiumDate != DateTime.MinValue:
                        return string.Format(Messages.HOTFIX_ELIGIBILITY_ALERT_DESCRIPTION_FREE, productVersionText, HelpersGUI.DateTimeToString(Version.HotfixEligibilityPremiumDate.ToLocalTime(), Messages.DATEFORMAT_DMY, true));
                    case hotfix_eligibility.cu when !unlicensed && Version.HotfixEligibilityNoneDate != DateTime.MinValue:
                        return string.Format(Messages.HOTFIX_ELIGIBILITY_ALERT_DESCRIPTION_CU, productVersionText, HelpersGUI.DateTimeToString(Version.HotfixEligibilityNoneDate.ToLocalTime(), Messages.DATEFORMAT_DMY, true), versionText);

                    // none
                    case hotfix_eligibility.none when unlicensed && Version.EolDate != DateTime.MinValue:
                        return string.Format(Messages.HOTFIX_ELIGIBILITY_ALERT_DESCRIPTION_EOL_FREE, productVersionText, HelpersGUI.DateTimeToString(Version.EolDate.ToLocalTime(), Messages.DATEFORMAT_DMY, true));
                    case hotfix_eligibility.none when Version.EolDate != DateTime.MinValue:
                        return string.Format(Messages.HOTFIX_ELIGIBILITY_ALERT_DESCRIPTION_EOL, productVersionText, HelpersGUI.DateTimeToString(Version.EolDate.ToLocalTime(), Messages.DATEFORMAT_DMY, true));

                    // default
                    default:
                        return string.Empty;
                }
            }
        }

        public override AlertPriority Priority => AlertPriority.Priority3;

        public override string AppliesTo => Helpers.GetName(Helpers.GetPoolOfOne(Connection));

        public override string FixLinkText => null;

        public override Action FixLinkAction => null;

        public override string HelpID => "HotfixEligibilityAlert";
        
        public override bool Equals(Alert other)
        {
            if (other is HotfixEligibilityAlert alert)
                return Connection == alert.Connection && Version == alert.Version;
            return base.Equals(other);
        }
        #endregion

        public static bool IsAlertNeeded(hotfix_eligibility hotfixEligibility, XenServerVersion version, bool licensed)
        {
            if (version == null)
                return false;

            if (hotfixEligibility == hotfix_eligibility.all && version.EolDate == DateTime.MinValue ||
                hotfixEligibility == hotfix_eligibility.premium && licensed && version.EolDate == DateTime.MinValue)
                return false;
            return true;
        }
    }
}
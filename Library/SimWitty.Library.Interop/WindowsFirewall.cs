// <copyright file="WindowsFirewall.cs" company="SimWitty (http://www.simwitty.org)">
//     Copyright © 2013 and distributed under the BSD license.
// </copyright>

namespace SimWitty.Library.Interop
{
    using System;
    using NetFwTypeLib; // COM reference

    /// <summary>
    /// Interacting with the Windows Firewall via COM interop
    /// </summary>
    public static class WindowsFirewall
    {
        /// <summary>
        /// Is the Windows firewall enabled? 
        /// </summary>
        /// <returns>Returns True if enabled, or false if disabled.</returns>
        public static bool IsEnabled()
        {
            INetFwPolicy2 policy = GetCurrentPolicy();
            NET_FW_PROFILE_TYPE2_ currentProfileTypes;
            currentProfileTypes = (NET_FW_PROFILE_TYPE2_)policy.CurrentProfileTypes;
            return policy.get_FirewallEnabled(currentProfileTypes);
        }

        /// <summary>
        /// Set the firewall status (enabled or disabled).
        /// </summary>
        /// <param name="enable">True to enable, or false to disable.</param>
        public static void SetFirewall(bool enable)
        {
            NET_FW_PROFILE_TYPE2_ currentProfileTypes;
            INetFwPolicy2 policy = GetCurrentPolicy();
            currentProfileTypes = (NET_FW_PROFILE_TYPE2_)policy.CurrentProfileTypes;
            policy.set_FirewallEnabled(currentProfileTypes, enable);
        }

        /// <summary>
        /// Check that the operating system is Windows 2008 R2 or higher.
        /// </summary>
        private static void CheckOperatingSystem()
        {
            string error = "The firewall functions do not support legacy operating systems. Please use Windows 7, Windows 8, Windows Server 2008 R2, or Windows Server 2012.";
            Version windows = Environment.OSVersion.Version;
            if (windows.Major < 6) throw new ApplicationException(error);
            if (windows.Minor < 1) throw new ApplicationException(error);
        }

        /// <summary>
        /// Get the current firewall policy object.
        /// </summary>
        /// <returns>Returns the current firewall policy.</returns>
        private static INetFwPolicy2 GetCurrentPolicy()
        {
            Type firewall = Type.GetTypeFromProgID("HNetCfg.FwPolicy2");
            INetFwPolicy2 policy = (INetFwPolicy2)Activator.CreateInstance(firewall);
            return policy;
        }
    }
}

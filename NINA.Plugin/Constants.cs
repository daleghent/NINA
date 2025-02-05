﻿#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Utility;
using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace NINA.Plugin {

    public static class Constants {
        internal static readonly Version ApplicationVersion = new Version(CoreUtil.Version);
        internal static readonly string ApplicationVersionWithoutRevision = GetPluginMinimumApplicationVersionWithoutRevision();
        public static readonly string UserExtensionsFolder = Path.Combine(CoreUtil.APPLICATIONTEMPPATH, "Plugins", ApplicationVersionWithoutRevision);
        public static readonly string StagingFolder = Path.Combine(CoreUtil.APPLICATIONTEMPPATH, "PluginStaging", ApplicationVersionWithoutRevision);
        public static readonly string DeletionFolder = Path.Combine(CoreUtil.APPLICATIONTEMPPATH, "PluginDeletion", ApplicationVersionWithoutRevision);
        public static readonly string BaseUserExtensionsFolder = Path.Combine(CoreUtil.APPLICATIONTEMPPATH, "Plugins");
        public static readonly string BaseStagingFolder = Path.Combine(CoreUtil.APPLICATIONTEMPPATH, "PluginStaging");
        public static readonly string BaseDeletionFolder = Path.Combine(CoreUtil.APPLICATIONTEMPPATH, "PluginDeletion");

        public static readonly string MainPluginRepository = "https://nighttime-imaging.eu/wp-json/nina/v1";

        private static string GetPluginMinimumApplicationVersionWithoutRevision() {
            var assembly = typeof(PluginCompatibilityMap).Assembly;
            var attribute = assembly.GetCustomAttributes<AssemblyMetadataAttribute>().FirstOrDefault(x => x.Key == "PluginMinimumApplicationVersion");
            var version = new Version(attribute?.Value ?? "3.0.0");
            return $"{version.Major}.{version.Minor}.{version.Build}";
        }
    }
}
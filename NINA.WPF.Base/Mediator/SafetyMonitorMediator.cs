﻿#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Interfaces;
using NINA.Equipment.Equipment.MySafetyMonitor;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Equipment.Interfaces.ViewModel;
using System;

namespace NINA.WPF.Base.Mediator {

    public class SafetyMonitorMediator : DeviceMediator<ISafetyMonitorVM, ISafetyMonitorConsumer, SafetyMonitorInfo>, ISafetyMonitorMediator {
        public event EventHandler<IsSafeEventArgs> IsSafeChanged {
            add { this.handler.IsSafeChanged += value; }
            remove { this.handler.IsSafeChanged -= value; }
        }
    }
}
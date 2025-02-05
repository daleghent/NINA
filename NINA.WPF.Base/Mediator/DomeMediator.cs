﻿#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Astrometry;
using NINA.Core.Enum;
using NINA.Equipment.Equipment.MyDome;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Equipment.Interfaces.ViewModel;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.WPF.Base.Mediator {

    public class DomeMediator : DeviceMediator<IDomeVM, IDomeConsumer, DomeInfo>, IDomeMediator {
        public bool IsFollowingScope => handler.FollowEnabled;

        public Task<bool> OpenShutter(CancellationToken cancellationToken) {
            return handler.OpenShutter(cancellationToken);
        }

        public Task<bool> EnableFollowing(CancellationToken cancellationToken) {
            return handler.EnableFollowing(cancellationToken);
        }

        public Task WaitForDomeSynchronization(CancellationToken cancellationToken) {
            return handler.WaitForDomeSynchronization(cancellationToken);
        }

        public Task<bool> CloseShutter(CancellationToken cancellationToken) {
            return handler.CloseShutter(cancellationToken);
        }

        public Task<bool> Park(CancellationToken cancellationToken) {
            return handler.Park(cancellationToken);
        }

        public Task<bool> FindHome(CancellationToken cancellationToken) {
            return handler.FindHome(cancellationToken);
        }

        public Task<bool> DisableFollowing(CancellationToken cancellationToken) {
            return handler.DisableFollowing(cancellationToken);
        }

        public Task<bool> SlewToAzimuth(double degrees, CancellationToken cancellationToken) {
            return handler.SlewToAzimuth(degrees, cancellationToken);
        }

        public Task<bool> SyncToScopeCoordinates(Coordinates coordinates, PierSide sideOfPier, CancellationToken cancellationToken) {
            return handler.SyncToScopeCoordinates(coordinates, sideOfPier, cancellationToken);
        }
        public event EventHandler<EventArgs> Synced {
            add { this.handler.Synced += value; }
            remove { this.handler.Synced -= value; }
        }
        public event Func<object, EventArgs, Task> Opened {
            add { this.handler.Opened += value; }
            remove { this.handler.Opened -= value; }
        }
        public event Func<object, EventArgs, Task> Closed {
            add { this.handler.Closed += value; }
            remove { this.handler.Closed -= value; }
        }
        public event Func<object, EventArgs, Task> Parked {
            add { this.handler.Parked += value; }
            remove { this.handler.Parked -= value; }
        }
        public event Func<object, EventArgs, Task> Homed {
            add { this.handler.Homed += value; }
            remove { this.handler.Homed -= value; }
        }
        public event Func<object, DomeEventArgs, Task> Slewed {
            add { this.handler.Slewed += value; }
            remove { this.handler.Slewed -= value; }
        }
    }
}
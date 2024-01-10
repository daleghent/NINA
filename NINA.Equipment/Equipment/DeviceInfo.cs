#region "copyright"

/*
    Copyright � 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Utility;
using System;
using System.Linq;
using System.Reflection;

namespace NINA.Equipment.Equipment {

    public class DeviceInfo : BaseINPC {
        private bool connected;
        public bool Connected {
            get => connected;
            set {
                if (connected != value) {
                    connected = value;
                    RaisePropertyChanged();
                }
            }
        }

        private string name;
        public string Name { get => name; set { name = value; RaisePropertyChanged(); } }

        private string description;

        public string Description {
            get => description;
            set {
                if (description != value) {
                    description = value;
                    RaisePropertyChanged();
                }
            }
        }

        private string driverInfo;

        public string DriverInfo {
            get => driverInfo;
            set {
                if (driverInfo != value) {
                    driverInfo = value;
                    RaisePropertyChanged();
                }
            }
        }

        private string driverVersion;

        public string DriverVersion {
            get => driverVersion;
            set {
                if (driverVersion != value) {
                    driverVersion = value;
                    RaisePropertyChanged();
                }
            }
        }

        private string deviceId;

        public string DeviceId {
            get => deviceId;
            set {
                if (deviceId != value) {
                    deviceId = value;
                    RaisePropertyChanged();
                }
            }
        }

        public static T CreateDefaultInstance<T>() where T : DeviceInfo, new() {
            return new T() {
                Connected = false
            };
        }

        public void Reset() {
            var defaultInstance = Activator.CreateInstance(this.GetType()) as DeviceInfo;
            defaultInstance.Connected = false;
            this.CopyFrom(defaultInstance);
        }

        public void CopyFrom(DeviceInfo other) {
            foreach (PropertyInfo property in this.GetType().GetProperties().Where(p => p.CanWrite)) {
                property.SetValue(this, property.GetValue(other, null), null);
            }
        }
    }
}
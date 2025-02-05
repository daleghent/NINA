#region "copyright"

/*
    Copyright � 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System;
using System.Runtime.Serialization;

namespace NINA.Equipment.Exceptions {

    [Serializable]
    public class GnssInvalidHostException : Exception {

        public GnssInvalidHostException() {
        }

        public GnssInvalidHostException(string message) : base(message) {
        }

        public GnssInvalidHostException(string message, Exception innerException) : base(message, innerException) {
        }

        protected GnssInvalidHostException(SerializationInfo info, StreamingContext context) : base(info, context) {
        }
    }
}
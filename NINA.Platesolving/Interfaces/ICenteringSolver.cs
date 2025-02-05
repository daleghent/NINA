#region "copyright"

/*
    Copyright � 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Model;
using NINA.Equipment.Model;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.PlateSolving.Interfaces {

    public interface ICenteringSolver {
        ICaptureSolver CaptureSolver { get; set; }

        Task<PlateSolveResult> Center(CaptureSequence seq, CenterSolveParameter parameter, IProgress<PlateSolveProgress> solveProgress, IProgress<ApplicationStatus> progress, CancellationToken ct);
        Task<CenteringSolveResult> CenterWithMeasurements(CaptureSequence seq, CenterSolveParameter parameter, IProgress<PlateSolveProgress> solveProgress, IProgress<ApplicationStatus> progress, CancellationToken ct);
    }
}
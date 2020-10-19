﻿#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Model;
using NINA.Sequencer.Container;
using NINA.Sequencer.DragDrop;
using NINA.Sequencer.SequenceItem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;

namespace NINA.Sequencer.Trigger {

    public interface ISequenceTrigger : ISequenceEntity {

        /// <summary>
        /// After each Sequence Item was processed this method will be called to determin if the trigger should be executed
        /// </summary>
        /// <param name="nextItem"></param>
        /// <returns></returns>
        bool ShouldTrigger(ISequenceItem nextItem);

        /// <summary>
        /// Will be called on sequence start for the trigger to set some initial parameters if requried
        /// </summary>
        void Initialize();

        /// <summary>
        /// Runs the actual trigger logic
        /// </summary>
        /// <param name="context">The container of the next sequence item to be processed. As triggers get called in a cascade the item might be in a child container.</param>
        /// <param name="progress"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task Run(ISequenceContainer context, IProgress<ApplicationStatus> progress, CancellationToken token);
    }
}
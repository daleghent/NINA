﻿#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using Newtonsoft.Json;
using NINA.Model;
using NINA.Sequencer.Exceptions;
using NINA.Sequencer.Validations;
using NINA.Utility.Mediator.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Sequencer.SequenceItem.Guider {

    [ExportMetadata("Name", "Lbl_SequenceItem_Guider_StopGuiding_Name")]
    [ExportMetadata("Description", "Lbl_SequenceItem_Guider_StopGuiding_Description")]
    [ExportMetadata("Icon", "StopGuiderSVG")]
    [ExportMetadata("Category", "Lbl_SequenceCategory_Guider")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]
    public class StopGuiding : SequenceItem, IValidatable {
        private IGuiderMediator guiderMediator;

        [ImportingConstructor]
        public StopGuiding(IGuiderMediator guiderMediator) {
            this.guiderMediator = guiderMediator;
        }

        private IList<string> issues = new List<string>();

        public IList<string> Issues {
            get => issues;
            set {
                issues = value;
                RaisePropertyChanged();
            }
        }

        public override Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            if (Validate()) {
                //todo missing progress
                return guiderMediator.StopGuiding(token);
            } else {
                throw new SequenceItemSkippedException(string.Join(",", Issues));
            }
        }

        public override object Clone() {
            return new StopGuiding(guiderMediator) {
                Icon = Icon,
                Name = Name,
                Description = Description,
            };
        }

        public bool Validate() {
            var i = new List<string>();
            if (!guiderMediator.GetInfo().Connected) {
                i.Add(Locale.Loc.Instance["LblGuiderNotConnected"]);
            }
            Issues = i;
            return i.Count == 0;
        }

        public override void AfterParentChanged() {
            Validate();
        }

        public override string ToString() {
            return $"Category: {Category}, Item: {nameof(StopGuiding)}";
        }
    }
}
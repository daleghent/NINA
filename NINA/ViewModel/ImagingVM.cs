﻿using NINA.Model;
using NINA.Model.MyCamera;
using NINA.Utility;
using nom.tam.fits;
using nom.tam.util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using static NINA.Model.SequenceModel;
using System.ComponentModel;

namespace NINA.ViewModel {
    class ImagingVM : ChildVM {

        public ImagingVM(ApplicationVM root) : base(root) {
            Name = "Imaging";
            ImageGeometry = (System.Windows.Media.GeometryGroup)System.Windows.Application.Current.Resources["ImagingSVG"];

            SnapExposureDuration = 1;
            SnapCommand = new AsyncCommand<bool>(() => CaptureImage(new Progress<string>(p => RootVM.Status = p)));
            CancelSnapCommand = new RelayCommand(CancelCaptureImage);
            StartSequenceCommand = new AsyncCommand<bool>(() => StartSequence(new Progress<string>(p => RootVM.Status = p)));
            CancelSequenceCommand = new RelayCommand(CancelSequence);

            ImageControl = new ImageControlVM();
        }

        private void CameraVM_PropertyChanged(object sender, PropertyChangedEventArgs e) {
            if (e.PropertyName == "Cam") {
                RaisePropertyChanged(e.PropertyName);
            }
        }

        public PHD2Client PHD2Client {
            get {
                return Utility.Utility.PHDClient;
            }
        }

        private Dispatcher _dispatcher = Dispatcher.CurrentDispatcher;

        private SequenceVM _seqVM;
        public SequenceVM SeqVM {
            get {
                if(_seqVM == null) {
                    _seqVM = new SequenceVM();
                }
                return _seqVM;
            }
            set {
                _seqVM = value;
                RaisePropertyChanged();
            }
        }

        private CameraVM _cameraVM;
        public CameraVM CameraVM {
            get {
                return _cameraVM;
            } set {
                _cameraVM = value;
                CameraVM.PropertyChanged += CameraVM_PropertyChanged;
                RaisePropertyChanged();
            }
        }

        private bool _loop;
        public bool Loop {
            get {
                return _loop;
            }
            set {
                _loop = value;
                RaisePropertyChanged();
            }

        }

        public ICamera Cam {
            get {
                return CameraVM.Cam;
            }
        }

        
        public FilterWheelModel FW {
            get {
                return CameraVM.FilterWheelVM.FW;
            }
        }



        private double _snapExposureDuration;
        public double SnapExposureDuration {
            get {
                return _snapExposureDuration;
            }

            set {
                _snapExposureDuration = value;
                RaisePropertyChanged();
            }
        }

        private int _exposureSeconds; 
        public int ExposureSeconds {
            get {
                return _exposureSeconds;
            }
            set {
                _exposureSeconds = value;
                RaisePropertyChanged();
            }
        }

        private String _expStatus;
        public String ExpStatus {
            get {
                return _expStatus;
            }

            set {
                _expStatus = value;                
                RaisePropertyChanged();
            }
        }

        private bool _isExposing; 
        public bool IsExposing {
            get {
                return _isExposing;
            } set {
                _isExposing = value;
                RaisePropertyChanged();
            }
        }
        
        private IAsyncCommand _snapCommand;
        public IAsyncCommand SnapCommand {
            get {
                return _snapCommand;
            }
            set {
                _snapCommand = value;
                RaisePropertyChanged();
            }
        }

        private IAsyncCommand _startSequenceCommand;
        public IAsyncCommand StartSequenceCommand {
            get {
                return _startSequenceCommand;
            }
            set {
                _startSequenceCommand = value;
                RaisePropertyChanged();
            }
        }

        

        private async Task ChangeFilter(SequenceModel seq, CancellationTokenSource tokenSource, IProgress<string> progress) {
            if (seq.FilterType != null && FW.Connected && FW.Position != seq.FilterType.Position) {
                await _dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() => {
                    FW.Position = seq.FilterType.Position;
                }));


                progress.Report(ExposureStatus.FILTERCHANGE);
                await Task.Run(() => {
                    while (FW.Position == -1) {
                        //Wait for filter change;                        
                        tokenSource.Token.ThrowIfCancellationRequested();
                    }
                });
                tokenSource.Token.ThrowIfCancellationRequested();                
            }
        }

        private void SetBinning(SequenceModel seq) {
            if (seq.Binning == null) {
                Cam.SetBinning(1, 1);
            }
            else {
                Cam.SetBinning(seq.Binning.X, seq.Binning.Y);
            }
        }

        private async Task Capture(SequenceModel seq, CancellationTokenSource tokenSource, IProgress<string> progress) {            
            double duration = seq.ExposureTime;
            progress.Report(string.Format(ExposureStatus.EXPOSING, 0, duration));
            bool isLight = false;
            if (Cam.HasShutter) {
                isLight = true;
            }
            Cam.StartExposure(duration, isLight);
            ExposureSeconds = 1;
            progress.Report(string.Format(ExposureStatus.EXPOSING, 1, duration));
            /* Wait for Capture */
            if (duration >= 1) {
                await Task.Run(async () => {
                    do {
                        await Task.Delay(1000, tokenSource.Token);
                        tokenSource.Token.ThrowIfCancellationRequested();
                        ExposureSeconds += 1;
                        progress.Report(string.Format(ExposureStatus.EXPOSING, ExposureSeconds, duration));
                    } while ((ExposureSeconds < duration) && Cam.Connected);
                });
            }
            tokenSource.Token.ThrowIfCancellationRequested();
        }

        private async Task<Array> Download(CancellationTokenSource tokenSource, IProgress<string> progress) {
            progress.Report(ExposureStatus.DOWNLOADING);
            return await Cam.DownloadExposure(tokenSource);
        }

        



        private async Task<bool> Save(SequenceModel seq, ushort framenr,  CancellationTokenSource tokenSource, IProgress<string> progress) {
            progress.Report(ExposureStatus.SAVING);

            var filter = string.Empty;
            if (FW.Filters != null) {
                filter = FW.Filters.ElementAt(FW.Position).Name;            
            }

            await ImageControl.SaveToDisk(seq.ExposureTime, filter, seq.ImageType, seq.Binning.Name, Cam.CCDTemperature, framenr, tokenSource, progress);
                        
            return true;
        }

        private async Task<bool> Dither(SequenceModel seq, CancellationTokenSource tokenSource, IProgress<string> progress) {
            if (seq.Dither && ((seq.ExposureCount % seq.DitherAmount) == 0)) {
                progress.Report(ExposureStatus.DITHERING);
                await PHD2Client.Dither();

                progress.Report(ExposureStatus.SETTLING);
                var time = 0;
                await Task.Run<bool>(async () => {
                    while (PHD2Client.IsDithering) {                        
                        await Task.Delay(100);
                        time += 100;

                        if(time > 20000) {
                            //Failsafe when phd is not sending settlingdone message
                            Notification.ShowWarning("PHD2 did not send SettleDone message in time. Skipping settle manually.", ToastNotifications.NotificationsSource.NeverEndingNotification);
                            PHD2Client.IsDithering = false;
                        }
                        tokenSource.Token.ThrowIfCancellationRequested();
                    }
                    return true;
                });
            }
            tokenSource.Token.ThrowIfCancellationRequested();
            return true;
        }

        public  async Task<bool> StartSequence(ICollection<SequenceModel> sequence, bool bSave, CancellationTokenSource tokenSource, IProgress<string> progress) {
            return await Task.Run<bool>(async () => {
                try {
                    IsExposing = true;

                    ushort framenr = 1;
                    foreach (SequenceModel seq in sequence) {
                        seq.Active = true;

                        while (seq.ExposureCount > 0) {
                            

                            /*Change Filter*/
                            await ChangeFilter(seq, tokenSource, progress);

                            if (!Cam.Connected) {
                                throw new OperationCanceledException();
                            }

                            /*Set Camera Binning*/
                            SetBinning(seq);

                            if (!Cam.Connected) {
                                throw new OperationCanceledException();
                            }

                            /*Capture*/
                            await Capture(seq, tokenSource, progress);

                            if(!Cam.Connected) {
                                throw new OperationCanceledException();
                            }

                            /*Download Image */
                            Array arr = await Download(tokenSource, progress);
                            if (arr == null) {
                                throw new OperationCanceledException();
                            }

                            await ImageControl.PrepareArray(arr);
                            arr = null;

                            /*Prepare Image for UI*/
                            progress.Report(ImagingVM.ExposureStatus.PREPARING);

                            await ImageControl.PrepareImage(progress, tokenSource);


                            if (!Cam.Connected) {
                                throw new OperationCanceledException();
                            }

                            /*Save to disk*/
                            if (bSave) {                               
                                await Save(seq, framenr, tokenSource, progress);
                            }

                            /*Dither*/
                            await Dither(seq, tokenSource, progress);
                            
                            if (!Cam.Connected) {
                                throw new OperationCanceledException();
                            }

                            seq.ExposureCount -= 1;
                            framenr++;
                        }
                        seq.Active = false;
                    }
                }
                catch (System.OperationCanceledException ex) {
                    Logger.Trace(ex.Message);
                }
                finally {
                    progress.Report(ExposureStatus.IDLE);
                    Cam.StopExposure();
                    IsExposing = false;
                }
                return true;
            });
        }

        private async Task<bool> StartSequence(IProgress<string> progress, CancellationToken token = new CancellationToken()) {
            _cancelSequenceToken = new CancellationTokenSource();
            return await StartSequence(SeqVM.Sequence, true, _cancelSequenceToken, progress);           
        }


        ImageControlVM _imageControl;
        public ImageControlVM ImageControl {
            get { return _imageControl; }
            set { _imageControl = value; RaisePropertyChanged(); }
        }

        public RelayCommand CancelSnapCommand {
            get {
                return _cancelSnapCommand;
            }

            set {
                _cancelSnapCommand = value;
                RaisePropertyChanged();
            }
        }

        public RelayCommand CancelSequenceCommand {
            get {
                return _cancelSequenceCommand;
            }

            set {
                _cancelSequenceCommand = value;
                RaisePropertyChanged();
            }
        }

        private void CancelCaptureImage(object o) {
            if (_captureImageToken != null) {
                _captureImageToken.Cancel();
            }
        }

        private void CancelSequence(object o) {
            if (_cancelSequenceToken != null) {
                _cancelSequenceToken.Cancel();
            }
        }

        CancellationTokenSource _cancelSequenceToken;
        private RelayCommand _cancelSequenceCommand;

        CancellationTokenSource _captureImageToken;
        private RelayCommand _cancelSnapCommand;

        private FilterWheelModel.FilterInfo _snapFilter;
        public FilterWheelModel.FilterInfo SnapFilter {
            get {
                return _snapFilter;
            }
            set {
                _snapFilter = value;
                RaisePropertyChanged();
            }
        }

        private BinningMode _snapBin;
        public BinningMode SnapBin {
            get {
                if(_snapBin == null) {
                    _snapBin = new BinningMode(1, 1);
                }
                return _snapBin;
            }
            set {
                _snapBin = value;
                RaisePropertyChanged();
            }
        }

        public async Task<bool> CaptureImage(IProgress<string> progress) {
            _captureImageToken = new CancellationTokenSource();
            if (IsExposing) {
                Notification.ShowWarning("Camera is busy");
                return true;
            } else {
                do {
                    List<SequenceModel> seq = new List<SequenceModel>();
                    seq.Add(new SequenceModel(SnapExposureDuration, ImageTypes.SNAP, SnapFilter, SnapBin, 1));
                    await StartSequence(seq,  true, _captureImageToken, progress);
                    _captureImageToken.Token.ThrowIfCancellationRequested();
                } while (Loop);
                return true;
            }
        }

        public async Task<bool> CaptureImage(double duration, bool bsave, IProgress<string> progress, CancellationTokenSource token, FilterWheelModel.FilterInfo filter = null, BinningMode binning = null) {
            if (IsExposing) {
                Notification.ShowWarning("Camera is busy");
                return true;
            }
            else {
                List<SequenceModel> seq = new List<SequenceModel>();
                seq.Add(new SequenceModel(duration, ImageTypes.SNAP, filter, binning, 1));
                return await StartSequence(seq, bsave, token, progress);
            }
        }

        public static class ExposureStatus {
            public const string EXPOSING = "Exposing {0}/{1}...";
            public const string DOWNLOADING = "Downloading...";
            public const string FILTERCHANGE = "Switching Filter...";
            public const string PREPARING = "Preparing...";
            public const string CALCHFR = "Calculating HFR...";
            public const string SAVING = "Saving...";
            public const string IDLE = "Idle";
            public const string DITHERING = "Dithering...";
            public const string SETTLING = "Settling...";
        }
    }
}

﻿using NINA.Model;
using NINA.Model.MyFilterWheel;
using NINA.Model.MyFocuser;
using NINA.Utility;
using NINA.Utility.Exceptions;
using NINA.Utility.Mediator;
using NINA.Utility.Mediator.Interfaces;
using NINA.Utility.Notification;
using NINA.Utility.Profile;
using Nito.AsyncEx;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace NINA.ViewModel {

    internal class SequenceVM : DockableVM, IFocuserConsumer, IFilterWheelConsumer {

        public SequenceVM(
                IProfileService profileService,
                TelescopeMediator telescopeMediator,
                FocuserMediator focuserMediator,
                FilterWheelMediator filterWheelMediator,
                GuiderMediator guiderMediator
        ) : base(profileService) {
            this.telescopeMediator = telescopeMediator;

            this.filterWheelMediator = filterWheelMediator;
            this.filterWheelMediator.RegisterConsumer(this);

            this.guiderMediator = guiderMediator;

            this.profileService = profileService;
            Title = "LblSequence";
            ImageGeometry = (System.Windows.Media.GeometryGroup)System.Windows.Application.Current?.Resources["SequenceSVG"];

            EstimatedDownloadTime = profileService.ActiveProfile.SequenceSettings.EstimatedDownloadTime;

            ContentId = nameof(SequenceVM);
            AddSequenceCommand = new RelayCommand(AddSequence);
            RemoveSequenceCommand = new RelayCommand(RemoveSequence);
            StartSequenceCommand = new AsyncCommand<bool>(() => StartSequence(new Progress<ApplicationStatus>(p => Status = p)));
            SaveSequenceCommand = new RelayCommand(SaveSequence);
            LoadSequenceCommand = new RelayCommand(LoadSequence);
            CancelSequenceCommand = new RelayCommand(CancelSequence);
            PauseSequenceCommand = new RelayCommand(PauseSequence);
            ResumeSequenceCommand = new RelayCommand(ResumeSequence);
            UpdateETACommand = new RelayCommand((object o) => CalculateETA());

            RegisterMediatorMessages();

            profileService.LocationChanged += (object sender, EventArgs e) => {
                var dso = new DeepSkyObject(Sequence.DSO.Name, Sequence.DSO.Coordinates, profileService.ActiveProfile.ApplicationSettings.SkyAtlasImageRepository);
                dso.SetDateAndPosition(SkyAtlasVM.GetReferenceDate(DateTime.Now), profileService.ActiveProfile.AstrometrySettings.Latitude, profileService.ActiveProfile.AstrometrySettings.Longitude);
                Sequence.SetSequenceTarget(dso);
            };
        }

        private void LoadSequence(object obj) {
            Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.Title = Locale.Loc.Instance["LblLoadSequence"];
            dialog.FileName = "Sequence";
            dialog.DefaultExt = ".xml";
            dialog.Filter = "XML documents|*.xml";

            if (dialog.ShowDialog() == true) {
                using (var s = new FileStream(dialog.FileName, FileMode.Open)) {
                    //var s = System.Xml.XmlReader.Create(dialog.FileName);
                    //var listXml = XElement.Load(path);

                    //System.IO.StringReader reader = new System.IO.StringReader(listXml.ToString());

                    var l = CaptureSequenceList.Load(
                        s,
                        profileService.ActiveProfile.FilterWheelSettings.FilterWheelFilters,
                        profileService.ActiveProfile.AstrometrySettings.Latitude,
                        profileService.ActiveProfile.AstrometrySettings.Longitude
                    );
                    Sequence = l;
                }
            }
        }

        private void SaveSequence(object obj) {
            Microsoft.Win32.SaveFileDialog dialog = new Microsoft.Win32.SaveFileDialog();
            dialog.Title = Locale.Loc.Instance["LblSaveSequence"];
            dialog.FileName = "Sequence";
            dialog.DefaultExt = ".xml";
            dialog.Filter = "XML documents (.xml)|*.xml";

            if (dialog.ShowDialog() == true) {
                Sequence.Save(dialog.FileName);
            }
        }

        private void ResumeSequence(object obj) {
            if (_pauseTokenSource != null) {
                _pauseTokenSource.IsPaused = false;
                RaisePropertyChanged(nameof(IsPaused));
            }
        }

        private void PauseSequence(object obj) {
            if (_pauseTokenSource != null) {
                _pauseTokenSource.IsPaused = true;
                RaisePropertyChanged(nameof(IsPaused));
            }
        }

        private void CancelSequence(object obj) {
            _canceltoken?.Cancel();
            RaisePropertyChanged(nameof(IsPaused));
        }

        private PauseTokenSource _pauseTokenSource;
        private CancellationTokenSource _canceltoken;

        public bool IsPaused {
            get {
                return _pauseTokenSource?.IsPaused ?? false;
            }
        }

        private ApplicationStatus _status;

        public ApplicationStatus Status {
            get {
                return _status;
            }
            set {
                _status = value;
                _status.Source = Title;

                if (Sequence.ActiveSequence != null) {
                    _status.Status2 = Locale.Loc.Instance["LblSequence"];
                    _status.ProgressType2 = ApplicationStatus.StatusProgressType.ValueOfMaxValue;
                    _status.Progress2 = Sequence.ActiveSequenceIndex;
                    _status.MaxProgress2 = Sequence.Count;

                    _status.Status3 = Locale.Loc.Instance["LblExposures"];
                    _status.ProgressType3 = ApplicationStatus.StatusProgressType.ValueOfMaxValue;
                    _status.Progress3 = Sequence.ActiveSequence.ProgressExposureCount;
                    _status.MaxProgress3 = Sequence.ActiveSequence.TotalExposureCount;
                }

                RaisePropertyChanged();

                Mediator.Instance.Request(new StatusUpdateMessage() { Status = _status });
            }
        }

        private Mutex mutex = new Mutex();

        public async Task AddDownloadTime(TimeSpan t) {
            await Task.Run(() => {
                var s = Stopwatch.StartNew();
                mutex.WaitOne();
                _actualDownloadTimes.Add(t);

                double doubleAverageTicks = _actualDownloadTimes.Average(timeSpan => timeSpan.Ticks);
                long longAverageTicks = Convert.ToInt64(doubleAverageTicks);
                EstimatedDownloadTime = new TimeSpan(longAverageTicks);
                mutex.ReleaseMutex();
            });
        }

        private void CalculateETA() {
            TimeSpan time = new TimeSpan();
            foreach (CaptureSequence s in Sequence) {
                var exposureCount = s.TotalExposureCount - s.ProgressExposureCount;
                time = time.Add(TimeSpan.FromSeconds(exposureCount * (s.ExposureTime + EstimatedDownloadTime.TotalSeconds)));
            }
            ETA = DateTime.Now.AddSeconds(time.TotalSeconds);
        }

        private List<TimeSpan> _actualDownloadTimes = new List<TimeSpan>();

        public TimeSpan EstimatedDownloadTime {
            get {
                return profileService.ActiveProfile.SequenceSettings.EstimatedDownloadTime;
            }
            set {
                profileService.ActiveProfile.SequenceSettings.EstimatedDownloadTime = value;
                RaisePropertyChanged();
                CalculateETA();
            }
        }

        private DateTime _eta;

        public DateTime ETA {
            get {
                return _eta;
            }
            private set {
                _eta = value;
                RaisePropertyChanged();
            }
        }

        private async Task<bool> StartSequence(IProgress<ApplicationStatus> progress) {
            try {
                if (Sequence.Count <= 0) {
                    return false;
                }
                _actualDownloadTimes.Clear();
                _canceltoken = new CancellationTokenSource();
                _pauseTokenSource = new PauseTokenSource();
                RaisePropertyChanged(nameof(IsPaused));

                CalculateETA();

                if (Sequence.SlewToTarget) {
                    progress.Report(new ApplicationStatus() { Status = Locale.Loc.Instance["LblSlewToTarget"] });
                    await telescopeMediator.SlewToCoordinatesAsync(Sequence.Coordinates);
                    if (Sequence.CenterTarget) {
                        progress.Report(new ApplicationStatus() { Status = Locale.Loc.Instance["LblCenterTarget"] });
                        var result = await Mediator.Instance.RequestAsync(new PlateSolveMessage() { SyncReslewRepeat = true, Progress = progress, Token = _canceltoken.Token });
                        if (result == null || !result.Success) {
                            progress.Report(new ApplicationStatus() { Status = Locale.Loc.Instance["LblPlatesolveFailed"] });
                            return false;
                        }
                    }
                }

                /* delay sequence start by given amount */
                var delay = Sequence.Delay;
                while (delay > 0) {
                    await Task.Delay(TimeSpan.FromSeconds(1), _canceltoken.Token);
                    delay--;
                    progress.Report(new ApplicationStatus() { Status = string.Format(Locale.Loc.Instance["LblSequenceDelayStatus"], delay) });
                }

                if (Sequence.AutoFocusOnStart) {
                    await Mediator.Instance.RequestAsync(new StartAutoFocusMessage() { Filter = Sequence.Items[0].FilterType, Token = _canceltoken.Token, Progress = progress });
                }

                if (Sequence.StartGuiding) {
                    progress.Report(new ApplicationStatus() { Status = Locale.Loc.Instance["LblStartGuiding"] });
                    var guiderStarted = await this.guiderMediator.StartGuiding(_canceltoken.Token);
                    if (!guiderStarted) {
                        Notification.ShowWarning(Locale.Loc.Instance["LblStartGuidingFailed"]);
                    }
                }

                return await ProcessSequence(_canceltoken.Token, _pauseTokenSource.Token, progress);
            } finally {
                progress.Report(new ApplicationStatus() { Status = string.Empty });
            }
        }

        //Instantiate a Singleton of the Semaphore with a value of 1. This means that only 1 thread can be granted access at a time.
        private static SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);

        private async Task<bool> ProcessSequence(CancellationToken ct, PauseToken pt, IProgress<ApplicationStatus> progress) {
            return await Task.Run<bool>(async () => {
                try {
                    //Asynchronously wait to enter the Semaphore. If no-one has been granted access to the Semaphore, code execution will proceed, otherwise this thread waits here until the semaphore is released
                    await semaphoreSlim.WaitAsync(ct);

                    /* Validate if preconditions are met */
                    if (!CheckPreconditions()) {
                        return false;
                    }

                    Sequence.IsRunning = true;

                    CaptureSequence seq;
                    var actualFilter = filterWheelInfo.SelectedFilter;
                    short prevFilterPosition = actualFilter?.Position ?? -1;
                    var lastAutoFocusTime = DateTime.UtcNow;
                    var lastAutoFocusTemperature = focuserInfo?.Temperature ?? double.NaN;
                    var exposureCount = 0;
                    while ((seq = Sequence.Next()) != null) {
                        exposureCount++;

                        await CheckMeridianFlip(seq, ct, progress);

                        Stopwatch seqDuration = Stopwatch.StartNew();

                        //Check if autofocus should be done
                        if (ShouldAutoFocus(seq, exposureCount, prevFilterPosition, lastAutoFocusTime, lastAutoFocusTemperature)) {
                            progress.Report(new ApplicationStatus() { Status = Locale.Loc.Instance["LblAutoFocus"] });
                            await Mediator.Instance.RequestAsync(new StartAutoFocusMessage() { Filter = seq.FilterType, Token = _canceltoken.Token, Progress = progress });
                            lastAutoFocusTime = DateTime.UtcNow;
                            lastAutoFocusTemperature = focuserInfo?.Temperature ?? double.NaN;
                            progress.Report(new ApplicationStatus() { Status = string.Empty });
                        }

                        progress.Report(new ApplicationStatus() { Status = Locale.Loc.Instance["LblPrepareExposure"] });
                        await Mediator.Instance.RequestAsync(
                            new CapturePrepareAndSaveImageMessage() {
                                Sequence = seq,
                                Save = true,
                                TargetName = Sequence.TargetName,
                                Progress = progress,
                                Token = ct
                            }
                        );
                        progress.Report(new ApplicationStatus() { Status = string.Empty });

                        seqDuration.Stop();

                        await AddDownloadTime(seqDuration.Elapsed.Subtract(TimeSpan.FromSeconds(seq.ExposureTime)));

                        if (pt.IsPaused) {
                            Sequence.IsRunning = false;
                            semaphoreSlim.Release();
                            progress.Report(new ApplicationStatus() { Status = Locale.Loc.Instance["LblPaused"] });
                            await pt.WaitWhilePausedAsync(ct);
                            progress.Report(new ApplicationStatus() { Status = Locale.Loc.Instance["LblResuming"] });
                            await semaphoreSlim.WaitAsync(ct);
                            Sequence.IsRunning = true;
                        }

                        actualFilter = filterWheelInfo.SelectedFilter;
                        prevFilterPosition = actualFilter?.Position ?? -1;
                    }
                } catch (OperationCanceledException) {
                } catch (CameraConnectionLostException) {
                } catch (Exception ex) {
                    Logger.Error(ex);
                    Notification.ShowError(ex.Message);
                } finally {
                    progress.Report(new ApplicationStatus() { Status = string.Empty });
                    Sequence.IsRunning = false;
                    semaphoreSlim.Release();
                }
                return true;
            });
        }

        private bool ShouldAutoFocus(CaptureSequence seq, int exposureCount, short previousFilterPosition, DateTime lastAutoFocusTime, double lastAutoFocusTemperature) {
            if (seq.FilterType != null && seq.FilterType.Position != previousFilterPosition
                    && seq.FilterType.Position >= 0
                    && Sequence.AutoFocusOnFilterChange) {
                /* Trigger autofocus after filter change */
                return true;
            }

            if (Sequence.AutoFocusAfterSetTime && (DateTime.UtcNow - lastAutoFocusTime) > TimeSpan.FromMinutes(Sequence.AutoFocusSetTime)) {
                /* Trigger autofocus after a set time */
                return true;
            }

            if (Sequence.AutoFocusAfterSetExposures && exposureCount % Sequence.AutoFocusSetExposures == 0) {
                /* Trigger autofocus after amount of exposures*/
                return true;
            }

            if (Sequence.AutoFocusAfterTemperatureChange && !double.IsNaN(focuserInfo?.Temperature ?? double.NaN) && Math.Abs(lastAutoFocusTemperature - focuserInfo.Temperature) > Sequence.AutoFocusAfterTemperatureChangeAmount) {
                /* Trigger autofocus after temperature change*/
                return true;
            }

            return false;
        }

        /// <summary>
        /// Checks if auto meridian flip should be considered and executes it
        /// 1) Compare next exposure length with time to meridian - If exposure length is greater
        ///    than time to flip the system will wait
        /// 2) Pause Guider
        /// 3) Execute the flip
        /// 4) If recentering is enabled, platesolve current position, sync and recenter to old
        ///    target position
        /// 5) Resume Guider
        /// </summary>
        /// <param name="seq">        Current Sequence row</param>
        /// <param name="tokenSource">cancel token</param>
        /// <param name="progress">   progress reporter</param>
        /// <returns></returns>
        private async Task CheckMeridianFlip(CaptureSequence seq, CancellationToken token, IProgress<ApplicationStatus> progress) {
            progress.Report(new ApplicationStatus() { Status = Locale.Loc.Instance["LblCheckMeridianFlip"] });
            await Mediator.Instance.RequestAsync(new CheckMeridianFlipMessage() { Sequence = seq, Token = token });
            progress.Report(new ApplicationStatus() { Status = string.Empty });
        }

        private bool CheckPreconditions() {
            bool valid = true;

            valid = HasWritePermission(profileService.ActiveProfile.ImageFileSettings.FilePath);

            return valid;
        }

        public bool HasWritePermission(string dir) {
            bool Allow = false;
            bool Deny = false;
            DirectorySecurity acl = null;

            if (Directory.Exists(dir)) {
                acl = Directory.GetAccessControl(dir);
            }

            if (acl == null) {
                Notification.ShowError(Locale.Loc.Instance["LblDirectoryNotFound"]);
                return false;
            }

            AuthorizationRuleCollection arc = acl.GetAccessRules(true, true, typeof(System.Security.Principal.SecurityIdentifier));
            if (arc == null)
                return false;
            foreach (FileSystemAccessRule rule in arc) {
                if ((FileSystemRights.Write & rule.FileSystemRights) != FileSystemRights.Write)
                    continue;
                if (rule.AccessControlType == AccessControlType.Allow)
                    Allow = true;
                else if (rule.AccessControlType == AccessControlType.Deny)
                    Deny = true;
            }

            if (Allow && !Deny) {
                return true;
            } else {
                Notification.ShowError(Locale.Loc.Instance["LblDirectoryNotWritable"]);
                return false;
            }
        }

        private void RegisterMediatorMessages() {
            Mediator.Instance.RegisterAsyncRequest(
                new SetSequenceCoordinatesMessageHandle(async (SetSequenceCoordinatesMessage msg) => {
                    Mediator.Instance.Request(new ChangeApplicationTabMessage() { Tab = ApplicationTab.SEQUENCE });
                    var sequenceDso = new DeepSkyObject(msg.DSO.AlsoKnownAs.FirstOrDefault() ?? msg.DSO.Name ?? string.Empty, msg.DSO.Coordinates, profileService.ActiveProfile.ApplicationSettings.SkyAtlasImageRepository);
                    await Task.Run(() => {
                        sequenceDso.SetDateAndPosition(SkyAtlasVM.GetReferenceDate(DateTime.Now), profileService.ActiveProfile.AstrometrySettings.Latitude, profileService.ActiveProfile.AstrometrySettings.Longitude);
                    });

                    Sequence.SetSequenceTarget(sequenceDso);
                    return true;
                })
            );
        }

        private CaptureSequenceList _sequence;

        public CaptureSequenceList Sequence {
            get {
                if (_sequence == null) {
                    if (File.Exists(profileService.ActiveProfile.SequenceSettings.TemplatePath)) {
                        using (var s = new FileStream(profileService.ActiveProfile.SequenceSettings.TemplatePath, FileMode.Open)) {
                            _sequence = CaptureSequenceList.Load(
                                s,
                                profileService.ActiveProfile.FilterWheelSettings.FilterWheelFilters,
                                profileService.ActiveProfile.AstrometrySettings.Latitude,
                                profileService.ActiveProfile.AstrometrySettings.Longitude
                            );
                        }
                    }
                    if (_sequence == null) {
                        /* Fallback when no template is set or load failed */
                        var seq = new CaptureSequence();
                        _sequence = new CaptureSequenceList(seq);
                        _sequence.DSO?.SetDateAndPosition(
                            SkyAtlasVM.GetReferenceDate(DateTime.Now),
                            profileService.ActiveProfile.AstrometrySettings.Latitude,
                            profileService.ActiveProfile.AstrometrySettings.Longitude
                        );
                        SelectedSequenceIdx = _sequence.Count - 1;
                    }
                }
                return _sequence;
            }
            set {
                _sequence = value;
                RaisePropertyChanged();
            }
        }

        private int _selectedSequenceIdx;

        public int SelectedSequenceIdx {
            get {
                return _selectedSequenceIdx;
            }
            set {
                _selectedSequenceIdx = value;
                RaisePropertyChanged();
            }
        }

        private ObservableCollection<string> _imageTypes;
        private TelescopeMediator telescopeMediator;
        private FilterWheelMediator filterWheelMediator;
        private FocuserInfo focuserInfo;
        private FilterWheelInfo filterWheelInfo;
        private GuiderMediator guiderMediator;

        public ObservableCollection<string> ImageTypes {
            get {
                if (_imageTypes == null) {
                    _imageTypes = new ObservableCollection<string>();

                    Type type = typeof(Model.CaptureSequence.ImageTypes);
                    foreach (var p in type.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)) {
                        var v = p.GetValue(null);
                        _imageTypes.Add(v.ToString());
                    }
                }
                return _imageTypes;
            }
            set {
                _imageTypes = value;
                RaisePropertyChanged();
            }
        }

        public void AddSequence(object o) {
            Sequence.Add(new CaptureSequence());
            SelectedSequenceIdx = Sequence.Count - 1;
        }

        private void RemoveSequence(object obj) {
            var idx = SelectedSequenceIdx;
            if (idx > -1) {
                Sequence.RemoveAt(idx);
                if (idx < Sequence.Count - 1) {
                    SelectedSequenceIdx = idx;
                } else {
                    SelectedSequenceIdx = Sequence.Count - 1;
                }
            }
        }

        public void UpdateFocuserInfo(FocuserInfo focuserInfo) {
            this.focuserInfo = focuserInfo;
        }

        public void UpdateFilterWheelInfo(FilterWheelInfo filterWheelInfo) {
            this.filterWheelInfo = filterWheelInfo;
        }

        public ICommand AddSequenceCommand { get; private set; }

        public ICommand RemoveSequenceCommand { get; private set; }

        public IAsyncCommand StartSequenceCommand { get; private set; }

        public ICommand CancelSequenceCommand { get; private set; }
        public ICommand PauseSequenceCommand { get; private set; }
        public ICommand ResumeSequenceCommand { get; private set; }
        public ICommand UpdateETACommand { get; private set; }
        public ICommand LoadSequenceCommand { get; private set; }
        public ICommand SaveSequenceCommand { get; private set; }
    }
}
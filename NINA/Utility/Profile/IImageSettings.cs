﻿namespace NINA.Utility.Profile {
    public interface IImageSettings {
        bool AnnotateImage { get; set; }
        double AutoStretchFactor { get; set; }
        int HistogramResolution { get; set; }
    }
}
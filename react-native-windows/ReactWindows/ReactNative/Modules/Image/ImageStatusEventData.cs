﻿namespace ReactNative.Modules.Image
{
    struct ImageStatusEventData
    {
        public ImageStatusEventData(ImageLoadStatus loadStatus)
            : this(loadStatus, default(ImageMetadata))
        {
        }

        public ImageStatusEventData(ImageLoadStatus loadStatus, ImageMetadata metadata)
        {
            LoadStatus = loadStatus;
            Metadata = metadata;
        }

        public ImageLoadStatus LoadStatus { get; }

        public ImageMetadata Metadata { get; }
    }
}

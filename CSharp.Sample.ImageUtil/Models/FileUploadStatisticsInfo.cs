﻿using System;

namespace CSharp.Sample.ImageUtil.Models
{
    public class FileUploadStatisticsInfo
    {
        public double SecondsToResponse { get; set; }

        public double SecondsToDatabaseOperation_CreateFileInfo { get; set; }

        public double SecondsToDatabaseOperation_UpadateFileInfo { get; set; }

        public double SecondsToFileProcessing { get; set; }

        public double SecondsToFileStorage { get; set; }
    }
}
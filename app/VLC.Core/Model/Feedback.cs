﻿using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using VLC.Utils;

namespace VLC.Model
{
    [DataContract]
    public class Feedback
    {
        private string platform = "W";
        private string platformVersion = "8.1/10";
        private string appVersion = Strings.AppVersion;
        private string device = Strings.DeviceModel;
        private string firmware = Strings.Firmware;

        public string Id { get; set; }

        [DataMember(Name = nameof(Comment))]
        public string Comment { get; set; }

        [DataMember(Name = nameof(Summary))]
        public string Summary { get; set; }

        [DataMember(Name = nameof(BackendLog))]
        public string BackendLog { get; set; }

        [DataMember(Name = nameof(FrontendLog))]
        public string FrontendLog { get; set; }

        [DataMember(Name = nameof(Fixed))]
        public bool Fixed { get; set; }

        [DataMember(Name = nameof(Platform))]
        public string Platform
        {
            get { return platform; }
            set { platform = value; }
        }

        [DataMember(Name = nameof(PlatformVersion))]
        public string PlatformVersion
        {
            get { return platformVersion; }
            set { platformVersion = value; }
        }

        [DataMember(Name = nameof(PlatformBuild))]
        public int PlatformBuild { get; set; }

        [DataMember(Name = nameof(AppVersion))]
        public string AppVersion
        {
            get { return appVersion; }
            set { appVersion = value; }
        }

        [DataMember(Name = nameof(Device))]
        public string Device
        {
            get { return device; }
            set { device = value; }
        }

        [DataMember(Name = nameof(Firmware))]
        public string Firmware
        {
            get { return firmware; }
            set { firmware = value; }
        }
    }
}

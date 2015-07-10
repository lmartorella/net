﻿using System;
using Lucky.Home.Core;

// ReSharper disable UnassignedField.Global

namespace Lucky.Home.Protocol
{
    public class HeloMessage
    {
        /// <summary>
        /// HOME
        /// </summary>
        [SerializeAsFixedString(4)]
        public string Preamble;

        internal const string PreambleValue = "HOME";

        /// <summary>
        /// HEL3 or HTBT
        /// </summary>
        [SerializeAsFixedString(4)]
        public string MessageCode;

        internal const string HeloMessageCode = "HEL3";
        internal const string HeartbeatMessageCode = "HTBT";
        internal const string SubNodeChanged = "NWND";
        
        /// <summary>
        /// Device ID
        /// </summary>
        public Guid DeviceId;

        /// <summary>
        /// Control TCP port
        /// </summary>
        public ushort ControlPort;
    }
}
﻿using Lucky.Home.Db;
using Lucky.Home.Power;
using Lucky.Home.Sinks;
using Lucky.Services;
using System;
using System.Linq;
using System.Text;
using System.Threading;

namespace Lucky.Home.Devices
{
    [Device("Samil Inverter")]
    [Requires(typeof(HalfDuplexLineSink))]
    class SamilInverterLoggerDevice : SamilInverterDeviceBase, ISolarPanelDevice
    {
        private Timer _timer;
        private bool _noSink;
        private bool _inNightMode = false;
        private DateTime _lastValidData = DateTime.Now;

        /// <summary>
        /// After this time of no samples, enter night mode
        /// </summary>
        private static readonly TimeSpan EnterNightModeAfter = TimeSpan.FromMinutes(2);

        /// <summary>
        /// After this time after an error, reconnect
        /// </summary>
        private static readonly TimeSpan CheckConnectionFirstTimeout = TimeSpan.FromSeconds(5);

        /// <summary>
        /// During day (e.g. when samples are working), retry every 10 seconds
        /// </summary>
        private static readonly TimeSpan CheckConnectionPeriodDay = TimeSpan.FromSeconds(10);

        /// <summary>
        /// During noght (e.g. when last sample is older that 2 minutes), retry every 2 minutes
        /// </summary>
        private static readonly TimeSpan CheckConnectionPeriodNight = TimeSpan.FromMinutes(2);

        /// <summary>
        /// Get a solar PV sample every 15 seconds
        /// </summary>
        private static readonly TimeSpan PollDataPeriod = TimeSpan.FromSeconds(15);

        public SamilInverterLoggerDevice()
            : base("SAMIL")
        {
            StartConnectionTimer();
        }

        private void StartConnectionTimer()
        {
            if (_timer != null)
            {
                _timer.Dispose();
            }

            // Poll SAMIL for login
            _timer = new Timer(o =>
            {
                CheckConnection();
            }, null, CheckConnectionFirstTimeout, InNightMode ? CheckConnectionPeriodNight : CheckConnectionPeriodDay);
        }

        private void StartDataTimer()
        {
            if (_timer != null)
            {
                _timer.Dispose();
            }

            // Poll SAMIL for data
            _timer = new Timer(o =>
            {
                PollData();
            }, null, TimeSpan.FromSeconds(1), PollDataPeriod);
        }

        ITimeSeries<PowerData> ISolarPanelDevice.Database
        {
            get
            {
                return (ITimeSeries<PowerData>)Database;
            }
            set
            {
                Database = (ITimeSeries<SamilPowerData>)value;
            }
        }

        public ITimeSeries<SamilPowerData> Database { get; set; }

        public PowerData ImmediateData { get; private set; }

        private void CheckConnection()
        {
            // Poll the line
            var line = Sinks.OfType<HalfDuplexLineSink>().FirstOrDefault();
            if (line == null)
            {
                // No sink
                if (!_noSink)
                {
                    _logger.Log("NoSink");
                    _noSink = true;
                }
                return;
            }
            else
            {
                if (_noSink)
                {
                    _logger.Log("Sink OK");
                    _noSink = false;
                }
                if (!LoginInverter(line))
                {
                    if (DateTime.Now - _lastValidData > EnterNightModeAfter)
                    {
                        InNightMode = true;
                    }
                }
            }
        }

        private bool InNightMode
        {
            get
            {
                return _inNightMode;
            }
            set
            {
                if (_inNightMode != value)
                {
                    _inNightMode = value;
                    _logger.Log("NightMode: " + value);
                    if (value)
                    {
                        StartConnectionTimer();
                    }
                }
            }
        }

        private bool CheckProtocol(HalfDuplexLineSink line, SamilMsg request, SamilMsg expResponse, string phase, bool checkPayload)
        {
            Action<SamilMsg> warn = checkPayload ? (Action<SamilMsg>)(w => ReportWarning("Strange payload " + phase, w)) : null;
            return (CheckProtocolWRes(line, phase, request, expResponse, (bytes, msg) => ReportFault("Unexpected " + phase, bytes, msg), warn)) != null;
        }

        private bool LoginInverter(HalfDuplexLineSink line)
        {
            // Send 3 logout messages
            for (int i = 0; i < 3; i++)
            {
                line.SendReceive(LogoutMessage.ToBytes(), false, "logout");
                Thread.Sleep(500);
            }
            var res = CheckProtocolWRes(line, "bcast", BroadcastRequest, BroadcastResponse, (bytes, msg) =>
                {
                    if (!InNightMode)
                    {
                        ReportFault("Unexpected broadcast response", bytes, msg);
                    }
                });
            if (res == null)
            {
                // Still continue to try login
                return false;
            }

            // Correct response!
            var id = res.Payload;
            _logger.Log("Found", "ID", Encoding.ASCII.GetString(id));

            // Now try to login as address 1
            var loginMsg = LoginMessage.Clone(id.Concat(new byte[] { AddressToAllocate }).ToArray());

            if (!CheckProtocol(line, loginMsg, LoginResponse, "login response", true))
            {
                // Still continue to try login
                return false;
            }

            // Now I'm logged in!
            // Go with msg 1
            if (!CheckProtocol(line, UnknownMessage1, UnknownResponse1, "unknown message 1", true))
            {
                // Still continue to try login
                return false;
            }
            // Go with msg 2
            if (!CheckProtocol(line, UnknownMessage2, UnknownResponse2, "unknown message 2", true))
            {
                // Still continue to try login
                return false;
            }
            // Go with get firmware
            if (!CheckProtocol(line, GetFwVersionMessage, GetFwVersionResponse, "get firmware response", false))
            {
                // Still continue to try login
                return false;
            }
            // Go with get conf info
            if (!CheckProtocol(line, GetConfInfoMessage, GetConfInfoResponse, "get configuration", true))
            {
                // Still continue to try login
                return false;
            }

            // OK!
            // Start data timer
            InNightMode = false;
            StartDataTimer();
            return true;
        }

        private void PollData()
        {
            var line = Sinks.OfType<HalfDuplexLineSink>().FirstOrDefault();
            if (line == null)
            {
                // disconnect
                StartConnectionTimer();
                return;
            }
            var res = CheckProtocolWRes(line, "pv", GetPvDataMessage, GetPvDataResponse, (bytes, msg) => ReportFault("Unexpected PV data", bytes, msg));
            if (res == null)
            {
                // Relogin!
                StartConnectionTimer();
                return;
            }
            // Decode and record data
            if (!DecodePvData(res.Payload))
            {
                // Report invalid msg
                ReportWarning("Invalid/strange PV data", res);
            }
            else
            {
                // Store last timestamp
                _lastValidData = DateTime.Now;
            }
        }

        private bool DecodePvData(byte[] payload)
        {
            if (payload.Length != 50)
            {
                return false;
            }

            int panelVoltage = ExtractW(payload, 1);
            int panelCurrent = ExtractW(payload, 2);
            int mode = ExtractW(payload, 5);
            int energyToday = ExtractW(payload, 6);
            int gridCurrent = ExtractW(payload, 19);
            int gridVoltage = ExtractW(payload, 20);
            int gridFrequency = ExtractW(payload, 21);
            int gridPower = ExtractW(payload, 22);
            int totalPower = (ExtractW(payload, 23) << 16) + ExtractW(payload, 24);

            var db = Database;
            if (db != null)
            {
                var data = new SamilPowerData
                {
                    PowerW = gridPower,
                    PanelVoltageV = panelVoltage / 10.0,
                    GridVoltageV = gridVoltage / 10.0,
                    PanelCurrentA = panelCurrent / 10.0,
                    GridCurrentA = gridCurrent / 10.0,
                    Mode = mode,
                    EnergyTodayW = energyToday * 10.0,
                    GridFrequencyHz = gridFrequency / 100.0,
                    TotalPowerKW = totalPower / 10.0
                };
                db.AddNewSample(data, DateTime.Now);
            }

            return payload.All(b => b == 0);
        }

        private ushort ExtractW(byte[] payload, int pos)
        {
            pos *= 2;
            var ret = WordAt(pos, payload);
            payload[pos++] = 0;
            payload[pos++] = 0;
            return ret;
        }

        private void ReportFault(string reason, byte[] msg, SamilMsg message)
        {
            if (message != null)
            {
                _logger.Log(reason, "Msg", message.ToString());
            }
            else
            {
                _logger.Log(reason, "Rcv", ToString(msg));
            }
        }

        private void ReportWarning(string reason, SamilMsg message)
        {
            _logger.Log(reason, "Warn. Payload:", ToString(message.Payload));
        }
    }
}

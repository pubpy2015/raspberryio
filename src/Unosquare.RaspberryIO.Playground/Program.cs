﻿namespace Unosquare.RaspberryIO.Playground
{
    using Camera;
    using Computer;
    using Gpio;
    using Peripherals;
    using Swan;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading;

    /// <summary>
    /// Main entry point class
    /// </summary>
    public partial class Program
    {
        /// <summary>
        /// Defines the entry point of the application.
        /// </summary>
        /// <param name="args">The CLI arguments.</param>
        public static void Main(string[] args)
        {
            $"Starting program at {DateTime.Now}".Info();

            Terminal.Settings.DisplayLoggingMessageType = LogMessageType.Info | LogMessageType.Warning | LogMessageType.Error;

            try
            {
                // A set of very simple tests:
                TestSystemInfo();

                // TestCaptureImage();
                // TestCaptureVideo();
                // TestLedStripGraphics();
                // TestLedStrip();
                // TestTag();
                // TestLedBlinking();
                // TestHardwarePwm();
                TestInfraredSensor();
            }
            catch (Exception ex)
            {
                "Error".Error(nameof(Main), ex);
            }
            finally
            {
                "Program finished.".Info();
                Terminal.Flush(TimeSpan.FromSeconds(2));
                if (System.Diagnostics.Debugger.IsAttached)
                    "Press any key to exit . . .".ReadKey();
            }
        }

        /// <summary>
        /// Tests the infrared sensor HX1838.
        /// </summary>
        public static void TestInfraredSensor()
        {
            var inputPin = Pi.Gpio[P1.Gpio23]; // BCM Pin 23 or Physical pin 16 on the right side of the header.
            var sensor = new InfraredSensor(inputPin, true);
            var emitter = new InfraredEmitter(Pi.Gpio[P1.Gpio18]);
            var pulseLengths = new long[] { 9000, 4500, 2250, 1687, 675, 225 };

            sensor.DataAvailable += (s, e) =>
            {
                var necData = InfraredSensor.NecDecoder.DecodePulses(e.Pulses);
                if (necData != null)
                {
                    $"NEC Data: {BitConverter.ToString(necData).Replace("-", " "),12}    Pulses: {e.Pulses.Length,4}    Duration(us): {e.TrainDurationUsecs,6}    Reason: {e.FlushReason}".Warn("IR");

                    // Test repeater signal
                    var outputPulses = InfraredEmitter.SnapPulseLengths(e.Pulses, pulseLengths);
                    emitter.Send(outputPulses);
                    var debugData = InfraredSensor.DebugPulses(outputPulses);
                    $"TX       Length: {outputPulses.Length,5}".Warn("IR");
                    debugData.Info("IR");
                }
                else
                {
                    if (e.Pulses.Length >= 4)
                    {
                        var debugData = InfraredSensor.DebugPulses(e.Pulses);
                        $"RX    Length: {e.Pulses.Length,5}; Duration: {e.TrainDurationUsecs,7}; Reason: {e.FlushReason}".Warn("IR");
                        debugData.Info("IR");
                    }
                    else
                    {
                        $"RX (Garbage): {e.Pulses.Length,5}; Duration: {e.TrainDurationUsecs,7}; Reason: {e.FlushReason}".Error("IR");
                    }
                }
            };

            Console.ReadLine();
        }

        /// <summary>
        /// Tests the SPI bus functionality.
        /// </summary>
        public static void TestSpi()
        {
            Pi.Spi.Channel0Frequency = SpiChannel.MinFrequency;

            var request = Encoding.UTF8.GetBytes("Hello over SPI");
            $"SPI Request: {BitConverter.ToString(request)}".Info();
            var response = Pi.Spi.Channel0.SendReceive(request);
            $"SPI Response: {BitConverter.ToString(response)}".Info();

            $"SPI Base Stream Request: {BitConverter.ToString(request)}".Info();
            Pi.Spi.Channel0.Write(request);
            response = Pi.Spi.Channel0.SendReceive(new byte[request.Length]);
            $"SPI Base Stream Response: {BitConverter.ToString(response)}".Info();
        }

        /// <summary>
        /// Tests the display.
        /// </summary>
        public static void TestDisplay()
        {
            var input = string.Empty;

            while (input.Equals("x") == false)
            {
                "Enter brightness value (0 to 255). Enter b to toggle Backlight, Enter x to Exit".Info();
                input = Console.ReadLine();

                if (input?.Equals("b") == true)
                {
                    Pi.PiDisplay.IsBacklightOn = !Pi.PiDisplay.IsBacklightOn;
                }
                else
                {
                    if (byte.TryParse(input, out var value))
                    {
                        if (value != Pi.PiDisplay.Brightness)
                        {
                            $"Current Value: {Pi.PiDisplay.Brightness}, New Value: {value}".Info();
                            Pi.PiDisplay.Brightness = value;
                        }
                    }
                }

                $"Display Status - Backlight: {Pi.PiDisplay.IsBacklightOn}, Brightness: {Pi.PiDisplay.Brightness}"
                    .Info();
            }

            Pi.PiDisplay.IsBacklightOn = true;
            Pi.PiDisplay.Brightness = 96;
            $"Display Status - Backlight: {Pi.PiDisplay.IsBacklightOn}, Brightness: {Pi.PiDisplay.Brightness}".Info();
        }

        /// <summary>
        /// Tests the LED blinking logic.
        /// </summary>
        public static void TestLedBlinking()
        {
            // Get a reference to the pin you need to use.
            // All methods below are exactly equivalent and reference the same pin
            var blinkingPin = Pi.Gpio[0];
            blinkingPin = Pi.Gpio[WiringPiPin.Pin00];
            blinkingPin = Pi.Gpio.Pin00;
            blinkingPin = Pi.Gpio.HeaderP1[11];
            blinkingPin = Pi.Gpio[P1.Gpio17];

            // Configure the pin as an output
            blinkingPin.PinMode = GpioPinDriveMode.Output;

            // perform writes to the pin by toggling the isOn variable
            var isOn = false;
            for (var i = 0; i < 20; i++)
            {
                isOn = !isOn;
                blinkingPin.Write(isOn);
                Thread.Sleep(500);
            }
        }

        /// <summary>
        /// Tests the hardware PWM.
        /// </summary>
        public static void TestHardwarePwm()
        {
            // TODO: Check out:
            // https://raspberrypi.stackexchange.com/questions/4906/control-hardware-pwm-frequency
            // https://stackoverflow.com/questions/20081286/controlling-a-servo-with-raspberry-pi-using-the-hardware-pwm-with-wiringpi
            var pin = Pi.Gpio[P1.Gpio18];
            pin.PinMode = GpioPinDriveMode.PwmOutput;
            pin.PwmMode = PwmMode.MarkSign;
            pin.PwmClockDivisor = 3; // 1 is 4096, possible values are all powers of 2 starting from 2 to 2048
            pin.PwmRange = 800; // Range valid values I still need to investigate
            pin.PwmRegister = 600; // (int)(pin.PwmRange * 0.95); // This goes from 0 to 1024

            var probe = new LogicProbe(Pi.Gpio[P1.Gpio17]);
            var probeBuffer = new List<LogicProbe.ProbeDataEventArgs>(1024);
            probe.ProbeDataAvailable += (s, e) =>
            {
                probeBuffer.Add(e);
                if (probeBuffer.Count >= 64)
                {
                    probe.Stop();
                }
            };

            probe.Start();
            while (probe.IsRunning)
                Thread.Sleep(15);

            Thread.Sleep(1000);
            foreach (var entry in probeBuffer)
            {
                Console.WriteLine(entry.ToString());
            }

            Console.WriteLine("finished");
        }

        private static void TestSystemInfo()
        {
            $"GPIO Controller initialized successfully with {Pi.Gpio.Count} pins".Info();
            $"{Pi.Info}".Info();
            $"Microseconds Since GPIO Setup: {Pi.Timing.MicrosecondsSinceSetup}".Info();
            $"Uname {Pi.Info.OperatingSystem}".Info();
            $"HostName {NetworkSettings.Instance.HostName}".Info();
            $"Uptime (seconds) {Pi.Info.Uptime}".Info();
            var timeSpan = Pi.Info.UptimeTimeSpan;
            $"Uptime (timespan) {timeSpan.Days} days {timeSpan.Hours:00}:{timeSpan.Minutes:00}:{timeSpan.Seconds:00}"
                .Info();

            NetworkSettings.Instance.RetrieveAdapters()
                .Select(adapter =>
                    $"Adapter: {adapter.Name,6} | IPv4: {adapter.IPv4,16} | IPv6: {adapter.IPv6,28} | AP: {adapter.AccessPointName,16} | MAC: {adapter.MacAddress,18}")
                .ToList()
                .ForEach(x => x.Info());
        }

        private static void TestCaptureImage()
        {
            var pictureBytes = Pi.Camera.CaptureImageJpeg(640, 480);
            const string targetPath = "/home/pi/picture.jpg";

            if (File.Exists(targetPath))
                File.Delete(targetPath);

            File.WriteAllBytes(targetPath, pictureBytes);
            $"Took picture -- Byte count: {pictureBytes.Length}".Info();
        }

        private static void TestCaptureVideo()
        {
            // Setup our working variables
            var videoByteCount = 0;
            var videoEventCount = 0;
            var startTime = DateTime.UtcNow;

            // Configure video settings
            var videoSettings = new CameraVideoSettings()
            {
                CaptureTimeoutMilliseconds = 0,
                CaptureDisplayPreview = false,
                ImageFlipVertically = true,
                CaptureExposure = CameraExposureMode.Night,
                CaptureWidth = 1920,
                CaptureHeight = 1080
            };

            try
            {
                // Start the video recording
                Pi.Camera.OpenVideoStream(videoSettings,
                    onDataCallback: (data) =>
                    {
                        videoByteCount += data.Length;
                        videoEventCount++;
                    },
                    onExitCallback: null);

                // Wait for user interaction
                startTime = DateTime.UtcNow;
                "Press any key to stop reading the video stream . . .".ReadKey();
            }
            catch (Exception ex)
            {
                $"{ex.GetType()}: {ex.Message}".Error();
            }
            finally
            {
                // Always close the video stream to ensure raspivid quits
                Pi.Camera.CloseVideoStream();

                // Output the stats
                var megaBytesReceived = (videoByteCount / (1024f * 1024f)).ToString("0.000");
                var recordedSeconds = DateTime.UtcNow.Subtract(startTime).TotalSeconds.ToString("0.000");
                $"Capture Stopped. Received {megaBytesReceived} Mbytes in {videoEventCount} callbacks in {recordedSeconds} seconds"
                    .Info();
            }
        }

        private static void TestColors()
        {
            var colors = new CameraColor[]
            {
                CameraColor.Black,
                CameraColor.White,
                CameraColor.Red,
                CameraColor.Green,
                CameraColor.Blue
            };

            foreach (var color in colors)
            {
                $"{color.Name,-15}: RGB Hex: {color.ToRgbHex(false)}    YUV Hex: {color.ToYuvHex(true)}".Info();
            }
        }
    }
}

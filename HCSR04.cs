using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Gpio;

namespace HardwareDevices
{
    // Untested, use at your own risk etc
    public class HCSR04
    {
        private GpioPin triggerPin { get; set; }
        private GpioPin echoPin { get; set; }

        private const double SPEED_OF_SOUND_METERS_PER_SECOND = 343;
        private static Stopwatch stopWatch = new Stopwatch();
        private static ManualResetEvent manualResetEvent = new ManualResetEvent(false);

        public HCSR04(int triggerPin, int echoPin)
        {
            GpioController controller = GpioController.GetDefault();

            //initialize trigger pin.
            this.triggerPin = controller.OpenPin(triggerPin);
            this.triggerPin.SetDriveMode(GpioPinDriveMode.Output);

            //initialize echo pin.
            this.echoPin = controller.OpenPin(echoPin);
            this.echoPin.SetDriveMode(GpioPinDriveMode.Input);
        }

        public double Distance
        {
            get
            {
                // convert the time into a distance
                // duration of pulse * speed of sound (343m/s)
                // remember to divide by two because we're measuring the time for the signal to reach the object, and return.
                return (SPEED_OF_SOUND_METERS_PER_SECOND / 2) * LengthOfHighPulse;
            }
        }

        private static double GetTimeUntilNextEdge(GpioPin pin, GpioPinValue edgeToWaitFor, int maximumTimeToWaitInMilliseconds)
        {
            var t = Task.Run(() =>
            {
                stopWatch.Reset();

                while (pin.Read() != edgeToWaitFor) { };

                stopWatch.Start();

                while (pin.Read() == edgeToWaitFor) { };

                stopWatch.Stop();

                return stopWatch.Elapsed.TotalSeconds;
            });

            bool isCompleted = t.Wait(TimeSpan.FromMilliseconds(maximumTimeToWaitInMilliseconds));

            if (isCompleted)
            {
                return t.Result;
            }
            else
            {
                return -1d;
            }
        }

        private static void Sleep(int delayMicroseconds)
        {
            manualResetEvent.WaitOne(
                TimeSpan.FromMilliseconds((double)delayMicroseconds / 1000d));
        }

        private double LengthOfHighPulse
        {
            get
            {
                // The sensor is triggered by a logic 1 pulse of 10 or more microseconds.
                // We give a short logic 0 pulse first to ensure a clean logic 1.
                this.triggerPin.Write(GpioPinValue.Low);
                Sleep(5);
                this.triggerPin.Write(GpioPinValue.High);
                Sleep(10);
                this.triggerPin.Write(GpioPinValue.Low);

                // Read the signal from the sensor: a HIGH pulse whose
                // duration is the time (in microseconds) from the sending
                // of the ping to the reception of its echo off of an object.
                return GetTimeUntilNextEdge(echoPin, GpioPinValue.High, 100);
            }
        }

    }
}

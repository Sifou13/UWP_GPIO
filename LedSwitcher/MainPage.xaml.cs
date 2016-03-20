using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Devices.Gpio;
using System.Threading.Tasks;
using System.Threading;


// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace LedSwitcher
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        GpioController controller;
        GpioPin yellowLedPin_5;
        GpioPin rgbLedPin_6;
        GpioPin rgbLedPinp_13;
        GpioPin rgbLedPin_17;
        GpioPin buttonPin_27;

        GpioOpenStatus status;

        CancellationTokenSource tokenSource;
        CancellationToken token;

        public MainPage()
        {
            this.InitializeComponent();

            controller = GpioController.GetDefault();
                        
            controller.TryOpenPin(5, GpioSharingMode.Exclusive, out yellowLedPin_5, out status);
            controller.TryOpenPin(6, GpioSharingMode.Exclusive, out rgbLedPin_6, out status);
            controller.TryOpenPin(13, GpioSharingMode.Exclusive, out rgbLedPinp_13, out status);
            controller.TryOpenPin(17, GpioSharingMode.Exclusive, out rgbLedPin_17, out status);
            controller.TryOpenPin(27, GpioSharingMode.Exclusive, out buttonPin_27, out status);
            
            if (buttonPin_27.IsDriveModeSupported(GpioPinDriveMode.InputPullUp))
                buttonPin_27.SetDriveMode(GpioPinDriveMode.InputPullUp);
            else
                buttonPin_27.SetDriveMode(GpioPinDriveMode.Input);

            yellowLedPin_5.Write(GpioPinValue.Low);
            rgbLedPin_6.Write(GpioPinValue.Low);
            rgbLedPinp_13.Write(GpioPinValue.Low);
            rgbLedPin_17.Write(GpioPinValue.Low);

            yellowLedPin_5.SetDriveMode(GpioPinDriveMode.Output);
            rgbLedPin_6.SetDriveMode(GpioPinDriveMode.Output);
            rgbLedPinp_13.SetDriveMode(GpioPinDriveMode.Output);
            rgbLedPin_17.SetDriveMode(GpioPinDriveMode.Output);

            buttonPin_27.ValueChanged += ButtonPin_27_ValueChanged;
            buttonPin_27.DebounceTimeout = TimeSpan.FromMilliseconds(50);

            txtStatus.Text = status.ToString();
        }

        private void ButtonPin_27_ValueChanged(GpioPin sender, GpioPinValueChangedEventArgs args)
        {
            GpioPin[] pinArray = new GpioPin[] { rgbLedPin_6, rgbLedPinp_13, rgbLedPin_17, yellowLedPin_5 };

            if (args.Edge == GpioPinEdge.RisingEdge)
                return;

            if (tokenSource == null)
            {
                tokenSource = new CancellationTokenSource();
                token = tokenSource.Token;

                DateTime startTime = DateTime.UtcNow;

                Task task = Task.Factory.StartNew((() =>
                {
                    while (!token.IsCancellationRequested)
                    {
                        token.ThrowIfCancellationRequested();

                        int currentIndex = new Random().Next(0, 3);

                        GpioPinValue pinValue = pinArray[currentIndex].Read();

                        pinArray[currentIndex].Write((GpioPinValue)Math.Abs((int)pinValue - 1));

                        System.Threading.Tasks.Task.Delay(250).Wait();
                    }
                }), token);
            }
            else
            {
                tokenSource.Cancel();
                tokenSource.Dispose();

                tokenSource = null;

                while (pinArray.Any(x => x.Read() == GpioPinValue.High))
                {
                    pinArray.Where(pin => pin.Read() == GpioPinValue.High).ToList().ForEach(x => x.Write(GpioPinValue.Low));
                }
            }

        }

        private void btnYellow_Click(object sender, RoutedEventArgs e)
        {
            TogglePin(rgbLedPin_17);
        }

        private void TogglePin(GpioPin pin)
        {
            if (controller != null)
            {
                GpioPinValue pinValue = pin.Read();

                pin.Write((GpioPinValue)Math.Abs((int)pinValue - 1));
            }
        }

        private void btnBlue_Click(object sender, RoutedEventArgs e)
        {
            TogglePin(rgbLedPinp_13);
        }

        private void btnRed_Click(object sender, RoutedEventArgs e)
        {
            TogglePin(rgbLedPin_6);
        }

        private void btnGreen_Click(object sender, RoutedEventArgs e)
        {
            TogglePin(yellowLedPin_5);
        }
    }
}

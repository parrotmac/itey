using System.Device.Gpio;

namespace itey.Services;

public class GPIOPinServiceEventArgs : EventArgs
{
    public int PinNumber { get; set; }
}

public class GPIOPinService : IDisposable
{
    public event EventHandler? PinPulsed;
    private readonly Thread gpioThread;
    private readonly int[] pinsToMonitor;
    private GpioController gpioController;
    private bool running;

    public GPIOPinService(int[] pinNumbers)
    {
        // Initialize the pins
        pinsToMonitor = pinNumbers;

        gpioController = new GpioController(PinNumberingScheme.Logical);
        // Setup
        foreach (var pin in pinsToMonitor)
        {
            gpioController.OpenPin(pin, PinMode.InputPullDown);
        }

        // Start a new thread that monitors the pin status
        running = true;
        gpioThread = new Thread(MonitorPins) { IsBackground = true };
        gpioThread.Start();
    }

    private void MonitorPins()
    {
        while (running)
        {
            foreach (var pin in pinsToMonitor)
            {
                // Check if pin is high
                if (gpioController.Read(pin) != PinValue.High) continue;
                // Raise event
                PinPulsed?.Invoke(this, new GPIOPinServiceEventArgs { PinNumber = pin });
            }

            Thread.Sleep(100); // Sleep for 100 milliseconds
        }
    }

    public void Dispose()
    {
        running = false;
        gpioThread.Join();
        gpioController.Dispose();
    }
}
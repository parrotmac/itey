using System.Diagnostics;
using Grpc.Core;
using Iot.Device.BuildHat;
using Iot.Device.BuildHat.Models;
using Iot.Device.BuildHat.Motors;

namespace itey.Services;


public class CommanderService : Commander.CommanderBase
{
    private readonly ILogger<CommanderService> _logger;
    private static Brick? _brick;
    private static GPIOPinService? _gpioPinService;
    
    public CommanderService(ILogger<CommanderService> logger)
    {
        var serialPort = Environment.GetEnvironmentVariable("SERIAL_PORT") ?? "/dev/serial0";
        _logger = logger;
        _brick ??= new Brick(serialPort);
        if (_gpioPinService == null)
        {
            _gpioPinService = new GPIOPinService(new int[] { 12, 13, 18, 19 });
            _gpioPinService.PinPulsed += (sender, e) =>
            {
                if (e is not GPIOPinServiceEventArgs args) return;
                switch (args.PinNumber)
                {
                    case 12:
                        ChooseAction("black");
                        break;
                    case 13:
                        ChooseAction("blue");
                        break;
                    case 18:
                        ChooseAction("yellow");
                        break;
                    case 19:
                        ChooseAction("red");
                        break;
                    default:
                        Console.WriteLine("Unknown pin pulsed: " + args.ToString());
                        break;
                }
            };
        }
        Console.WriteLine("Waiting for Build Hat Information: " + _brick.BuildHatInformation.ToString());
        Console.WriteLine("Waiting for sensor to connect to port A");
        _brick.WaitForSensorToConnect(SensorPort.PortA);
        Console.WriteLine("Sensor connected to port A");
    }

    private void NerdsTowerDispense()
    {
        if (_brick == null) return;
        var motorB = (ActiveMotor)_brick.GetMotor(SensorPort.PortB);
        
        motorB.TargetSpeed = 10;
        motorB.MoveToPosition(-160, true);
        motorB.TargetSpeed = 30;
        motorB.MoveToPosition(-40, true);
        motorB.TargetSpeed = 10;
        motorB.MoveToPosition(-5, true);
        
        Thread.Sleep(750);
        
        motorB.MoveToPosition(-180, true);
    }
    
    private void PencilDispense()
    {
        if (_brick == null) return;
        var motorA = (ActiveMotor)_brick.GetMotor(SensorPort.PortA);
        motorA.TargetSpeed = 40;
        motorA.MoveToPosition(-40, true);
        Thread.Sleep(500);
        motorA.MoveToPosition(0, true);
    }
    
    public override Task<EchoResponse> Echo(EchoRequest request, ServerCallContext context)
    {
        return Task.FromResult(new EchoResponse
        {
            Message = request.Message
        });
    }
    
    public override Task<HatInfoResponse> GetHatInfo(HatInfoRequest request, ServerCallContext context)
    {
        if (_brick == null) throw new Exception("Brick not initialized");
        
        var info = _brick.BuildHatInformation;
        var signature = "";
        if (info?.Signature != null) {
            signature = BitConverter.ToString(info.Signature).Replace("-", "");
        }
        return Task.FromResult(new HatInfoResponse
        {
            Version = info?.Version,
            FirmwareDate = info?.FirmwareDate.ToString(),
            Signature = signature,
            InputVoltage = (float)_brick.InputVoltage.Volts,
        });
    }
    
    public override Task<SetMotorPositionResponse> SetMotorPosition(SetMotorPositionRequest request, ServerCallContext context)
    {
        if (_brick == null) throw new Exception("Brick not initialized");
        var motorA = (ActiveMotor)_brick.GetMotor(SensorPort.PortA);
        switch (request.MotorID)
        {
            case "A":
                var direction = PositionWay.Shortest;
                switch (request.Direction.ToLower())
                {
                    case "shortest":
                    case "s":
                        direction = PositionWay.Shortest;
                        break;
                    case "clockwise":
                    case "c":
                        direction = PositionWay.Clockwise;
                        break;
                    case "anticlockwise":
                    case "a":
                        direction = PositionWay.AntiClockwise;
                        break;
                    default:
                        break;
                }
                motorA.TargetSpeed = (int)request.Speed;
                var position = (int)request.Position;
                Console.WriteLine("SetMotorPosition: Moving motor " + request.MotorID + " to " + position + " degrees " + direction);
                // motorA.MoveToAbsolutePosition(position, direction, true);
                motorA.MoveToPosition(position, true);
                break;
            default:
                Console.WriteLine("SetMotorPosition: Not implemented for motor " + request.MotorID);
                break;
        }
        
        return Task.FromResult(new SetMotorPositionResponse
        {
            Position = request.Position
        });
    }
    
    public override Task<GetMotorPositionResponse> GetMotorPosition(GetMotorPositionRequest request, ServerCallContext context)
    {
        if (_brick == null) throw new Exception("Brick not initialized");
        var motorA = (ActiveMotor)_brick.GetMotor(SensorPort.PortA);
        var motorB = (ActiveMotor)_brick.GetMotor(SensorPort.PortB);
        var position = 0;
        switch (request.MotorID)
        {
            case "A":
                position = motorA.Position;
                break;
            case "B":
                position = motorB.Position;
                break;
            default:
                Console.WriteLine("GetMotorPosition: Not implemented for motor " + request.MotorID);
                break;
        }
        
        return Task.FromResult(new GetMotorPositionResponse
        {
            Position = position
        });
    }
    
    public override Task<NerdsTowerCalibrateResponse> NerdsTowerCalibrate(NerdsTowerCalibrateRequest request, ServerCallContext context)
    {
        if (_brick == null) throw new Exception("Brick not initialized");
        var motorB = (ActiveMotor)_brick.GetMotor(SensorPort.PortB);
        const int cycles = 5;
        const int zeroPosition = -180;
        motorB.TargetSpeed = 20;
        for(var i = 0; i < cycles; i++)
        {
            motorB.MoveToPosition(zeroPosition, true);
            Thread.Sleep(100);
            motorB.MoveToPosition(zeroPosition + 10, true);
            Thread.Sleep(100);
        }
        motorB.MoveToAbsolutePosition(zeroPosition, PositionWay.Shortest, true);
        
        return Task.FromResult(new NerdsTowerCalibrateResponse
        {
            Position = motorB.Position
        });
    }
    
    public override Task<NerdsTowerDispenseResponse> NerdsTowerDispense(NerdsTowerDispenseRequest request, ServerCallContext context)
    {
        NerdsTowerDispense();
        return Task.FromResult(new NerdsTowerDispenseResponse());
    }
    
    
    public override Task<PencilDispenseResponse> PencilDispense(PencilDispenseRequest request, ServerCallContext context)
    {
        PencilDispense();
        return Task.FromResult(new PencilDispenseResponse());
    }
    
    
    
    
    
    /*
     * This code lets us choose what happens when a button is pressed.
     */
    void ChooseAction(String buttonColor)
    {
        if (buttonColor == "red")
        {
            Console.WriteLine("Red button pressed");
        }
        else if (buttonColor == "blue")
        {
            Console.WriteLine("Blue button pressed");
        }
        else if (buttonColor == "yellow")
        {
            PencilDispense();
        }
        else if (buttonColor == "black")
        {
            NerdsTowerDispense();
        }
        else
        {
            Console.WriteLine("I don't know what to do with " + buttonColor);
        }
    }
}
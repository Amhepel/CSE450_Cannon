//using System;
//using System.Threading;
//using Microsoft.SPOT;
//using Microsoft.SPOT.Hardware;

//namespace Hero_Simple_Application2
//{
//    public class Program
//    {
//        public static void Main()
//        {
//            /* create a gamepad object */
//            CTRE.Phoenix.Controller.GameController myGamepad = new
//            CTRE.Phoenix.Controller.GameController(new CTRE.Phoenix.UsbHostDevice(0));
//            CTRE.Phoenix.MotorControl.CAN.TalonSRX myTalon = new
//            CTRE.Phoenix.MotorControl.CAN.TalonSRX(1);
//            CTRE.Phoenix.MotorControl.CAN.TalonSRX myTalon2 = new
//            CTRE.Phoenix.MotorControl.CAN.TalonSRX(2);
//            /* simple counter to print and watch using t3he debugger */
//            int counter = 0;
//            /* loop forever */

//            while (true)
//            {


//                /* added inside the while loop */
//                if (myGamepad.GetConnectionStatus() == CTRE.Phoenix.UsbDeviceConnection.Connected)
//                {

//                    /* print the axis value */
//                    if (myGamepad.GetAxis(0)!=0)
//                    {
//                        Debug.Print("axis0:" + myGamepad.GetAxis(0));
//                    }
//                    if (-0.1 > myGamepad.GetAxis(1) || myGamepad.GetAxis(1) > 0.1)
//                    {
//                        Debug.Print("axis1:" + myGamepad.GetAxis(1));
//                    }
//                    if (myGamepad.GetAxis(2) != 0)
//                    {
//                        Debug.Print("axis2:" + myGamepad.GetAxis(2));
//                    }
//                    if (myGamepad.GetAxis(3) != 0)
//                    {
//                        Debug.Print("axis3:" + myGamepad.GetAxis(3));
//                    }


//                    myTalon.Set(CTRE.Phoenix.MotorControl.ControlMode.PercentOutput, myGamepad.GetAxis(0));
//                    myTalon2.Set(CTRE.Phoenix.MotorControl.ControlMode.PercentOutput, myGamepad.GetAxis(3));

//                    /* allow motor control */
//                    CTRE.Phoenix.Watchdog.Feed();
//                }
//                /* increment counter */
//                ++counter; /* try to land a breakpoint here and hover over 'counter' to see it's current
//value. Or add it to the Watch Tab */
//                /* wait a bit */
//                System.Threading.Thread.Sleep(10);
//            }
//        }
//    }
//}



/**
 * Test project for using the Xbox360 Controller (and F710 gamepad in XInput Mode).
 * This will be supported in the next release of the CTR Toolsuite (Coming soon).
 * This test project will likely require HERO 0.14.X.X (expect a few tweaks before then).
 * Xinput mode on F710 gives us...
 * - analog shoulder triggers (not just digitial)
 * - vibration
 * - LED control (if using XBOX 360 controller)
 * - More resolute axis data (16bit)
 */

using System;
using Microsoft.SPOT;
using System.Text;

using CTRE.Phoenix;
using CTRE.Phoenix.Controller;
using CTRE.Phoenix.MotorControl;
using CTRE.Phoenix.MotorControl.CAN;

namespace HERO_XInput_Gampad_Example
{
    public class Program
    {
        /* the goal is to plug in a Xinput Logitech Gamepad or Xbox360 style controller */
        GameController _gamepad = new GameController(UsbHostDevice.GetInstance(0));

        TalonSRX _tal = new TalonSRX(1);
        TalonSRX _tal2 = new TalonSRX(2);

        /* Create the DriverModule to control some LEDs or solenoids */
        CTRE.Gadgeteer.Module.DriverModule _driver = new CTRE.Gadgeteer.Module.DriverModule(CTRE.HERO.IO.Port5);


        bool[] _buttons = new bool[20];

        float[] _sticks = new float[6];

        /* dont let rumbling go for too long, otherwise it will eat up battery */
        int _rumblingTimeMs = 0;
        int _rumblinSt = 0;

        public void RunForever()
        {
            /* enable XInput, if gamepad is in DInput it will disable robot.  This way you can
             * use X mode for drive, and D mode for disable (instead of vice versa as the 
             * stock HERO implementation traditionally does). */
            UsbHostDevice.GetInstance(0).SetSelectableXInputFilter(UsbHostDevice.SelectableXInputFilter.XInputDevices);
            /* Factory Default all hardware to prevent unexpected behaviour */
            _tal.ConfigFactoryDefault();
            _tal2.ConfigFactoryDefault();
            while (true)
            {
                if (_gamepad.GetConnectionStatus() == UsbDeviceConnection.Connected)
                {
                    Watchdog.Feed();
                }

                /* get buttons */
                bool[] btns = new bool[_buttons.Length];
                for (uint i = 1; i < 20; ++i)
                    btns[i] = _gamepad.GetButton(i);

                /* get sticks */
                for (uint i = 0; i < _sticks.Length; ++i)
                    _sticks[i] = _gamepad.GetAxis(i);

                /* yield for a bit, and track timeouts */
                System.Threading.Thread.Sleep(10);
                if (_rumblingTimeMs < 5000)
                    _rumblingTimeMs += 10;

                /* update the Talon using the shoulder analog triggers */
                //_tal.Set(ControlMode.PercentOutput, (_sticks[5] - _sticks[4]) * 0.60f);

                if (_sticks[4] == 1) // Only enable tal
                {
                    _tal.Set(0, (_sticks[3] - _sticks[2]) * 0.60f);
                }
                else if (_sticks[5] == 1) // only enable tal2
                {
                    _tal2.Set(0, (_sticks[2] - _sticks[3]) * 0.60f);
                }
                else // Default enable both when triggers not pressed
                {
                    _tal.Set(0, (_sticks[3] - _sticks[2]) * 0.60f);
                    _tal2.Set(0, (_sticks[2] - _sticks[3]) * 0.60f);
                }          

                /* fire some solenoids based on buttons */
                _driver.Set(1, _buttons[1]);
                _driver.Set(2, _buttons[2]);
                _driver.Set(3, _buttons[3]);
                _driver.Set(4, _buttons[4]);

                /* rumble state machine */
                switch (_rumblinSt)
                {
                    /* rumbling is disabled, require some off time to save battery */
                    case 0:
                        _gamepad.SetLeftRumble(0);
                        _gamepad.SetRightRumble(0);

                        if (_rumblingTimeMs < 100)
                        {
                            /* waiting for off-time */
                        }
                        else if ((btns[1] && !_buttons[1])) /* button off => on */
                        {
                            /* off time long enough, user pressed btn */
                            _rumblingTimeMs = 0;
                            _rumblinSt = 1;
                            _gamepad.SetLeftRumble(0xFF);
                        }
                        else if ((btns[2] && !_buttons[2])) /* button off => on */
                        {
                            /* off time long enough, user pressed btn */
                            _rumblingTimeMs = 0;
                            _rumblinSt = 1;
                            _gamepad.SetRightRumble(0xFF);
                        }
                        break;
                    /* already vibrating, track the time */
                    case 1:
                        if (_rumblingTimeMs > 500)
                        {
                            /* vibrating too long, turn off now */
                            _rumblingTimeMs = 0;
                            _rumblinSt = 0;
                            _gamepad.SetLeftRumble(0);
                            _gamepad.SetRightRumble(0);
                        }
                        else if ((btns[3] && !_buttons[3]))  /* button off => on */
                        {
                            /* immedietely turn off */
                            _rumblingTimeMs = 0;
                            _rumblinSt = 0;
                            _gamepad.SetLeftRumble(0);
                            _gamepad.SetRightRumble(0);
                        }
                        else if ((btns[1] && !_buttons[1])) /* button off => on */
                        {
                            _gamepad.SetLeftRumble(0xFF);
                        }
                        else if ((btns[2] && !_buttons[2])) /* button off => on */
                        {
                            _gamepad.SetRightRumble(0xFF);
                        }
                        break;
                }
                /* this will likley be replaced with a strongly typed interface,
                 * control the LEDs on the center XBOX emblem. */
                if (btns[5] && !_buttons[5]) { _gamepad.SetLEDCode(6); }
                if (btns[6] && !_buttons[6]) { _gamepad.SetLEDCode(7); }
                if (btns[7] && !_buttons[7]) { _gamepad.SetLEDCode(8); }
                if (btns[8] && !_buttons[8]) { _gamepad.SetLEDCode(9); }

                /* build line to print */
                StringBuilder sb = new StringBuilder();
                foreach (float stick in _sticks)
                {
                    sb.Append(Format(stick));
                    sb.Append(",");
                }

                sb.Append("-");
                for (uint i = 1; i < _buttons.Length; ++i)
                {
                    if (_buttons[i])
                    {
                        sb.Append("b" + i + ",");
                    }
                }

                /* print useful info */
                sb.AppendLine();
                Debug.Print(sb.ToString());

                /* save button states for button-change states */
                _buttons = btns;
            }
        }
        /**
         * @param x arbitrary float
         * @return string version of x truncated to "X.XX" 
         */
        String Format(float x)
        {
            x *= 100;
            x = (int)x;
            x *= 0.01f;
            return "" + x;
        }
        public static void Main()
        {
            new Program().RunForever();
        }
    }
}

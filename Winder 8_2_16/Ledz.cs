/*------------------------------------------------------------------------------
File: Ledz.cs                                          Updated: 12/03/2012

Objective: Ledz class -- for lighting the LEDs connected to the U-HID on the
           Winder button box. Requires PacDrive.dll in the same folder as
           Winder.exe.
See Also: Bbox.cs, Buttons.txt
Problems:
Modifications:
 Date       Who   Comments
--------------------------------------------------------------------------------
This code was mostly extracted from PacDrive.cs in the PacDrive SDK, by Ben 
Baker. The only device of interest to Winder is the U-HID, and the only action
of interest is lighting (and extinguishing) the LEDs, so considerable 
simplification was done. In particular, since id, in the PacDrive calls, must be
zero, it is not a needed input to the methods here. A constructor and method 
FlashLED were added, along with methods SetAllLEDsOn and SetAllLEDsOff.

The LEDs are wired "upside down" on the U-HID. That is, using PacDrive to turn
them on actually turns them off, and vice versa. This reversal is accomplished
internally here when the PacDrive methods are called.

While PacDrive accomodates 16 LEDs, there are only 8 configured on the U-HID.
They are the lights for button numbers on the "button box" as follows:
  LED 0 => button 6, LED 1 => button 10, LED 2 => button 8, LED 3 => button 9,
  LED 4 => button 5, LED 5 => button  4, LED 6 => button 3, LED 7 => button 15
------------------------------------------------------------------------------*/
using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.IO;

using Zlib;  // Logz class

namespace WinderProject
{
  class Ledz
  {
    //================= INSTANCE VARIABLES and PROPERTIES ======================
    public enum DevType  // name changed to avoid SlimDX conflict
    { Unknown, PacDrive, UHID, PacLED64 };

    private static int numDev = 0;  // formerly called m_numDevices
    public  static int NumDevices { get { return numDev; } }

    public static bool NoFlash = false;  // terminates FlashLED if true

    [DllImport("PacDrive.dll", CallingConvention = CallingConvention.StdCall)]
    private static extern int PacInitialize();

    [DllImport("PacDrive.dll", CallingConvention = CallingConvention.StdCall)]
    private static extern void PacShutdown();

    [DllImport("PacDrive.dll", CallingConvention = CallingConvention.StdCall)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PacSetLEDStates(int id, ushort data);

    [DllImport("PacDrive.dll", CallingConvention = CallingConvention.StdCall)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PacSetLEDState(int id, int port, 
                                  [MarshalAs(UnmanagedType.Bool)] bool state);

    [DllImport("PacDrive.dll", CallingConvention = CallingConvention.StdCall)]
    private static extern int PacGetDeviceType(int id);

    //============================== METHODS ===================================
    /*--------------------------------------------------------------------------
     Initialize: initializes the LEDs. Returns true if one and only one device
       is found; returns false otherwise. Because the LEDs are wired "upside 
       down", they all turn on when power is first applied. They are all turned
       off here. Then, LEDs 0, 2, 4, 6, and 7 are turned on.
    --------------------------------------------------------------------------*/
    public static bool Initialize()
    {
      numDev = PacInitialize();
      if (numDev == 0)
      {
        Util.W2("Error: No PacDrive devices found");
        return false;
      }

      if (numDev != 1)
      {
        Util.W2("Error: More than one PacDrive device found");
        return false;
      }

      SetAllLEDsOff();  // turn off all the LEDs
      Delay(1000);
      SetLEDStates((ushort)213);  // turn on LEDs  0,  2,  4,   6, and   7 
      Delay(1000);                //  box buttons  6,  8,  5,   3, and  15
      return true;
    }  // end Initialize

    /*--------------------------------------------------------------------------
     Shutdown: shuts down all PacDrive, PacLED64, and U-HID devices
    --------------------------------------------------------------------------*/
    public static void Shutdown()
    {
      PacShutdown();
    }

    /*--------------------------------------------------------------------------
     GetDeviceType: returns the type (corresponding to a string in the DevType
       enum) of device Id. 
     
     (In Winder, the type should be "U-HID"). ??? no longer used
    --------------------------------------------------------------------------*/
    public static DevType GetDeviceType(int Id)
    {
      return (DevType)PacGetDeviceType(Id);
    }

    /*--------------------------------------------------------------------------
     SetLEDStates: sets the states of all 16 LEDs
     
     If input Data is a ushort of 16 bits, each bit corresponds to one LED; 
     bit n = 1 sets LED n on, bit n = 0 sets LED n off.
     
     If input Data is an array of 16 booleans, then Data[n] = true sets Led n
     on and Data[n] = false sets LED n off.
     
     Returns true for success, false for failure.
    --------------------------------------------------------------------------*/
    public static bool SetLEDStates(ushort Data)
    {
      return PacSetLEDStates(0, (ushort)(65535 - Data));
    }

    public bool SetLEDStates(bool[] Data)
    {
      ushort dataSend = 0;
      for (int i = 0; i < Data.Length; i++)
        if (!Data[i]) dataSend |= (ushort)(1 << i);

      return PacSetLEDStates(0, dataSend);
    }  // end SetLEDStates

    /*--------------------------------------------------------------------------
     SetLEDState: sets the state of one LED (numbered Port)
     
     Input Port is the LED number. if input State = true, that LED is set on, 
     while if input State = false, that LED is set off.
     
     Returns true for success, false for failure.
    --------------------------------------------------------------------------*/
    public static bool SetLEDState(int Port, bool State)
    {
      return PacSetLEDState(0, Port, !State);
    }  // end SetLEDState

    /*--------------------------------------------------------------------------
     SetAllLEDsOn:
    --------------------------------------------------------------------------*/
    public static bool SetAllLEDsOn()
    {
      return PacSetLEDStates(0, (ushort)0);
    }  // end SetAllLEDsOn

    /*--------------------------------------------------------------------------
     SetAllLEDsOff:
    --------------------------------------------------------------------------*/
    public static bool SetAllLEDsOff()
    {
      return PacSetLEDStates(0, (ushort)65535);
    }  // end SetAllLEDsOff

    /*--------------------------------------------------------------------------
     FlashLED: flashes LED number Port for ncycles cycles. ton and toff are the
       on and off time durations, each cycle, in seconds.
     
     If class variable NoFlash is false, the LED will be extinguished and the
     method terminated. This is primarily to support running FlashLED in its
     own thread. Before returning, NoFlash is reset to false.
    --------------------------------------------------------------------------*/
    public static void FlashLED(int Port, int ncycles, double ton, double toff)
    {
      // Set up the timer.
      TimeSpan tson = new TimeSpan((int)(ton * 10000000));    // time span on
      TimeSpan tsoff = new TimeSpan((int)(toff * 10000000));  // time span off

      for (int n = 0; n < ncycles; n++)  // loop over the on-off cycles
      {
        SetLEDState(Port, true);   // turn  on LED Port --------
        DateTime tend = DateTime.Now + tson;
        while (DateTime.Now < tend)    // waste time for ton sec
          if (NoFlash)
          {
            NoFlash = false;
            SetLEDState(Port, false);  // turn off LED Port
            return;
          }

        SetLEDState(Port, false);  // turn off LED Port --------
        tend = DateTime.Now + tsoff;
        while (DateTime.Now < tend)    // waste time for toff sec
          if (NoFlash)
          {
            NoFlash = false;  // LED Port is already off
            return;
          }

      }  // end for
    }  // end FlashLED

    /*--------------------------------------------------------------------------
     Delay: waste time for nmsec milleseconds
    --------------------------------------------------------------------------*/
    public static void Delay(int nmsec)
    {
      int ntics = nmsec * 10000;  // 
      TimeSpan dt = new TimeSpan(ntics);
      DateTime tend = DateTime.Now + dt;
      while (DateTime.Now < tend) ;  // waste time
    }  // end Delay

    /*--------------------------------------------------------------------------
     LED_Test: Turns on all LEDS, turns them all off, then flashes all numLeds 
       LEDs.
    --------------------------------------------------------------------------*/
    public static void LED_Test(int numLeds)
    {
      Console.WriteLine("LED_Test begins");

      SetAllLEDsOn();
      Console.WriteLine("All LEDs should be on");
      Console.WriteLine("Hit Enter to extinguish LEDs");
      Console.ReadLine();
      SetAllLEDsOff();
      Console.WriteLine("All LEDs should be off");

      for (int n = 0; n < numLeds; n++)  // flash all LEDs, one at a time
      {
        // Flash LED n, 4 cycles, on 0.5 seconds, off 0.2 seconds, each cycle.
        Console.WriteLine("Hit Enter to flash LED " + n.ToString());
        Console.ReadLine();
        FlashLED(n, 4, 0.5, 0.2);
      }
    }  // end LED_Test
  }  // end Ledz class
}  // end WinderProject namespace

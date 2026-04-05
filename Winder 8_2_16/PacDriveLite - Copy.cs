/*------------------------------------------------------------------------------
File: PacDriveLite.cs                                  Updated: 07/30/2012

Objective: PacDriveLite class -- for lighting the LEDs connected to the U-HID.
           Requires PacDrive.dll in the same folder as Winder.exe.
See Also:
Problems: WIP
Modifications:
 Date       Who   Comments
--------------------------------------------------------------------------------
This code was mostly extracted from PacDrive.cs in the PacDrive SDK, by Ben 
Baker. The only device of interest here is the U-HID, and the only action of 
interest is lighting (and extinguishing) the LEDs, so considerable 
simplification was done. A constructor and method FlashLED were added.
------------------------------------------------------------------------------*/
//..|....1....|....2....|....3....|....4....|....5....|....6....|....7....|....8
using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.IO;

using Zlib;  // Logz class

namespace WinderProject
{
  class PacDriveLite
  {
    //==========================================================================
    //           INSTANCE VARIABLES and PROPERTIES
    //==========================================================================
    private Logz lz;  // the log file

    public enum DeviceTypeL  // name changed to avoid SlimDX conflict
    {
      Unknown, PacDrive, UHID, PacLED64
    };

    private int numDev = 0;  // formerly called m_numDevices
    public int NumDevices
    {
      get { return numDev; }
    }

    public bool NoFlash = false;  // terminates FlashLED if true

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

    //==========================================================================
    //           METHODS
    //==========================================================================
    /*--------------------------------------------------------------------------
     PacDriveLite: constructor: sets the logz file and writes to it; initializes
                   the object; returns pdok as true if only one device is found;
                   returne pdok false otherwise.
    --------------------------------------------------------------------------*/
    public PacDriveLite(Logz lzx, ref bool pdok)
    {
      // Set the log file
      lz = lzx;
      lz.Sl("   --- PacDriveLite constructor ---");
      lz.Bl();

      pdok = false;  // temporary

      numDev = PacInitialize();
      lz.Sl("Number of PacDrive devices found: " + numDev.ToString());
      if (numDev == 0) return;

      for (int nd = 0; nd < numDev; nd++)
      {
        PacDriveLite.DeviceTypeL devt = GetDeviceType(nd);
        lz.Sl(" -- device " + nd.ToString() + " is type " + devt);
      }

      if (numDev != 1) return;
      pdok = true;
    }  // end constructor

    /*--------------------------------------------------------------------------
     Initialize: initializes all PacDrive, PacLED64, and U-HID devices 
                 connected. Returns the number of such devices found.
     
     In Winder use, there is expected to be only one U-HID device, so 
     m_numDevices should be 1, and the "Id" of the device in the methods here
     should be 0. This method no longer used.
    --------------------------------------------------------------------------*/
    public int Initialize()
    {
      numDev = PacInitialize();
      return numDev;
    }

    /*--------------------------------------------------------------------------
     Shutdown: shuts down all PacDrive, PacLED64, and U-HID devices
    --------------------------------------------------------------------------*/
    public void Shutdown()
    {
      PacShutdown();
    }

    /*--------------------------------------------------------------------------
     GetDeviceType: returns the type (corresponding to a string in the 
                    DeviceTypeL enum) of device Id. 
     
     (In Winder, the type should be "U-HID").
    --------------------------------------------------------------------------*/
    public DeviceTypeL GetDeviceType(int Id)
    {
        return (DeviceTypeL)PacGetDeviceType(Id);
    }

    /*--------------------------------------------------------------------------
     SetLEDStates: sets the states of all 16 LEDs
     
     If input Data is a ushort of 16 bits, each bit corresponds to one LED; 
     bit n = 1 sets LED n on, bit n = 0 sets LED n off.
     
     If input Data is an array of 16 booleans, then Data[n] = true sets Led n
     on and Data[n] = false sets LED n off.
     
     Returns true for success, false for failure.
    --------------------------------------------------------------------------*/
    public bool SetLEDStates(int Id, ushort Data)
    {
      return PacSetLEDStates(Id, Data);
    }

    public bool SetLEDStates(int Id, bool[] Data)
    {
        ushort dataSend = 0;

        for (int i = 0; i < Data.Length; i++)
            if (Data[i]) dataSend |= (ushort)(1 << i);

        return PacSetLEDStates(Id, dataSend);
    }

    /*--------------------------------------------------------------------------
     SetLEDState: sets the state of one LED
     
     Input Port is the LED number. if input State = true, that LED is set on, 
     whilw if inout State = false, that LED is set off.
     
     Returns true for success, false for failure.
    --------------------------------------------------------------------------*/
    public bool SetLEDState(int Id, int Port, bool State)
    {
      return PacSetLEDState(Id, Port, State);
    }

    /*--------------------------------------------------------------------------
     FlashLED: flashes LED number Port for ncycles cycles. ton and toff are the
               on and off time durations, each cycle, in seconds.
     
     If class variable NoFlash is false, the LED will be extinguished and the
     method terminated. This is primarily to support running FlashLED in its
     own thread. Before returning, NoFlash is reset to false.
    --------------------------------------------------------------------------*/
    public void FlashLED(int Id, int Port, int ncycles, double ton, double toff)
    {
      // Set up the timer.
      TimeSpan tson = new TimeSpan((int)(ton * 10000000));    // time span on
      TimeSpan tsoff = new TimeSpan((int)(toff * 10000000));  // time span off

      for (int n = 0; n < ncycles; n++)  // loop over the on-off cycles
      {
        SetLEDState(Id, Port, true);   // turn on LED Port
        DateTime tend = DateTime.Now + tson;
        while (DateTime.Now < tend)  // waste time for ton sec
          if (NoFlash)
          {
            NoFlash = false;
            SetLEDState(Id, Port, false);  // turn off LED Port
            return;
          }

        SetLEDState(Id, Port, false);  // turn off LED Port
        tend = DateTime.Now + tsoff;
        while (DateTime.Now < tend)  // waste time for toff sec
          if (NoFlash)
          {
            NoFlash = false;  // LED Port is already off
            return;
          }

      }  // end for
    }  // end FlashLED

  }  // end PacDriveLite class
}  // end WinderProject namespace

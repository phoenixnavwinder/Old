/*------------------------------------------------------------------------------
File: Bbox.cs                                          Updated: 01/03/2013

Objective: Bbox class code -- deals with the Winder button box
See Also: Ledz.cs, Buttons.txt
Problems: WIP
Modifications:
 Date       Who   Comments
--------------------------------------------------------------------------------
The Winder "Button Box" (BB) is a box with 21 push buttons on it, numbered from
1 to 21. There is a two-axis joystick on the box, and a foot pedal plugs into
the box. All of the buttons appear to have lights (LEDs), but only 8 of the LEDs
are configured to light. Inside the box is a Universal Human Interface Device 
(U-HID). The BB is equipped with a USB connector to communicate with a PC. 

Two different classes are used to communicate with the BB. This (Bbox) class 
depends on software known as SlimDX. It treats the box as a joystick with push 
buttons. The foot pedal is just a third joystick axis. The other class (Ledz)
derives from software known as PacDriveLite. It is used solely to light or 
extinguish the LEDs.
------------------------------------------------------------------------------*/
using System;
using System.Collections.Generic;
using System.Linq;
using SlimDX.Windows;  // some version of SlimDX.dll is used
using SlimDX.DirectInput;

using Zlib;  // Logz class

namespace WinderProject
{
  class Bbox
  {
    //================== INSTANCE VARIABLES and PROPERTIES =====================
    private static Logz lz;      // the logz file
    private static bool logzOn;  // true if logging is wanted
    public  static bool LogzOn { set { logzOn = value; } 
                                 get { return logzOn; } }
    private static bool logzXYZ;
    public  static bool LogzXYZ { set { logzXYZ = value; } 
                                  get { return logzXYZ; } }
    private static DirectInput directInput;
    private static Joystick    joystick;
    private static Guid        joystickInstanceGuid;
    private static Guid        joystickProductGuid;

    private static int BuffSize = 64;  // size of joystick states buffer

    // XYZ items --  X & Y are the joystick and Z is the foot pedal
    /*
      The raw, integer, XYZ values from the joystick (those obtained from 
      state.X, .Y, and .Z and maintained in rx, ry, and rz) have a minimum of 
      2047 (2^11 - 1) and a maximum of 65535 (2^16 - 1). The midpoint value is
      33791. After subtracting the midpoint value from X and Y, they range from
      -31744 to 31744. After subtracting the minimum value from Z, it ranges 
      from 0 to 63488. The current offset values are kept in cx, cy, and cz.
      The values in sx, sy, and sz are the "scaled" current XYZ values. 
     
      The cz (foot pedal) values are "upside down". That is, the maximum value
      corresponds to no depression, while the minimum value corresponds to
      maximum depression of the pedal. The Z scaling inverts this, as: 
               sz = 1 - cz / Zscale     (0.0 <= sz <= 1.0)
      so sz = 0 is no depression, and sz = 1 is full depression.
     
      X and Y scaling is more complex. If cx is between -XYclim and XYclim, 
      then sx is set to zero. This is the "dead zone". If cx > XYclim, then
      sx = (cx - XYclim) / XYscale, ranging from 0.0 to 1.0. If cx < XYclim, 
      then sx = (cx + XYclim) / XYscale, ranging from -1.0 to 0.0. 
      sy is similarly obtained from cy.
    */
    private static int    rx, ry, rz;  // current "raw" XYZ
    private static int    cx, cy, cz;  // current "offset" XYZ
    private static double sx, sy, sz;  // current "scaled" XYZ

    public  static double JoyX { get { return sx; } }
    public  static double JoyY { get { return sy; } }
    public  static double FootPedal { get { return sz; } }
    private static bool   footPedalP;  // true => foot pedal present
    public  static bool   FootPedalP { set { footPedalP = value; } }

    private static int    XYoff   = 33791;
    private static int    Zoff    = 2047;
    private static int    XYclim  = 2000;  //???
    private static double XYscale = 31744.0 - (double)XYclim;
    private static double Zscale  = 63488.0;

    // Button items
    private static int NBUTTONS = 21;  // the number of buttons to track
    // current button states
    private static bool[] buttonState = new bool[NBUTTONS];
    // # changes this interval
    private static int[] buttonChange = new int[NBUTTONS];
    // did any button change this interval?
    private static bool anyButtonChange; 

    // BB2CB is box button to code button, while CB2BB is code button to box 
    // button. I.e., if cb = CC2CB[cb], then bb = BB2CB[bb], where cb is a code
    // button number and bb is the corresponding box button number.
    private static int[] BB2CB = { -1,  4,  3, 14, 16, 18, 19,  5, 15, 13,
                               17,  1,  2,  0, 20,  7,  9, 11, 12, 10,  8,  6 };
    private static int[] CB2BB = { 13, 11, 12,  2,  1,  7, 21, 15, 20, 16, 
                               19, 17, 18,  9,  3,  8,  4, 10,  5,  6, 14 }; 

    // Mode items (see Buttons.txt)
    private static bool virtualRotOn;     // false => off (virtual rotation)
    public  static bool VirtualRotOn { get { return virtualRotOn; } 
                                       set { virtualRotOn = value; } }
    private static bool joystickRate;     // false => position
    private static bool joystickFast;     // false => slow
    private static bool activeBreakOn;    // false => off
    private static bool inactiveBreakOn;  // false => off
    private static bool recordOn;         // false => goto is on

    private static bool xservoOn;         // false => off
    private static bool yservoOn;         // false => off

    // Sampling items
    private static double   sampleTimeStep;   // the time step in seconds
    private static int      sampleTics;       // the time step in tics
    private static TimeSpan sampleInterval;   // time step as a TimeSpan
    private static DateTime sampleTime;       // the DateTime for next sample
    private static double   sampleTimeTotal;  // the total sampling time
    private static int      numSamplesTotal;  // the total # of samples to take
    private static int      numSamples;       // the # of samples taken so far
    public  static int      NumSamples { get { return numSamples; } }

    private static string[] buttonAct = {"X", "X", "X", "VR off", "VR on",
      "rate/position toggle", "fast/slow toggle", "swap end macro",
      "record/goto toggle", "active break on/off toggle", 
      "inactive break on/off toggle", "right start", "left start", "park",
      "home theta", "servo on/off toggle", "not used", "pause", "X", "X",
      "X", "X"};

    //=========================== PUBLIC METHODS ===============================
    /*--------------------------------------------------------------------------
     Initialize: called by Winder.Setup ??? WIP
     
     Sets the log file; initializes the LEDs; finds a gameController (joystick) 
     and, if one is found, instantiates it as a Joystick. Initializes the button 
     states. Returns false if LEDs are not initialized or no joystick is found, 
     otherwise returns true.
   
     Sets class instance variables: lz, logzOn, logzXYZ, directInput, joystick,
     joystickInstanceGuid, joystickProductGuid, buttonState, virtualRotOn,
     joystickRate, joystickFast, activeBreakOn, inactiveBreakOn, recordOn,
     xservoOn, yservoOn.
    --------------------------------------------------------------------------*/
    public static bool Initialize(Logz lzX)
    {
      lz = lzX;  // set the log file
      logzOn = true;  // default initialization
      logzXYZ = true;

      lz.Bl();
      lz.Sl("Button Box Initialization");

      Console.WriteLine();
      Console.WriteLine("Be sure power is on to the Button Box"
                        + ", then hit Enter");
      Console.ReadLine();

      // Initialize the LEDs
      bool ledzok = Ledz.Initialize();

      if (!ledzok)
      {
        Util.W2W(" -- improper LEDs (PacDrive) initialization");
        Ledz.Shutdown();
        //pacDriveI = false;
        return false;
      }
      Util.W2(" -- LEDs (PacDrive) initialized");

      directInput = new DirectInput();  // initialize DirectInput.
      joystickInstanceGuid = Guid.Empty;  // find the Game Controller Guids.

      foreach (DeviceInstance deviceInstance in
               directInput.GetDevices(DeviceClass.GameController,
               DeviceEnumerationFlags.AllDevices))
      {
        joystickInstanceGuid = deviceInstance.InstanceGuid;
        joystickProductGuid = deviceInstance.ProductGuid;
      }

      // If Joystick not found, it's over.
      if (joystickInstanceGuid == Guid.Empty)
      {
        Util.W2("Error: No GameController found.");
        return false;
      }

      // Instantiate the joystick.
      joystick = new Joystick(directInput, joystickInstanceGuid);

      if (logzOn)
      {
        lz.Bl();
        lz.Sl("Found GameController with instance GUID: "
              + joystickInstanceGuid);
        lz.Sl("                       and product GUID: "
              + joystickProductGuid);
        Capable();  // logz the joystick capabilities.
      }

      // Initialize the button states.
      for (int n = 0; n < NBUTTONS; n++) buttonState[n] = false;

      // Initialize the modes.
      virtualRotOn = false;
      joystickRate = true;
      joystickFast = true;
      activeBreakOn = false;
      inactiveBreakOn = false;
      recordOn = true;
      xservoOn = false;
      yservoOn = false;

      Util.W2(" -- Button Box initialized");
      return true;
    }  // end Initialize

    /*--------------------------------------------------------------------------
     SampleSetup: called by Winder.Setup
    --------------------------------------------------------------------------*/
    public static void SampleSetup()
    {
      // Set BufferSize in order to use buffered data.
      joystick.Properties.BufferSize = BuffSize;

      // Acquire the joystick.
      joystick.Acquire();
    }  // end SampleSetup

    /*--------------------------------------------------------------------------
     SampleTimerSetup: this version used when the number of samples is known
     
     Currently not used
    --------------------------------------------------------------------------*/
    public static void SampleTimerSetup(double sampleTimeStepx,
                                        int numSamplesTotalx)
    {
      sampleTimeStep  = sampleTimeStepx;
      numSamplesTotal = numSamplesTotalx;
      sampleTics      = (int)(sampleTimeStep * 10000000);
      sampleTimeTotal = sampleTimeStep * numSamplesTotal;
      sampleInterval  = new TimeSpan(sampleTics);

      // Write the time info to the log file.
      if (logzOn)
      {
        lz.Sl(" -- time step (seconds) : " + sampleTimeStep.ToString()
              + "   (tics) : " + sampleTics.ToString()); ;
        lz.Sl(" -- number of time steps: " + numSamplesTotal.ToString()
              + " total time (seconds): " + sampleTimeTotal.ToString());
      }
    }

    /*--------------------------------------------------------------------------
     SampleTimerSetup: this version used when the number of samples is not known
     
     Called by Winder.Setup
    --------------------------------------------------------------------------*/
    public static void SampleTimerSetup(double sampleTimeStepx)
    {
      sampleTimeStep = sampleTimeStepx;
      sampleTics = (int)(sampleTimeStep * 10000000);
      sampleInterval = new TimeSpan(sampleTics);

      // Write the time step info to the logz file.
      if (logzOn)
        lz.Sl(" -- time step (seconds) : " + sampleTimeStep.ToString()
              + "   (tics) : " + sampleTics.ToString()); ;
    }  // end SampleTimerSetup

    /*--------------------------------------------------------------------------
     SampleStart: called just as a sequence of samples starts, before the calls
       to SampleOnce
    --------------------------------------------------------------------------*/
    public static void SampleStart()
    {
      rx = 33791;  // the current raw values of X, Y, and Z
      ry = 33791;  //   these values correspond to sx = sy = sz = 0
      rz = 65535;
      numSamples = 0;  // # samples so far
      joystick.Poll();  // poll and ignore the results (flush data)
      sampleTime = DateTime.Now + sampleInterval;
    }  // end SampleStart

    /*--------------------------------------------------------------------------
     SampleOnce: wait until time for the next sample, then get it. Note that 
       SampleSetup, SampleTimerSetup, and SampleStart must be called before the 
       first call here. 
      
     Called by Winder.Position, Winder.Wind, and Winder.TestJoystick
    --------------------------------------------------------------------------*/
    public static void SampleOnce()
    {
      while (DateTime.Now < sampleTime) ;  // waste time
      sampleTime = DateTime.Now + sampleInterval;
      Jpoll();     // get new rx, ry, rz and button info
      ScaleXYZ();  // get new cx, cy, cz, sx, sy, sz
      if (anyButtonChange) ButtonAction();
      numSamples++;

      if (logzOn)
      {
        // log the XYZ values.
        if (logzXYZ)
          lz.Sl("   " + numSamples.ToString().PadLeft(4)
                      + cx.ToString().PadLeft(8)
                      + sx.ToString("F5").PadLeft(10)
                      + cy.ToString().PadLeft(8)
                      + sy.ToString("F5").PadLeft(10)
                      + cz.ToString().PadLeft(8)
                      + sz.ToString("F5").PadLeft(10));

        // log button changes.
        if (anyButtonChange) LogzButtonChange(numSamples);
      }
    }  // end SampleOnce

    /*--------------------------------------------------------------------------
     Sample: samples the joystick each sampleTimeStepx seconds for 
       numSamplesTotalx. Writes the output to the logz file.
       (Mostly useful for debugging.)
     
     Calls: SampleSetup, SampleTimerSetup, SampleStart, SampleOnce
    --------------------------------------------------------------------------*/
    public static void Sample(double sampleTimeStepx, int numSamplesTotalx)
    {
      lz.Bl();
      lz.Sl("  -- Sample Output --");
      // Set up.
      SampleSetup();
      SampleTimerSetup(sampleTimeStepx, numSamplesTotalx);

      // Write the sample output header.
      lz.Bl();
      lz.Sl("               XYZ OUTPUT (unscaled and scaled)");
      lz.Sl("     T      X       XS        Y       YS        Z       ZS");

      SampleStart();
      while (numSamples <= numSamplesTotal) SampleOnce();
    }  // end Sample

    /*--------------------------------------------------------------------------
     DisplayModes: called by Winder.DisplayFullStatus
    --------------------------------------------------------------------------*/
    public static void DisplayModes()
    {
      string st;
      if (virtualRotOn) st = "VR is on ";
      else              st = "VR is off";
      
      if (joystickRate) st += ", joy mode is rate    ";
      else              st += ", joy mode is position";
      if (joystickFast) st += ", joy speed is fast";
      else              st += ", joy speed is slow";
      Util.W2(st);

      if (activeBreakOn) st = " active break is on ";
      else st = " active break is off";
      if (inactiveBreakOn) st += ", inactive break is on ";
      else                 st += ", inactive break is off";
      if (recordOn) st += ", record mode is on";
      else          st += ", goto   mode is on";
      Util.W2(st);
    }  // end DisplayModes

    /*--------------------------------------------------------------------------
     ButtonTest: useful for debugging; should be called from Winder.Main
    --------------------------------------------------------------------------*/
    public static void ButtonTest()
    {
      Console.WriteLine("Button Test -- hit Enter to proceed");
      Console.ReadLine();

      SampleTimerSetup(0.01);
      SampleSetup();
      SampleStart();
      bool goon = true;
      while (goon)
      {
        Console.WriteLine("Press a button -- then hit Enter");
        Console.ReadLine();
        SampleOnce();
        if (anyButtonChange)
        {
          for (int nb = 0; nb < NBUTTONS; nb++)
            if (buttonChange[nb] != 0)
              Console.WriteLine(" --- code button " + nb.ToString()
                       + ", box button " + CB2BB[nb].ToString()
                       + ", changed " + buttonChange[nb].ToString()
                       + " times and is now " + buttonState[nb].ToString());
        }
        else
          Console.WriteLine(" -- no button change");

        Console.WriteLine("Hit Enter to try again; hit X Enter to quit");
        string line = Console.ReadLine();
        if (line.Length != 0) goon = false;
      }
      Console.WriteLine("Button test over");

    }  // end ButtonTest

    //=========================== PRIVATE METHODS ==============================
    /*--------------------------------------------------------------------------
     Capable: logz joystick capabilities (useful for debugging)
    --------------------------------------------------------------------------*/
    private static void Capable()
    {
      // Extract the capabilities.
      Capabilities jcap    = joystick.Capabilities;
      int axesCount        = jcap.AxesCount;
      int buttonCount      = jcap.ButtonCount;
      int driverVersion    = jcap.DriverVersion;
      int firmwareRevision = jcap.FirmwareRevision;
      int hardwareRevision = jcap.HardwareRevision;
      bool isHID           = jcap.HumanInterfaceDevice;
      int povCount         = jcap.PovCount;
      DeviceType devType   = jcap.Type;
      int subtype          = jcap.Subtype;
      DeviceFlags devFlags = jcap.Flags;

      // Send the results to the logz file.
      lz.Bl();
      lz.Sl("Joystick Capabilities Summary");
      lz.Sl(" -- axes count       : " + axesCount.ToString());
      lz.Sl(" -- button count     : " + buttonCount.ToString());
      lz.Sl(" -- driver version   : " + driverVersion.ToString());
      lz.Sl(" -- firmware revision: " + firmwareRevision.ToString());
      lz.Sl(" -- hardware revision: " + hardwareRevision.ToString());
      lz.Sl(" -- is HID?          : " + isHID.ToString());
      lz.Sl(" -- POV count        : " + povCount.ToString());
      lz.Sl(" -- device type      : " + devType.ToString());
      lz.Sl(" -- subtype          : " + subtype.ToString());
      lz.Sl(" -- device flags     : " + devFlags.ToString());
    }  // end Capable

 
    /*--------------------------------------------------------------------------
     Jpoll: polls the joystick; gets the currently available buffered data
       and uses it to update the current values rx, ry, and rz and the button
       state information. Called by SampleOnce.
     
     datas is a list of "joystick states". Each item in the list contains many
     values, but only one of them is not zero. A zero is not a value; it means 
     no new information is provided. Here, the only values of interest are the
     states X, Y, and Z (X and Y are the actual joystick, and Z is the foot 
     pedal); or any of the 21 button states.
    --------------------------------------------------------------------------*/
    private static void Jpoll()
    {
      // Initialize the button changes.
      for (int n = 0; n < NBUTTONS; n++) buttonChange[n] = 0;
      anyButtonChange = false;

      joystick.Poll();
      IList<JoystickState> datas = joystick.GetBufferedData();
      int nstates = datas.Count();
      foreach (JoystickState state in datas)
      {
        if (state.X != 0)
        {
          rx = state.X;
          continue;
        }
        if (state.Y != 0)
        {
          ry = state.Y;
          continue;
        }
        if (state.Z != 0)
        {
          rz = state.Z;
          continue;
        }

        // X, Y, and Z are 0, so look at the buttons
        bool[] buttons = state.GetButtons();
        for (int n = 0; n < NBUTTONS; n++)
        {
          if (buttons[n] != buttonState[n])  // a button state changed
          {
            buttonState[n] = buttons[n];     // update the button state
            buttonChange[n]++;               // record that it changed
            anyButtonChange = true;
          }
        }
      }  // end foreach
    }  // end Jpoll

    /*--------------------------------------------------------------------------
     ScaleXYZ: get (cx, cy, cz) and (sx, sy, sz) from (rz, ry, rz)
    --------------------------------------------------------------------------*/
    private static void ScaleXYZ()
    {
      cx = rx - XYoff;  // apply offsets
      cy = ry - XYoff;
      cz = rz - Zoff;

      if (cx < -XYclim)  // scale X
        sx = (double)(cx + XYclim) / XYscale;
      else
      {
        if (cx > XYclim)
          sx = (double)(cx - XYclim) / XYscale;
        else sx = 0.0;
      }

      if (cy < -XYclim)  // scale Y
        sy = (double)(cy + XYclim) / XYscale;
      else
      {
        if (cy > XYclim)
          sy = (double)(cy - XYclim) / XYscale;
        else sy = 0.0;
      }

      if (footPedalP) sz = 1.0 - (double)cz / Zscale;  // scale Z
      else            sz = 0.0;   // no foot pedal => zero output
    }  // end ScaleXYZ

    /*--------------------------------------------------------------------------
     ButtonAction: respond to button presses; called by SampleOnce
    --------------------------------------------------------------------------*/
    private static void ButtonAction()
    {
      bool xa;

      for (int nbb = 3; nbb < 18; nbb++)  // nbb is button-box button number
      {
        int ncb = BB2CB[nbb];  // the code number of the button
        // Act only if the button was released.
        if (buttonChange[ncb] != 0 && buttonState[ncb] == false)
        {    
          string line = " -- button " + nbb.ToString().PadLeft(2)
                        + " pressed: " + buttonAct[nbb];
          Util.W2(line);
          
          switch (nbb)  // switch on the button box button number
          {
            case 3:  // set virtual rotation off
              SetVirtualRotation(false);
              break;
            case 4:  // set virtual rotation on
              SetVirtualRotation(true);
              break;
            case 5:  // toggle joystick rate/position  ??? not used
              joystickRate = !joystickRate;
              Ledz.SetLEDState(4, joystickRate);
              break;
            case 6:  // toggle joystick fast/slow
              joystickFast = !joystickFast;
              Ledz.SetLEDState(0, joystickFast);
              Winder.SetSpeed(joystickFast);
              break;
            case 7:  // swap end macro ???
             break;
            case 8:  // toggle record/goto
              recordOn = !recordOn;
              Ledz.SetLEDState(2, recordOn);
              if (recordOn) Console.WriteLine(" -- record set on");
              else          Console.WriteLine(" -- goto set on");
              break;
            case 9:  // toggle active brake
              activeBreakOn = !activeBreakOn;
              if (activeBreakOn && inactiveBreakOn)
              {
                Util.W2W("Both brakes may not be on -- action denied");
                activeBreakOn = false;
              }
              Ledz.SetLEDState(3, activeBreakOn);
              Mbox.SetBrake(true, activeBreakOn);
              break;
            case 10:  // toggle inactive brake
              inactiveBreakOn = !inactiveBreakOn;
              if (activeBreakOn && inactiveBreakOn)
              {
                Util.W2W("Both brakes may not be on -- action denied");
                inactiveBreakOn = false;
              }
              Ledz.SetLEDState(1, inactiveBreakOn);
              Mbox.SetBrake(false, inactiveBreakOn);
              break;
            case 11:  // record/goto right start position for active side
              xa = Winder.Xactive;
              Mbox.RecordGoTo(recordOn, xa, 1);
              break;
            case 12:  // record/goto left start position for active side
              xa = Winder.Xactive;
              Mbox.RecordGoTo(recordOn, xa, 2);
             break;
            case 13:  // record/goto park Z for active side
              xa = Winder.Xactive;
              Mbox.RecordGoTo(recordOn, xa, 3);
              break;
            case 14:  // record/goto home theta for active side
              xa = Winder.Xactive;
              Mbox.RecordGoTo(recordOn, xa, 4);
              //lz.Sl(" -- record on is " + recordOn.ToString() 
              //    + " -- xs        is " + recordOn.ToString());
              break;
            case 15:  // toggle inactive side servo on/off
              xa = Winder.Xactive;
              if (xa)  // Y is the inactive side
              {
                yservoOn = !yservoOn;
                Mbox.SetServo(4, yservoOn);
              }
              else  // X is the inactive side
              {
                xservoOn = !xservoOn;
                Mbox.SetServo(3, xservoOn);
              }
              break;
            case 16:  // not used
              break;
            case 17:  // send Winder to pause mode
              Winder.PauseMode = true;
              break;
          }
        }
      }
    }  // end ButtonAction

    /*--------------------------------------------------------------------------
     SetVirtualRotation:
    --------------------------------------------------------------------------*/
    public static void SetVirtualRotation(bool vrOn)
    {
      if (Winder.WindMode)
      {
        Util.W2W(" *** cannot change virtual rotation in wind mode");
        return;
      }

      if (vrOn && virtualRotOn)
      {
        Util.W2W(" *** virtual votation is already on");
        return;
      }
      if (!vrOn && !virtualRotOn)
      {
        Util.W2W(" *** virtual votation is already off");
        return;
      }

      virtualRotOn = vrOn;
      Ledz.SetLEDState(6, !vrOn);
      Util.Delay(1000);                    // saz led test for LED issue jan 3
      Ledz.SetLEDState(5,  vrOn);
      Winder.SetVirtualRotation(vrOn);
    }  // end SetVirtualRotation

    /*--------------------------------------------------------------------------
     LogzButtonChange:
    --------------------------------------------------------------------------*/
    private static void LogzButtonChange(int ns)
    {
      for (int nb = 0; nb < NBUTTONS; nb++)
      {
        if (buttonChange[nb] != 0)
          lz.Sl(" --- step " + ns.ToString() + "; button " + nb.ToString()
                + " changed " + buttonChange[nb].ToString()
                + " times and is now " + buttonState[nb].ToString());
      }
    }  // end LogzButtonChange

    /*--------------------------------------------------------------------------
     Test: called by Winder.Test
    --------------------------------------------------------------------------*/
    public static void Test()
    {
      Console.WriteLine();
      string st = "Button box test entered. Type a character followed by Enter";
      Console.WriteLine(st);
      st = "J -> joystick test, L -> LED test";
      Console.WriteLine(st);
      st = Console.ReadLine();
      char ch = Char.ToUpper(st[0]);
      switch (ch)
      {
        case 'J':
          st = "Sampling joystick at 0.02 sec intervals for 4 seconds";
          Console.WriteLine(st);
          Sample(0.02, 400);
          break;
        case 'L':
          Console.WriteLine("LED test proceeds");
          Ledz.LED_Test(8);
          break;
      }
    }  // end Test
  }  // end class Bbox
}  // end namespace WinderProject





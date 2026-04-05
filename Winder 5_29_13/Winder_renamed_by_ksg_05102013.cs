/*------------------------------------------------------------------------------
File: Winder.cs                                        Updated: 12/13/2012

Objective: Winder program
See Also: Logz.cs, Parmz.cs, Ledz.cs, Bbox.cs, Mbox.cs, Sportz.cs. Jrkz.cs
          Util.cs, Winder.txt, Buttons.txt, Mbox.txt, Jrkz.txt
Problems: WIP!
--------------------------------------------------------------------------------
 CLASS ELEMENTS: Bbox manages the button box buttons, joystick, and foot pedal. 
 Ledz manages the PC controlled LEDs on the button box. Mbox manages the Hub 
 444 with its four stepper motor drivers in the motor control box. Jrkz  manages 
 the Pololu Jrk motor controllers. Sportz manages serial port setup for Mbox 
 and for Jrkz. Logz manages the log file. Parmz manages the paramerers files.
 Util has some utility methods.
------------------------------------------------------------------------------*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SlimDX.Windows;
using SlimDX.DirectInput;

using Zlib;  // Logz and Parmz classes

namespace WinderProject
{
  class Winder
  {
    //=================== STATIC VARIABLES AND PROPERTIES ======================
    private static Logz lz;    // the log file object

    // Several constants declared here are public so they may easily be shared 
    //  with the motor box (Mbox) code. They are: hub444ComPort, xPololuComPort,
    //  yPololuComPort, hub444P, numSMD, xServoVolts, yServoVolts, motorCurrent,
    //  idleCurrent, delayTime, and smdStepsPerRev. 

    // From Winder.parmz
    // -- com port numbers (constants)
    public  static int hub444ComPort;  // the Hub 444 is COMn, where this is n
    public  static int xPololuComPort;
    public  static int yPololuComPort;

    // -- configuration items
    private static bool machineParmzP;  // true => read the Machine parmz
    private static bool coilParmzP;     // true => read the Coil parmz
    private static bool buttonBoxP;     // true => button box present
    private static bool footPedalP;     // true => foot pedal present
    private static bool lazySusanP;     // true => lazy Susan switch used
    private static bool motorBoxP;      // true => motor box present
    public  static bool hub444P;        // false => no Hub 444, thus one SMD
    public  static int  numSMD;         // # SMDs on the Hub 444
    private static int  xCanSmd;        // X can stepper motor driver #
    private static int  xStageSmd;      // X stage stepper motor driver #
    private static int  yCanSmd;        // Y can stepper motor driver #
    private static int  yStageSmd;      // Y stage stepper motor driver #
    private static double sampleTimeStep;  // in seconds

    private static double[] wparray;  // the array of 16 Winder parameters
    private static double[] mparray;  // the array of 22 machine parameters
    private static double[] cparray;  // the array of 5 coil parameters

    private static bool operating;        // false => terminate
    private static bool pauseMode;        // false => no pause
    public  static bool PauseMode { set { pauseMode = value; } }  
    private static bool positionMode;     // false => position off
    public static  bool PositionMode { get { return positionMode; } }
    private static bool windMode;         // false => wind off
    public  static bool WindMode { get { return windMode; } }
    private static bool newLayer;         //???

    // X, Y, left, right, active, inactive. These are not independent variables.
    //  leftActive is synonymous with virtual rotation off
    //  xactive = ( (leftActive && xleft) || (!leftActive && !xleft) )
    //  xleft = ( (leftActive && xactive) || (!leftActive && !xactive) )
    //  leftActive = ( (xactive && xleft) || (!xactive && !xleft) )
    private static bool xleft;    // true => X stage is on the left
    public  static bool Xleft { get { return xleft; } }
    private static bool xactive;  // true => X stage is active
    public  static bool Xactive { get { return xactive; } }
    private static bool leftActive;  // true => left side is active
    public  static bool LeftActive { get { return leftActive; } }
    public  static bool xPark;  // false => X side Jrk in servo
    public  static bool yPark;  // false => Y side Jrk in servo

    private static int  canSmd;    // active can rotation SMD #
    private static int  stageSmd;  // active stage translation SMD #

    // Constants
    // -- from Machine.parmz  (rps => motor revolutions/second)
    // ---- Pololu Jrk related
    public  static double xServoVolts;
    public  static double yServoVolts;

    // ---- motor current related
    public  static double motorCurrent;  // amperes
    public  static double idleCurrent;   // amperes
    public  static double delayTime;     // seconds

    // ---- can related       (tps => can turns/second)
    public  static int    smdStepsPerRev;
    private static int    smdRevsPerCanTurn; 
    private static double fpSpeedFast;  // tps
    private static double fpSpeedSlow;  // tps
    private static double yjSpeedFast;  // tps
    private static double yjSpeedSlow;  // tps
    private static double maxCanAccel;  // (tps)/sec
    private static double canFlVel;     // can SM FL velocity rps
    private static double canFlAcc;     // can SM FL accel (rps)/sec

    // ---- stage related
    private static double motionPerSMDRev;  // inches/revolution
    private static double xjSpeedFast;      // inches/sec
    private static double xjSpeedSlow;      // inches/sec
    private static double maxStageAccel;    // SM (rps)/sec
    private static double bumpAngle;        // degrees after TDC
    private static int    noBumpTurns;
    private static double stageFlVel;       // stage SM FL velocity rps
    private static double stageFlAcc;       // stage SM FL accel (rps)/sec
    private static double stepsPerInch;     // stage # steps per inch (derived)
    public  static double StepsPerInch { get { return stepsPerInch; } }

    // -- from Coil.parmz
    private static double spoolWidth;      // flange-to-flange, inches
    private static double coreDiameter;    // inches
    private static int    coilLayers;      // layers to wind
    private static int    baseLayerTurns;  // number turns on layer 1
    private static double fiberDiam;       // inches (after conv. from microns)

    private static int    bumpCounts;      // # steps to move stage for bump
    private static int    bumpNumber;      // # bumps this layer

    // Turns per layer
    private static int    baseLayerTurnsOp;    // operator entered value
    private static int    baseLayerTurnsComp;  // computed from fiber diameter
    private static int    baseLayerTurnsAct;   // the actual turns wound
    private static int[]  turnsPerLayer;

    private static int    stepsPerCanTurn; // SM steps for one full can turn
    private static double canRpsPerFp;    // can   SM rps for foot pedal = 1.0
    private static double canRpsPerYj;    // can   SM rps for Y joystick = 1.0
    private static double stageRpsPerXj;  // stage SM rps for X joystick = 1.0

    private static int    layerNumber;  // first layer is 1
    private static bool   oddLayer;     // false => even layer number
    private static int    turnNumber;   // within the current layer
    private static int    tdcCanPos;    // can pos after move to top dead center
    public  static int    TdcCanPos { set { tdcCanPos = value; } }
    private static double canJogSpeed;        // SM rps
    private static double canJogSpeedLast;    // last commanded jog speed
    private static double canJogAccel;        // SM (rps)/sec
    private static double stageJogSpeed;      // SM rps
    private static double stageJogSpeedLast;  // last commanded jog speed

    // winding items
    private static int    canPos;
    private static int    canMove;
    private static int    canPosStart;

    // position display items
    private static int    canPosP;
    private static int    stagePosP;
    private static double canAngP;
    private static double canAngPLast;
    private static double stageInchesP;
    private static double stageInchesPLast;

      //Added by KSG
    private static bool canCorrect;  //used by CorrectCanPossition to make sure the correct can is active in StartLayer

    //============================== METHODS ===================================
    /*--------------------------------------------------------------------------
     Main:
    --------------------------------------------------------------------------*/
    static void Main(string[] args)
    {
      Initialize();  // may call Test and set operating false, which will 
                     //  go to Terminate      
      while (operating)
      {
        if (pauseMode) Pause();
        else if (positionMode) Position();
        else if (windMode) Wind();
      }
    
      Terminate();
    }  // end Main

    //============================= PUBLIC METHODS =============================
    /*--------------------------------------------------------------------------
     SetVirtualRotation: called by Bbox SetVirtualRotation when button 3 or 4 is 
       pressed. In Winder, leftActive is true if VR is off, false if VR is on.
    --------------------------------------------------------------------------*/
    public static void SetVirtualRotation(bool vron)
    {
      leftActive = !vron;
      xactive = ((xleft && leftActive) || (!xleft && !leftActive));

      if (!pauseMode)
      {
        StopJogging();
        SetSMDs();
        CommenceJogging();
      }

      if (vron) Util.W2("Virtual rotation set on");
      else      Util.W2("Virtual rotation set off");
    }  // end SetVirtualRotation

    /*--------------------------------------------------------------------------
     SetSpeed: called by Bbox ButtonAction when button 6 is pressed. Also used
       to initialize speed as fast or slow
    --------------------------------------------------------------------------*/
    public static void SetSpeed(bool fast)
    {
      if (fast)
      {
        canRpsPerFp = smdRevsPerCanTurn * fpSpeedFast;
        canRpsPerYj = smdRevsPerCanTurn * yjSpeedFast;
        stageRpsPerXj = xjSpeedFast / motionPerSMDRev;
        Util.W2("Joystick speed set to fast");
      }
      else  // must be slow
      {
        canRpsPerFp = smdRevsPerCanTurn * fpSpeedSlow;
        canRpsPerYj = smdRevsPerCanTurn * yjSpeedSlow;
        stageRpsPerXj = xjSpeedSlow / motionPerSMDRev;
        Util.W2("Joystick speed set to slow");
      }
    }  // end SetSpeed

    //========================= PRIVATE METHODS ================================
    /*--------------------------------------------------------------------------
     Test: manages test mode
    --------------------------------------------------------------------------*/
    private static void Test()
    {
      Console.WriteLine();
      string st = "Test mode entered. Type a character followed by Enter";
      Console.WriteLine(st);
      st = "B -> test button box, M -> test motor box";
      Console.WriteLine(st);
      st = Console.ReadLine();
      char ch = Char.ToUpper(st[0]);
      switch (ch)
      {
        case 'B':
          Bbox.Test();
          break;
        case 'M':
          Mbox.Test();
          break;
      }
    }  // end Test

    /*--------------------------------------------------------------------------
     Terminate: end Winder. Terminate happens when operating is false on the 
       exit of Pause.
    --------------------------------------------------------------------------*/
    private static void Terminate()
    {
      //???if (buttonBoxP && pacDriveI) Ledz.Shutdown();  // close the PacDrive port
      if (buttonBoxP) Ledz.Shutdown();  // close the PacDrive port
      if (motorBoxP) Mbox.Close();  //??? Pololu later

      Console.WriteLine("Winder is done -- to terminate hit Enter");
      Console.ReadLine();
      lz.Close();  // close the logz file
    }  // end Terminate

    /*--------------------------------------------------------------------------
     Pause: manage pause mode. Pause mode happens when pauseMode becomes true,
       usually when button 17 (pause) is pressed.
    --------------------------------------------------------------------------*/
    private static void Pause()
    {
      // Stop jogging. End positioning or winding. 
      StopJogging();
      if (positionMode)
      {
        positionMode = false;
        Util.W2("Position mode stops; pausing");
      }
      if (windMode)
      {
        windMode = false;
        Util.W2("Wind mode stops; pausing");
      }

      pauseMode = false;  // turn off pause mode
      Console.WriteLine();
      string st = "Pause mode entered. Type a character followed by Enter";
      Console.WriteLine(st);
      st = "P -> position, W -> wind, E -> end layer, S -> status" 
            + ", V -> toggle VR, T -> terminate";
      Console.WriteLine(st);
      st = Console.ReadLine();
      if (st.Length == 0)  // operator typed nothing
      {
        pauseMode = true;  // so try again
        return;
      }
      char ch = Char.ToUpper(st[0]);
      switch (ch)
      {
        case 'P':  // go to position mode
          Util.W2("Transition to position mode");
          positionMode = true;
          break;
        case 'W':  // go to wind mode
          Util.W2("Transition to wind mode");
          windMode = true;
          break;
        case 'E':  // end layer
          EndLayer();
          pauseMode = true;
          break;
        case 'S':  // display status
          DisplayFullStatus();
          pauseMode = true;
          break;
        case 'V':  // toggle virtual rotation
          bool vrOn = Bbox.VirtualRotOn;
          Bbox.SetVirtualRotation(!vrOn);
          pauseMode = true;
          break;
        case 'T':  // terminate
          operating = false;
          break;
        default:
          pauseMode = true;  // improper input, try again
          break;
      }
    }  // end Pause

    /*--------------------------------------------------------------------------
     StopJogging:
    --------------------------------------------------------------------------*/
    private static void StopJogging()
    {
      if (positionMode)
      {
        Mbox.StopJogging(canSmd);    // stop jogging
        Mbox.StopJogging(stageSmd);  // stop jogging
      }
      if (windMode)
        Mbox.StopJogging(canSmd);  // stop jogging
    }  // end StopJogging

    /*--------------------------------------------------------------------------
     CommenceJogging:
    --------------------------------------------------------------------------*/
    private static void CommenceJogging()
    {
      Mbox.JogSetup(canSmd, canJogAccel, 0.0);
      Util.Delay(20);
      Mbox.CommenceJogging(canSmd);        // commence jogging the can
      Util.Delay(20);
      Mbox.ChangeJogSpeed(canSmd, 0.0);    //  be sure speed is zero
      if (positionMode)
      {
        Mbox.JogSetup(stageSmd, maxStageAccel, 0.0);
        Util.Delay(20);
        Mbox.CommenceJogging(stageSmd);      // commence jogging the stage
        Util.Delay(20);
        Mbox.ChangeJogSpeed(stageSmd, 0.0);  //  be sure speed is zero
      }
    }  // end CommenceJogging

    /*--------------------------------------------------------------------------
     SetSMDs:
    --------------------------------------------------------------------------*/
    private static void SetSMDs()
    {
      if (xactive) { canSmd = xCanSmd; stageSmd = xStageSmd; }
      else { canSmd = yCanSmd; stageSmd = yStageSmd; }
    }  // end SetSMDs

    /*--------------------------------------------------------------------------
     Position: manages the position mode of Winder. Position mode occurs when
       positionMode is true on exiting Pause. Initializes, then positions. 
       Ceases when button 17 (pause) is pressed.           
    --------------------------------------------------------------------------*/
    private static void Position()
    {
      // Check the lazy Susan position before starting.
      if (!LazySusan()) return; // switch is not closed
 
      Util.W2("Position mode begins.......................................");
      DisplayStatus();

      SetSMDs();  // set the SMD numbers

      canJogSpeedLast = 0.0;
      stageJogSpeedLast = 0.0;
      Bbox.LogzOn = false;
      Bbox.LogzXYZ = false;  // shut off button box sampling logging
      Mbox.ComResLogz = false;  // shut off Mbox command logging

      canAngPLast = 5000.0;  // set so initial values will be displayed
      stageInchesPLast = 100.0;
      CommenceJogging();
      Bbox.SampleStart();
      //TestJoystick();  //???

      while (true)  // this is actual positioning
      {
        Bbox.SampleOnce();  // sample the button box info
        if (pauseMode) return;  // button 17 may have been pressed
        CanJog();
        StageJog();
        DisplayPosition();
      }
    }  // end Position

    /*--------------------------------------------------------------------------
     Wind: manages the wind mode of Winder. Wind mode occurs when windMode is
       true on exiting Pause. Initializes, as needed, then winds. Ceases when
       button 17 (pause) is pressed.       
    --------------------------------------------------------------------------*/
    private static void Wind()
    {
      // Check the lazy Susan position before starting.
      if (!LazySusan()) return;  // switch is not closed
    
      Util.W2("Wind mode begins...........................................");
      if (newLayer)  // start a new layer
      {
        StartLayer();
        newLayer = false;
      }
      else  // resume winding the current layer
      {
        //TestJoystick();  //???
        CommenceJogging();
        lz.Sl("Jogging resumes on layer " + layerNumber.ToString());
        canJogSpeedLast = 0.0;
        DisplayLayer();
      }

      Bbox.SampleStart();
      int bc;
      while (true)  // actual winding
      {
        Bbox.SampleOnce();  // sample the button box info
        if (pauseMode) return;  // button 17 may have been pressed
       
        CanJog();

        // See if a can turn is complete.
        canPos = Mbox.GetIP(canSmd);
        canMove = canPos - canPosStart;
        if (canMove >= stepsPerCanTurn)  // a turn is complete
        {
          turnNumber++;
          DisplayLayer();
          lz.Sl(" -- can turn = " + turnNumber.ToString().PadLeft(4)
                + ",  can steps = " + canPos.ToString().PadLeft(10));
          canPosStart += stepsPerCanTurn;
          // Bump the stage
          if (turnNumber - bumpNumber > noBumpTurns)
          {
            if (oddLayer) bc = -bumpCounts;
            else bc = bumpCounts;
            Mbox.Bump(stageSmd, bc);
            bumpNumber++;
          }
        }
        else if (canMove < 0)  // "back up" a turn -- no bump back
        {
          turnNumber--;
          DisplayLayer();
          lz.Sl(" -- can turn = " + turnNumber.ToString().PadLeft(4)
                + ",  can steps = " + canPos.ToString().PadLeft(10));
          canPosStart -= stepsPerCanTurn;
        }
      }  // end while (!positioningOn)
    } // end Wind

    /*--------------------------------------------------------------------------
     TestJoystick: ??? debug
     
     When the joystick and foot pedal are idle, they should read zero.
    --------------------------------------------------------------------------*/
    private static void TestJoystick()
    {
      Bbox.SampleOnce();
      double fp = Bbox.FootPedal;
      if (fp != 0) Util.W2(" -- foot pedal = " + fp.ToString());
      double xj = Bbox.JoyX;
      if (xj != 0) Util.W2(" -- x joystick = " + xj.ToString());
      double yj = Bbox.JoyY;
      if (yj != 0) Util.W2(" -- y joystick = " + yj.ToString());
      lz.Flush();
    }  // end TestJoystick

    /*--------------------------------------------------------------------------
     CanJog: called by Position and Wind
    --------------------------------------------------------------------------*/
    private static void CanJog()
    {
      canJogSpeed = Bbox.FootPedal * canRpsPerFp;
      if (canJogSpeed == 0.0)  // substitute joystick y for the foot pedal
        canJogSpeed = -Bbox.JoyY * canRpsPerYj;

      if (canJogSpeed != canJogSpeedLast)  // do not send unneeded command
      {
        Mbox.ChangeJogSpeed(canSmd, canJogSpeed); 
        canJogSpeedLast = canJogSpeed;
      }
    }  // end CanJog

    /*--------------------------------------------------------------------------
     StageJog: called by Position
    --------------------------------------------------------------------------*/
    private static void StageJog()
    {
      stageJogSpeed = Bbox.JoyX * stageRpsPerXj;
      if (stageJogSpeed != stageJogSpeedLast)  // do not send unneeded command
      {
        Mbox.ChangeJogSpeed(stageSmd, stageJogSpeed);
        stageJogSpeedLast = stageJogSpeed;
      }
    }  // end StageJog

    /*--------------------------------------------------------------------------
     StartLayer: called by Wind
    --------------------------------------------------------------------------*/
    private static void StartLayer()
    {
      layerNumber++;  // increment the layer number
      leftActive = !Bbox.VirtualRotOn;  // left is active if VR is off

      // Consistency check
      int m    = layerNumber / 2;
      oddLayer = ((layerNumber - 2 * m) == 1);
      m = m - 2 * (m / 2);  // m is 0 if (layerNumber/2) is even,
                            // m is 1 if (layerNumber/2) is odd 

        //Begin KSG Edit.  This code identifies the state the machine should be in and displays a message to the user

        ////There could be a problem here if the machine doesn't recognize that it was rotated. 
        ////This checks for the correct active can.  If the wrong can is active and a rotation is not detected you could get stuck in an infinite loop

      string setup = "";   //string to hold user messages
      canCorrect = false;  //assume the can is in the wrong possition.  Can't get out of the loop until in correct possition
      int r = layerNumber % 4; //identifies the state the machine should be in based off patern

      while (!canCorrect) //Begin Loop
      {
          
          if (layerNumber == 1)  //if layer 1
          {
              setup = "Verify X on left, Insert pin in X tensioner, Turn Y tensioner power on, Press enter to continue";
              Console.WriteLine(setup);
              setup = Console.ReadLine();
              CheckCorrectCanPossition(1);  //If the can is correct break the loop
          }
          else if (layerNumber == 2) //if layer two
          {
              setup = "Rotate machine, Remove pin from X tensioner, Turn Y tensioner power on, Press enter to continue";
              Console.WriteLine(setup);
              setup = Console.ReadLine();
              CheckCorrectCanPossition(2);
          }
          else // any other layer
          {
   
              switch (r)
              {
                  case 1:  // layer 1, 5, 9, 13, 17 etc
                      setup = "Make sure X can is left.  Press enter to continue";
                      Console.WriteLine(setup);
                      setup = Console.ReadLine();
                      CheckCorrectCanPossition(1);
                      break;
                  case 2:  // layer 2, 6, 10, 14, 18 etc
                      setup = "Make sure Y can is left.  Press enter to continue";
                      Console.WriteLine(setup);
                      setup = Console.ReadLine();
                      CheckCorrectCanPossition(2);
                      break;
                  case 3:  // layer 3, 7, 11, 15, 19
                      setup = "Make sure Y can is left.  Press enter to continue";
                      Console.WriteLine(setup);
                      setup = Console.ReadLine();
                      CheckCorrectCanPossition(2);
                      break;
                  case 0:  // layer 4, 8, 12, 16, 20 etc
                      setup = "Make sure X can is left.  Press enter to continue";
                      Console.WriteLine(setup);
                      setup = Console.ReadLine();
                      CheckCorrectCanPossition(1);
                      break;
              }
          }
      } //End loop


      //if (m == 1 && xactive)  // this is an error, wrong side is active
      //{
      //  Util.W2W(" *** wrong side is active");
      //}
        //KSG commented out these lines because I dont think the check is correct.  There are some odd layers that should have x left
      
      
      //End KSG Edit

      SetSMDs();  // set the SMD numbers
      
      Mbox.TopDeadCenter(canSmd);  // start at top dead center
      Util.W2("Winding a layer begins...............................");

      lz.Sl(" -- Attempting to set cans to correct start possition");
        //Begin KSG edit
        //Based on the layer sets the cans to the correct possitions to prepare for winding the next layer

      SetBreaksAndMode();
      if (layerNumber == 1)  //if layer 1.  x > y
      {
          //go to left start possition
          Mbox.RecordGoTo(false, true, 2);  // ({true for record, false for goto}, {true if x left, false if y left}, {1 for right start, 2 for left start, 3 for park, 4 for home theta})

          //x/active tensioner off

          //y/inactive tentioner in park mode
          Bbox.YservoOn = false;
          Mbox.SetServo(4, false); // ({3 for x, 4 for y} {true for servo, false for park})

      }
      else if (layerNumber == 2) //if layer two.  y < x
      {
          //right start position
          Mbox.RecordGoTo(false, false, 1);  // ({true for record, false for goto}, {true if x left, false if y left}, {1 for right start, 2 for left start, 3 for park, 4 for home theta})

          //x/inactive tensioner in servo mode
          Bbox.XservoOn = true;
          Mbox.SetServo(3, true); // ({3 for x, 4 for y} {true for servo, false for park})

          //y/active tensioner in park
          Bbox.YservoOn = false;
          Mbox.SetServo(4, false); // ({3 for x, 4 for y} {true for servo, false for park})

      }
      else // any other layer
      {
          switch (r)
          {
              case 1:  // layer 1, 5, 9, 13, 17 etc.    x > y

                  //left start possition
                  Mbox.RecordGoTo(false, true, 2);  // ({true for record, false for goto}, {true if x left, false if y left}, {1 for right start, 2 for left start, 3 for park, 4 for home theta})

                  //x/active tensioner in servo mode
                  Bbox.XservoOn = true;
                  Mbox.SetServo(3, true); // ({3 for x, 4 for y} {true for servo, false for park})

                  //y/inactive tensioner in park
                  Bbox.YservoOn = false;
                  Mbox.SetServo(4, false); // ({3 for x, 4 for y} {true for servo, false for park})

                  break;
              case 2:  // layer 2, 6, 10, 14, 18 etc.   y < x

                  //right start possition
                  Mbox.RecordGoTo(false, false, 1);  // ({true for record, false for goto}, {true if x left, false if y left}, {1 for right start, 2 for left start, 3 for park, 4 for home theta})

                  //x/inactive tensioner in servo mode
                  Bbox.XservoOn = true;
                  Mbox.SetServo(3, true); // ({3 for x, 4 for y} {true for servo, false for park})

                  //y/active tensioner in park
                  Bbox.YservoOn = false;
                  Mbox.SetServo(4, false); // ({3 for x, 4 for y} {true for servo, false for park})

                  break;
              case 3:  // layer 3, 7, 11, 15, 19 etc.    y > x

                  //left start possition
                  Mbox.RecordGoTo(false, false, 2);  // ({true for record, false for goto}, {true if x left, false if y left}, {1 for right start, 2 for left start, 3 for park, 4 for home theta})

                  //x/inactive tensioner in servo mode
                  Bbox.XservoOn = true;
                  Mbox.SetServo(3, true); // ({3 for x, 4 for y} {true for servo, false for park})

                  //y/active tensioner in park
                  Bbox.YservoOn = false;
                  Mbox.SetServo(4, false); // ({3 for x, 4 for y} {true for servo, false for park})

                  break;
              case 0:  // layer 4, 8, 12, 16, 20 etc.   x < y

                  //right start possition
                  Mbox.RecordGoTo(false, true, 1);  // ({true for record, false for goto}, {true if x left, false if y left}, {1 for right start, 2 for left start, 3 for park, 4 for home theta})

                  //x/active tensioner in park mode
                  Bbox.XservoOn = false;
                  Mbox.SetServo(3, false); // ({3 for x, 4 for y} {true for servo, false for park})

                  //y/inactive tensioner in servo
                  Bbox.YservoOn = true;
                  Mbox.SetServo(4, true); // ({3 for x, 4 for y} {true for servo, false for park})

                  break;
          }
      }
        //End KSG Edit

      lz.Sl(" -- getting layer start position");
      canPosStart = Mbox.GetIP(canSmd);  //KSG NOTE this gets the Imediate Position (IP) of the 'encoder'.  I believe this is used to determine jogging.  I think this should go after the positioning code I added but I could be wrong.
      turnNumber = 0;
      bumpNumber = 0;

      // Set up for jogging the can motor.
      CommenceJogging();
      lz.Sl(" -- jogging begins on layer " + layerNumber.ToString());
      DisplayLayer();
      canJogSpeedLast = 0.0;
      Bbox.LogzOn = false;
      Bbox.LogzXYZ = false;  // shut off button box sampling logging
      Mbox.ComResLogz = false;  // shut off Mbox command logging
    }  // end StartLayer

    /*--------------------------------------------------------------------------
   Sets inactive break off, active break on, and gotomode.
   Called by StartLayer
   * 
   * Added by KSG
  --------------------------------------------------------------------------*/
    private static void SetBreaksAndMode()
    {
        //inactive break off
        Bbox.InactiveBreakOn = false;
        Ledz.SetLEDState(1, false);
        Mbox.SetBrake(false, false); //false = inactive break, false = off
        System.Threading.Thread.Sleep(300); // Al did this whenever he set a break state.  So I copied it.  Not sure if needed.

        //active break on
        Bbox.ActiveBreakOn = true;
        Ledz.SetLEDState(3, true);
        Mbox.SetBrake(true, true); //true = active break, true = on
        System.Threading.Thread.Sleep(300); // Al did this whenever he set a break state.  So I copied it.  Not sure if needed.

        //make sure in goto mode
        Bbox.RecordOn = false;
        Ledz.SetLEDState(2, false);
    }


    /*--------------------------------------------------------------------------
     CheckCorrectCanPossition: make sure the correct can is on the active side.  Set
     * activeCan to 1 to check if the X can is active.  Set activeCan to 2 to 
     * check if Y can is active.  If the correct can is active then canCorrect
     * is changed to true.  This breaks the loop in StartLayer and allows code
     * to continue
     * 
     Called by StartLayer
     * 
     * Added by KSG
    --------------------------------------------------------------------------*/
    private static void CheckCorrectCanPossition(int activeCan)
    {
        //KSG Copied code from LazySusan.  Didnt want to use that function because if the LazySusan is open it dumps to pause mode.

        int lazy = 0;  // lazy Susan position {+1 xleft, 0 switch open, -1 y left}     
        lazy = Mbox.GetLazySusanPosition();
        if (lazy != 0)  // the switch closed
        {
            xleft = (lazy == 1);
            xactive = ((leftActive && xleft) || (!leftActive && !xleft));
        }
        else
        {
            Util.W2W("Lazy Susan not closed");  // warning message  
        }
        //KSG end of copied code

        if (activeCan == 1 && xactive) // if activeCan was passed 1 AND the X can is active
        {
            canCorrect = true;
        }
        if (activeCan == 2 && !xactive) // if activeCan was passed 2 AND the Y can is active
        {
            canCorrect = true;
        }

    }

    /*--------------------------------------------------------------------------
     LazySusan: makes sure the lazy Susan switch is closed. If not, sets 
       pauseMode true and returns false. If so, sets booleans xleft and xactive, 
       and returns true.
     
     Called by Position, Wind, and CheckCorrectCanPossition
    --------------------------------------------------------------------------*/
    private static bool LazySusan()
    {
      if (!lazySusanP)  // if there is no lazy Susan switch (debugging)
      {
        xleft = true;
        xactive = true;
        return true;
      }

      int lazy = 0;  // lazy Susan position {+1 xleft, 0 switch open, -1 y left}     
      lazy = Mbox.GetLazySusanPosition();
      if (lazy != 0)  // the switch closed
      {
        xleft = (lazy == 1);
        xactive = ((leftActive && xleft) || (!leftActive && !xleft));
        return true;
      }
            
      Util.W2W("Lazy Susan not closed");  // warning message
      pauseMode = true;
      return false;      
    }  // end LazySusan

    /*--------------------------------------------------------------------------
     DisplayPosition: called by Position. Displays if the can has moved 20 
       degrees or the stage has moved 0.2 inches.
    --------------------------------------------------------------------------*/
    private static void DisplayPosition()
    {
      canPosP = Mbox.GetIP(canSmd);
      stagePosP = Mbox.GetIP(stageSmd);
      canAngP = 360.0 * (double)canPosP / (double)stepsPerCanTurn;
      stageInchesP = (double)stagePosP / stepsPerInch;
      if (   (Math.Abs(canAngP - canAngPLast) >= 20.0)
          || (Math.Abs(stageInchesP - stageInchesPLast) >= 0.2))
      {
        Util.W2(" -- can angle = " + canAngP.ToString("F2").PadLeft(9)
           + ", stage position = " + stageInchesP.ToString("F4").PadLeft(8));
        canAngPLast = canAngP;
        stageInchesPLast = stageInchesP;
      }
    }  // end DisplayPosition

    /*--------------------------------------------------------------------------
     DisplayStatus: called by Position and Wind (and by DisplayFullStatus)
    --------------------------------------------------------------------------*/
    private static void DisplayStatus()
    {
      string st;
      if (leftActive) st  = "Left side active";
      else            st  = "Right side active";
      if (xleft)      st += "; X is left";
      else            st += "; Y is left";
      if (xPark)      st += "; X Jrk in park";
      else            st += "; X Jrk in servo";
      if (yPark)      st += "; Y Jrk in park";
      else            st += "; Y Jrk in servo";
      Util.W2(st);
    }  // end DisplayStatus

    /*--------------------------------------------------------------------------
     DisplayFullStatus: called by Pause
    --------------------------------------------------------------------------*/
    private static void DisplayFullStatus()
    {
      Console.Clear();
      Util.W2("Status Display");
      Console.WriteLine();
      DisplayStatus();
      Bbox.DisplayModes();

      // X and Y can angles and stage inch positions
      int cp = Mbox.GetIP(xCanSmd);
      int sp = Mbox.GetIP(xStageSmd);
      double ca = 360.0 * (double)cp / (double)stepsPerCanTurn;
      double si = (double)sp / stepsPerInch;
      Util.W2("X can angle = " + ca.ToString("F2").PadLeft(9)
          + ", X stage position = " + si.ToString("F4").PadLeft(8));
      cp = Mbox.GetIP(yCanSmd);
      sp = Mbox.GetIP(yStageSmd);
      ca = 360.0 * (double)cp / (double)stepsPerCanTurn;
      si = (double)sp / stepsPerInch;
      Util.W2("Y can angle = " + ca.ToString("F2").PadLeft(9)
          + ", Y stage position = " + si.ToString("F4").PadLeft(8));

      Mbox.DisplayStagePositions();
    }  // end DisplayFullStatus

    /*--------------------------------------------------------------------------
     DisplayLayer: called by Wind
    --------------------------------------------------------------------------*/
    private static void DisplayLayer()
    {
      Console.Clear();
      Console.WriteLine();
      if (layerNumber == 1)
        Console.WriteLine(" Winding layer   1" + ", turn "
                          + turnNumber.ToString().PadLeft(4));
      else
      {
        int nt = baseLayerTurnsAct;
        if (!oddLayer) nt--;  // one fewer turns on an even layer
        Console.WriteLine(" Winding layer " + layerNumber.ToString().PadLeft(3)
                          + ", turn " + turnNumber.ToString().PadLeft(4)
                          + " of " + nt.ToString());
      }
    }  // end DisplayLayer

    /*--------------------------------------------------------------------------
     EndLayer: called by Pause
    --------------------------------------------------------------------------*/
    private static void EndLayer()
    {
      Mbox.TopDeadCenter(canSmd);  // go to top dead center
      turnNumber = ((tdcCanPos + 10) / stepsPerCanTurn);  //??? 10 needed?
      Console.WriteLine(" -- actual turns this layer = " 
                        + turnNumber.ToString());

      turnsPerLayer[layerNumber - 1] = turnNumber;
      Console.WriteLine();
      Console.WriteLine(" Layer " + layerNumber.ToString() + " ends");
      if (layerNumber == 1)
      {
        baseLayerTurnsAct = turnNumber;  // save this key value
        Console.WriteLine(" Enter a number of turns for layer 1.");
        string line = Console.ReadLine();
        baseLayerTurnsOp = Int32.Parse(line);  // get the value
        lz.Sl(" -- Actual turns for layer 1 = "
              + baseLayerTurnsAct.ToString());
        lz.Sl(" -- Operator entered turns for layer 1 = "
              + baseLayerTurnsOp.ToString());
      }
        //Added by KSG.  This puts the active side in park position at the end of the layer.  Then whenever a new layer is started we are in a predictable possition.
      bool xa = Winder.Xactive;  //True if x left, false if y left
      Mbox.RecordGoTo(false, xa, 3);  // ({true for record, false for goto}, {true if x left, false if y left}, {1 for right start, 2 for left start, 3 for park, 4 for home theta})
        //End KSG addition

      newLayer = true;
    }  // end EndLayer

    //===========================  STARTUP METHODS =============================
    /*--------------------------------------------------------------------------
     Initialize: called by Main
    --------------------------------------------------------------------------*/
    private static void Initialize()
    {
      int ht = Console.LargestWindowHeight;
      if (ht > 50) ht = 50;
      Console.WindowHeight = ht;
      int wd = Console.LargestWindowWidth;
      if (wd > 85) wd = 85;
      Console.WindowWidth = wd;
      Console.BufferHeight = 300;
      Console.BackgroundColor = ConsoleColor.White;
      Console.ForegroundColor = ConsoleColor.DarkBlue;
      Console.Clear();
      Console.WriteLine("Welcome to Winder -- to proceed hit Enter");
      Console.ReadLine();

      // Initialize the log file.
      if (!CreateLogFile()) Terminate();
      Util.Lz = lz;  // pass the log file to the Util class

      // Read the parameters (parmz) files.
      bool parmzok = ParmzRead();
      if (parmzok)
        Console.WriteLine(" -- parameters files read");
      else
      {
        Util.W2W("Parameters file read error -- terminating");
        Terminate();
      }

      // Initialize the Button Box (joystick, footpedal, buttons, LEDs)
      if (buttonBoxP)
        if (!Bbox.Initialize(lz)) Terminate();

      // Initialize the Motor Box (hub444P, SMDs, JRKs)
      if (motorBoxP) Mbox.Initialize(lz);

      lz.Flush();

      Console.WriteLine();
      Console.WriteLine("Hit Enter to proceed, or Enter T for test mode");
      string str = Console.ReadLine();
      if (str.Length == 0)  // any character is regarded as a T
      {
        operating = true;
        Setup();  // prepare to operate
        pauseMode = true;  // start in pause mode
      }
      else
      {
        operating = false;
        Test();  // test mode selected
      }
    }  // end Initialize

    /*--------------------------------------------------------------------------
     Setup: called by Initialize. Prepares to operate.
    --------------------------------------------------------------------------*/
    private static void Setup()
    {
      layerNumber = 0;     // it is incremented in StartLayer
      newLayer    = true;
      leftActive  = true;  // presume Virtual Rotation is off
      if (!LazySusan())   // set xleft and xactive
      {
        Util.W2W("Winder terminating; close Lazy Susan and restart");
        lz.Flush();
        Terminate();
      }

      xPark      = true;
      yPark      = true;

      lz.Bl();
      lz.Sl("Setup ---");
    
      // Inform the button box if there is a foot pedal or not
      Bbox.FootPedalP = footPedalP;

      // Inform Mbox of the X and Y can and stage SMD numbers
      Mbox.SetSmds(xCanSmd, xStageSmd, yCanSmd, yStageSmd);

      // Set constants.
      SetSpeed(true);  // fast by default to begin with
      bumpCounts = (int)((fiberDiam / motionPerSMDRev) * smdStepsPerRev);
      lz.Sl(" -- bump counts = " + bumpCounts.ToString());
      stepsPerCanTurn = smdStepsPerRev * smdRevsPerCanTurn;
      Mbox.StepsPerCanTurn = stepsPerCanTurn;  // pass this on to Mbox
      lz.Sl(" -- steps per can turn = " + stepsPerCanTurn.ToString());
      canJogAccel = maxCanAccel * smdRevsPerCanTurn;
      lz.Sl(" -- can jog acceleration (SM (rev/sec)/sec) = "
            + canJogAccel.ToString());
      stepsPerInch = (double)smdStepsPerRev / motionPerSMDRev;

      // Turns per layer
      baseLayerTurnsComp = (int)(spoolWidth / fiberDiam);
      lz.Sl(" -- computed base layer turns = " + baseLayerTurnsComp.ToString());
      int bltd = baseLayerTurns - baseLayerTurnsComp;
      if (bltd >= 10 || bltd <= -10)
      {
        Console.WriteLine("Specified base layer turns = "
                    + baseLayerTurns.ToString());
        Console.WriteLine("Computed  base layer turns = "
                    + baseLayerTurnsComp.ToString());
        Console.WriteLine("Difference is " + bltd.ToString());
        Console.WriteLine("To terminate, type X (Enter)"
                    + ", to proceed, type (Enter)");
        string str = Console.ReadLine();
        if (str[0] == 'X') Terminate();
      }
      turnsPerLayer = new int[coilLayers];  // room to store turns counts

      // Set can and stage FL limits for velocity and acceleration.
      Mbox.StepsPerInch = stepsPerInch;
      Mbox.SetFlLimitsP(canFlVel, canFlAcc, stageFlVel, stageFlAcc);

      // Prepare for button box sampling.
      Bbox.SampleSetup();
      Bbox.SampleTimerSetup(sampleTimeStep);
    }  // end Setup

    /*--------------------------------------------------------------------------
     CreateLogFile: called by Initialize
    --------------------------------------------------------------------------*/
    private static bool CreateLogFile()
    {
      // Create a log file.
      lz = new Logz();
      bool lzok = lz.Open(@"..\..\Winder.logz");
      if (!lzok)  // file not opened
      {
        Console.WriteLine(lz.Errstr1);
        Console.WriteLine(lz.Errstr2);
        return false;
      }
      lz.Flush();
      Console.WriteLine(" -- Log file created");
      return true;
    }  // end CreateLogFile

    /*--------------------------------------------------------------------------
     ParmzRead: reads the parameters files, echoing them to the log file. If any
       file cannot be read, returns false; if all ok, returns true.
     
     Called by Initialize
    --------------------------------------------------------------------------*/
    private static bool ParmzRead()
    {
      // First the Winder program parameters. These are always read.
      bool pok = true;
      Parmz wpz = new Parmz(lz, @"..\..\Winder.parmz", 16, ref pok);
      if (!pok)
      {
        Util.W2W("Error reading Winder parameters file");
        return false;
      }
      wparray = wpz.Parray;  // retrieve the Winder parameters
      int n = 0;
      hub444ComPort = (int)wparray[n++];
      xPololuComPort = (int)wparray[n++];
      yPololuComPort = (int)wparray[n++];
      machineParmzP = (wparray[n++] == 1);
      coilParmzP = (wparray[n++] == 1);
      buttonBoxP = (wparray[n++] == 1);
      footPedalP = (wparray[n++] == 1);
      lazySusanP = (wparray[n++] == 1);
      motorBoxP = (wparray[n++] == 1);
      hub444P = (wparray[n++] == 1);
      numSMD = (int)wparray[n++];
      xCanSmd = (int)wparray[n++];
      xStageSmd = (int)wparray[n++];
      yCanSmd = (int)wparray[n++];
      yStageSmd = (int)wparray[n++];
      sampleTimeStep = wparray[n++];

      // Next the machine parameters
      if (machineParmzP) if (!MachineParmzRead()) return false;
      
      // Finally, the coil parameters
      if (coilParmzP) if (!CoilParmzRead()) return false;
   
      return true;
    }  // end ParmzRead

    /*--------------------------------------------------------------------------
     MachineParmzRead: reads the machine parameters file, echoing it to the log 
       file. If the file cannot be read, returns false; if all ok, returns true.
      
     Called by ParmzRead
    --------------------------------------------------------------------------*/
    private static bool MachineParmzRead()
    {
      bool mpzok = true;
      Parmz mpz = new Parmz(lz, @"..\..\Machine.parmz", 22, ref mpzok);
      if (!mpzok)
      {
        Util.W2W("Error reading Machine parameters file");
        return false;
      }
      mparray = mpz.Parray;  // retrieve the machine parameters

      // -- Pololu Jrk related
      int n = 0; 
      xServoVolts = mparray[n++];  // volts
      yServoVolts = mparray[n++];  // volts

      // -- motor current related
      motorCurrent = mparray[n++];  // amperes
      idleCurrent  = mparray[n++];  // amperes
      delayTime    = mparray[n++];  // seconds

      // -- can related
      smdStepsPerRev = (int)mparray[n++];
      smdRevsPerCanTurn = (int)mparray[n++]; 
      fpSpeedFast = mparray[n++];  // turns/second
      fpSpeedSlow = mparray[n++];  // turns/second
      yjSpeedFast = mparray[n++];  // turns/second
      yjSpeedSlow = mparray[n++];  // turns/second
      maxCanAccel = mparray[n++];  // (turns/second)/second
      canFlVel    = mparray[n++];  // SM revolutions/second
      canFlAcc    = mparray[n++];  // SM (revolutions/second)/second

      // -- stage related
      motionPerSMDRev = mparray[n++];  // inches/revolution
      xjSpeedFast = mparray[n++];      // inches/sec
      xjSpeedSlow = mparray[n++];      // inches/sec (but here, microns/sec)
      maxStageAccel = mparray[n++];    // SM (revolutions/second)/second
      bumpAngle = mparray[n++];        // degrees after TDC
      noBumpTurns = (int)mparray[n++];
      stageFlVel = mparray[n++];       // SM revolutions/second
      stageFlAcc = mparray[n++];       // SM (revolutions/second)/second

      return true;
    }  // end MachineParmzRead

    /*--------------------------------------------------------------------------
     CoilParmzRead: reads the coil parameters file, echoing it to the log file.
       If the file cannot be read, returns false; if all ok, returns true.
      
     Called by ParmzRead
    --------------------------------------------------------------------------*/
    private static bool CoilParmzRead()
    {
      bool cpzok = true;
      Parmz cpz = new Parmz(lz, @"..\..\Coil.parmz", 5, ref cpzok);
      if (!cpzok) 
      {
        Util.W2W("Error reading Coil parameters file");
        return false;
      }
      cparray = cpz.Parray;  // retrieve the coil parameters
      int n = 0;
      coreDiameter = cparray[n++];        // inches
      spoolWidth = cparray[n++];          // flange-to-flange, inches
      coilLayers = (int)cparray[n++];     // # of layers to wind
      baseLayerTurns = (int)cparray[n++]; // # turns on layer 1
      fiberDiam = cparray[n++]/25400.0;   // inches (from microns)
      return true;
    }  // end CoilParmzRead
  }  // end Winder class
}  // end WinderProject namespace

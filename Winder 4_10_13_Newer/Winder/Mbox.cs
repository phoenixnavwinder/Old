/*------------------------------------------------------------------------------
File: Mbox.cs                                          Updated: 12/03/2012

Objective: Manage the stepper motor driver (SMD) serial ports for Winder
See Also: Sportz.cs, Mbox.txt for more explanation
Problems: WIP!
Modifications:
 Date       Who   Comments
------------------------------------------------------------------------------*/
using System;
using System.IO.Ports;
using System.Threading;

using Zlib;  // for Logz

namespace WinderProject
{
  public class Mbox 
  {
    //================= INSTANCE VARIABLES and PROPERTIES ======================
    private static Logz lz;  // the log file
    private static SerialPort serialPort;
    private static Jrkz jrkx;  // the Jrkz object for the X side
    private static Jrkz jrky;  // the Jrkz object for the Y side

    private static int[]  turnOnInts;    // the sequence of 3 turn-on characters
    private static int    stepsPerRev;  // SMD steps per revolution
    private static int    stepsPerCanTurn;
    public  static int    StepsPerCanTurn { set { stepsPerCanTurn = value; } }
    private static double stepsPerInch;
    public  static double StepsPerInch { set { stepsPerInch = value; } }

    // status related
    private static bool   hub444P;     // false => no hub, thus one SMD
    private static int    numSMD;      // # of SMDs on the Hub 444
 
    // Smd numbers
    private static int    xCanSmd;     // X side can stepper motor driver #
    private static int    xStageSmd;   // X side stage stepper motor driver #
    private static int    yCanSmd;     // Y side can stepper motor driver #
    private static int    yStageSmd;   // Y side stage stepper motor driver #

    // jogging[n] is true if SMD n (1 to 4) is jogging
    private static bool[] jogging = new bool[5];
 
    // saved stage encoder position values 
    private static int    rightStartX;
    private static int    rightStartY;
    private static int    leftStartX;
    private static int    leftStartY;
    private static int    parkX;
    private static int    parkY;
   
    // command related
    private static int    comSMD;       // the command SMD number (1 to 4, or 0)
    private static string comString;    // command string (without SMD #)
    private static string comCode;      // command code (2 characters)
    private static string comValueString;  // command value string
    private static double comValue;        // command value
    private static bool   comRes;       // command elicits an ordinary response
    private static bool   comResLogz;   // if true, logz the command & response
    public  static bool   ComResLogz { get { return comResLogz; } 
                                     set { comResLogz = value; } }
    
    // response related
    private static int    resSMD;      // the response SMD number (1 to 4, or 0)
    private static string resString;   // response string (without SMD #)
    private static int    resType;     // response type, 1 to 4
    private static string resCode;     // response command code (2 characters)
    private static string resValueString;  // response value string
    private static double resValue;        // the response value, if numerical  
    
    // Ack/Nack related
    private static bool ackNackOn;    // true => AckNack enabled; false => not
    private static bool ackNackExpected; // true => an AckNack response expected
    private static char ackNackChar;  // the ackNack character: '%', '*', or '?'
    private static int  ackNackInt;   // the second ackNack character, if any
    private static string[] ackNackCodes =
    { "No code", "Command timed out", "Parameter is too long",
      "Too few parameters", "Too many paramaters", "Parameter out of range", 
      "Command buffer (queue) full", "Cannot process command", 
      "Program running", "Bad Password", "Comm port error", "Bad character" };

    // Misc
    private static double ccVal;  // SM running current, amps 
    private static double ciVal;  // SM idle current, amps
    
    //=========================== PUBLIC METHODS ===============================
    /*--------------------------------------------------------------------------
     Initialize: called by Winder.Initialize
    --------------------------------------------------------------------------*/
    public static void Initialize(Logz lzx)
    {
      lz = lzx;  // get the log file object

      Console.WriteLine();
      Console.WriteLine("Be sure power is on to the Motor Box"
                        + ", then hit Enter");
      Console.ReadLine();

      // Initialize the stepper motor drivers.
      lz.Bl();
      Util.W2("Motor Control Box Initialization");
      serialPort = new SerialPort();  // create a new SerialPort object
      Sportz.PortSetup(lz, serialPort, Winder.hub444ComPort);  // port set up
      ackNackOn = false;  //??? mostly for debug when TurnOn is not called
      Console.WriteLine();
      Util.W2(" -- Motor Box SMDs initialized");
      stepsPerRev = Winder.smdStepsPerRev;
      numSMD = Winder.numSMD;
      hub444P = Winder.hub444P;
      TurnOn();  // test the SMDs
      SetMotorCurrent(Winder.motorCurrent, Winder.idleCurrent, Winder.delayTime);

      if (Winder.xPololuComPort != 0)  // X Jrk motor controller connected
      {
          jrkx = new Jrkz();
          jrkx.Setup(lz, Winder.xPololuComPort);
          jrkx.SendCommandBytes(Winder.xServoVolts);
          lz.Sl(" -- X Pololu Jrk motor controller initialized");
      }

      if (Winder.yPololuComPort != 0)  // Y Jrk motor controller connected
      {
          jrky = new Jrkz();
          jrky.Setup(lz, Winder.yPololuComPort);
          jrky.SendCommandBytes(Winder.yServoVolts);
          lz.Sl(" -- Y Pololu Jrk motor controller initialized");
      }
    }  // end Initialize

    /*--------------------------------------------------------------------------
     TurnOn: uses the command comsole to correctly turn on the SMD and assure
       that it is working. Resets the baud rate.
     
     Called by Winder.InitializeMotorBox
    --------------------------------------------------------------------------*/
    public static void TurnOn()
    {
      comResLogz = true;
      ackNackOn  = true;

      lz.Bl();
      lz.Sl("Starting Mbox.TurnOn");

      // There is sometimes a turn-on response {63, 4, 20}. Read it.
      //  -- With the Hub 444 it is {63, 19, 124}.
      if (TurnOnRead())
        lz.Sl(" -- turn on int sequence is " + turnOnInts[0].ToString()
              + ", " + turnOnInts[1].ToString()
              + ", " + turnOnInts[2].ToString());
      else
        lz.Sl(" - no turn on int sequence");

      if (hub444P)  // numSMD = # stepper motor drivers
        for (int n = 1; n <= numSMD; n++)
        {
          Util.Delay(20);
          lz.Sl(" -- motor number " + n.ToString());
          SetAckNackOn(n); 
          Util.Delay(20);
          CommandSend(n, "PR", true);  // confirm ptotocol
          Util.Delay(20);
          CommandSend(n, "EG", stepsPerRev);
          Util.Delay(20);
          CommandSend(n, "EG", true);  // confirm
          Util.Delay(20);
          CommandSend(n, "SP", 0);
          Util.Delay(20);
          CommandSend(n, "IP", true);
          jogging[n] = false;
        }
      else  // no Hub 444, single motor driver
      {
        SetAckNackOff(0);
        ResetBaudRate(5);  // set baud rate to 115200 
      }

      // Save the turn-on motor current values.
      CommandSend(1, "CC", true);
      ccVal = resValue;
      CommandSend(1, "CI", true);
      ciVal = resValue;
      lz.Sl(" -- CC and CI values are " + ccVal.ToString()
            + ", " + ciVal.ToString());

      lz.Flush();
    }  // end TurnOn

    /*--------------------------------------------------------------------------
     Close: closes the port
     
     Called by Winder.Terminate
    --------------------------------------------------------------------------*/
    public static void Close()
    {
      serialPort.Close();
    }  // end Close

    /*--------------------------------------------------------------------------
     SetMotorCurrent: sets the current, the idle current, and the delay time
     
     Called by Winder.InitializeMotorBox
    --------------------------------------------------------------------------*/
    public static void SetMotorCurrent(double motorCurrent, double idleCurrent, 
                                       double delayTime)
    {
      for (int n = 1; n <= numSMD; n++)
      {
        CommandSend(n, "CD", delayTime);
        Util.Delay(20);
        CommandSend(n, "CC", motorCurrent);
        Util.Delay(20);
        CommandSend(n, "CI", idleCurrent);
        Util.Delay(20);
      }
    }  // end SetMotorCurrent

    /*--------------------------------------------------------------------------
     SetSmds: sets the numbers for X and Y can and stage motors
     
     Called by Winder.Setup
    --------------------------------------------------------------------------*/
    public static void SetSmds(int xc, int xs, int yc, int ys)
    {
      xCanSmd   = xc;
      xStageSmd = xs;
      yCanSmd   = yc;
      yStageSmd = ys;
    }  // end SetSmds

    /*--------------------------------------------------------------------------
     SetFlLimits: sets velocity and acceleration limits for can and stage motors
     
     Called by Winder.Setup
    --------------------------------------------------------------------------*/
    public static void SetFlLimitsP(double cV, double cA, double sV, double sA)
    {
      int nd = 20;  // delay msec
      Util.Delay(nd);
      CommandSend(1, "VE", cV);
      Util.Delay(nd);
      CommandSend(2, "VE", cV);
      Util.Delay(nd);
      CommandSend(1, "AC", cA);
      Util.Delay(nd);
      CommandSend(2, "AC", cA);
      Util.Delay(nd);
      CommandSend(3, "VE", sV);
      Util.Delay(nd);
      CommandSend(4, "VE", sV);
      Util.Delay(nd);
      CommandSend(3, "AC", sA);
      Util.Delay(nd);
      CommandSend(4, "AC", sA);
      lz.Sl(" -- can   Fl velocity = " + cV.ToString());
      lz.Sl(" -- can   Fl accel    = " + cA.ToString());
      lz.Sl(" -- stage Fl velocity = " + sV.ToString());
      lz.Sl(" -- stage Fl accel    = " + sA.ToString());
    }  // end SetFlLimits

    /*--------------------------------------------------------------------------
     GetIP: get the immediate encoder position (used while jogging)
    --------------------------------------------------------------------------*/
    public static int GetIP(int nsmd)
    {
      CommandSend(nsmd, "IP", true);    // read encoder position
      return (int)resValue;
    }  // end GetIP

    /*--------------------------------------------------------------------------
     GetLazySusanPosition: returns +1 if X stage is on left
                           returns -1 if Y stage is on left
                           returns  0 if neither (swap in process)

     The Winder lazy Susan left side switch is wired to SMD1 as input STEP, and
     the right side switch is wired as DIR. These switches are normally open.
     They are closed when the X stage is in contact. When the IS command goes to 
     SMD1 it responds with 
       IS=10000010   if the X stage is on the left
       IS=10000001   if the X stage is on the right
       IS=10000011   if the X stage is on neither side (in transit)   
     
     Called by Winder.LazySusan
    --------------------------------------------------------------------------*/
    public static int GetLazySusanPosition()
    {
      Util.Delay(100);
      CommandSend(1, "IS", true);    // input status command
      if (resValueString.Length != 8)
      {
        Util.W2(" *** bad response to IS command (for Lazy Susan) is: "
                  + resValueString);
        return 0;
      }
      for (int i = 0; i < 8; i++)
      {
        if (resValueString[i] != '0' && resValueString[i] != '1')
        {
          Util.W2(" *** bad response to IS command (for Lazy Susan) is: "
                    + resValueString);
          return 0;
        }
      }
      if (resValueString[6] == '0' && resValueString[7] == '0')
      {
        Util.W2(" *** bad response to IS command (for Lazy Susan) is: "
                  + resValueString);
        return 0;
      }
      if (resValueString[7] == '0') return  1;  // X on left
      if (resValueString[6] == '0') return -1;  // X on right
      return 0;
    }  // end GetLazySusanPosition

    /*--------------------------------------------------------------------------
     RecordGoTo: called by Bbox ButtonAction when button 11, 12, 13, or 14 is 
       pressed. 
     
     Input rec is true if record is on; false if goto is on. Input xs is true 
     if the X stage is indicated. Input n = 1 for Right Start, n = 2 for Left 
     Start, n = 3 for Park, n = 4 for home theta. 
    --------------------------------------------------------------------------*/
    public static void RecordGoTo(bool rec, bool xs, int n)
    {
      bool jogStop;
      int nsmd;
      int pos;
      int move = 0;
      int target = 0;
      string[] positions = { "rightStart", "leftStart", "park", "homeTheta" };
      string[] xory = { "X", "Y" };
      int nxory;
      if (xs) nxory = 0;
      else    nxory = 1;

      if (n < 4)  // right start, left start, or park
      {
        if (xs) nsmd = xStageSmd;  // the stage motor driver number
        else    nsmd = yStageSmd;
        pos = GetIP(nsmd);  // the current stage position
        double posi = (double)pos / stepsPerInch;
        string stpos = posi.ToString("F4").PadLeft(8);

        if (rec)  // mode is record
        {
          if (xs)  // X stage
          {
            if      (n == 1) rightStartX = pos;
            else if (n == 2) leftStartX = pos;
            else if (n == 3) parkX = pos;
          }
          else  // Y stage
          {
            if      (n == 1) rightStartY = pos;
            else if (n == 2) leftStartY = pos;
            else if (n == 3) parkY = pos;
          }
          Util.W2(" -- record " + positions[n - 1] + xory[nxory] + " to be "
                    + stpos + " inches");
        }
        else  // mode is goto 
        {
          // First, cease jogging   
          jogStop = false;
          if (jogging[nsmd])
          {
            StopJogging(nsmd);
            jogStop = true;
          }
          Util.Delay(20);        
          if (xs)  // X stage
          {
            if      (n == 1) target = rightStartX;
            else if (n == 2) target = leftStartX;
            else if (n == 3) target = parkX;
          }
          else  // Y stage
          {
            if      (n == 1) target = rightStartY;
            else if (n == 2) target = leftStartY;
            else if (n == 3) target = parkY;
          }
          double tin = (double)target / stepsPerInch;
          string stin = tin.ToString("F4").PadLeft(8);
          move = target - pos;
          CommandSend(nsmd, "FL", move);
          Util.W2(" -- goto " + positions[n - 1] + xory[nxory] + " at "
                    + stin + " inches");

          // Resume jogging
          if (jogStop) CommenceJogging(nsmd);
        }
        return;
      }

      // n is 4, so home theta is indicated
      if (xs) nsmd = xCanSmd;  // the can motor driver number
      else    nsmd = yCanSmd;
      if (rec)
      {
        jogStop = false;
        if (jogging[nsmd])
        {
          StopJogging(nsmd);
          jogStop = true;
        }
        Util.Delay(20);        
        CommandSend(nsmd, "SP", 0);  // set the current position to zero
        Util.Delay(20);
        int nscp = GetIP(nsmd);
        Util.W2(" -- home theta; current can position reset to zero");
        if (jogStop) CommenceJogging(nsmd);
      }
      else  // mode is goto 
      {
        TopDeadCenter(nsmd);
        lz.Sl(" -- goto " + positions[3] + xory[nxory] + " is TDC");
      }
    }  // end RecordGoTo

    /*--------------------------------------------------------------------------
     TopDeadCenter: positions the can in its TDC (top dead center) position, and
       resets the encoder reading to zero. In positioning mode, TDC is called by 
       Bbox method RecordGoto, which is called by Winder method RecordGoto, 
       which is called by Bbox method ButtonAction when button 14 (home theta 
       position) is called (if the record/goto mode is goto). In winding mode,
       TDC is called both by StartLayer, as a layer begins, and by EndLayer, as 
       the layer ends. If the can SMD is jogging when the call comes here, it 
       must temporarily turned off, and it must be turned back on.
    --------------------------------------------------------------------------*/
    public static void TopDeadCenter(int nsmd)
    {
      bool jogStop = false;
      if (jogging[nsmd])
      {
        StopJogging(nsmd);  // stop jogging
        jogStop = true;
      }

      int canPos = GetIP(nsmd);
      if (canPos == 0)
      {
        if (jogStop) CommenceJogging(nsmd);  // start jogging
        Util.W2("TDC aborted as position is zero");
        return;
      }

      // Find nearest number of integral turns, then go there.
      int turns = (int)Math.Round((double)canPos / (double)stepsPerCanTurn);
      int desiredPos = turns * stepsPerCanTurn;
      CommandSend(nsmd, "FP", desiredPos);
      Util.Delay(1000);  // wait one second for the move
      int tdcCanPos = GetIP(nsmd);
      Winder.TdcCanPos = tdcCanPos;  // pass this back to Winder
      Util.Delay(20);
      ResetEncoder(nsmd);
      canPos = GetIP(nsmd);
      lz.Sl(" -- can position after reset  is " + canPos.ToString());
      if (jogStop) CommenceJogging(nsmd);  // start jogging
    }  // TopDeadCenter

    /*--------------------------------------------------------------------------
     Bump: move the stage by a fiber diameter. bumpCounts is the fiber diameter
       in steps.
     
     Called by Winder.Wind
    --------------------------------------------------------------------------*/
    public static void Bump(int nsmd, int bumpCounts)
    {
      CommandSend(nsmd, "FL", bumpCounts);
    }  // end Bump

    /*--------------------------------------------------------------------------
     Switch: set the switch on or off. Used to toggle brakes or servos
     
     Called by Winder.SetBrake or Winder.SetServo
    --------------------------------------------------------------------------*/
    public static void Switch(int nsmd, bool on)
    {
      if (on) CommandSend(nsmd, "IO", 0);   // on
      else    CommandSend(nsmd, "IO", 1);   // off
    }  // end Switch

    /*--------------------------------------------------------------------------
     DisplayStagePositions: X and Y right start, left start and park
     
     Called by Winder.DisplayFullStatus
    --------------------------------------------------------------------------*/
    public static void DisplayStagePositions()
    {
      double rs = (double)rightStartX / stepsPerInch;
      double ls = (double)leftStartX / stepsPerInch;
      double pk = (double)parkX / stepsPerInch;
      string sr = rs.ToString("F4").PadLeft(8);
      string sl = ls.ToString("F4").PadLeft(8);
      string sp = pk.ToString("F4").PadLeft(8);
      string st = "X right = " + sr + ", X left = " + sl + ", X park = " + sp;
      Util.W2(st);

      rs = (double)rightStartY / stepsPerInch;  // then the Y side
      ls = (double)leftStartY / stepsPerInch;
      pk = (double)parkY / stepsPerInch;
      sr = rs.ToString("F4").PadLeft(8);
      sl = ls.ToString("F4").PadLeft(8);
      sp = pk.ToString("F4").PadLeft(8);
      st = "Y right = " + sr + ", Y left = " + sl + ", Y park = " + sp;
      Util.W2(st);
    }  // end DisplayStagePositions

    /*--------------------------------------------------------------------------
     SetBrake: called by Bbox ButtonAction when button 9 or 10 is pressed. If
       input active is true, the active break is set, otherwise the inactive 
       break is set. If on is true, the brake is set on, otherwise the brake is
       set off. Bbox ButtonAction first assures that both brakes will not be on.
    --------------------------------------------------------------------------*/
    public static void SetBrake(bool active, bool on)
    {
      int nsmd;  // the SMD the brake relay is connected to
      bool xactive = Winder.Xactive;
      if ((active && xactive) || (!active && !xactive)) nsmd = 1;
      else nsmd = 2;
      Mbox.Switch(nsmd, on);

      if (active)
      {
          if (on) Util.W2("Active brake set on");
        else Util.W2("Active brake set off");
      }
      else
      {
        if (on) Util.W2("Inactive brake set on");
        else Util.W2("Inactive brake set off");
      }
    }  // SetBrake

    /*--------------------------------------------------------------------------
     SetServo: called by Bbox ButtonAction when button 15 is pressed.
    
     Input nsmd is the number of the stepper motor driver that controls the
     servo switch. nsmd is 3 for the X side or 4 for thr Y side. Input on is 
     true if the servo is to be set on.
    --------------------------------------------------------------------------*/
    public static void SetServo(int nsmd, bool on)
    {
      Mbox.Switch(nsmd, on);  // ??? may want !on here
      Ledz.SetLEDState(7, !on);  // LED is lit if servo is not on (park mode)

      string side;
      if (nsmd == 3)  // X side being toggled
      {
        side = "X";
        Winder.xPark = !on;
      }
      else  // Y side being toggled
      {
        side = "Y";
        Winder.yPark = !on;
      }

      string ps = "to park";
      if (on) ps = "to servo";
      Util.W2(side + " side Jrk set to " + ps);
    }  // end SetServo

    //====================== PUBLIC JOG-RELATED METHODS ========================
    /*--------------------------------------------------------------------------
     JogSetup: prepare to jog
    --------------------------------------------------------------------------*/
    public static void JogSetup(int nsmd, double adrate, double speed)
    {
      CommandSend(nsmd, "IF", "D");     // immediate format => decimal for IP
      CommandSend(nsmd, "JA", adrate);  // jog accel rate in rev/sec/sec
      CommandSend(nsmd, "JS", speed);   // jog speed in rev/sec
    }  // end JogSetup

    /*--------------------------------------------------------------------------
     CommenceJogging:
    --------------------------------------------------------------------------*/
    public static void CommenceJogging(int nsmd)
    {
      CommandSend(nsmd, "CJ", false);
      jogging[nsmd] = true;
    }  // end CommenceJogging

    /*--------------------------------------------------------------------------
     ChangeJogSpeed:
    --------------------------------------------------------------------------*/
    public static void ChangeJogSpeed(int nsmd, double speed)
    {
      CommandSend(nsmd, "CS", speed);
    }  // end ChangeJogSpeed

    /*--------------------------------------------------------------------------
     StopJogging:
    --------------------------------------------------------------------------*/
    public static void StopJogging(int nsmd)
    {
      CommandSend(nsmd, "SJ", false);
      jogging[nsmd] = false;
    }  // end StopJogging


    //======================= PUBLIC DEBUG TEST METHODS ========================
    /*--------------------------------------------------------------------------
     JogTest: This is a simple, temporary, test routine. (See Jog.txt)  
    --------------------------------------------------------------------------*/
    public static void JogTest(int nsmd)
    {
      lz.Sl(" JogTest begins --------------------");

      JogSetup(nsmd, 10, 1);
      CommandSend(nsmd, "IP", true);    // read encoder position

      CommandSend(nsmd, "CJ", false);  // commence jogging
      Util.Delay(1000);
      CommandSend(nsmd, "SJ", false);  // stop jogging
      CommandSend(nsmd, "IP", true);   // read encoder position

      CommandSend(nsmd, "DI", -1);     // jog direction negative
      Util.Delay(10);
      CommandSend(nsmd, "CJ", false);  // commence jogging
      Util.Delay(1000);
      CommandSend(nsmd, "SJ", false);  // stop jogging
      CommandSend(nsmd, "IP", true);   // read encoder position

      CommandSend(nsmd, "DI", 1);      // jog direction positive
      CommandSend(nsmd, "SP", 0);      // encoder position to zero
      CommandSend(nsmd, "CJ", false);  // commence jogging
      for (int i = 0; i < 5; i++)
      {
        Util.Delay(1000);
        CommandSend(nsmd, "IP", true);   // read encoder position
      }
      CommandSend(nsmd, "SJ", false);  // stop jogging
      CommandSend(nsmd, "IP", true);   // read encoder position

      lz.Sl(" JogTest ends --------------------");
    }  // end JogTest

    //=========================== PRIVATE METHODS ==============================
    /*--------------------------------------------------------------------------
     MotorTest: used if Winder parameter motorTest is true
    --------------------------------------------------------------------------*/
    private static void MotorTest(int n)
    {
      Console.WriteLine("\n Motor " + n.ToString() + " test");
      Console.WriteLine(" Green LED should be flashing. Hit Enter.");
      Console.ReadLine();

      CommandSend(n, "MD", false);  // motor disable
      Console.WriteLine(" Green LED should be steady. Hit Enter.");
      Console.ReadLine();

      CommandSend(n, "ME", false);  // motor enable
      Console.WriteLine(" Green LED should be flashing. Hit Enter.");
      Console.ReadLine();

      CommandSend(n, "PR", true);  // get protocol
      Util.Delay(2000);
      CommandSend(n, "EG", stepsPerRev);  // set pulses per rev
      Console.WriteLine(" motor should turn one revolution");
      Util.Delay(2000);
      CommandSend(n, "FL", stepsPerRev);  // move one rev
      Util.Delay(2000);

      Console.Clear();
    }   // end MotorTest

    /*--------------------------------------------------------------------------
     TurnOnRead: Check for a turn-on response from the SMD. If the power was 
       actually off when the TurnOn was called, then a 3 character sequence 
       {63, 4, 20}  should be sent. If the unit had been on and read before, 
       then there will be no such sequence.
    --------------------------------------------------------------------------*/
    private static bool TurnOnRead()
    {
      int n;
      int nch = 0;
      turnOnInts = new int[3];
      while (true)
      {
        try { n = serialPort.ReadChar(); }
        catch (TimeoutException) { return false; }

        turnOnInts[nch++] = n;
        if (nch > 2) return true;
      }
    }  // end TurnOnRead

    /*--------------------------------------------------------------------------
     ResetBaudRate: the input integer (1 to 5) resets the baud rate to BR[bri]
     
     When Setup is run, the PC side of the port is set to 9600 baud. When TurnOn
     is run, and power goes on to the SMD, it is also set for 9600 baud. This 
     routine is then used to increase the baud rate, usually to 115200 baud,
     with bri = 5. If the Hub 444 is present, and is in the router mode (as it 
     is) the rate limit is 9600, so this method should not be used.
    --------------------------------------------------------------------------*/
    private static void ResetBaudRate(int bri)
    {
      int[] BR = { 0, 9600, 19200, 38400, 57600, 115200 };
      if (hub444P)  return;
      CommandSend(0, "BR", bri);
      serialPort.BaudRate = BR[bri];
    }  // end ResetBaudRate

    /*--------------------------------------------------------------------------
     CommandSend: send a command to the SMD, and deal with the response.
     
     There are five versions of "CommandSend". Input nsmd is the number of the
     SMD on the HUB (1 to 4, or 0), which becomes comSMD. If hub444P is false, 
     there is no hub, and nsmd is ignored. If there is a hub, and nsmd == 0, the
     command is sent to all SMDs. Input code is the two-character command code,
     which becomes comCode.
     
     When there is a Hub 444 with fewer than 4 SMDs, no commands are sent to the
     nonexistent SMDs.
     
     The first version, for type 1 "no value" commands. Input res (becomes 
     comRes) is true if the command elicits a response; is false if not.
     
     The second version, for type 2 "numeric value" commands. Input value is the
     numeric value. Numeric value commands do not elicit responses. The SMD does
     not accomodate long value strings such as 0.1234567890. These are limited
     to four digits past the decimal point.
     
     The third version is like the second, except that the value is an integer,
     not a double.
          
     The third version, for type 3 "string value" commands. Input str is the
     string value. String value commands do not elicit responses.
     
     The final version, usually only called by the other three, presumes that 
     the entire command (except the SMD number and the carriage return) is 
     already in comString, and that comSMD and comRes are set. All commands are 
     finally sent by this method, which gets the response, if there is one, and 
     handles the Ack/Nack situation.
    
     While the CommandSend methods are public, for software development reasons,
     they are not called from Winder. They might transition to private.
    --------------------------------------------------------------------------*/
    private static void CommandSend(int nsmd, string code, bool res)  // type 1
    {
      comSMD = nsmd;
      comCode = code;
      comRes = res;
      comValue = 0.0;
      comValueString = null;
      comString = comCode;  // complete command string (less nsmd)
      CommandSend();
    }

    // type 2
    private static void CommandSend(int nsmd, string code, double value)
    {
      comSMD = nsmd;
      comCode = code;
      comRes = false;
      comValue = value;  // save the value
      string st1 = value.ToString();      // limit value string to 4 digits
      string st2 = value.ToString("F4");  //  after the decimal point
      if (st1.Length > st2.Length)  comValueString = st2;
      else                          comValueString = st1;
      // complete command string (less nsmd)
      comString = comCode + comValueString;
      CommandSend();
    }

    private static void CommandSend(int nsmd, string code, int value)  // type 2
    {
      comSMD = nsmd;
      comCode = code;
      comRes = false;
      comValue = (double)value;  // save the value as double
      comValueString = value.ToString();
      // complete command string (less nsmd)
      comString = comCode + comValueString;
      CommandSend();
    }

    private static void CommandSend(int nsmd, string code, string str)  // type 3
    {
      comSMD = nsmd;
      comCode = code;
      comRes = false;
      comValue = 0.0;
      comString = comCode + str;  // complete command string (less nsmd)
      CommandSend();
    }

    private static void CommandSend()  // assumes comSMD, comString,
    {                                  //  and comRes are set
      if (hub444P && comSMD > numSMD) return;  // do not send to SMD not there

      string comSent;
      if (hub444P)  // prefix the command with the SMD number and add the
        comSent = comSMD.ToString() + comString + '\r';  // carriage return
      else  // no Hub 444, so just a single SMD, no number prefix on command
        comSent = comString + '\r';  // just add the carriage return
      serialPort.Write(comSent);  // actually send the command
      resString   = null;  // remove any prior response
      ackNackChar = ' ';   // reset the AckNack character and integer
      ackNackInt  = 0;
      bool resOk  = false;
      ackNackExpected = ackNackOn && !comRes;
      if (ackNackExpected)  resOk = ResponseRead();  // read AckNack response
      else  Util.Delay(10);

      if (comRes)
      {
        resOk = ResponseRead();  // read ordinary response
        if (resOk) ResponseParse();
        else if (comResLogz)  lz.Sl(" --  response not received");
      }

      if (comResLogz) LogzComRes();
    }  // end CommandSend

    /*--------------------------------------------------------------------------
     ResponseRead: reads a string of response characters up to a carriage
       return, and deletes the carriage return. Returns false if the read times 
       out. 
     
     Detects AckNack responses, and if an ordinary response is still expected
     (which should not happen) goes on to read the ordinary response.
    --------------------------------------------------------------------------*/
    private static bool ResponseRead()
    {
      resString = null;
      // read up to the carriage return, and delete the carriage return
      try { resString = serialPort.ReadTo("\r"); }
      catch (TimeoutException)
      {
        lz.Sl(" -- missing response to command " 
              + comSMD.ToString() + comString);
        return false;
      }

      // Get, and remove, any SMD number from the response.
      if (hub444P && Char.IsDigit(resString[0]))
      {
        resSMD = Int32.Parse(resString.Substring(0, 1));
        resString = resString.Substring(1);  // remove the SMD digit
      }
      else resSMD = 0;
      
      // Detect an AckNack response
      if (resString[0] == '%' || resString[0] == '*' || resString[0] == '?')
      {
        ackNackChar = resString[0];
        if (resString.Length > 1) ackNackInt = (int)resString[1];
        else ackNackInt = 0;  // means no second character
       
        if (!ackNackExpected)
        {
          lz.Sl(" --- unexpected AckNack response " + resString 
                + " to command " + comString);
        }
        resString = null;  // for when there is no ordinary response
        if (comRes) return ResponseRead();  // try again for ordinary response
      }
      return true;  // not an AckNack response
    }  // end ResponseRead

    /*--------------------------------------------------------------------------
     ResponseParse: parse an ordinary response message from the SMD. Determine
       the type (resType 1 to 3)
                    
     Presumes the response string is in resString, and it is not an AckNack
     response.
     
     This method is called by CommandSend when a response is expected.
     Sets resType, resCode, resValueString, and resValue.
    --------------------------------------------------------------------------*/
    private static void ResponseParse()
    {
      // Clear out any prior response items. 
      resType = 0;            // response type, 1 to 3
      resCode = null;         // response command code (2 characters)
      resValueString = null;  // response value string
      resValue = 0.0;         // the response value, if numerical (type 1)

      int nch = resString.Length;

      // Look for the equals sign as character 2.
      if (nch > 3 && resString[2] == '=')  // a value (type 1 or 2)
      {
        resCode = resString.Substring(0, 2);  // extract the command code
        resValueString = resString.Substring(3, nch - 3);  // value string
        // try to extract a numerical value
        if (double.TryParse(resValueString, out resValue))
        resType = 1;  // yes, numerical value
        else resType = 2;  // no, string value
        return;
      }

      // type 3 -- pure string response
      resType = 3;
      resValueString = resString;  // value string is the entire string
    }  // end ResponseParse

    /*--------------------------------------------------------------------------
     LogzComRes: logz the last sent command and response
    --------------------------------------------------------------------------*/
    private static void LogzComRes()
    {
      string line = comSMD.ToString() + " command: " 
                    + comString.PadRight(10) + " ";
      if (ackNackChar == ' ')  // there was no AckNack response
        line = line + "   ";
      else  // there was an AckNack response
        line = line + ackNackChar + " ";

      if (resString != null)  // there was an ordinary response
        line = line + resSMD.ToString() + " response: " + resString.PadRight(9) 
               + ", type " + resType.ToString();
      lz.Sl(line);

      if (ackNackChar == '?')  // add a line for AckNack error response
      {
        int n = ackNackInt;
        if (n > 11) n = 0;
        lz.Sl(" --- AckNack error: int = " + ackNackInt.ToString().PadLeft(2)
              + ", " + ackNackCodes[n]);
      }
    }  // end LogzComRes

    /*--------------------------------------------------------------------------
     GetDriveInfo: sends commands to the SMD requesting information and logz the
       responses.
    --------------------------------------------------------------------------*/
    private static void GetDriveInfo(int nsmd)
    {
      comResLogz = true;
      lz.Sl("Getting Drive Information");

      // Configuration commands ------------------------------------------------
      // Get model and revision.
      CommandSend(nsmd, "MV", true);
      if (resString == null)  resString = "~~~~~~~~";
      lz.Sl(" -- model & revision are: " + resString);
      Util.Delay(5);

      // Get model number. Note the special port read here.
      CommandSend(nsmd, "MN", true);
      char mnch = ' ';
      try { mnch = (char)serialPort.ReadChar(); }
      catch (TimeoutException) { }

      //if (resChar == (char)0) resChar = '~';
      lz.Sl(" -- model number (character) is: " + mnch);

      // Get firmware version number.
      CommandSend(nsmd, "RV", true);
      if (resValueString == null) resValueString = "~";
      lz.Sl(" --  firmware version is: " + resValueString);

      // Request Status.
      CommandSend(nsmd, "RS", true);
      if (resValueString == null) resValueString = "~";
      lz.Sl(" -- status is: " + resValueString);

      // Get status code.
      CommandSend(nsmd, "SC", true);
      if (resValueString == null) resValueString = "~";
      lz.Sl(" -- status code is: " + resValueString);

      // Communications commands -----------------------------------------------
      // Get protocol.
      CommandSend(nsmd, "PR", true);
      if (resValueString == null) resValueString = "~";
      lz.Sl(" -- protocol is: " + resValueString);

      // Get baud rate.
      string[] brstr = { "no value", "9600", "19200", "38400", "57600",
                         "115200" };
      CommandSend(nsmd, "BR", true);
      if (resValueString == null) resValueString = "~";
      lz.Sl(" -- baud rate is: " + resValueString + " = " 
             + brstr[(int)resValue]);

      lz.Bl();
    }  // end GetDriveInfo

    /*--------------------------------------------------------------------------
     SetAckNackOn: sets Ack/Nack on
    --------------------------------------------------------------------------*/
    private static void SetAckNackOn(int nsmd)
    {     
      CommandSend(nsmd, "PR", 4);
      ackNackOn = true;
    }  // SetAckNackOn

    /*--------------------------------------------------------------------------
      SetAckNackOff: sets Ack/Nack off.
    --------------------------------------------------------------------------*/
    private static void SetAckNackOff(int nsmd)
    {
      CommandSend(nsmd, "PR", 1);
      ackNackOn = false;
    }  // SetAckNackOff

    /*--------------------------------------------------------------------------
     GetSP: get the encoder position (used while not jogging)
    --------------------------------------------------------------------------*/
    private static int GetSP(int nsmd)
    {
      CommandSend(nsmd, "SP", true);    // read encoder position
      //lz.Sl(" -- response to GetSP is " + resString);
      int pos = (int)resValue;
      return pos;
    }  // end GetEP

    /*--------------------------------------------------------------------------
     ResetEncoder: 
    --------------------------------------------------------------------------*/
    private static void ResetEncoder(int nsmd)
    {
      CommandSend(nsmd, "SP", 0);  // reset the position to zero
      Util.Delay(20);
    }  // end Reset encoder

    /*--------------------------------------------------------------------------
     Test: called by Winder.Test
    --------------------------------------------------------------------------*/
    public static void Test()
    {
      Console.WriteLine();
      string st = "Motor box test entered. Enter motor number (1 to 4)";
      Console.WriteLine(st);
      st = Console.ReadLine();
      int nsmd = Int32.Parse(st);
      st = "Enter a character: C -> command test, J -> jog test";
      Console.WriteLine(st);
      st = Console.ReadLine();
      char ch = Char.ToUpper(st[0]);
      switch (ch)
      {
        case 'C':
          Console.WriteLine("command test of motor " + nsmd.ToString());
          MotorTest(nsmd);
          break;
        case 'J':
          st = "jog test of motor " + nsmd.ToString() + ", results to log file";
          Console.WriteLine(st);
          JogTest(nsmd);
          break;
      }
    }  // end Test
  }  // end Mbox class
}  // end WinderProject namespace



/*------------------------------------------------------------------------------
File: Jrkz.cs                                          Updated: 10/21/2012

Objective: Manage the Pololu Jrk serial ports for Winder
See Also:  Sportz.cs, Pololu Jrk USB Motor Controller User's Guide
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
  public class Jrkz 
  {
    //================= INSTANCE VARIABLES and PROPERTIES ======================
    private Logz lz;  // the logz file
    private SerialPort serialPort;

    // Command bytes to send for servo
    private byte[] sbytes; 

    //============================= METHODS ====================================
    /*--------------------------------------------------------------------------
     Setup: connects to the logz file (lzx); creates a new serial port; sets up 
       the port; opens the port. This follows the constructor.
    --------------------------------------------------------------------------*/
    public void Setup(Logz lzx, int nComPort)
    {
      lz = lzx;  // get the logz file object
      serialPort = new SerialPort();  // create a new SerialPort object
      Sportz.PortSetup(lz, serialPort, nComPort);  // set up the port
    }  // end Setup

    /*--------------------------------------------------------------------------
     SendCommandBytes: 
    --------------------------------------------------------------------------*/
    public void SendCommandBytes(double servoVolts)                     
    {
      sbytes = V2B(servoVolts);
      serialPort.Write(sbytes, 0, 2);
    }  // end SetCommandBytes

    /*--------------------------------------------------------------------------
     V2B: input volts is the voltage to set in a Jrk "Set Target High 
       Resolution" command. Return bb (2 bytes) contains the command bytes.
     
     See the Pololu Jrk USB Motor Controller User's Guide, p. 31.
    --------------------------------------------------------------------------*/
    private byte[] V2B(double volts)
    {
        int vint = (int)(volts * 4096.0 / 5.0);
        if (vint < 0) vint = 0;
        if (vint > 4095) vint = 4095;
        byte[] bb = new byte[2];
        bb[0] = (byte)(192 + vint - (vint / 128) * 128);
        bb[1] = (byte)(vint / 32);
        return bb;
    }  // end V2B
  }  // end Jrkz class
}  // end WinderProject namespace

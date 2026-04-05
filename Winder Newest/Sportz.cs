/*------------------------------------------------------------------------------
File: Sportz.cs                                        Updated: 12/03/2012

Objective: Initialize serial ports for Winder
See Also:
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
  class Sportz
  {
    /*--------------------------------------------------------------------------
     PortSetup: sets the port name, the baud rate, the parity, the data bits, 
       the stop bits, the handshake, and the read and write timeouts, and opens 
       the port. If Input comPortNum is 5, for example, then the com port name 
       is "COM5".
    --------------------------------------------------------------------------*/
    public static void PortSetup(Logz lz, SerialPort serialPort, 
                                 int comPortNum)
    {
      // Establish the serial port properties.
      //  This may be done either manually (via the console), or automatically.
      //  The first group of settings that call SetPort methods, are manual.
      //  The second set are automatic. Use comments to select each property 
      //  from one or the other group.

      // Manual properties setting group ------------------------------
      //serialPort.PortName = SetPortName(serialPort.PortName);
      //serialPort.BaudRate = SetPortBaudRate(serialPort.BaudRate);
      //serialPort.Parity = SetPortParity(serialPort.Parity);
      //serialPort.DataBits = SetPortDataBits(serialPort.DataBits);
      //serialPort.StopBits = SetPortStopBits(serialPort.StopBits);
      //serialPort.Handshake = SetPortHandshake(serialPort.Handshake);

      // Automatic properties setting group ---------------------------
      string comPortName = "COM" + comPortNum.ToString();
      serialPort.PortName = comPortName;
      serialPort.BaudRate = 9600;
      serialPort.Parity = (Parity)Enum.Parse(typeof(Parity), "None");
      serialPort.DataBits = 8;
      serialPort.StopBits = (StopBits)Enum.Parse(typeof(StopBits), "One");
      serialPort.Handshake = (Handshake)Enum.Parse(typeof(Handshake), "None");

      // Set the read/write timeouts, in milleseconds
      serialPort.ReadTimeout = 500;  //?? should these be 20
      serialPort.WriteTimeout = 500;

      serialPort.Open();

      // Logz the settings
      lz.Bl();
      lz.Sl("Serial port open");
      lz.Sl(" -- name     : " + serialPort.PortName);
      lz.Sl(" -- baud rate: " + serialPort.BaudRate.ToString());
      lz.Sl(" -- parity   : " + serialPort.Parity);
      lz.Sl(" -- data bits: " + serialPort.DataBits.ToString());
      lz.Sl(" -- stop bits: " + serialPort.StopBits);
      lz.Sl(" -- handshake: " + serialPort.Handshake);
      lz.Bl();
      lz.Flush();

    }  // end Setup

    //====================== PORT SETTING METHODS ==============================
    /*--------------------------------------------------------------------------
     SetPortName:
    --------------------------------------------------------------------------*/
    public string SetPortName(string defaultPortName)
    {
      string portName;

      Console.WriteLine("Available Ports:");
      foreach (string s in SerialPort.GetPortNames())
        Console.WriteLine("   {0}", s);

      Console.Write("COM port({0}): ", defaultPortName);
      portName = Console.ReadLine();

      if (portName == "")  portName = defaultPortName;
      return portName;
    }  // end SetPortName

    /*--------------------------------------------------------------------------
     SetPortBaudRate
    --------------------------------------------------------------------------*/
    public int SetPortBaudRate(int defaultPortBaudRate)
    {
      string baudRate;

      Console.Write("Baud Rate({0}): ", defaultPortBaudRate);
      baudRate = Console.ReadLine();

      if (baudRate == "")  baudRate = defaultPortBaudRate.ToString();
    
      return int.Parse(baudRate);
    }  // end SetPortBaudRate

    /*--------------------------------------------------------------------------
     SetPortParity:
    --------------------------------------------------------------------------*/
    public Parity SetPortParity(Parity defaultPortParity)
    {
      string parity;

      Console.WriteLine("Available Parity options:");
      foreach (string s in Enum.GetNames(typeof(Parity)))
        Console.WriteLine("   {0}", s);

      Console.Write("Parity({0}):", defaultPortParity.ToString());
      parity = Console.ReadLine();

      if (parity == "")  parity = defaultPortParity.ToString();

      return (Parity)Enum.Parse(typeof(Parity), parity);
    }  // end SetPortParity
    /*--------------------------------------------------------------------------
     SetPortDataBits:
    --------------------------------------------------------------------------*/
    public int SetPortDataBits(int defaultPortDataBits)
    {
      string dataBits;

      Console.Write("Data Bits({0}): ", defaultPortDataBits);
      dataBits = Console.ReadLine();

      if (dataBits == "")  dataBits = defaultPortDataBits.ToString();

      return int.Parse(dataBits);
    }   // end SetPortDataBits

    /*--------------------------------------------------------------------------
     SetPortStopBits:
    --------------------------------------------------------------------------*/
    public StopBits SetPortStopBits(StopBits defaultPortStopBits)
    {
      string stopBits;

      Console.WriteLine("Available Stop Bits options:");
      foreach (string s in Enum.GetNames(typeof(StopBits)))
        Console.WriteLine("   {0}", s);

      Console.Write("Stop Bits({0}):", defaultPortStopBits.ToString());
      stopBits = Console.ReadLine();

      if (stopBits == "") stopBits = defaultPortStopBits.ToString();

      return (StopBits)Enum.Parse(typeof(StopBits), stopBits);
    }  //  end SetPortStopBits

    /*--------------------------------------------------------------------------
     SetPortHandshake
    --------------------------------------------------------------------------*/
    public Handshake SetPortHandshake(Handshake defaultPortHandshake)
    {
      string handshake;

      Console.WriteLine("Available Handshake options:");
      foreach (string s in Enum.GetNames(typeof(Handshake)))
        Console.WriteLine("   {0}", s);

      Console.Write("Handshake({0}):", defaultPortHandshake.ToString());
      handshake = Console.ReadLine();

      if (handshake == "")  handshake = defaultPortHandshake.ToString();

      return (Handshake)Enum.Parse(typeof(Handshake), handshake);
    }  // end SetPortHandshake

  }  // end class Sportz
}  // end namespace WinderProject

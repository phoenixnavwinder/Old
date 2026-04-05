/*------------------------------------------------------------------------------
File: Util.cs                                          Updated: 12/03/2012

Objective: Winder program utility methods class
See Also: 
Problems: 
Modifications:  (search for ??? to find questionable code)
 Date       Who   Comments
------------------------------------------------------------------------------*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Zlib;  // Logz class

namespace WinderProject
{
  class Util
  {
    //================== INSTANCE VARIABLES and PROPERTIES =====================
    private static Logz lz;      // the logz file
    public  static Logz Lz { set { lz = value; } }

    //=========================== PUBLIC METHODS ===============================
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
     W2: writes the input string to both the console and the log file
    --------------------------------------------------------------------------*/
    public static void W2(string line)
    {
      Console.WriteLine(line);
      lz.Sl(line);
    }  // end W2

    /*--------------------------------------------------------------------------
     W2W: writes the input string to both the console and the log file as a 
       warning message in red and beeps
    --------------------------------------------------------------------------*/
    public static void W2W(string line)
    {
      Console.Beep();
      Console.ForegroundColor = ConsoleColor.Red;
      Console.WriteLine(line);
      Console.ForegroundColor = ConsoleColor.DarkBlue;
      lz.Sl(line);
    }  // end W2W

    /*--------------------------------------------------------------------------
     Angpvd: returns the principal value of an angle in degrees: 
              -180.0 <= return < 180.0
    --------------------------------------------------------------------------*/
    public static double Angpvd(double ang)
    {
      if (-180.0 <= ang && ang < 180.0) return ang;
      if (ang <= 540.0 || ang < -540.0)
          ang -= 360.0 * Math.Floor((ang + 180.0) / 360.0);
      else if (ang >= 0.0) ang -= 360.0;  // actually, ang >= 180.0
      else ang += 360.0;                  // actually, ang < -180.0
      return ang;
    }  // end Angpvd
  }  // end Util class
}  // end WinderProject namespace

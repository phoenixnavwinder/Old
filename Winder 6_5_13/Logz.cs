/*------------------------------------------------------------------------------
File: Logz.cs                                          Updated: 10/13/2012

Objective: Logz class code -- assists in writing a log file
See Also:
Problems: none
Modifications:
 Date       Who   Comments
------------------------------------------------------------------------------*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Zlib
{
  public class Logz
  {
    //================== INSTANCE VARIABLES and PROPERTIES =====================
    private string fileName;  // file path to the log file
    private FileStream outFile;
    private StreamWriter writer;
    private string errstr1;
    public string Errstr1
    {
      get
      {
        return errstr1;
      }
    }
    private string errstr2;
    public string Errstr2
    {
      get
      {
        return errstr2;
      }
    }

    //============================== METHODS ===================================
    /*--------------------------------------------------------------------------
     Open: opens the log file for writing 
   
     Input argument is the path to the file.
     Sets class items fileName, outFile, writer, and perhaps errstr1 and 
     errstr2.
     Returns true if all goes well, or false if an exception is encountered.
    --------------------------------------------------------------------------*/
    public bool Open(string fileNameX)
    {
      // Save the file name (path).
      fileName = fileNameX;
      try  // Open the file
      {
        outFile = new FileStream(fileName, FileMode.Create, FileAccess.Write);
      }
      catch (Exception e)
      {
        errstr1 = " -- unable to open Logz file: " + fileName;
        errstr2 = "Exception is: " + e.ToString();
        return false;
      }

      try  // Connect a writer to the stream.
      {
        writer = new StreamWriter(outFile);
      }
      catch (Exception e)
      {
        errstr1 = "Unable to write Logz file: " + fileName;
        errstr2 = "Exception is: " + e.ToString();
        return false;
      }

      // Write the file name and the date and time.
      Sl("Logz file: " + fileName);
      Bl();
      DateTime thisDay = DateTime.Now;
      Sl(thisDay.ToString());

      return true;  // success
    }  // end Open

    /*--------------------------------------------------------------------------
     Close: Writes a last line, then closes the file
    --------------------------------------------------------------------------*/
    public void Close()
    {
      Bl();
      Sl("*** end of Logz file: " + fileName);
      writer.Close();
      outFile.Close();
    }  // end Close

    /*--------------------------------------------------------------------------
     Flush: flushes the writer to the file
    --------------------------------------------------------------------------*/
    public void Flush()
    {
      writer.Flush();
    }  // end Flush

    /*--------------------------------------------------------------------------
     Bl: writes a blank line or n blank lines
    --------------------------------------------------------------------------*/
    public void Bl()
    {
      writer.WriteLine();
    }  // end of Bl

    public void Bl(int n)  // this version writes n blank lines
    {
      while (n-- > 0) writer.WriteLine();
    }  // end Bl

    /*--------------------------------------------------------------------------
     Pl: Writes a parameter line (for Parmz file echoing)
     
     np is the parameter number, and vp is the parameter value
    --------------------------------------------------------------------------*/
    public void Pl(int np, double vp)
    {
      Sl(" -- parm " + np.ToString("D2") + " = " + vp.ToString());
    }  // end P1

    /*--------------------------------------------------------------------------
     Sl: Writes a string line
    --------------------------------------------------------------------------*/
    public void Sl(string str)
    {
      writer.WriteLine(str);
    }  // end Sl

  }  // end Logz class
}  // end Zlib namespace

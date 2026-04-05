/*------------------------------------------------------------------------------
File: Parmz.cs                                         Updated: 10/13/2012

Objective: Parmz class code -- assists in reading parameter files
See Also:
Problems: none
Modifications:
 Date       Who   Comments
--------------------------------------------------------------------------------
A parameter file is a text file, often with the extension .parmz. Blank lines 
and lines that begin with // are ignored. The remaining lines are "parameter 
lines", one of which is shown below.
 
14: Max can angular acceleration :  2.3 dpss
 
Parameter lines begin with a two-digit parameter number, in positions 0 and 1,
followed by a colon character, in position 2. The parameter numbers must start 
with 01 and be in sequence. Following the fitst colon is a parameter name, which
is ignored, and then comes a second colon character. Following that is the 
parameter value, which is treated as a double even though it might be an 
integer. Anything after the value--often the units--is ignored.
------------------------------------------------------------------------------*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Zlib
{
  public class Parmz
  {
    //==================== INSTANCE VARIABLES and PROPERTIES ===================
    private Logz lz;  // the log file
    private string fileName;  // file path to the parameters file
    private FileStream inFile;
    private StreamReader reader;
    private int numpz;  // number of parameters to read
    private int numpzr;  // number of parameters read so far

    private double[] parray;  // parameters array
    public double[] Parray  { get { return parray; }
    }
    private int pnum;  // parameter number
    private double pval;  // parameter value
    private string pline;  // parameter line

    //============================== METHODS ===================================
    /*--------------------------------------------------------------------------
     Parmz: a constructor that opens, reads, and closes a Parmz file
     
     Ref pzok is true if all went well, false if not.
    --------------------------------------------------------------------------*/
    public Parmz(Logz lzX, string fileNameX, int numpzX, ref bool pzok)
    {
      pzok = false;
      lz = lzX;  // set the log file.
      bool openok = Open(fileNameX, numpzX);  // open the file
      if (!openok) return;
      bool readok = Read();  // read the file
      if (!readok) return;
      Close();
      pzok = true;
    }  // end Parmz

    /*--------------------------------------------------------------------------
    Open: Opens the Parmz text file for reading. 
   
    fileNameX is the path to the file. numpzX is the number of parameters to
    read. The Logz file should be set first.
    Sets class items fileName, numpz, numpzr, inFile, reader.
    Returns true if all goes well, or false if an exception is encountered.
    --------------------------------------------------------------------------*/
    public bool Open(string fileNameX, int numpzX)
    {
      // Save the file name and parameters number
      fileName = fileNameX;
      numpz = numpzX;
      parray = new double[numpz];  // space for the parameters
      numpzr = 0;  // no parameters read so far

      lz.Bl();
      lz.Sl("Attempting to open Parmz file: " + fileName);

      try  // Open the file  
      {
        inFile = new FileStream(fileName, FileMode.Open, FileAccess.Read);
      }
      catch (Exception e)
      {
        lz.Sl(" -- unable to open file");
        lz.Sl(" -- exception is: " + e.ToString());
        return false;
      }

      try  // File open, so connect a reader to the stream.
      {
        reader = new StreamReader(inFile);
      }
      catch (Exception e)
      {
        lz.Sl(" -- unable to read file");
        lz.Sl(" -- exception is: " + e.ToString());
        return false;
      }

      lz.Sl("Parmz file opened for " + numpz.ToString() + " parameters");
      return true;  // success
    }  // end Open

    /*--------------------------------------------------------------------------
     Close: Closes the Parmz file.
    --------------------------------------------------------------------------*/
    public void Close()
    {
      reader.Close();
      inFile.Close();
      lz.Sl("Parmz file closed: " + fileName);
    }  // end Close
 
    /*--------------------------------------------------------------------------
     ParseLine: reads and parses a line of the parameters file to find the 
       parameter number and its value.
    
     Returns 0 if all goes well; 1 for an empty or comment line, 2 for an
     uninterpretable line (an error).
       
     Sets class items: pline, pnum, errstr, pval,
    --------------------------------------------------------------------------*/
    public int ParseLine()
    {
      try  // Read a line of the file.
      {
        pline = reader.ReadLine();
        if (pline == null)
        {
          lz.Sl(" -- unexpected end of file");
          return 2; 
        }
      }
      catch (Exception e)
      {
        lz.Sl(" -- failed to read line; exception: " + e.ToString());
        return 2;
      }

      pnum = 0;  // temporary
      pval = -1.0;

      string line = pline;  // copy the parameters line

      // Ignore blank lines and comment lines that begin with //.
      line = line.TrimStart(' ');  // remove any leading blanks
      if (line.Length == 0 || line.StartsWith(@"//"))
        return 1;

      // Determine that the first two characters are the parameter number
      // and the third character is a colon. Extract the parameter number.
      if (!Char.IsDigit(line, 0) || !Char.IsDigit(line, 1) || line[2] != ':')
      {
        lz.Sl(" -- no parameter number followed by colon");
        return 2;
      }
      string lstr = line.Substring(0, 2);
      pnum = Int32.Parse(lstr);

      // Assure that there is a second colon on the line. If so, remove all
      // up to and including that colon.
      int cix = line.IndexOf(':', 3);
      if (cix == -1)
      {
        lz.Sl(" -- no second colon on line");
        return 2;
      }
      cix++;
      line = line.Substring(cix, line.Length - cix);
      line = line.TrimStart(' ');

      // Extract the parameter value string part.
      if (line.Length == 0)
      {
        lz.Sl(" -- no parameter value on line");
        return 2;
      }

      cix = line.IndexOf(' ', 1);  // find a space
      // If there is a space, get the substring up to the space.
      if (cix != -1)
        line = line.Substring(0, cix);
       
      // Extract the parms value from what remains.
      try
      {
        pval = Double.Parse(line);  // get the value
      }

      catch (Exception e)
      {
        lz.Sl(" -- value extraction failed: " + e.ToString());
        return 2;
      }

      return 0;  // all ok
    }  // end ParseLine

    /*--------------------------------------------------------------------------
     Read: Reads the (previously opened) Parmz file.
    
     Returns true if successful, false otherwise
    --------------------------------------------------------------------------*/
    public bool Read()
    {
      int plrv;  // ParseLine return value

      while (true)
      {
        plrv = ParseLine();  // read and parse one line of the file
        switch (plrv)
        {
          case 0: // apparently successful read of a parameters line
            lz.Pl(pnum, pval);
            if (pnum != ++numpzr)
            {
              lz.Sl(" -- number error on parameter: " + numpzr.ToString());
              return false;
            }
            parray[numpzr-1] = pval;  // remember the -1!
            if (numpzr == numpz) return true;  // reading is done
            break;
          case 1:  // the line was blank or was a comment line, so read on
            break;
          case 2:  // there was a read or parse error
            return false;
        }  // end switch
      }  // end while

    }  // end Read

  }  // end Parmz class
}  // end Zlib namespace

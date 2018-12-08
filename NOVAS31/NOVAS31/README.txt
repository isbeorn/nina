Approved for Public Release

Naval Observatory Vector Astronomy Software (NOVAS)
C Edition

Version C3.1 - March 2011

I. Introduction

NOVAS is an integrated package of ANSI C functions for computing many commonly needed quantities in positional astronomy.  The package can supply, in one or two function calls, the instantaneous coordinates of any star or solar system body in a variety of coordinate systems.  At a lower level, NOVAS also supplies astrometric utility transformations, such as those for precession, nutation, aberration, parallax, and the gravitational deflection of light.  The computations are accurate to better than one milliarcsecond.  See section 4.2 of the User's Guide (file NOVAS_C3.1_Guide.pdf in the distribution package) for a complete list of the NOVAS C3.1 functions.

The NOVAS package is relatively easy-to-use and can be incorporated into data reduction programs, telescope control systems, and simulations.  A basic knowledge of positional astronomy is required for effective use of NOVAS; see Chapter 1 of the User's Guide.  The Astronomical Almanac Online glossary (http://asa.usno.navy.mil/SecM/Glossary.html) provides definitions of relevant terms.

NOVAS 3.0, released in December 2009, was the first major update of the NOVAS algorithms since 1998.  NOVAS C3.0 improved the accuracy of star and solar system body position calculations (e.g., apparent places) by including several small effects not previously implemented in the code.  New convenience subroutines and functions were also added.  Most importantly, NOVAS C3.0 fully implemented recent International Astronomical Union resolutions in positional astronomy, including new reference system definitions and updated models for precession and nutation (see Chapters 1 and 5 of the User's Guide and references therein for details).

II. Key Changes Between Versions C3.0 and C3.1

NOVAS C3.1 fixes several minor issues in C3.0 and includes a few new features.  A complete list of changes from version C3.0 to C3.1 is given in Appendix B of the User's Guide.  The main changes are as follows:

  - New function cel2ter transforms a vector from the celestial system (GCRS) to the terrestrial system (ITRS).  It complements function ter2cel.

  - Function wobble has a new direction input parameter. 

  - The ephemeris manager (eph_manager.c and eph_manager.h) has added support for JPL's DE421 lunar and planetary ephemerides.  Function ephem_open also has an additional output argument, de_number, which provides the DE number (e.g., 405, 421) of the opened ephemeris file.  The function names Ephem_Open, Ephem_Close, Planet_Ephemeris, State, Interpolate, and Split were all changed to lower case (i.e., ephem_open, ephem_close, planet_ephemeris, state, interpolate, and split) to conform to internal coding standards.

  - To prevent possible string overflows, two new preprocessor constants, SIZE_OF_OBJ_NAME and SIZE_OF_CAT_NAME have been defined in novas.h. SIZE_OF_OBJ_NAME defines the length of the starname character array in the cat_entry structure and the length of the name character array in the object structure.  SIZE_OF_CAT_NAME defines the length of the catalog character array in the cat_entry structure.  Although the actual lengths of the arrays are unchanged, the make_cat_entry and make_object function prototypes and definitions now use these new preprocessor constants in their argument lists.  Other NOVAS functions also make use of the new preprocessor constants.
  
  - Updated function precession fixes a bug that occurs when input argument jd_tdb2 is T0 on the first call to the function.
  
  - The nutation models in nutation.c -- iau2000a, iau2000b, and nu2000k -- now use static storage class for the large 'const' arrays.

III. Software Installation, Validation, and Quick-Start

Installation is simple: Copy all NOVAS files to a directory on your local system.

Validation of the NOVAS code is covered in Chapter 2 of the User's Guide.  **You are strongly encouraged to run the "checkout" program(s) appropriate for your particular application.**  The basic validation, which all users should run, is main function checkout-stars.c.

To compute positions of solar system bodies and star positions at NOVAS' full level of accuracy (i.e., better than a few milliarcseconds), you will have to install one of the JPL solar system ephemerides, such as DE405 or DE421.  This process involves running a few Fortran programs to create a binary ephemeris file, but once that is done, ephemeris access can be all C-based using the code in NOVAS file solsys1.c.  Instructions for installing the JPL ephemerides are provided in Appendix C of the User's Guide.  [Tip: To compute star positions (e.g., apparent places) to an accuracy no better than a few milliarcseconds, you can avoid having to install the JPL ephemerides.  See the instructions in sections 2.2 and 2.3 of the User's Guide; additional information about "solarsystem version 3" is in Chapter 4.]

The quick-start for learning to use NOVAS is main function example.c.  This program demonstrates how to use NOVAS for several common applications.  See Chapter 3 of the User's Guide for details.

IV.  Using NOVAS in Your Applications

NOVAS has no licensing requirements.  If you use NOVAS in an application, an acknowledgement of the Astronomical Applications Department of the U.S. Naval Observatory would be appropriate.  Brief descriptions of applications that use NOVAS are welcome; use technical-support contact information below.  Your input helps us justify continued development of NOVAS. 

The User's Guide is the official reference for NOVAS C3.1 and may be cited as 
Bangert, J., Puatua, W., Kaplan, G., Bartlett, J., Harris, W., Fredericks, A., & Monet, A. 2011, User's Guide to NOVAS Version C3.1 (Washington, DC: USNO).

V.  Technical Support

To access the NOVAS Web pages, go to  http://www.usno.navy.mil/USNO/astronomical-applications and follow the NOVAS link on the page.

Have a question about NOVAS?  Contact the developers by e-mail at help@aa.usno.navy.mil.  We do not provide technical support by telephone.  If you have questions about NOVAS output, be sure to run the validation program(s) before contacting us.  Comments and suggestions are also welcome.


Astronomical Applications Department
U.S. Naval Observatory
3450 Massachusetts Ave., NW
Washington, DC  20392-5420

/*
  Naval Observatory Vector Astrometry Software (NOVAS)
  C Edition, Version 3.1
 
  eph_manager.h: Header file for eph_manager.c 
 
  U. S. Naval Observatory
  Astronomical Applications Dept.
  Washington, DC 
  http://www.usno.navy.mil/USNO/astronomical-applications
*/
#define _CRT_SECURE_NO_DEPRECATE

#ifndef _EPHMAN_
#define _EPHMAN_

/* Portable EXPORT macro: use __declspec on Windows, visibility attribute on GCC/Clang, or empty fallback */
#ifndef EXPORT
# if defined(_WIN32) || defined(__CYGWIN__)
#  define EXPORT __declspec(dllexport)
# elif defined(__GNUC__)
#  define EXPORT __attribute__((visibility("default")))
# else
#  define EXPORT
# endif
#endif
/*
   Standard libraries
*/

#ifndef __MATH__
   #include <math.h>
#endif

#ifndef __STDLIB__
   #include <stdlib.h>
#endif

#ifndef __STDIO__
   #include <stdio.h>
#endif

/*
   External variables
*/

extern short int KM;

extern int IPT[3][12], LPT[3];

extern long int  NRL, NP, NV;
extern long int RECORD_LENGTH;

extern double SS[3], JPLAU, PC[18], VC[18], TWOT, EM_RATIO;
extern double *BUFFER;

extern FILE *EPHFILE;

/*
   Function prototypes
*/

EXPORT short int ephem_open (char *ephem_name,

                      double *jd_begin, double *jd_end, 
                      short int *de_number);

EXPORT short int ephem_close (void);

EXPORT short int planet_ephemeris (double tjd[2], short int target,
                            short int center, 

                            double *position, double *velocity);

EXPORT short int state (double *jed, short int target,

                 double *target_pos, double *target_vel);

EXPORT void interpolate (double *buf, double *t, long int ncm, long int na,

                  double *position, double *velocity);

EXPORT void split (double tt, double *fr);

#endif

/*
  Naval Observatory Vector Astrometry Software (NOVAS)
  C Edition, Version 3.1

  solarsystem.h: Header file for solsys1.c, solsys2.c, & solsys3.c

  U. S. Naval Observatory
  Astronomical Applications Dept.
  Washington, DC
  http://www.usno.navy.mil/USNO/astronomical-applications
*/
#define _CRT_SECURE_NO_DEPRECATE

#ifndef _SOLSYS_
   #define _SOLSYS_


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
   Function prototypes
*/

EXPORT short int solarsystem (double tjd, short int body, short int origin,

                          double *position, double *velocity);

EXPORT short int solarsystem_hp (double tjd[2], short body, short origin,

                             double *position, double *velocity);


#endif

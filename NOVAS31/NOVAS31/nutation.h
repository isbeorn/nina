/*
   Naval Observatory Vector Astrometry Software (NOVAS)
   C Edition, Version 3.1
   
   nutation.h: Header file for nutation models

   U. S. Naval Observatory
   Astronomical Applications Dept.
   Washington, DC 
   http://www.usno.navy.mil/USNO/astronomical-applications
*/
#define _CRT_SECURE_NO_DEPRECATE

#ifndef _NUTATION_
   #define _NUTATION_


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

EXPORT void iau2000a (double jd_high, double jd_low,

                  double *dpsi, double *deps);

EXPORT void iau2000b (double jd_high, double jd_low,

                  double *dpsi, double *deps);

EXPORT void nu2000k (double jd_high, double jd_low,

                 double *dpsi, double *deps);

#endif

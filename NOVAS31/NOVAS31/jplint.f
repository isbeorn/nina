C-----------------------------------------------------------------------
C  Naval Observatory Vector Astrometry Software (NOVAS)
C  C Edition, Version 3.1
C 
C  jplint.f: Interface to JPL ephemeris-access software use w/ solsys2.c 
C 
C  U. S. Naval Observatory
C  Astronomical Applications Dept.
C  Washington, DC 
C  http://www.usno.navy.mil/USNO/astronomical-applications
C-----------------------------------------------------------------------


C-----------------------------------------------------------------------
      SUBROUTINE jplint (tjdtdb,target,center, posvel,error)
C
C---PURPOSE:    To serve as the interface between the JPL Fortran
C               code that accesses the solar system ephemerides; returns
C               the position and velocity of the body at the input time.
C
C---REFERENCES: JPL. 2007, "JPL Planetary and Lunar Ephemerides: Export
C                   Information," (Pasadena, CA: JPL) 
C                   http://ssd.jpl.nasa.gov/?planet_eph_export.
C
C---INPUT
C   ARGUMENTS:  tjdtdb = TDB Julian date at which the body's position
C                        and velocity is desired.
C                        (Double precision)
C               target = Body identification number for the "target" 
C                        point;  Mercury = 1,...,Pluto = 9,
C                        Moon = 10, Sun = 11, Solar system barycenter =
C                        12, Earth-Moon barycenter = 13.
C                        (Integer)
C               center = Body identification number for the "origin" 
C                        point;  Mercury = 1,...,Pluto = 9,
C                        Moon = 10, Sun = 11, Solar system barycenter =
C                        12, Earth-Moon barycenter = 13.
C                        (Integer)
C
C---OUTPUT
C   ARGUMENTS:  posvel = Array containing the position and velocity
C                        components (rectangular coordinates on the
C                        equator and equinox of J2000.0) of 'target'
C                        relative to 'origin'.  The units are AU and 
C                        AU/day, respectively.
C
C---COMMON
C   BLOCKS:     None.
C
C---ROUTINES
C   CALLED:     SUBROUTINE const (JPL supplied)
C               SUBROUTINE pleph (JPL supplied)
C
C---VER./DATE/
C   PROGRAMMER: V1.0/10-97/JAB (USNO/AA)
C               V1.1/10-10/JLB (USNO/AA) update references
C
C---NOTES:      1. The arguments of JPL subroutine 'pleph' have changed
C                  several times over the course of years.  This 
C                  subroutine ('jplint') supports the version supplied by
C                  JPL in their software package provided on the 1997
C                  CD-ROM (see reference).
C               2. Any future changes to the JPL arguments should be 
C                  accomodated in this subroutine ('jplint'), keeping
C                  its arguments constant.  This should minimize impact
C                  on the calling program.                                        
C
C-----------------------------------------------------------------------

C-----'cdim' is simply an upper limit on the number of constants stored
C     in the ephemeris header.  It can be adjusted when the actual value
C     ('nvals') is known.

      INTEGER cdim
      PARAMETER (cdim = 400)

      CHARACTER name(cdim)*6

      INTEGER target, center, nvals, error, i

      DOUBLE PRECISION tjdtdb, limits(3), value(cdim), posvel(6)


C-----Call JPL subroutine 'const' to get the time limits of the
C     ephemeris.

      CALL const ( name,value,limits,nvals)

C-----Check that the input time is within the ephemeris limits.
C     Return the position and velocity of the body if it is.

      IF ((tjdtdb .LT. limits(1)) .OR. (tjdtdb .GT. limits(2))) THEN
         error = 1
         DO 10 i = 1,6
            posvel(i) = 0.0d0
   10    CONTINUE
       ELSE
         error = 0
         CALL pleph (tjdtdb,target,center, posvel)
      END IF

      RETURN
      END
      
C-----------------------------------------------------------------------
      SUBROUTINE jplihp (tjdtdb,target,center, posvel,error)
C
C---PURPOSE:    To serve as the interface between the JPL Fortran
C               code that accesses the solar system ephemerides; returns
C               the position and velocity of the body at the input time.
C
C---REFERENCES: JPL. 2007, "JPL Planetary and Lunar Ephemerides: Export
C                   Information," (Pasadena, CA: JPL) 
C                   http://ssd.jpl.nasa.gov/?planet_eph_export.
C
C---INPUT
C   ARGUMENTS:  tjdtdb = Array containing the TDB Julian date at which 
C                        the body's position and velocity is desired.
C                        The Julian date may be split any way (although 
C                        the first element is usually the "integer" 
C                        part, and the second element is the 
C                        "fractional" part).  (Double precision)
C               target = Body identification number for the "target" 
C                        point;  Mercury = 1,...,Pluto = 9,
C                        Moon = 10, Sun = 11, Solar system barycenter =
C                        12, Earth-Moon barycenter = 13.
C                        (Integer)
C               center = Body identification number for the "origin" 
C                        point;  Mercury = 1,...,Pluto = 9,
C                        Moon = 10, Sun = 11, Solar system barycenter =
C                        12, Earth-Moon barycenter = 13.
C                        (Integer)
C
C---OUTPUT
C   ARGUMENTS:  posvel = Array containing the position and velocity
C                        components (rectangular coordinates on the
C                        equator and equinox of J2000.0) of 'target'
C                        relative to 'origin'.  The units are AU and 
C                        AU/day, respectively.
C
C---COMMON
C   BLOCKS:     None.
C
C---ROUTINES
C   CALLED:     SUBROUTINE const (JPL supplied)
C               SUBROUTINE dpleph (JPL supplied)
C
C---VER./DATE/
C   PROGRAMMER: V1.0/12-06/JAB (USNO/AA)
C               V1.1/10-10/JLB (USNO/AA) update references
C
C---NOTES:      1. The arguments of JPL subroutine 'pleph' have changed
C                  several times over the course of years.  This 
C                  subroutine ('jplihp') supports the version supplied by
C                  JPL in their software package provided on the 1997
C                  CD-ROM (see reference).
C               2. Any future changes to the JPL arguments should be 
C                  accomodated in this subroutine ('jplihp'), keeping
C                  its arguments constant.  This should minimize impact
C                  on the calling program.                                        
C
C-----------------------------------------------------------------------

C-----'cdim' is simply an upper limit on the number of constants stored
C     in the ephemeris header.  It can be adjusted when the actual value
C     ('nvals') is known.

      INTEGER cdim
      PARAMETER (cdim = 400)

      CHARACTER name(cdim)*6

      INTEGER target, center, nvals, error, i

      DOUBLE PRECISION tjdtdb(2), tjd, limits(3), value(cdim), 
     . posvel(6)


C-----Call JPL subroutine 'const' to get the time limits of the
C     ephemeris.

      CALL const ( name,value,limits,nvals)

C-----Check that the input time is within the ephemeris limits.
C     Return the position and velocity of the body if it is.

      tjd = tjdtdb(1) + tjdtdb(2)
      IF ((tjd .LT. limits(1)) .OR. (tjd .GT. limits(2))) THEN
         error = 1
         DO 10 i = 1,6
            posvel(i) = 0.0d0
   10    CONTINUE
       ELSE
         error = 0
         CALL dpleph (tjdtdb,target,center, posvel)
      END IF

      RETURN
      END


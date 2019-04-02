/*
  Naval Observatory Vector Astrometry Software (NOVAS)
  C Edition, Version 3.1

  cio_file.c: Produces binary file of RA values for CIO

  U. S. Naval Observatory
  Astronomical Applications Dept.
  Washington, DC
  http://www.usno.navy.mil/USNO/astronomical-applications
*/
#define _CRT_SECURE_NO_DEPRECATE

#include <stdio.h>
#include <stdlib.h>

int main (void)
{

/*
   Program to produce a binary direct access file of right ascension
   values of the celestial intermediate origin (CIO), given a formatted
   text file of the same data.  Each input and output data record
   contains a TDB Julian date and a right ascension value in
   arcseconds.

   The input formatted text file is 'CIO_RA.TXT'. It is included in the
   NOVAS-C package.
*/

   char identifier[25];

   long int  header_size, record_size, i, n_recs;

   double jd_tdb, ra_cio, jd_first, jd_last, interval, jd_beg, jd_end,
      t_int, jd_1, ra_1, jd_n, ra_n;

   size_t double_size, long_size;

   FILE *in_file, *out_file;


   double_size = sizeof (double);
   long_size = sizeof (long int);
   header_size = (long) ((size_t) 3 * double_size + long_size);
   record_size = (long) ((size_t) 2 * double_size);

/*
   Open the input formatted text file.
*/

   if ((in_file = fopen ("CIO_RA.TXT", "r")) == NULL)
   {
      printf ("Error opening input file.\n");
      return (1);
   }

/*
   Open the output binary, random-access file.
*/

   if ((out_file = fopen ("cio_ra.bin", "wb+")) == NULL)
   {
      printf ("Error opening output file.\n");
      if (in_file)
         fclose (in_file);
      return (1);
   }

/*
   Read input file identifier.
*/

   fgets (identifier, sizeof(identifier), in_file);

/*
   Read the input file and write the output file.
*/

   i = 0L;
   while (!feof (in_file))
   {

/*
   Read a record from the input file.
*/

      fscanf (in_file, " %lf %lf ", &jd_tdb, &ra_cio);
      i++;

/*
   If this is the first record, capture the Julian date and position
   the output file pointer so as reserve space for the output file
   header.
*/

      if (i == 1L)
      {
         jd_first = jd_tdb;
         fseek (out_file, header_size, SEEK_SET);
      }

/*
   If this is the second record, compute the time interval between
   input data points, assuming a constant interval.
*/

      if (i == 2L)
         interval = jd_tdb - jd_first;

/*
   Capture the value of the Julian date of the last data point.
*/

      jd_last = jd_tdb;

/*
   Write a regular data record to the output file.
*/

      fwrite (&jd_tdb, double_size, (size_t) 1, out_file);
      fwrite (&ra_cio, double_size, (size_t) 1, out_file);
      if (ferror (out_file))
      {
         printf ("Error on output file while writing record %ld.\n", i);
         if (in_file)
            fclose (in_file);
         if (out_file)
            fclose (out_file);
         return (1);
      }
   }

/*
   Now write the file header.
*/

   fseek (out_file, 0L, SEEK_SET);
   fwrite (&jd_first, double_size, (size_t) 1, out_file);
   fwrite (&jd_last, double_size, (size_t) 1, out_file);
   fwrite (&interval, double_size, (size_t) 1, out_file);
   fwrite (&i, long_size, (size_t) 1, out_file);
   if (ferror (out_file))
   {
      printf ("Error on output file while writing file header.\n");
      if (in_file)
         fclose (in_file);
      if (out_file)
         fclose (out_file);
      return (1);
   }

/*
   Do a trial read of the file header, the first data record, and the
   last data record, then write summary to standard output.
*/

   rewind (out_file);
   fread (&jd_beg, double_size, (size_t) 1, out_file);
   fread (&jd_end, double_size, (size_t) 1, out_file);
   fread (&t_int, double_size, (size_t) 1, out_file);
   fread (&n_recs, long_size, (size_t) 1, out_file);

   fread (&jd_1, double_size, (size_t) 1, out_file);
   fread (&ra_1, double_size, (size_t) 1, out_file);
   fseek (out_file, -(record_size), SEEK_END);
   fread (&jd_n, double_size, (size_t) 1, out_file);
   fread (&ra_n, double_size, (size_t) 1, out_file);

   printf ("Results from program cio_file:\n\n");
   printf ("Input file identifier: %s\n", identifier);
   printf ("%ld records read from the input file:\n", n_recs);
   printf ("   First Julian date: %f\n", jd_beg);
   printf ("   Last Julian date:  %f\n", jd_end);
   printf ("   Data interval: %f days\n\n", t_int);
   printf ("First data point: %f  %f\n", jd_1, ra_1);
   printf ("Last data point:  %f  %f\n\n", jd_n, ra_n);
   printf ("Binary file cio_ra.bin created.\n");

/*
   Close files.
*/

   if (in_file)
      fclose (in_file);
   if (out_file)
      fclose (out_file);

   return (0);
}

#ifndef SCANNER_H
#define SCANNER_H

/* ---------------------- constants --------------------- */

#define MAX_BEZLEN 128
#define SCAN_BUFSIZE 514

/* ---------------------- functions --------------------- */

PUBLIC BOOL scan_init( FILE  *src_fp );

PUBLIC BOOL scan_get( Symbol     *sk,
                      Merkmal     *m,
                      UINT32   *line );

#endif

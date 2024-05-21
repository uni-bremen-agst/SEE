#ifndef MACROS_H
#define MACROS_H

#include <sys/types.h>

#define LINUX
/* #define SUN */

/* ------ Constants ------ */

#define TRUE  1
#define FALSE 0

/* -------- Macros ------------- */

#define XALLOCTYPE( TYPE ) (TYPE *)xmalloc( sizeof( TYPE ) )

#define MIN(x,y) ((x)<(y) ? (x) : (y))
#define MAX(x,y) ((x)>(y) ? (x) : (y))

/* ------ Datatypes ------ */

typedef    long int   	    INT32;
typedef    unsigned long    UINT32;
typedef    short int  	    INT16;
typedef    unsigned short   UINT16;
typedef    char             INT8;
typedef    unsigned char    UINT8;

typedef    char             BOOL;

/* ------- Variables ------ */

extern BOOL debug_flag;
extern BOOL verbose_flag;
extern BOOL optimize_flag;
extern BOOL gencode_flag;
extern BOOL range_check_flag;

#endif

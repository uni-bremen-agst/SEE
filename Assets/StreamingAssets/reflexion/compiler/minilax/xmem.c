#include "macros.h"

#include <stdlib.h>
#include <stdarg.h>

#include "import.h"
#include "error.h"

#include "export.h"
#include "xmem.h"

/* ------- local variable ------- */

static UINT32 allocedmem = 0L;


/* ---- memory management ---- */

PUBLIC void *xmalloc(size_t size)
{
    char func[] = "xmalloc";
    void *memptr;

    if ( ( memptr = (void *)calloc( 1, size ) ) == NULL )
    {
        log_error( ERR_ABORT, MEMORY_ERROR, E_ALLOCATE, func, 0 );
        return NULL;
    }
    
    allocedmem += size;

    return memptr;
}


PUBLIC void *xcalloc(size_t nitems, size_t size)
{
    char func[] = "xcalloc";

    void *memptr;

    if ( ( memptr = (void *)calloc( nitems, size ) ) == NULL )
    {
        log_error( ERR_ABORT, MEMORY_ERROR, E_ALLOCATE, func, 0 );
        return NULL;
    }

    allocedmem += size * nitems;

    return memptr;
}


PUBLIC void xfree( void *ptr )
{
    free( ptr );
    return;
}


PUBLIC void xmemusage( FILE *fp )
{
    fprintf( fp, "%ld bytes allocated\n", allocedmem );
    return;
}

#ifndef XMEM_H
#define XMEM_H

#include <stdio.h>

/* ------- Functions ------ */

void *xmalloc( size_t size );

void *xcalloc( size_t nitems,
	       size_t size );

void xfree( void *ptr );

void xmemusage( FILE *fp );

#endif

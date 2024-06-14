#include "macros.h"

#include <string.h>

#include "import.h"
#include "xmem.h"

#include "export.h"
#include "argument.h"


PRIVATE char *index( const char *s, int c );


PUBLIC ARGUMENTS *get_arg ( int             argc,
                            char         *argv[],
                            const char  *mono_sw )
{
    ARGUMENTS  *arglist = NULL;
    ARGUMENTS   *argptr;

    int               i;  /* Laufvariable */


    if ( argc == 0 )  return NULL;
    
    for ( i = 1; i < argc; i ++ )
    {
        if ( argv[i][0] == '-' )
        {
            if ( ( argptr = xmalloc( sizeof( ARGUMENTS ) ) ) == NULL )
                return NULL;
            
            argptr->sw    = argv[i][1];
            
            /* checken, ob sw in mono_sw ist */
      
            if ( index( mono_sw, (int)(argptr->sw) ) )
            {
                /* kein Argument */
                argptr->entry = NULL;
            }
            else
            {
                /* mit Argument */
                if ( argv[i][2] == '=' )
                {
                    argptr->entry = xmalloc( strlen( argv[i] ) - 3 + 1 );
                    strcpy( argptr->entry, argv[i] + 3 );
                }
                else if ( argv[i][2] != 0 )
                {
                    argptr->entry = xmalloc( strlen( argv[i] ) - 2 + 1 );
                    strcpy( argptr->entry, argv[i] + 2 );
                }
                else
                {
                    if ( i+1 == argc )
                        argptr->entry = NULL;
                    else
                    {
                        if ( argv[i+1][0] == '-' )
                            argptr->entry = NULL;
                        else
                        {
                            argptr->entry = xmalloc( strlen( argv[i+1] ) + 1 );
                            strcpy( argptr->entry, argv[i+1] );
                            i ++;
                        }
                    }
                }
            }
        }
        else
        {
            if ( ( argptr = xmalloc( sizeof( ARGUMENTS ) ) ) == NULL )
                return NULL;
            
            argptr->sw = 0;
            
            argptr->entry = xmalloc( strlen( argv[i] ) + 1 );
            strcpy( argptr->entry, argv[i] );
        }

        argptr->next = arglist;
        arglist      = argptr;
    }

    return arglist;
}


PRIVATE char *index( const char *s, int c )
{
    char *ptr;
    
    if ( s == NULL )  return NULL;
    
    for ( ptr = (char *)s; ( *ptr ) && ( *ptr != (char)c ); ptr ++ );

    if ( *ptr )  return ptr;

    return NULL;
}

#include "macros.h"

#include <string.h>

#include "import.h"
#include "xmem.h"
#include "error.h"

#include "export.h"
#include "constab.h"


/* --------------------- global variables ------------------- */

static ConstTable  *constab = NULL;
static INT32   stringOffset = 0;
static UINT32 stringList[1000][2];  /* max. 1000 strings */
static UINT32 stringCount = 0;

/* ------------------------- functions ---------------------- */

/*------------------------------------------------------------------------*\
|                                                                          |
| Funktion     :  ctab_insert   Einfuegen von Daten in die Konstantentab.  |
|                                                                          |
| Eingabe      :  data          Zeiger auf den Anfang der Daten            |
|                 len           Laenge des Datenfeldes                     |
|                 is_string     TRUE falls die Daten ein String sind       |
|                                                                          |
| Ausgabe      :  id            Identifikationsnummer der Daten            |
|                                                                          |
| Ergebnis     :  TRUE = kein Fehler, FALSE = Fehler aufgetreten           |
|                                                                          |
\*------------------------------------------------------------------------*/

PUBLIC BOOL ctab_insert( UINT8 *data, UINT16 len, UINT32 *id, BOOL is_string )
{
    ConstTable *bt;

    char func[] = "ctab_insert";


    if ( (constab == NULL) || ( (constab != NULL) &&
	 (constab->freeix + len + 4 > CONSTABSIZE) ) )
    {
	if (( bt = XALLOCTYPE( ConstTable )) == NULL)
	{
	    log_error( ERR_ABORT, MEMORY_ERROR, E_ALLOCATE , func, 0 );
	    return FALSE;
	}

	if (( bt->data = xcalloc( CONSTABSIZE, sizeof(char) )) == NULL )
	{
	    log_error( ERR_ABORT, MEMORY_ERROR, E_ALLOCATE, func, 0 );
	    return FALSE;
	}

	bt->nr = (constab == NULL) ? (0) : (constab->nr + 1);
	bt->freeix = 0;
	bt->next   = constab;
	constab    = bt;
    }

    *( (UINT16 *)( constab->data + constab->freeix ) ) = len;

    memcpy( constab->data + constab->freeix + 4, data, len );

    *id = ( (UINT32)(constab->nr) << 16 ) | ( (UINT32)(constab->freeix) );

    constab->freeix += len + 4;
    if ( len & 3 )  constab->freeix += 4 - ( len & 3 );

    if ( is_string )
    {
        stringList[stringCount][0] = *id;
        stringList[stringCount][1] = stringOffset;

        stringOffset += len;
        if ( len & 3 )
            stringOffset += 4 - ( len & 3 );
        stringCount ++;
    }

    return TRUE;
}


/*------------------------------------------------------------------------*\
|                                                                          |
| Funktion     :  ctab_lookup   Holen von Daten aus der Konstantentabelle  |
|                                                                          |
| Eingabe      :  id            Identifikationsnummer der Daten            |
|                                                                          |
| Ausgabe      :  data          Zeiger auf die gewuenschten Daten          |
|                 len           Laenge des Datenfeldes (nur bei len!=NULL) |
|                                                                          |
| Ergebnis     :  TRUE = kein Fehler, FALSE = Fehler aufgetreten           |
|                                                                          |
\*------------------------------------------------------------------------*/

PUBLIC BOOL ctab_lookup( UINT32 id, UINT8 **data, UINT16 *len )
{
    ConstTable *bt;
    UINT16 tabnr;
    UINT16 tabidx;
    
    tabnr = (UINT16)(id >> 16);
    tabidx = (UINT16)(id & 0xffff);
    
    for ( bt = constab;
          ( bt != NULL ) && ( bt->nr != tabnr );
          bt = bt->next )
        ;
        
    if ( ( bt == NULL ) || ( bt->nr != tabnr ) )   /* nicht gefunden? */
        return FALSE;
    
    if ( len != NULL )
    {
        *len = *( (UINT16 *)( bt->data + tabidx ) );
    }
    
    *data = bt->data + tabidx + 4;

    return TRUE;
}


PUBLIC UINT32 ctab_get_stroff( UINT32 id )
{
    UINT16 i;

    for ( i = 0; i < stringCount; i ++ )
        if ( stringList[i][0] == id )
            return stringList[i][1];

    log_error( ERR_FATAL, SYSTEM_ERROR, E_NOT_IN_CONSTAB, NULL, 0 );

    return 0;
}


PUBLIC BOOL ctab_dump_strings( FILE *fp )
{
    UINT8  *data;
    UINT32   out;
    UINT16   len;
    UINT16  i, j;

    for ( i = 0; i < stringCount; i ++ )
    {
        ctab_lookup( stringList[i][0], &data, &len );
        for ( j = 0; j < ( len + 3 ) / 4; j ++ )
	{
#ifdef SUN
            out  = ((UINT32)*((UINT16 *)data + 2*j)) << 16;
            out |= ((UINT32)*((UINT16 *)data + 2*j + 1));
#else
	    out = *((UINT32 *)data + j );
#endif
            fprintf( fp, "%lu\n", out );
        }
    }
    
    return TRUE;
}

#ifndef CONSTAB_H
#define CONSTAB_H

/* ------------------------ macros ------------------------------ */

#define CONSTABSIZE 8192

/* -------------------------- types ---------------------------- */

typedef struct ConstTable     /* ein Element einer verketteten Liste von  */
                              /* Konstantentabellen fuer effiziente       */
                              /* Speicherung von Konstanten */
{
    UINT16                nr;   /* Nummer der Konstantentabelle */

    UINT8              *data;   /* die eigentlichen Daten */
    UINT16            freeix;   /* Anfang des freien Bereichs */

    struct ConstTable  *next;   /* Zeiger auf die naechste Konst.tabelle */

} ConstTable;


/* ------------------------ functions -------------------------- */

PUBLIC BOOL ctab_insert( UINT8     *data,
			 UINT16      len,
			 UINT32      *id,
			 BOOL  is_string );

PUBLIC BOOL ctab_lookup( UINT32 id,
			 UINT8 **data,
			 UINT16 *len );

PUBLIC UINT32 ctab_get_stroff( UINT32 id );

PUBLIC BOOL ctab_dump_strings( FILE *fp );

#endif

#include "macros.h"

#include <ctype.h>
#include <string.h>

/* -------------------------- import -------------------------- */

#include "import.h"
#include "xmem.h"
#include "symtable.h"
#include "constab.h"
#include "error.h"

/* -------------------------- export -------------------------- */

#include "export.h"
#include "scanner.h"

/* ------------------------ local variables ------------------ */

PRIVATE FILE         *fp;  /* Eingabedatei mit Quelltext */
PRIVATE UINT32 bez_count;  /* Anzahl der Bezeichner */
PRIVATE UINT32    lineno;  /* aktuelle Eingabezeile */

PRIVATE char *buffer;      /* Puffer fuer Eingabedatei */
PRIVATE char *buffer_ptr;  /* Zeiger auf das aktuelle Zeichen im Puffer */
PRIVATE UINT16 buffer_fill; /* Anzahl der zuletzt eingelesenen Zeichen */
PRIVATE BOOL scan_eof      = FALSE;
PRIVATE BOOL scan_eof_next = FALSE;

/* ---------------------- local functions ------------------- */

PRIVATE BOOL scan_readbuffer( void );
PRIVATE char scan_getchar( void );
PRIVATE char scan_nextchar( void );
PRIVATE void scan_ungetchar( void );


/*------------------------------------------------------------------------*\
|                                                                          |
| Funktion     :  scan_init     initialisiert den Scanner                  |
|                                                                          |
| Eingabe      :  src_fp        Datei, die den Quelltext enth„lt           |
|                                                                          |
| Ausgabe      :  ----                                                     |
|                                                                          |
| Ergebnis     :  TRUE = kein Fehler, FALSE = Fehler aufgetreten           |
|                                                                          |
\*------------------------------------------------------------------------*/

PUBLIC BOOL scan_init( FILE  *src_fp )
{
    char func[] = "scan_init";


    if ( ( fp = src_fp ) == NULL )
    {
	log_error( ERR_ABORT, FILE_ERROR, E_OPEN_FILE, func, 0 );
	return FALSE;
    }

    if ( ( buffer = (char *)xmalloc( SCAN_BUFSIZE ) ) == NULL )
    {
	log_error( ERR_ABORT, MEMORY_ERROR, E_ALLOCATE, func, 0 );
	return FALSE;
    }
    
    scan_readbuffer();

    if ( ! sym_init() )
        return FALSE;

    bez_count = 0;
    lineno = 1;

    return TRUE;
}


/* ----------- Routinen zum Einlesen des Quelltextes ------------- */

PRIVATE BOOL scan_readbuffer( void )
{
    buffer[0] = buffer[buffer_fill+2-2];
    buffer[1] = buffer[buffer_fill+2-1];
    buffer_fill = fread( buffer + 2, 1, SCAN_BUFSIZE - 2, fp );
    buffer_ptr = (char *)buffer + 2;
/*  memset( buffer_ptr + buffer_fill, 0, SCAN_BUFSIZE - 2 - buffer_fill ); */
    scan_eof = scan_eof_next;
    scan_eof_next = ( buffer_fill == 0 );
    return TRUE;
}

PRIVATE char scan_getchar( void )
{
    char z;

    if ( scan_eof )  return ' ';
    z = *buffer_ptr;
    if ( z == '\n' )
	lineno ++;
    buffer_ptr++;
    if ( buffer_ptr >= buffer + 2 + buffer_fill )
	scan_readbuffer();

    return z;
}

PRIVATE char scan_nextchar( void )
{
    return *buffer_ptr;
}

PRIVATE void scan_ungetchar( void )
{
    buffer_ptr --;
    if ( *buffer_ptr == '\n' )
	lineno --;

    return;
}


/*------------------------------------------------------------------------*\
|                                                                          |
| Funktion     :  scan_get      holt das naechste Symbol vom Scanner       |
|                                                                          |
| Eingabe      :  ----                                                     |
|                                                                          |
| Ausgabe      :  sk            syntaktische Kategorie des Symbols         |
|                 m             zugehoeriges Merkmal des Symbols           |
|                 line          aktuelle Zeilennummer                      |
|                                                                          |
| Ergebnis     :  TRUE = kein Fehler, FALSE = Fehler aufgetreten           |
|                                                                          |
\*------------------------------------------------------------------------*/

PUBLIC BOOL scan_get( Symbol     *sk,
                      Merkmal     *m,
		      UINT32   *line )
{
    char bez[MAX_BEZLEN];  /* Zwischenspeicher fuer einen Bezeichner */
    char               z;  /* das aktuelle Zeichen */
    int                i;  /* Laufvariable */

    UINT32         value;  /* Auswertung einer numerischen Konstanten */
    INT32           expo;  /* Gesamt-Exponent bei Real-Zahl */
    UINT32  realconst[2];  /* nochmal das gleiche zum speichern */
    INT32        expoadd;  /* Korrektur des Exponenten bei Real-Zahl */
    INT8         expofak;  /* Vorzeichen des Exponenten bei Real-Zahl */

    char func[] = "scan_get";


    /* --- zunaechst white space ueberlesen --- */

    restart:

    do
	z = scan_getchar();
    while ( isspace( z ) && !scan_eof );

    *m = 0;
    *line = lineno;

    if ( ( scan_eof ) && ( isspace(z) ) )
    {
	*sk = tok_eof;
	return TRUE;
    }

    switch ( z )
    {
	case ':' : if ( scan_nextchar() == '=' )
		   {
		       *sk = tok_assign;
		       scan_getchar();
		   }
		   else
		       *sk = tok_istype;
		   return TRUE;
	case ';' : *sk = tok_eocmd;  return TRUE;
	case '.' : z = scan_nextchar();
		   if ( isdigit( z ) )  /* Real-Konstante! */
		   {
                       z = '.';
                       break;
                   }
                   if ( z == '.' )
		   {
                       *sk = tok_range;
                       scan_getchar();
                   }
                   else
                       *sk = tok_eoprog;
                   return TRUE;
        case ',' : *sk = tok_comma;  return TRUE;
        case '(' : if ( scan_nextchar() == '*' )
                   {
                      scan_getchar();

                      do      /* Kommentare ueberlesen */
		      {
                          for ( z = scan_getchar(); z != '*'; )
                              z = scan_getchar();
                          z = scan_getchar();

                      } while (z != ')');

                      goto restart;
                   }
                   else
                       *sk = tok_lbrack;
                   return TRUE;
        case ')' : *sk = tok_rbrack; return TRUE;
	case '[' : *sk = tok_lidx;   return TRUE;
        case ']' : *sk = tok_ridx;   return TRUE;
        case '+' : if ( scan_nextchar() == '+' )
                   {
                       *sk = tok_newopr;
                       *m  = OP_CONCAT;
                       scan_getchar();
                   }
                   else
                   {
                       *sk = tok_addopr;
                       *m  = OP_ADD;
                   }
		   return TRUE;
        case '*' : *sk = tok_multopr; *m = OP_MULT;  return TRUE;
        case '<' : *sk = tok_relop;
                   if ( scan_nextchar() == '=' )
                   {
                       *m  = REL_LEQ;
                       scan_getchar();
                   }
                   else
                       *m = REL_LOWER;
                   return TRUE;
        case '>' : *sk = tok_relop;
                   if ( scan_nextchar() == '=' )
		   {
                       *m  = REL_GEQ;
                       scan_getchar();
                   }
                   else
                       *m  = REL_GREATER;
                   return TRUE;
        case '=' : if ( ( z = scan_nextchar() ) == '=' )
                   {
                       *sk = tok_relop;
                       *m  = REL_EQUAL;
                       scan_getchar();
                   }
		   return TRUE;
        case '-' : *sk = tok_addopr;  *m = OP_MINUS;  return TRUE;
        case '/' : *sk = tok_multopr; *m = OP_DIV;    return TRUE;
        case '%' : *sk = tok_newopr;  *m = OP_MOD;    return TRUE;
        case '"' : for ( i = 0, z = scan_getchar();
                         isprint( z ) && ( z != '"' );
                         i++ )
                   {
                       bez[i] = z;
                       z = scan_getchar();
                   }
                   bez[i] = 0;
                   
		   if ( z != '"' )
                   {
                       log_error( ERR_ERROR, SYNTAX_ERROR, E_STRING_NOT_TERMINATED, NULL, lineno );
                   }
                   
                   if ( ! ctab_insert( (UINT8 *)bez, i+1, m, TRUE ) )
                   {
                       log_error( ERR_ABORT, SYSTEM_ERROR, E_INSERT_IN_CONSTAB, func, lineno );
                       return FALSE;
                   }
                   
                   *sk = tok_stringconst;

		   return TRUE;

    }

    if ( isalpha( z ) )
    {
        for ( i = 0; isalpha( z ) || isdigit( z ); i++ )
        {
            bez[i] = z;
            z = scan_getchar();
        }

        scan_ungetchar();

        bez[i] = 0;

        if ( ! sym_lookup( bez, sk, m ) )
        {
            bez_count ++;
            sym_insert( bez, tok_ident, bez_count );
            *sk = tok_ident;
            *m = bez_count;
        }

        return TRUE;  /* Bezeichner oder Schluesselwort erkannt */
    }

    if ( isdigit( z ) || ( z == '.' ) )  /* Integer- oder Real-Konstante */
    					 /* ODER '..' !!!!!! */
    {
        /* zuerst Integer-Konstante lesen (evtl. 0 Zeichen) */
        /* bis zum Punkt */

        value = 0;

        while ( isdigit( z ) )
        {
            /* --> hier sollte evtl. noch eine Pruefung auf
                   zu grosse Zahlen erfolgen !!!            <-- */

            value = value * 10 + z - '0';
            z = scan_getchar();
        }

        /* bei Integer-Konstante sind wir fertig, bei Real
           geht's jetzt erst richtig los... */

        if ( z == '.' )
        {
            z = scan_getchar();

            if ( isdigit( z ) )
            {
                expo    = 0;
                expofak = 1;

                expoadd = -1;
                value   = value * 10 + z - '0';

                z = scan_getchar();

                while ( isdigit( z ) )
                {
                    value = value * 10 + z - '0';
                    expoadd--;

                    z = scan_getchar();
                }

                if ( z == 'E' )
                {
                    z = scan_getchar();

                    if ( z == '+' )
                        expofak = 1;
                    else if ( z == '-' )
                        expofak = -1;
                    else
                    {
                        log_error( ERR_ERROR, SYNTAX_ERROR, E_REALCONST_EXP_SIGN, NULL, *line );
                        return FALSE;
                    }

                    expo = 0;

                    z = scan_getchar();

                    while ( isdigit( z ) )
                    {
                        expo = expo * 10 + z - '0';
                        z = scan_getchar();
                    }
                }

                expo = expo * expofak + expoadd;

                scan_ungetchar();

                realconst[0] = (UINT32)value;
                realconst[1] = (UINT32)expo;

		if ( ! ctab_insert( (UINT8 *)realconst, 8, m, FALSE ) )
                {
                    log_error( ERR_ABORT, SYSTEM_ERROR, E_INSERT_IN_CONSTAB, func, *line );
                    return FALSE;
                }

                *sk = tok_realconst;
                return TRUE;
            }
            else
            {
                scan_ungetchar();
                scan_ungetchar();
                *sk = tok_intconst;
                *m = value;
                return TRUE;
            }
        }
        else
        {
            scan_ungetchar();
            *sk = tok_intconst;
            *m  = value;
            return TRUE;
        }
    }

    return FALSE;
}


#include "macros.h"

#include <string.h>
#include <stdlib.h>

/* -------------------------- import -------------------------- */

#include "import.h"
#include "xmem.h"
#include "error.h"

/* -------------------------- export -------------------------- */

#include "export.h"
#include "symtable.h"

/* ------------------- global variables ----------------------- */

PUBLIC SymKeyEntry sym_keytable[] =  /* Initialisierungspaare fuer       */
{                                    /* Eintragung der Schluesselwoerter */
    { "ARRAY",     tok_array   },    /* und zugeh. syntakt. Klassen in   */
    { "BEGIN",     tok_begin   },    /* die Symboltabelle                */
    { "BOOLEAN",   tok_bool    },    /* Wird auch fuer Fehlermeldungen   */
    { "DECLARE",   tok_decl    },    /* benutzt!                         */
    { "DO",        tok_do      },
    { "ELSE",      tok_else    },
    { "END",       tok_end     },
    { "FALSE",     tok_false   },
    { "IF",        tok_if      },
    { "INTEGER",   tok_int     },
    { "NOT",       tok_not     },
    { "OF",        tok_of      },
    { "PROCEDURE", tok_proc    },
    { "PROGRAM",   tok_program },
    { "READ",      tok_read    },
    { "REAL",      tok_real    },
    { "THEN",      tok_then    },
    { "TRUE",      tok_true    },
    { "VAR",       tok_var     },
    { "WHILE",     tok_while   },
    { "WRITE",     tok_write   },

    { "FUNCTION",  tok_func    },
    { "RETURN",    tok_return  },
    { "FAIL",      tok_fail    },
    { "WRITELN",   tok_writeln },
    { "STRING",    tok_string  },
    { "FORMAT",    tok_format  },

    { "':'",       tok_istype  },
    { "';'",       tok_eocmd   },
    { "':='",      tok_assign  },
    { "'('",       tok_lbrack  },
    { "')'",       tok_rbrack  },
    { "'.'",       tok_eoprog  },
    { "','",       tok_comma   },
    { "'..'",      tok_range   },
    { "'['",       tok_lidx    },
    { "']'",       tok_ridx    },
    { "'+' or '-'",tok_addopr  },
    { "'*' or '/'",tok_multopr },
    { "'<', '<=', '==', '>=' or '>'", tok_relop },
    { "'++' or '%'", tok_newopr },
    { "integer constant", tok_intconst  },
    { "real constant",    tok_realconst },
    { "string constant",  tok_stringconst },
    { "identifier",       tok_ident     },
    { "end of file",      tok_eof       }
};

PRIVATE SymHashPtr sym_hashtable[SYM_HASHTABSIZE];  /* Hash-Tabelle */

PRIVATE SymBezTable  *bez_table;  /* Bezeichner-Tabelle */

PRIVATE UINT32        sym_count;  /* Zaehler fuer Anzahl der Eintraege  */
                                  /* in der Hash-Tabelle (just for fun) */


/*------------------------------------------------------------------------*\
|                                                                          |
| Funktion     :  sym_init      initialisiert die Symboltabelle mit den    |
|                               lexem/symbol-Paaren aus sym_keytable       |
|                                                                          |
| Eingabe      :  ----                                                     |
|                                                                          |
| Ausgabe      :  ----                                                     |
|                                                                          |
| Ergebnis     :  TRUE = kein Fehler, FALSE = Fehler aufgetreten           |
|                                                                          |
\*------------------------------------------------------------------------*/

PUBLIC BOOL sym_init( void )
{
    SymKeyEntry   *ptr;  /* zum Durchlaufen des sym_keytable */
    int              i;  /* Laufvariable */

    char func[] = "sym_init";


    /* -- das hier sollte eigentlich unnoetig sein: -- */

    for ( i = 0; i < SYM_HASHTABSIZE; i ++ )
        sym_hashtable[i] = NULL;

    bez_table = NULL;
    sym_count = 0;

    /* -- Bezeichner eintragen -- */

    for ( ptr = sym_keytable; *(ptr->lexem) != 39; ptr++ ) /* bis zum ' */
    {
        if ( ! sym_insert( ptr->lexem, ptr->sk, 0 ) )
        {
            log_error( ERR_ABORT, SYSTEM_ERROR, E_INIT_SYMTABLE, func, 0 );
            return FALSE;
        }
    }

    return TRUE;
}



/*------------------------------------------------------------------------*\
|                                                                          |
| Funktion     :  sym_insert    fuegt ein neues Token/Symbol-Paar in       |
|                               die Symboltabelle ein. Dieses Paar darf    |
|                               noch nicht in der Tabelle vorhanden sein!  |
|                               (Achtung: dies wird nicht ueberprueft!)    |
|                                                                          |
| Eingabe      :  lexem         der zum Symbol gehoerige ASCII-Bezeichner  |
|                 sk            die syntaktische Klasse                    |
|                 m             das zugehoerige Merkmal                    |
|                                                                          |
| Ausgabe      :  ----                                                     |
|                                                                          |
| Ergebnis     :  TRUE = kein Fehler, FALSE = Fehler aufgetreten           |
|                                                                          |
\*------------------------------------------------------------------------*/

PUBLIC BOOL sym_insert( char     *lexem,
                        Symbol       sk,
                        Merkmal       m )
{
    UINT32  hashsum;  /* die Summe ueber alle Zeichen */
    UINT16      len;  /* die Laenge des Token */
    SymHashPtr  neu;  /* neuer Eintrag */
    SymBezTable *bt;  /* neue Texttabelle */
    int           i;  /* Laufvariable */

    char func[] = "sym_insert";


    len = strlen( lexem );

    /* -- die Hashfunktion: -- */

    hashsum = 0;
    for ( i = 0; i < len; i++ )
        hashsum += lexem[i];

    hashsum %= SYM_HASHTABSIZE;

    /* -- jetzt an dieser Stelle vorne in die verk. Liste haengen -- */

    if ( (neu = XALLOCTYPE( SymHashEntry ) ) == NULL )
    {
        log_error( ERR_ABORT, MEMORY_ERROR, E_ALLOCATE, func, 0 );
        return FALSE;
    }

    neu->len     = len;
    neu->sk      = sk;
    neu->m       = m;
    neu->next    = sym_hashtable[hashsum];
    sym_hashtable[hashsum] = neu;

    /* -- den Bezeichner in die Texttabelle eintragen, -- */
    /* -- bei Ueberlauf neue Texttabelle davorhaengen    -- */

    if ( (bez_table == NULL) || ( (bez_table != NULL) &&
         (bez_table->freeix + len > SYM_BEZTABSIZE) ) )
    {
        if (( bt = XALLOCTYPE( SymBezTable )) == NULL)
        {
            log_error( ERR_ABORT, MEMORY_ERROR, E_ALLOCATE, func, 0 );
            return FALSE;
        }

        if (( bt->data = xcalloc( SYM_BEZTABSIZE, sizeof(char) )) == NULL )
        {
            log_error( ERR_ABORT, MEMORY_ERROR, E_ALLOCATE, func, 0 );
            return FALSE;
        }

        bt->freeix = 0;
        bt->next   = bez_table;
        bez_table  = bt;
    }

    neu->bez_tab = bez_table;
    neu->bez_ix  = bez_table->freeix;

    strncpy( (char *)bez_table->data + bez_table->freeix, lexem, len );
    bez_table->freeix += len;

    sym_count++;

    return TRUE;
}


/*------------------------------------------------------------------------*\
|                                                                          |
| Funktion     :  sym_lookup    gibt die Informationen ueber ein in der    |
|                               Symboltabelle gespeichertes Lexem zurueck  |
|                                                                          |
| Eingabe      :  lexem         das gesuchte Lexem                         |
|                                                                          |
| Ausgabe      :  sk            die syntaktische Kategorie zum Lexem       |
|                 m             das zugehoerige Merkmal zum Lexem          |
|                                                                          |
| Ergebnis     :  TRUE = kein Fehler, FALSE = Fehler aufgetreten           |
|                                                                          |
\*------------------------------------------------------------------------*/

PUBLIC BOOL sym_lookup( char      *lexem,
                        Symbol       *sk,
                        Merkmal       *m )
{
    SymHashPtr    elem;  /* aktuelles Element der verketteten Liste */
    UINT32     hashsum;
    UINT16         len;  /* Laenge des Lexems */
    int              i;  /* Laufvariable */


    len = strlen( lexem );

    /* -- die Hashfunktion: -- */

    hashsum = 0;
    for ( i = 0; i < len; i++ )
        hashsum += lexem[i];

    hashsum %= SYM_HASHTABSIZE;

    /* -- jetzt dort die verkettete Liste untersuchen -- */

    for ( elem = sym_hashtable[hashsum];
          elem != NULL;
          elem = elem->next )
    {
        if ( elem->len == len )
        {
            if ( ! strncmp( (char *)elem->bez_tab->data + elem->bez_ix,
                            lexem, len ))
            {
                *sk = elem->sk;
                *m  = elem->m;
                return TRUE;
            }
        }
    }

    return FALSE;
}

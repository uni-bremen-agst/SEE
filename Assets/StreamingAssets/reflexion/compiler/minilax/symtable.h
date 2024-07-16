#ifndef SYMTABLE_H
#define SYMTABLE_H

/* ------------------------ constants ------------------------- */

#define SYM_HASHTABSIZE 411
#define SYM_BEZTABSIZE 8192

/* -------------------------- types --------------------------- */

typedef enum                /* codes for allowed symbols */
{
    tok_array,  tok_begin,   tok_bool,   tok_decl,
    tok_do,     tok_else,    tok_end,    tok_false,
    tok_if,     tok_int,     tok_not,    tok_of,
    tok_proc,   tok_program, tok_read,   tok_real,
    tok_then,   tok_true,    tok_var,    tok_while,
    tok_write,

    tok_func,   tok_return,  tok_fail,   tok_writeln,
    tok_string, tok_format,

    tok_istype, tok_eocmd,   tok_assign, tok_lbrack, tok_rbrack,
    tok_eoprog, tok_comma,   tok_range,  tok_lidx,   tok_ridx,
    tok_addopr, tok_multopr, tok_relop,

    tok_newopr,

    tok_intconst, tok_realconst, tok_stringconst, tok_ident,

    tok_eof

} Symbol;


typedef enum { OP_ERROR, REL_LOWER, REL_LEQ, REL_EQUAL,
	       REL_GREATER, REL_GEQ,
	       OP_ADD, OP_MINUS, OP_MULT, OP_DIV,
	       OP_MOD, OP_CONCAT, OP_NOT } comptype;

typedef UINT32 Merkmal;


typedef struct SymKeyEntry
{
    char     *lexem;  /* der ASCII-Bezeichner des Tokens */
    Symbol       sk;  /* die syntaktische Klasse des Symbols */

} SymKeyEntry;


typedef struct SymBezTable    /* ein Element einer verketteten Liste von  */
                              /* Texttabellen fuer effiziente Speicherung */
                              /* von Konstanten Bezeichnern               */
{
    char               *data;   /* die eigentlichen Daten */
    UINT16            freeix;   /* Anfang des freien Bereichs */

    struct SymBezTable *next;   /* Zeiger auf die naechste Texttabelle */

} SymBezTable;


typedef struct SymHashEntry *SymHashPtr;

typedef struct SymHashEntry   /* ein Element der verketteten Liste eines */
                              /* Hash-Wertes (wg. Kollisionsaufloesung!) */
{
    Symbol            sk;  /* syntaktische Kategorie */
    Merkmal            m;  /* Merkmal des Symbols */

    UINT16           len;  /* Laenge des Bezeichners */

    SymBezTable *bez_tab;  /* Nummer der Texttabelle, in der der     */
                           /* zugehoerige Bezeichner gespeichert ist */
    UINT16        bez_ix;  /* Index des Bezeichners in der Texttabelle */

    SymHashPtr      next;  /* Zeiger auf das naechste Element der Liste */

} SymHashEntry;


/* ------------------- global variables --------------------- */

PUBLIC SymKeyEntry sym_keytable[];


/* ------------------------ functions ----------------------- */

PUBLIC BOOL sym_init( void );               /* init symbol table with  */
                                            /* key pairs               */

PUBLIC BOOL sym_insert( char    *lexem,     /* insert one lexem/symbol */
                        Symbol      sk,     /* pair into symbol table  */
                        Merkmal merkmal );

PUBLIC BOOL sym_lookup( char     *lexem,    /* get info back */
                        Symbol      *sk,
                        Merkmal *merkmal );

#endif

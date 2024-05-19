#ifndef ARGUMENT_H
#define ARGUMENT_H

/* ---------------------------- types ------------------------------- */

typedef struct arguments
{
    char               sw;             /*  Schalter                       */
    char               *entry;         /*  Argument zum Schalter          */
    struct arguments   *next;          /*  Zeiger auf naechstes Argument  */
				                                   /*  oder NULL                      */
} ARGUMENTS;


/* -------------------------- functions ----------------------------- */

PUBLIC ARGUMENTS *get_arg ( int            argc,    /* Anzahl Argumente. */
                            char        *argv[],    /* Argumentliste. */
                            const char *mono_sw );  /* String mit allen */
                                                    /* Zeichen, die kein */
                                                    /* Argument haben. */

#endif

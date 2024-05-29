#ifndef ERROR_H
#define ERROR_H

#include <stdio.h>

/* ------------ types ---------- */

typedef enum { ERR_NOTICE, ERR_COMMENT, ERR_WARNING,
               ERR_ERROR, ERR_FATAL, ERR_ABORT
             } error_class;

typedef enum
{
    FILE_ERROR,			/* file errors */
    MEMORY_ERROR,		/* out of memory etc. problems */
    SYSTEM_ERROR,		/* these are CODING errors!! */
    
    SYNTAX_ERROR,
    SEMANTIC_ERROR,
    TYPE_ERROR
    
} error_type;

typedef enum
{
    /* file errors */

    E_OPEN_FILE,                /* could not open a file */
    E_FILE_EMPTY,               /* source file is empty */

    /* memory errors */

    E_ALLOCATE,                 /* could not allocate memory */

    /* syntax errors */

    E_STRING_NOT_TERMINATED,    /* missing " in line */
    E_REALCONST_EXP_SIGN,       /* sign of exponent in real constant is missing */

    /* semantic errors */

    E_SYMBOL_EXPECTED,          /* the specified symbol was expected */
    E_MISSING_SEMICOLON,        /* missing semicolon at the end of a 
    				   statement (inserted) */
    E_DECLARED_TWICE,		/* identifier declared twice */
    E_LWB_GREATER_UPB,		/* lower bound of array exceeds upper bound */
    E_NO_FUNC_OR_PROC,		/* identifier is not a procedure/function */
    E_NO_VARIABLE,		/* identifier is not a variable or index */
    E_UNDECLARED,		/* an identifier is used without decl. */
    E_FUNC_NO_RETURN,           /* the last statement of a function was not RETURN */
    E_NEVER_REACHED,		/* there is code that will never be executed */

    /* type errors */

    E_NO_SIMPLE_TYPE_FORMAL,	/* formal not of simple type in declaration */
    E_WRONG_TYPE,		/* type could not be coerced to req. type */
    E_PARAM_TYPE,		/* parameter type doesn't match declared type */
    E_PARAM_COUNT,		/* number or parameters different from decl. */
    E_PARAM_IN_PROC_RETURN,     /* the return of a procedure contains a parameter */
    E_NO_PARAM_IN_FUNC_RETURN,  /* the return of a function contains no parameter */
    E_NO_SIMPLE_TYPE_ACTUAL,	/* actual not of simple type while VAR used */
    				/* -> must be variable or index */
    E_BOOLEAN_NEEDED,		/* must be BOOLEAN! */
    E_WRONG_LHS_TYPE,		/* wrong type on left hand side of operation */
    E_WRONG_RHS_TYPE,		/* wrong type on right hand side of op. */
    E_TOO_MANY_INDICES,         /* index doesn't match declaration */
    E_NOT_INDEX_TYPE,           /* used other index than integer */
    E_NO_READ_ARRAY,            /* can't read entire ARRAYs directly */

    /* system errors */

    E_INSERT_IN_CONSTAB,        /* could not insert value into constab */
    E_NOT_IN_CONSTAB,		/* could not read value from constab */
    E_INIT_SCANNER,             /* could not init scanner */
    E_INIT_SYMTABLE,		/* could not init symtable */
    E_SYM_KEYTABLE,             /* coding error in sym_keytable field */
    E_ILLEGAL_TAG,              /* illegal tag in an atn_node */
    E_NO_ROOT,			/* root == NULL */
    E_ILLEGAL_POINTER,		/* NULL pointer expected */
    E_ILLEGAL_OPERAND,          /* combination of operand types and modes not allowed */
    E_MORE_ERRORS		/* error storage capacity exceeded */
    
} error_code;

typedef struct error_entry
{
    error_class class;
    error_type   type;
    error_code   code;
    char        *info;
    UINT32       line;
} error_entry;

/* -------- functions ---------- */

PUBLIC void log_error ( error_class  errclass,
			error_type    errtype,
			error_code    errcode,
			char            *info,
			UINT32           line );

PUBLIC void print_error( FILE   *fp,
                         INT16  max );

#endif

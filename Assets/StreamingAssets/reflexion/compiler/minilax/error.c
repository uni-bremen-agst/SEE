#include "macros.h"

#include <stdio.h>
#include <string.h>

#include "import.h"

#include "export.h"
#include "error.h"

/* --------- definitions ------- */

#define MAX_ERRORS 100

/* --------- variables --------- */

PRIVATE error_entry err_buffer[MAX_ERRORS];

PRIVATE char err_class_string[ERR_ABORT+1][12] =
{ "Notice", "Comment", "Warning", "Error", "Fatal Error", "Abort Error" };

PRIVATE char err_type_string[TYPE_ERROR+1][9] =
{ "File", "Memory", "Internal", "Syntax", "Semantic", "Type" };

PRIVATE INT16 err_count   = 0;

/* --------- functions --------- */

PUBLIC void log_error ( error_class class,
			error_type  etype,
			error_code   code,
			char        *info,
			UINT32       line )
{
    if ( class >= ERR_FATAL )  gencode_flag = FALSE;

    if ( err_count < MAX_ERRORS )
    {
	err_buffer[err_count].class = class;
	err_buffer[err_count].type  = etype;
	err_buffer[err_count].code  = code;
	err_buffer[err_count].info  = info;
	err_buffer[err_count].line  = line;
    }
    err_count ++;
}


PUBLIC void print_error( FILE   *fd,
                         INT16  max )
{
    INT16 ind, max_ind;

    max_ind  =  (max <= 0) ?  MIN( err_count, MAX_ERRORS )
                           :  MIN( err_count, max );

    if ( max_ind < 0 )
    {
        fprintf( fd, "No error reported.\n" );
    }
    else
    {
        fprintf( fd, "number of errors reported: %d\n", err_count );

	for ( ind = 0;  ind < max_ind;  ind ++ )
        {
	    if ( err_buffer[ind].type < SYNTAX_ERROR )
                fprintf( fd, "%s: %s Error in %s : ", err_class_string[err_buffer[ind].class],
						err_type_string[err_buffer[ind].type],
    					        err_buffer[ind].info );
   	    else
   	        fprintf( fd, "%s: %s Error in line %ld : ", err_class_string[err_buffer[ind].class],
   	        				      err_type_string[err_buffer[ind].type],
   	        				      err_buffer[ind].line );

            switch ( err_buffer[ind].code )
            {
		/* file errors */
    		case E_OPEN_FILE :  fprintf( fd, "could not open file" ); break;
    		case E_FILE_EMPTY : fprintf( fd, "file is empty" ); break;

    		/* memory errors */
    		case E_ALLOCATE : fprintf( fd, "could not allocate memory" ); break;

    		/* syntax errors */
    		case E_STRING_NOT_TERMINATED : fprintf( fd, "string not terminated" ); break;
    		case E_REALCONST_EXP_SIGN : fprintf( fd, "sign of exponent in real constant missing" ); break;

    		/* semantic errors */
    		case E_SYMBOL_EXPECTED : fprintf( fd, "%s expected", err_buffer[ind].info ); break;
    		case E_MISSING_SEMICOLON : fprintf( fd, "semicolon missing (inserted)" ); break;
    		case E_DECLARED_TWICE : fprintf( fd, "identifier declared twice" ); break;
		case E_LWB_GREATER_UPB : fprintf( fd, "lower bound of array exceeds upper bound" ); break;
    		case E_NO_FUNC_OR_PROC : fprintf( fd, "identifier is not a procedure/function" ); break;
    		case E_NO_VARIABLE : fprintf( fd, "identifier is not a variable" ); break;
    		case E_UNDECLARED : fprintf( fd, "identifier undeclared" ); break;
		case E_FUNC_NO_RETURN : fprintf( fd, "function might not return a value" ); break;
		case E_NEVER_REACHED : fprintf( fd, "code is never reached => ignoring" ); break;

    		/* type errors */
    		case E_NO_SIMPLE_TYPE_FORMAL : fprintf( fd, "formal must be of simple type" ); break;
		case E_WRONG_TYPE : fprintf( fd, "types don't match" );
				    if ( err_buffer[ind].info )  fprintf( fd, ", %s expected", err_buffer[ind].info );
				    break;
		case E_PARAM_TYPE : fprintf( fd, "actual type doesn't match declaration (#%s)", err_buffer[ind].info ); break;
		case E_PARAM_COUNT : fprintf( fd, "number or parameters differs from declaration" ); break;
		case E_PARAM_IN_PROC_RETURN : fprintf( fd, "procedure return must not have any parameters" ); break;
		case E_NO_PARAM_IN_FUNC_RETURN : fprintf( fd, "function return must have one parameter" ); break;
		case E_NO_SIMPLE_TYPE_ACTUAL : fprintf( fd, "actual must be of simple type" ); break;
		case E_BOOLEAN_NEEDED : fprintf( fd, "condition is not a BOOLEAN expression" ); break;
		case E_WRONG_LHS_TYPE : fprintf( fd, "illegal type on left hand side" ); break;
		case E_WRONG_RHS_TYPE : fprintf( fd, "illegal type on right hand side" ); break;
		case E_TOO_MANY_INDICES : fprintf( fd, "more indices used than declared" ); break;
		case E_NOT_INDEX_TYPE : fprintf( fd, "index is not an INTEGER expression" ); break;
		case E_NO_READ_ARRAY : fprintf( fd, "cannot read ARRAYs directly" ); break;

		/* system errors */
		case E_INSERT_IN_CONSTAB : fprintf( fd, "could not insert value into constants table" ); break;
		case E_NOT_IN_CONSTAB : fprintf( fd, "requested constab id does not exist" ); break;
		case E_INIT_SCANNER : fprintf( fd, "could not initialize scanner" ); break;
		case E_INIT_SYMTABLE : fprintf( fd, "could not initialize symbol table" ); break;
		case E_SYM_KEYTABLE : fprintf( fd, "illegal entry in sym_keytable field" ); break;
		case E_ILLEGAL_TAG : fprintf( fd, "illegal tag in an %s node", err_buffer[ind].info ); break;
		case E_NO_ROOT : fprintf( fd, "no root" ); break;
		case E_ILLEGAL_POINTER : fprintf( fd, "illegal pointer detected" ); break;
		case E_ILLEGAL_OPERAND : fprintf( fd, "illegal combination of operand modes and types" ); break;
		case E_MORE_ERRORS : fprintf( fd, "more errors reported" ); break;
	    }
            fprintf( fd, "\n" );
        }
    }

    return;
}

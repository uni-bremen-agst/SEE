#include <stdio.h>
#include <string.h>

#include "macros.h"

#include "import.h"
#include "xmem.h"
#include "error.h"
#include "argument.h"
#include "symtable.h"
#include "scanner.h"
#include "parser.h"
#include "abstree.h"
#include "prntree.h"
#include "semantic.h"
#include "typechk.h"
#include "cbam.h"
#include "codegenS.h"
#include "codegen.h"

#include "export.h"


void print_help( char *progname )
{
    printf( "Syntax: %s [-o outfilename] [-help] [-v] [-debug] [-O] [-R] infilename\n"
            "-o outfilename  set output file name\n"
            "-help           print this help screen\n"
            "-v              show verbose messages during compilation\n"
            "-debug          show debug information during compilation\n"
            "-O              code improvement (optimization)\n"
            "-R              disable range checking\n"
            "-S              force use of the stack generation scheme\n"
            "-N              force use of the new code generation scheme\n",
            progname );
}

typedef BOOL (*code_generator) (FILE *fp);

code_generator current_code_generator;

void set_current_code_generator (code_generator generator) {

  current_code_generator = generator;

}

int main( int argc, char *argv[] )
{
    FILE              *fp;
    ARGUMENTS        *arg;
    char  infilename[256];
    char outfilename[256] = "a.cbam";
    char        sepline[] = "-------------------";
    char           func[] = "main";
    BOOL   old_stack_flag = FALSE;
    BOOL new_codegen_flag = FALSE;

    printf( "%s%s%s%s\n", sepline, sepline, sepline, sepline );
    printf( "MiniLAX Compiler   Compilerbau Praktikum WS 95/96   by Jochen Quante\n" );
    printf( "%s%s%s%s\n", sepline, sepline, sepline, sepline );

    arg = get_arg( argc, argv, "hvdORSN" );

    for ( ; arg != NULL; arg = arg->next )
    {
        switch ( arg->sw )
        {
            case 0 :   printf( "input file = %s\n", arg->entry );
                       strcpy( infilename, arg->entry );
                       break;
            case 'h' : print_help( argv[0] );
                       return TRUE;
            case 'v' : printf( "verbose mode\n" );
                       verbose_flag = TRUE;
                       break;
            case 'd' : printf( "debug mode\n" );
                       debug_flag = TRUE;
                       break;
            case 'O' : printf( "optimization enabled\n" );
                       optimize_flag = TRUE;
                       break;
            case 'R' : printf( "range checking disabled\n" );
                       range_check_flag = FALSE;
                       break;
            case 'o' : strcpy( outfilename, arg->entry );
                       break;
            case 'S' : printf( "using old stack code generation\n" );
            	       old_stack_flag = TRUE;
            	       break;
            case 'N' : printf( "using new code generation\n" );
            	       new_codegen_flag = TRUE;
            	       break;
            default :  print_help( argv[0] );
                       return FALSE;
        }
    }

    if ( ! *infilename )
    {
	print_help( argv[0] );
	return FALSE;
    }

    printf( "output file = %s\n", outfilename );
    
    if ( ( fp = fopen( infilename, "rt" ) ) == NULL )
    {
	log_error( ERR_ABORT, FILE_ERROR, E_OPEN_FILE, func, 0 );
	print_error( stdout, 0 );
	return FALSE;
    }

    if ( verbose_flag || debug_flag )
	printf( "-----------parsing-----------\n" );

    if ( ! scan_init(fp) )
    {
	log_error( ERR_ABORT, SYSTEM_ERROR, E_INIT_SCANNER, func, 0 );
	print_error( stdout, 0 );
	return FALSE;
    }

    if ( ! parse() )
    {
	print_error( stdout, 0 );
	return FALSE;
    }

    fclose( fp );

    if ( debug_flag )
    {
	printf( "\n===================result of parser==================\n" );
	if ( ! print_tree( FALSE ) )
	{
	    print_error( stdout, 0 );
	    return FALSE;
	}
    }

    if ( verbose_flag || debug_flag )
	printf( "------semantic analysis------\n" );

    if ( ! sem_analysis() )
    {
	print_error( stdout, 0 );
	return FALSE;
    }

    if ( debug_flag )
    {
	printf( "\n=============result of semantic analysis=================\n" );
	if ( ! print_tree( TRUE ) )
	{
	    print_error( stdout, 0 );
	    return FALSE;
	}
    }

    if ( verbose_flag || debug_flag )
	printf( "--------type checking--------\n" );

    if ( ! type_check() )
    {
	print_error( stdout, 0 );
	return FALSE;
    }

    if ( debug_flag )
    {
	printf( "=====================result of type check======================\n" );
	if ( ! print_tree( TRUE ) )
	{
	    print_error( stdout, 0 );
	    return FALSE;
	}
    }

    if ( gencode_flag )
    {
        if ( ( fp = fopen( outfilename, "wt" ) ) == NULL )
        {
	    log_error( ERR_ABORT, FILE_ERROR, E_OPEN_FILE, func, 0 );
	    print_error( stdout, 0 );
	    return FALSE;
        }

	if ( debug_flag || verbose_flag )
	    printf( "--------generating code------\n" );

	
	/*	if ( ! new_codegen_flag && ( old_stack_flag || ! optimize_flag ) )
	{
	    if ( ! code_gen_stack( fp ) )
	    {
	        print_error( stdout, 0 );
	        return FALSE;
	    }
	}
	else
	{
	    if ( ! code_gen( fp ) )
	    {
	        print_error( stdout, 0 );
	        return FALSE;
	    }
	}
	*/

	if ( ! new_codegen_flag && ( old_stack_flag || ! optimize_flag ) )
	{
	  set_current_code_generator (code_gen_stack);
	}
	else
	{
	  set_current_code_generator (code_gen);
	}

	if ( ! current_code_generator( fp ) )
	  {
	    print_error( stdout, 0 );
	    return FALSE;
	  }
	
	fclose( fp );

        printf( "compilation finished\n" );
    }
    else
        printf( "No code generated due to errors.\n" );

    print_error( stdout, 0 );

    if ( debug_flag )  xmemusage( stdout );

    return TRUE;
}

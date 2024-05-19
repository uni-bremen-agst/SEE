#include "macros.h"

#include <stdio.h>

#include "import.h"
#include "error.h"
#include "symtable.h"
#include "constab.h"
#include "abstree.h"
#include "semantic.h"

#include "export.h"
#include "prntree.h"

/* -------------- macros ----------------- */

#define RFF( X ) { if ( ! (X) ) return FALSE; }
#define OUT( X ) printf( "%*c%s\n", 2*depth, ' ', X )
#define OUTL( X ) printf( "%3ld%*c%s\n", node ? node->line : -1, 2*depth-3, ' ', X )

/* ------------- local variables --------- */

PRIVATE UINT16  depth = 0;
PRIVATE BOOL   attrib = FALSE;

/* ---------- local functions ------------ */

PRIVATE BOOL print_decl( atn_decl *node );
PRIVATE BOOL print_formals( atn_formals *node );
PRIVATE BOOL print_formal( atn_formal *node );
PRIVATE BOOL print_decls( atn_decls *node );
PRIVATE BOOL print_type( atn_type *node );
PRIVATE BOOL print_stats( atn_stats *node );
PRIVATE BOOL print_stat( atn_stat *node );
PRIVATE BOOL print_actuals( atn_actuals *node );
PRIVATE BOOL print_expr( atn_expr *node );
PRIVATE BOOL print_index( atn_index *node );
PRIVATE BOOL print_name( atn_name *node );
PRIVATE BOOL print_null( BOOL valid );


PUBLIC BOOL print_tree( BOOL print_attribs )
{
    attrib = print_attribs;
    return print_decl( root );
}

PRIVATE BOOL print_decl( atn_decl *node )
{
    struct tEnv *hiddenenv;

    depth++;
    if ( ! node )
        print_null( FALSE );
    switch ( node->tag )
    {
        case PROC : OUTL( "DECL: tag = PROC" );
        	    RFF( print_name( node->tree.proc.name ) )
        	    RFF( print_formals( node->tree.proc.formals ) )
        	    RFF( print_decls( node->tree.proc.decls ) )
        	    RFF( print_stats( node->tree.proc.stats ) )
        	    break;
        
        case FUNC : OUTL( "DECL: tag = FUNC" );
        	    RFF( print_name( node->tree.func.name ) )
        	    RFF( print_formals( node->tree.func.formals ) )
        	    RFF( print_type( node->tree.func.type ) )
        	    RFF( print_decls( node->tree.func.decls ) )
        	    RFF( print_stats( node->tree.func.stats ) )
        	    break;
        
        case VAR : OUTL( "DECL: tag = VAR" );
        	   RFF( print_name( node->tree.var.name ) )
        	   RFF( print_type( node->tree.var.type ) )
        	   break;
        
        default : log_error( ERR_ABORT, SYSTEM_ERROR, E_ILLEGAL_TAG,
        		     "decl", node->line );
                  return FALSE;
    }

    if ( attrib )
    {
	hiddenenv = sem_resolvehidden( &( node->hidden ) );
    
        printf( "%*cident = %ld, object = %lx, env = %lx, hidden = &%lx\n",
            2*depth+2, ' ', (UINT32)node->ident, (UINT32)node->object,
            ( node->env != NoEnv ) ? (UINT32)(node->env) : 0,
            ( hiddenenv != NoEnv ) ? (UINT32)hiddenenv : 0 );
    }

    depth--;
    return TRUE;
}

PRIVATE BOOL print_formals( atn_formals *node )
{
    depth++;
    
    OUT( "FORMALS: List = {" );
    
    while ( node )
    {
        RFF( print_formal( node->tree.formal ) )

        if ( attrib )
            printf( "%*cdecls_in = %lx, decls_out = %lx\n",
                2*depth+2, ' ',
                (UINT32)node->decls_in, (UINT32)node->decls_out );

        node = node->tree.formals;
    }
    
    OUT( "  }  // end of list of formals" );

    depth--;
    return TRUE;
}

PRIVATE BOOL print_formal( atn_formal *node )
{
    depth++;
    if ( ! node )  RFF( print_null( FALSE ) )
    OUTL( "FORMAL" );
    RFF( print_name( node->tree.name ) )
    RFF( print_type( node->tree.type ) )
    
    if ( attrib )
        printf( "%*cident = %ld, object = %lx\n",
            2*depth+2, ' ',
            (UINT32)node->ident, (UINT32)node->object );

    depth--;
    return TRUE;
}

PRIVATE BOOL print_decls( atn_decls *node )
{
    struct tEnv *hiddenenv;

    depth++;
    OUT( "DECLS: List = {" );
    
    while ( node )
    {
        RFF( print_decl( node->tree.decl ) )
 
        if ( attrib )
        {
            hiddenenv = sem_resolvehidden( &( node->hidden ) );
        
            printf( "%*cdecls_in = %lx, decls_out = %lx, hidden = &%lx\n\n",
                2*depth+2, ' ',
                (UINT32)node->decls_in, (UINT32)node->decls_out,
                ( hiddenenv != NoEnv ) ? (UINT32)hiddenenv : 0 );
        }

        node = node->tree.decls;
    }
    
    OUT( "  }  // end of list of decls" );

    depth--;
    return TRUE;
}

PRIVATE BOOL print_type( atn_type *node )
{
    depth++;
    if ( ! node )  RFF( print_null( FALSE ) )
    switch ( node->tag )
    {
        case INTEGER : OUTL( "TYPE: tag = INTEGER" ); break;
        case REAL :    OUTL( "TYPE: tag = REAL" );    break;
        case BOOLEAN : OUTL( "TYPE: tag = BOOLEAN" ); break;
        case STRING :  OUTL( "TYPE: tag = STRING" );  break;
        case ARRAY :   printf( "%*cTYPE: tag = ARRAY, bounds = [%d..%d]\n", 2*depth, ' ',
        			node->tree.array.lwb, node->tree.array.upb );
        	       RFF( print_type( node->tree.array.type ) )
        	       break;
        case REF :     OUTL( "TYPE: tag = REF" );
        	       RFF( print_type( node->tree.reftype ) )
        	       break;
        default :      log_error( ERR_ABORT, SYSTEM_ERROR, E_ILLEGAL_TAG,
        			  "type", node->line );
        	       return FALSE;
    }
    depth--;
    return TRUE;
}

PRIVATE BOOL print_stats( atn_stats *node )
{
    depth++;
    OUT( "STATS: List = {" );

    while ( node )
    {
        RFF( print_stat( node->tree.stat ) )
        node = node->tree.stats;
    }

    OUT( "  }  // end of list of stats" );
    depth--;
    return TRUE;
}

PRIVATE BOOL print_stat( atn_stat *node )
{
    depth++;
    if ( ! node )  RFF( print_null( FALSE ) )
    switch ( node->tag )
    {
        case ASSIGN : OUTL( "STAT: tag = ASSIGN" );
        	      RFF( print_index( node->tree.assign.index ) )
        	      RFF( print_expr( node->tree.assign.expr ) )
        	      break;
        case READ :
        case WRITE :
        case WRITELN :
        case CALL :   OUTL( "STAT: tag = CALL" );
                      if ( node->tag == CALL )
        	          RFF( print_name( node->tree.call.name ) )
        	      else
        	          printf( "%*cNAME: READ | WRITE | WRITELN\n", 2*depth+2, ' ' );
        	      RFF( print_actuals( node->tree.call.actuals ) )
        	      break;
        case IF :     OUTL( "STAT: tag = IF_THEN_ELSE" );
        	      RFF( print_expr( node->tree.if_.expr ) )
        	      OUTL( "  [THEN]" );
        	      RFF( print_stats( node->tree.if_.stats_then ) )
        	      OUTL( "  [ELSE]" );
        	      RFF( print_stats( node->tree.if_.stats_else ) )
        	      break;
        case WHILE :  OUTL( "STAT: tag = WHILE");
        	      RFF( print_expr( node->tree.while_.expr ) )
        	      OUTL( "  [DO]" );
        	      RFF( print_stats( node->tree.while_.stats ) )
        	      break;
        case RETURN : OUTL( "STAT: tag = RETURN");
        	      if ( node->tree.returnexpr )
             	          RFF( print_expr( node->tree.returnexpr ) )
        	      break;
        case FAIL :   OUTL( "STAT: tag = FAIL");
        	      if ( node->tree.returnexpr )
        	          RFF( print_expr( node->tree.failexpr ) )
        	      break;
        default :     log_error( ERR_ABORT, SYSTEM_ERROR, E_ILLEGAL_TAG,
        			 "stat", node->line );
                      return FALSE;
    }
    depth--;
    return TRUE;
}

PRIVATE BOOL print_actuals( atn_actuals *node )
{
    depth++;
    OUT( "ACTUALS: List = {" );

    while ( node )
    {
        RFF( print_expr( node->tree.expr ) )
        node = node->tree.actuals;
    }

    OUT( "  }  // end of list of actuals" );
    depth--;
    return TRUE;
}

PRIVATE BOOL print_expr( atn_expr *node )
{
    UINT8 *data;

    depth++;
    if ( ! node )  RFF( print_null( FALSE ) )
    switch ( node->tag )
    {
        case EXPR :       printf( "%*cEXPR: Operator = %d\n",
                                  2*depth, ' ', node->tree.expr.operator );
        		  RFF( print_expr( node->tree.expr.expr1 ) )
                          if ( node->tree.expr.operator == OP_NOT )
                          {
                              print_null( TRUE );
                          }
                          else
                          {
          		      OUTL( "  [2nd op]" );
        		      RFF( print_expr( node->tree.expr.expr2 ) )
        		  }
        		  break;
        case INTCONST :   printf( "%*cEXPR: tag = INTCONST, val = %ld\n", 2*depth, ' ',
                                  node->tree.intconst );
                          break;
        case REALCONST :  printf( "%*cEXPR: tag = REALCONST, val = ConstTable(%08lx)",
        			  2*depth, ' ', node->tree.realconst );
                          ctab_lookup( (UINT32)(node->tree.realconst), &data, NULL );
                          printf( " -> %ld*10^%ld\n", *( (UINT32 *)data ),
                          			      *( (INT32 *)data + 1) );
                          break;
        case BOOLCONST :  printf( "%*cEXPR: tag = BOOLCONST, val = %d\n", 2*depth, ' ',
                                  node->tree.boolconst );
                          break;
        case STRINGCONST : printf( "%*cEXPR: tag = STRINGCONST, val = ConstTable(%08lx)",
        			   2*depth, ' ', node->tree.stringconst );
                           ctab_lookup( (UINT32)(node->tree.stringconst), &data, NULL );
                           printf( " -> %s\n", data );
                           break;
        case IFTHENELSE : OUTL( "EXPR: IF_THEN_ELSE" );
        		  RFF( print_expr( node->tree.ifthenelse.expr_if ) )
        		  OUTL( "  [THEN]" );
        		  RFF( print_expr( node->tree.ifthenelse.expr_then ) )
        		  OUTL( "  [ELSE]" );
        		  RFF( print_expr( node->tree.ifthenelse.expr_else ) )
        		  break;
        case FORMAT :     OUTL( "EXPR: tag = FORMAT" );
        		  RFF( print_expr( node->tree.formatexpr ) )
        		  break;
        case FUNCALL :    OUTL( "EXPR: tag = FUNCTION_CALL" );
        		  RFF( print_name( node->tree.funcall.name ) )
        		  RFF( print_actuals( node->tree.funcall.actuals ) )
        		  break;
        case EINDEX :     OUTL( "EXPR: tag = INDEX" );
        		  RFF( print_index( node->tree.index ) )
        		  break;
        default :     log_error( ERR_ABORT, SYSTEM_ERROR, E_ILLEGAL_TAG,
        			 "expr", node->line );
                      return FALSE;
    }
    if ( node->type )  RFF( print_type( node->type ) )
    if ( node->coercion == CO_INTTOREAL )
        OUTL( "  coercion = INTTOREAL" );

    depth--;
    return TRUE;
}

PRIVATE BOOL print_index( atn_index *node )
{
    depth++;
    if ( ! node )  RFF( print_null( FALSE ) )
    switch( node->tag )
    {
        case INDEX : OUTL( "INDEX: tag = INDEX" );
        	     RFF( print_index( node->tree.index.index ) )
        	     RFF( print_expr( node->tree.index.expr ) )
                     break;

        case NAME :  OUTL( "INDEX: tag = NAME" );
        	     RFF( print_name( node->tree.name ) )
                     break;

        default :    log_error( ERR_ABORT, SYSTEM_ERROR, E_ILLEGAL_TAG,
        			"index", node->line );
                     return FALSE;
    }
    if ( node->type )  RFF( print_type( node->type ) )

    depth--;
    return TRUE;
}

PRIVATE BOOL print_name( atn_name *node )
{
    depth++;
    if ( ! node )
    {
        print_null( TRUE );  /* weil READ, WRITE, WRITELN etc. noch nicht fertig! */
    }
    else
    {
        printf( "%*cNAME: id = %ld\n", 2*depth, ' ', node->ident );

        if ( attrib )
            printf( "%*cobject = %lx\n", 2*depth+2, ' ', (UINT32)node->object );
    }

    depth--;
    return TRUE;
}

PRIVATE BOOL print_null( BOOL valid )
{
    depth++;
    printf( "%*cNULL\n", 2*depth, ' ' );
    if ( ! valid )  return FALSE;
    depth--;
    return TRUE;
}

#include "macros.h"

#include <string.h>
#include <stdio.h>

#include "import.h"
#include "error.h"
#include "constab.h"
#include "symtable.h"
#include "abstree.h"
#include "typechk.h"
#include "cbam.h"
#include "threeadr.h"

/* #include "codelist.h" */
PUBLIC BOOL cl_dump_code( FILE *fp );
PUBLIC BOOL cl_print( void );


#include "export.h"
#include "codegen.h"


/* -------------------- macros ----------------------- */

#define RETURN_ERR( F )  if ( ! ( F ) )  return FALSE
#define UP4( S )  (((S) & 3) ? (S)+(4-((S) & 3)) : (S))

/* --------------- local variables ------------------- */

UINT16             depth = 0;
label_type range_check_label;
INT32          string_cr = -1;
INT32       string_range = -1;
UINT32          popcount = 0;  /* number of additional elements on stack */
		               /* after a statement */

/* --------------- local functions ------------------- */

PRIVATE BOOL cogen_decl( atn_decl *node );
PRIVATE INT8 cogen_stats( atn_stats *node );
PRIVATE label_type cogen_expr( atn_expr *node );
PRIVATE label_type cogen_index( atn_index *node );

PRIVATE BOOL cogen_call_any( struct tObject *object, atn_actuals *actuals );
PRIVATE BOOL cogen_copy_array( label_type dest, label_type src, atn_type *type );

PRIVATE UINT8 typ_align( atn_type *node );
PRIVATE UINT16 typ_length( atn_type *node );

PRIVATE INT32 seg_new( void );
PRIVATE INT32 seg_insert( UINT8 align, UINT16 length );
PRIVATE INT32 seg_length( void );



PUBLIC BOOL code_gen( FILE *fp )
{
    atn_decls *decls;

    root->object->label = a3_get_label();
    
    /* test if procedures or functions are declared */

    for ( decls = root->tree.proc.decls;
          decls && ( decls->tree.decl->tag == VAR );
          decls = decls->tree.decls );
    
    /* if so, overjump them (since procs + funcs are processed first) */

    if ( decls )
        a3_add_op( A3_GOTO, oLABEL, root->object->label );


    cogen_decl( root );
    
    if ( debug_flag )
    {
        printf( "==========result of intermediate code generation===========\n" );
        a3_print();
    }

    if ( optimize_flag )
    {
        if ( debug_flag || verbose_flag )
            printf( "---------optimizing----------\n" );
        a3_optimize();
        
        if ( debug_flag )
        {
            printf( "==========result of optimization===========\n" );
            a3_print();
        }
    }

    a3_codegen();

    if ( debug_flag )
    {
        printf( "==============result of code generation==================\n" );
        cl_print();
    }

    if ( debug_flag || verbose_flag )  printf( "---------writing code--------\n" );

    cl_dump_code( fp );
    fprintf( fp, "S\n" );
    ctab_dump_strings( fp );
    
    return TRUE;
}


PRIVATE BOOL cogen_decl( atn_decl *node )
{
    atn_formals   *formals;
    atn_decls       *decls;
    struct tObject *object;
    UINT16    formalsspace;
    UINT16   variablespace;
    INT8          returned;

    if ( ( node->tag != PROC ) && ( node->tag != FUNC ) )
	return TRUE;

    /* first assign stack locations for the formals */

    seg_new();

    for ( formals = ( node->tag == PROC ) ? node->tree.proc.formals
					  : node->tree.func.formals;
	  formals != NULL;
	  formals = formals->tree.formals )
    {
	object = formals->tree.formal->object;
	object->location = seg_insert( typ_align( object->tree.type ),
			 	       typ_length( object->tree.type ) );
    }

    formalsspace = seg_length();
    node->object->location = formalsspace;

    /* then assign stack locations for the local variables */

    for ( decls = ( node->tag == PROC ) ? node->tree.proc.decls
					: node->tree.func.decls;
	  decls != NULL;
	  decls = decls->tree.decls )
    {
	if ( decls->tree.decl->tag == VAR )
	{
	    object = decls->tree.decl->object;
	    object->location = seg_insert( typ_align( object->tree.type ),
					   typ_length( object->tree.type ) );
	}
    }
    
    variablespace = seg_length();

    /* now process local procedure and function declarations */

    depth ++;
    
    for ( decls = ( node->tag == PROC ) ? node->tree.proc.decls
					: node->tree.func.decls;
	  decls != NULL;
	  decls = decls->tree.decls )
    {
	if ( decls->tree.decl->tag != VAR )
        {
            if ( ! decls->tree.decl->object->label )
                decls->tree.decl->object->label = a3_get_label();
	    RETURN_ERR( cogen_decl( decls->tree.decl ) );
	}
    }

    depth --;

    /* generate code */
    
    a3_set_label( node->object->label );

    if ( ( !depth ) || ( variablespace - formalsspace > 0 ) )
        a3_add_op( A3_BINARY_OP, oREG, 29, oREG, 29,
                   oCLONG, variablespace - formalsspace + ( depth ? 0 : 4 ), ADD );

    RETURN_ERR( returned = cogen_stats( ( node->tag == PROC ) ? node->tree.proc.stats
    				                              : node->tree.func.stats ) );
    returned --;

    if ( !returned )
    {
        if ( node->tag == FUNC )     /* return a value */
        {
	    /* default return function, used only if none other is present */
	    /* return value is undefined in this case */

	    log_error( ERR_WARNING, SEMANTIC_ERROR, E_FUNC_NO_RETURN, NULL, node->line );

	    switch ( type_simplify( node->tree.func.type )->tag )
	    {
	        case BOOLEAN :
	        case INTEGER : a3_add_op( A3_BINARY_OP, oREG, 29, oREG, 29,
	        			  oCLONG, 4, ADD );
	        	       a3_add_op( A3_RTS, oCLONG, 1 );
	    		       break;
	        case REAL : a3_add_op( A3_BINARY_OP, oREG, 29, oREG, 29,
	        		       oCLONG, 8, ADD );
	        	    a3_add_op( A3_RTS, oCLONG, 2 );
	    		    break;
	        case STRING : printf( "return STRING not implemented\n" );
	    		      break;
	        case ARRAY : a3_add_op( A3_BINARY_OP, oREG, 29, oREG, 29, oCLONG,
	        		        UP4( typ_length( node->tree.func.type ) ), ADD );
	        	     a3_add_op( A3_RTS, oCLONG, UP4( typ_length( node->tree.func.type ) ) / 4 );
	    		     break;
	        default : printf( "error\n" );
	    }
        }
        else
	    if ( depth > 0 )
	        a3_add_op( A3_RTS, oCLONG, 0 );
	    else
	        a3_add_op( A3_HALT, oCLONG, 0 );
    }

    return TRUE;
}


PRIVATE INT8 cogen_stats( atn_stats *node )
{
    atn_stat *stat;
    label_type label1, label2;
    INT8 returned = FALSE;
    label_type var_expr, var_adr;

    for ( ;  ( node != NULL ) && ( !returned );  node = node->tree.stats )
    {
	stat = node->tree.stat;
	
	popcount = 0;

        switch ( stat->tag )
	{
            case ASSIGN :
                var_expr = cogen_expr( stat->tree.assign.expr );
            	var_adr = cogen_index( stat->tree.assign.index );

		switch ( type_finaltype( stat->tree.assign.expr ) )
            	{
            	    case ARRAY : cogen_copy_array( var_adr, var_expr, stat->tree.assign.index->type );
            			 a3_free_mem32l( var_expr );
            		      	 break;
	            case REAL :  a3_add_op( A3_ASSIGN, oVFLOAT_IND, var_adr,
	            				       oVFLOAT, var_expr );
            			 a3_free_mem32f( var_expr );
            		      	 break;
		    case BOOLEAN : a3_add_op( A3_ASSIGN, oVBYTE_IND, var_adr,
		    					 oVBYTE, var_expr );
            			   a3_free_mem32l( var_expr );
		    		   break;
   	  	    case INTEGER : a3_add_op( A3_ASSIGN, oVLONG_IND, var_adr,
   	  	    					 oVLONG, var_expr );
            			   a3_free_mem32l( var_expr );
     			           break;
     		    default : printf( "switch not handled\n" );
            	}
            	
            	a3_free_mem32l( var_adr );
		break;

	    case CALL :
	        cogen_call_any( stat->tree.call.name->object,
			        stat->tree.call.actuals );

		if ( ! stat->tree.call.name->object->label )
		    stat->tree.call.name->object->label = a3_get_label();
		
		break;

	    case IF :
	        var_expr = cogen_expr( stat->tree.if_.expr );
		/* boolean result in expr1 */
		label1 = a3_get_label(); /* ELSE */
		a3_add_op( A3_COND, oVLONG, var_expr, oCLONG, 0,
		                    oLABEL, label1, R_EQ );
		a3_free_mem32l( var_expr );
		cogen_stats( stat->tree.if_.stats_then );
		label2 = a3_get_label(); /* ENDIF */
		a3_add_op( A3_GOTO, oLABEL, label2 );
		a3_set_label( label1 );
		cogen_stats( stat->tree.if_.stats_else );
		a3_set_label( label2 );
		break;

	    case WHILE :
	        a3_set_label( label1 = a3_get_label() );  /* LOOP */
		var_expr = cogen_expr( stat->tree.while_.expr );
		/* boolean result in expr1 */
		label2 = a3_get_label(); /* END */
		a3_add_op( A3_COND, oVLONG, var_expr, oCLONG, 0,
		                    oLABEL, label2, R_EQ );
		cogen_stats( stat->tree.while_.stats );
		a3_add_op( A3_GOTO, oLABEL, label1 );
		a3_set_label( label2 );
		a3_free_mem32l( var_expr );
		break;

	    case WRITE :
	    case WRITELN :
	        cogen_expr( stat->tree.call.actuals->tree.expr );
			   /* STRING output is handled in cogen_expr,
			      since STRINGs are only used within WRITE/LN */
                
		if ( stat->tag == WRITELN )
		{
		    a3_add_op( A3_FRAME, oCLONG, 0, oCLONG, 1 );

		    if ( string_cr == -1 )
		        ctab_insert( (UINT8 *)"\n", 2, (UINT32 *)&string_cr, TRUE );

		    a3_add_op( A3_ASSIGN, oREG_IX, 29, -1, oSTRING_ID, string_cr );
		    a3_add_op( A3_JSR, oCLONG, -20 );
		}
		break;

	    case READ :
	        if ( stat->tree.call.actuals->tree.expr->tag != EINDEX )
	    	{
	    	    log_error( ERR_ABORT, SYSTEM_ERROR, E_ILLEGAL_TAG, NULL, 0 );
	    	    return FALSE;
	    	}
	    	a3_add_op( A3_FRAME, oCLONG, 0, oCLONG, 0 );

	    	switch ( type_realtype( stat->tree.call.actuals->tree.expr ) )
	    	{
	    	    case INTEGER : a3_add_op( A3_JSR, oCLONG, -40 );
	    	    		   /* result is on the stack */
	    	    		   a3_add_op( A3_POPL );
	    	    		   var_expr = a3_get_mem32l();
	    	    		   a3_add_op( A3_ASSIGN, oVLONG, var_expr,
	    	    		                         oREG_IND, 29 );
	    	    		   var_adr = cogen_index( stat->tree.call.actuals->tree.expr->tree.index );
	    	    		   a3_add_op( A3_ASSIGN, oVLONG_IND, var_adr,
	    	    		   			 oVLONG, var_expr );
	    	    		   a3_free_mem32l( var_expr );
	    	    		   a3_free_mem32l( var_adr );
	    	    		   break;
	    	    case REAL : a3_add_op( A3_JSR, oCLONG, -48 );
	    	    		a3_add_op( A3_POPF );
	    	    		var_expr = a3_get_mem32f();
	    	    		a3_add_op( A3_ASSIGN, oVFLOAT, var_expr,
	    	    				      oREG_IND, 29 );
	    	    		var_adr = cogen_index( stat->tree.call.actuals->tree.expr->tree.index );
				a3_add_op( A3_ASSIGN, oVFLOAT_IND, var_adr,
						      oVFLOAT, var_expr );
	    	    		a3_free_mem32f( var_expr );
	    	    		a3_free_mem32l( var_adr );
	    	    		break;
	    	    case BOOLEAN : a3_add_op( A3_JSR, oCLONG, -56 );
	    	    		   /* result is on the stack */
	    	    		   a3_add_op( A3_POPL );
	    	    		   var_expr = a3_get_mem32l();
	    	    		   a3_add_op( A3_ASSIGN, oVLONG, var_expr,
	    	    		                         oREG_IND, 29 );
	    	    		   var_adr = cogen_index( stat->tree.call.actuals->tree.expr->tree.index );
	    	    		   a3_add_op( A3_ASSIGN, oVBYTE_IND, var_adr,
	    	    		   			 oVLONG, var_expr );
	    	    		   a3_free_mem32l( var_expr );
	    	    		   a3_free_mem32l( var_adr );
	    	    		   break;
	    	    case ARRAY : log_error( ERR_ERROR, TYPE_ERROR, E_NO_READ_ARRAY, NULL, stat->line );
	    	    		 break;
	    	    case STRING : printf( "READ( string ) not implemented!\n" );
	    	    		  break;
	    	    default : log_error( ERR_FATAL, SYSTEM_ERROR, E_ILLEGAL_TAG, "stat", 0 );
	    	}
		break;

	    case RETURN :
	        if ( !depth )
	        {
	    	    a3_add_op( A3_HALT, oCLONG, 0 );
	    	}
	    	else
	    	{
	    	    if ( stat->tree.returnexpr )
		    {
		        var_expr = cogen_expr( stat->tree.returnexpr );
		        switch ( type_finaltype( stat->tree.returnexpr ) )
		        {
		            case ARRAY : var_adr = a3_get_mem32l();
		            		 a3_add_op( A3_ASSIGN, oVLONG, var_adr, oREG, 29 );
		            		 a3_add_op( A3_BINARY_OP, oREG, 29, oREG, 29,
		           		            oCLONG, UP4( typ_length( stat->tree.returnexpr->type ) ), ADD );
		            		 cogen_copy_array( var_adr, var_expr, stat->tree.returnexpr->type );
		            		 a3_add_op( A3_RTS, oCLONG, UP4( typ_length( stat->tree.returnexpr->type ) ) / 4 );
		            		 a3_free_mem32l( var_adr );
		            		 a3_free_mem32l( var_expr );
		        	         break;
		            case REAL : a3_add_op( A3_PUSHF, oVFLOAT, var_expr );
		   	                a3_add_op( A3_RTS, oCLONG, 2 );
		   	                a3_free_mem32f( var_expr );
		                        break;
			    case BOOLEAN :
			    case INTEGER : a3_add_op( A3_PUSHL, oVLONG, var_expr );
			      		   a3_add_op( A3_RTS, oCLONG, 1 );
			      		   a3_free_mem32l( var_expr );
			          	   break;
			    default : printf( "switch not handled\n" );
			}
		    }
		    else
		        a3_add_op( A3_RTS, oCLONG, 0 );
		}
		returned = TRUE;
		break;

	    case FAIL : if ( stat->tree.failexpr )
	    		{
	    	            var_expr = cogen_expr( stat->tree.failexpr );
	    	            a3_add_op( A3_HALT, oVLONG, var_expr );
	    	            a3_free_mem32l( var_expr );
	    	        }
	    	        else
	    	            a3_add_op( A3_HALT, oCLONG, 1 );
			break;

	    default : printf( "--- switch not handled\n" );
	}
	
	if ( popcount )
	{
	    a3_add_op( A3_BINARY_OP, oREG, 29, oREG, 29, oCLONG, -popcount, ADD );
	    popcount = 0;
	}
    }

    if ( returned && node )
    {
        log_error( ERR_NOTICE, SEMANTIC_ERROR, E_NEVER_REACHED, NULL, node->tree.stat->line );
    }

    return returned + 1;
}


PRIVATE label_type cogen_expr( atn_expr *node )
{
    label_type label1, label2;
    label_type var_expr1, var_expr2, var_result = -1;
    UINT8 *data;

    if ( !node )
    {
        printf( "!!! node==NULL in COGEN_EXPR() !!!\n" );
        return -1;
    }

    switch ( node->tag )
    {
	case EXPR :

	    if ( node->tree.expr.op_type == REAL )
	    {
		var_expr2 = cogen_expr( node->tree.expr.expr2 );
		var_expr1 = cogen_expr( node->tree.expr.expr1 );
		
		switch ( node->tree.expr.operator )
       	        {
       	            case OP_MULT :  var_result = a3_get_mem32f();
				    a3_add_op( A3_BINARY_OP, oVFLOAT, var_result,
       	            			       oVFLOAT, var_expr1, oVFLOAT, var_expr2, MULT );
       	            		    break;
       	            case OP_DIV :   var_result = a3_get_mem32f();
				    a3_add_op( A3_BINARY_OP, oVFLOAT, var_result,
       	            			       oVFLOAT, var_expr1, oVFLOAT, var_expr2, DIV );
       	            		    break;
       	            case OP_ADD :   var_result = a3_get_mem32f();
				    a3_add_op( A3_BINARY_OP, oVFLOAT, var_result,
       	            			       oVFLOAT, var_expr1, oVFLOAT, var_expr2, ADD );
       	            		    break;
       	            case OP_MINUS : var_result = a3_get_mem32f();
				    a3_add_op( A3_BINARY_OP, oVFLOAT, var_result,
       	            			       oVFLOAT, var_expr1, oVFLOAT, var_expr2, SUB );
       	            		    break;
		    case REL_LOWER :
		    case REL_LEQ :
		    case REL_EQUAL :
		    case REL_GEQ :
		    case REL_GREATER : var_result = a3_get_mem32l();
       	            		       label1 = a3_get_label();
       	            		       switch ( node->tree.expr.operator )
       	            		       {
       	            		           case REL_LOWER   : a3_add_op( A3_COND, oVFLOAT, var_expr1, oVFLOAT, var_expr2, oLABEL, label1, R_LOWER ); break;
       	            		           case REL_LEQ     : a3_add_op( A3_COND, oVFLOAT, var_expr1, oVFLOAT, var_expr2, oLABEL, label1, R_LEQ ); break;
       	            		           case REL_EQUAL   : a3_add_op( A3_COND, oVFLOAT, var_expr1, oVFLOAT, var_expr2, oLABEL, label1, R_EQ ); break;
       	            		           case REL_GEQ     : a3_add_op( A3_COND, oVFLOAT, var_expr2, oVFLOAT, var_expr1, oLABEL, label1, R_LEQ ); break;
       	            		           case REL_GREATER : a3_add_op( A3_COND, oVFLOAT, var_expr2, oVFLOAT, var_expr1, oLABEL, label1, R_LOWER ); break;
       	            		           default : log_error( ERR_FATAL, SYSTEM_ERROR, E_ILLEGAL_TAG, "expr_op", 0 );
       	            		       }
       	            		       a3_add_op( A3_ASSIGN, oVLONG, var_result, oCLONG, 0 );
       	            		       label2 = a3_get_label();
       	            		       a3_add_op( A3_GOTO, oLABEL, label2 );
       	            		       a3_set_label( label1 );
       	            		       a3_add_op( A3_ASSIGN, oVLONG, var_result, oCLONG, 1 );
       	            		       a3_set_label( label2 );
				       break;
		    default : log_error( ERR_FATAL, SYSTEM_ERROR, E_ILLEGAL_TAG, "expr_op", 0 );
			      return FALSE;
       	        }
       	        a3_free_mem32f( var_expr1 );
       	        a3_free_mem32f( var_expr2 );
	    }
	    else if ( node->tree.expr.op_type == STRING )
	    {
/*			cogen_expr( node->tree.expr.expr2 );
			cl_add_op( PUSHL, mDIRECT, otREG, 0, mNONE );
  	                cl_add_op( PUSHL, mDIRECT, otREG, 0, mNONE );
			cogen_expr( node->tree.expr.expr1 );
                        cl_add_op( POPL, mNONE );
                        cl_add_op( POPL, mNONE );
                        cl_add_op( MOVL, mDIRECT, otREG, 1,
                        		 mIND, otREG, 29, mNONE ); */
		/* addresses of the strings are in R0 and R1 now */

		switch ( node->tree.expr.operator )
		{
		    case REL_EQUAL : printf( "\tMOVL R3 #0\n" );
				     printf( "\tMOVL R4 #0\n" );
				     printf( "\tMOVL R2 #0\n" );
				     printf( "loop:\tMOVB R3 R0[R2]\n" );
				     printf( "\tMOVB R4 R1[R2]\n" );
				     printf( "\tNEGS R4 R4\n" );
				     printf( "\tADDS R4 R3 R4\n" );
				     printf( "\tBSALL #diff #2 #2\n" );  /* jump if R3-R4 != 0 */
				     /* now test if both strings end here */
				     printf( "\tCPL R3\n" );
				     printf( "\tNOT\n" );
				     printf( "\tBSALL #loop #2 #2\n" );
				     printf( "\tMOVL R0 #1\n" );  /* yes, they are equal! */
				     printf( "\tBR #cont\n" );
				     printf( "diff:\tMOVL R0 #0\n" );  /* here, they are not equal */
				     printf( "cont:\n" );
				     break;
		    case OP_CONCAT : /* string concatenation: output immediately! */
				     cogen_expr( node->tree.expr.expr1 );
				     cogen_expr( node->tree.expr.expr2 );
				     break;
		    default : log_error( ERR_FATAL, SYSTEM_ERROR, E_ILLEGAL_TAG, "expr_op", 0 );
			      return FALSE;
		}
	    }
	    else   /* treat BOOLEANs like INTEGERs! */
	    {
	        var_result = a3_get_mem32l();
	        
		if ( node->tree.expr.operator != OP_NOT )
		{
		    var_expr2 = cogen_expr( node->tree.expr.expr2 );
		    var_expr1 = cogen_expr( node->tree.expr.expr1 );
		}
       	        else
		    var_expr1 = cogen_expr( node->tree.expr.expr1 );

		switch ( node->tree.expr.operator )
		{
		    case REL_LOWER :
		    case REL_LEQ :
		    case REL_EQUAL :
		    case REL_GEQ :
		    case REL_GREATER : label1 = a3_get_label();
       	            		       switch ( node->tree.expr.operator )
       	            		       {
       	            		           case REL_LOWER   : a3_add_op( A3_COND, oVLONG, var_expr1, oVLONG, var_expr2, oLABEL, label1, R_LOWER ); break;
       	            		           case REL_LEQ     : a3_add_op( A3_COND, oVLONG, var_expr1, oVLONG, var_expr2, oLABEL, label1, R_LEQ ); break;
       	            		           case REL_EQUAL   : a3_add_op( A3_COND, oVLONG, var_expr1, oVLONG, var_expr2, oLABEL, label1, R_EQ ); break;
       	            		           case REL_GEQ     : a3_add_op( A3_COND, oVLONG, var_expr2, oVLONG, var_expr1, oLABEL, label1, R_LEQ ); break;
       	            		           case REL_GREATER : a3_add_op( A3_COND, oVLONG, var_expr2, oVLONG, var_expr1, oLABEL, label1, R_LOWER ); break;
       	            		           default : log_error( ERR_FATAL, SYSTEM_ERROR, E_ILLEGAL_TAG, "expr_op", 0 );
       	            		       }
       	            		       a3_add_op( A3_ASSIGN, oVLONG, var_result, oCLONG, 0 );
       	            		       label2 = a3_get_label();
       	            		       a3_add_op( A3_GOTO, oLABEL, label2 );
       	            		       a3_set_label( label1 );
       	            		       a3_add_op( A3_ASSIGN, oVLONG, var_result, oCLONG, 1 );
       	            		       a3_set_label( label2 );
       	            		       a3_free_mem32l( var_expr1 );
       	            		       a3_free_mem32l( var_expr2 );
				       break;
       	            
		    case OP_NOT : if ( node->tree.expr.op_type != BOOLEAN )
       	            	  	  {
       	            		      log_error( ERR_FATAL, SYSTEM_ERROR, E_ILLEGAL_TAG, "expr_op", 0 );
       	            		      return FALSE;
       	            	 	  }
       	            	 	  a3_add_op( A3_UNARY_OP, oVLONG, var_result,
       	            	 	  	     oVLONG, var_expr1, LNOT );
       	            	 	  a3_free_mem32l( var_expr1 );
       	            		  break;

       	            case OP_MULT : a3_add_op( A3_BINARY_OP, oVLONG, var_result,
       	            			      oVLONG, var_expr1, oVLONG, var_expr2, MULT );
     	            		   a3_free_mem32l( var_expr1 );
       	            		   a3_free_mem32l( var_expr2 );
       	            		   break;
       	            case OP_DIV : a3_add_op( A3_BINARY_OP, oVLONG, var_result,
       	            			      oVLONG, var_expr1, oVLONG, var_expr2, DIV );
     	            		  a3_free_mem32l( var_expr1 );
       	            		  a3_free_mem32l( var_expr2 );
       	            		  break;
       	            case OP_ADD : a3_add_op( A3_BINARY_OP, oVLONG, var_result,
       	            			      oVLONG, var_expr1, oVLONG, var_expr2, ADD );
     	            		  a3_free_mem32l( var_expr1 );
       	            		  a3_free_mem32l( var_expr2 );
       	            		  break;
       	            case OP_MINUS : a3_add_op( A3_BINARY_OP, oVLONG, var_result,
       	            			      oVLONG, var_expr1, oVLONG, var_expr2, SUB );
     	            		    a3_free_mem32l( var_expr1 );
       	            		    a3_free_mem32l( var_expr2 );
       	            		    break;
       	            case OP_MOD : a3_add_op( A3_BINARY_OP, oVLONG, var_result,
       	            			      oVLONG, var_expr1, oVLONG, var_expr2, MOD );
     	            		  a3_free_mem32l( var_expr1 );
       	            		  a3_free_mem32l( var_expr2 );
       	            		  break;
		    default : log_error( ERR_FATAL, SYSTEM_ERROR, E_ILLEGAL_TAG, "expr_op", 0 );
       	        }
       	    }
	    break;

        case IFTHENELSE :
            var_expr1 = cogen_expr( node->tree.ifthenelse.expr_if );
            /* boolean result in var_expr1 */
	    label1 = a3_get_label();
	    a3_add_op( A3_COND, oVLONG, var_expr1, oCLONG, 0, oLABEL, label1, R_EQ );
	    a3_free_mem32l( var_expr1 );
            var_result = cogen_expr( node->tree.ifthenelse.expr_then );
	    label2 = a3_get_label();
	    a3_add_op( A3_GOTO, oLABEL, label2 );
	    a3_set_label( label1 );
	    var_result = cogen_expr( node->tree.ifthenelse.expr_else );
	    a3_set_label( label2 );
            break;

        case FUNCALL :
            cogen_call_any( node->tree.funcall.name->object,
       			    node->tree.funcall.actuals );
	    switch ( type_realtype( node ) )
            {
        	case BOOLEAN :
        	case INTEGER : a3_add_op( A3_POPL );
        		       var_result = a3_get_mem32l();
        	               a3_add_op( A3_ASSIGN, oVLONG, var_result,
        		       		             oREG_IND, 29 );
        		       break;
                case REAL : a3_add_op( A3_POPF );
                	    var_result = a3_get_mem32f();
        		    a3_add_op( A3_ASSIGN, oVFLOAT, var_result,
        		    			  oREG_IND, 29 );
        		    break;
        	case ARRAY : var_result = a3_get_mem32l();
        		     a3_add_op( A3_BINARY_OP, oVLONG, var_result,
        		     		oREG, 29, oCLONG, - UP4( typ_length( node->tree.funcall.name->object->tree.type ) ), ADD );
        		     popcount += UP4( typ_length( node->tree.funcall.name->object->tree.type ) );
        		     break;
                default : printf( "not implemented\n" );
       	    }
            break;

        case EINDEX :
            var_expr1 = cogen_index( node->tree.index );
	    /* address in var_expr1 */
            if ( type_realtype( node ) != ARRAY )
                if ( type_realtype( node ) != REAL )
                {
                    var_result = a3_get_mem32l();
                    a3_add_op( A3_ASSIGN, oVLONG, var_result, oVLONG_IND, var_expr1 );
                    a3_free_mem32l( var_expr1 );
                }
     		else
     		{
        	    var_result = a3_get_mem32f();
        	    a3_add_op( A3_ASSIGN, oVFLOAT, var_result, oVFLOAT_IND, var_expr1 );
        	    a3_free_mem32l( var_expr1 );
        	}
            else
                var_result = var_expr1;
                /* ARRAY: return pointer to first element in R0 */
            break;

        case INTCONST :    var_result = a3_get_mem32l();
        		   a3_add_op( A3_ASSIGN, oVLONG, var_result, oCLONG, node->tree.intconst );
        		   break;
	case REALCONST :   ctab_lookup( (UINT32)(node->tree.realconst), &data, NULL );
			   var_result = a3_get_mem32f();
			   a3_add_op( A3_ASSIGN, oVFLOAT, var_result,
			   	                 oCFLOAT, *( (INT32 *)data ),
			   	                          *( (INT32 *)data + 1 ) );
			   break;
	case BOOLCONST :   var_result = a3_get_mem32l();
			   a3_add_op( A3_ASSIGN, oVLONG, var_result,
			   	      oCLONG, node->tree.boolconst );
			   break;
	case STRINGCONST : a3_add_op( A3_FRAME, oCLONG, 0, oCLONG, 1 );
			   a3_add_op( A3_ASSIGN, oREG_IX, 29, -1,
			   	      oSTRING_ID, node->tree.stringconst );
			   a3_add_op( A3_JSR, oCLONG, -20 );
			   break;

	case FORMAT :
	    var_expr1 = cogen_expr( node->tree.formatexpr );

	    switch ( type_finaltype( node->tree.formatexpr ) )
	    {
		case BOOLEAN : a3_add_op( A3_FRAME, oCLONG, 0, oCLONG, 1 );
			       var_expr2 = a3_get_mem32l();
			       a3_add_op( A3_BINARY_OP, oVLONG, var_expr2,
			       		  oREG, 29, oCLONG, -4, ADD );
			       a3_add_op( A3_ASSIGN, oVBYTE_IND, var_expr2, oVLONG, var_expr1 );
			       a3_free_mem32l( var_expr1 );
			       a3_free_mem32l( var_expr2 );
			       a3_add_op( A3_JSR, oCLONG, -16 );
			       break;
	        case INTEGER : a3_add_op( A3_FRAME, oCLONG, 0, oCLONG, 1 );
			       a3_add_op( A3_ASSIGN, oREG_IX, 29, -1,
			                             oVLONG, var_expr1 );
			       a3_free_mem32l( var_expr1 );
			       a3_add_op( A3_JSR, oCLONG, -8 );
			       break;
	        case REAL : a3_add_op( A3_FRAME, oCLONG, 0, oCLONG, 3 );
			    a3_add_op( A3_ASSIGN, oREG_IX, 29, -1,
			    	                  oVFLOAT, var_expr1 );
			    a3_free_mem32f( var_expr1 );
			    a3_add_op( A3_JSR, oCLONG, -12 );
			    break;
	        case STRING : break;
		default : printf( "[switch not handled]\n" );
	    }
    }

    if ( node->coercion == CO_INTTOREAL )
    {
        var_expr1 = var_result;
        var_result = a3_get_mem32f();
    	a3_add_op( A3_UNARY_OP, oVFLOAT, var_result, oVLONG, var_expr1, INT2FLOAT );
    	a3_free_mem32l( var_expr1 );
    }

    return var_result;
}


PRIVATE BOOL cogen_call_any( struct tObject *object, atn_actuals *actuals )
{
    atn_formals *formals;
    label_type var_param, var_adr_base = -1, var_adr = -1;
    INT32 offset;

    a3_add_op( A3_FRAME, oCLONG, 1 + depth - object->depth,
    			 oCLONG, object->location / 4 + 1 );

    if ( object->tree.formals )
    {
        var_adr_base = a3_get_mem32l();
        a3_add_op( A3_BINARY_OP, oVLONG, var_adr_base, oREG, 29,
    		   oCLONG, - object->location, ADD );
    }

    for ( formals = object->tree.formals;
    	  actuals != NULL;
	  actuals = actuals->tree.actuals,
	  formals = formals->tree.formals )
    {
        if ( type_refdepth( formals->tree.formal->tree.type ) > 1 )
	    /* calc address only for var parameters */
	    if ( actuals->tree.expr->tag == EINDEX )
	        var_param = cogen_index( actuals->tree.expr->tree.index );
	    else
	        log_error( ERR_ERROR, SYSTEM_ERROR, E_ILLEGAL_TAG, "expr (EINDEX expected)", 0 );
        else
	    var_param = cogen_expr( actuals->tree.expr ); /* value */

        offset = formals->tree.formal->object->location;

	if ( type_refdepth( formals->tree.formal->tree.type ) > 1 )
	{
	    /* VAR parameter */
	    
	    a3_add_op( A3_ASSIGN, oVLONG_IX, var_adr_base, offset / 4,
	                          oVLONG, var_param );
	    a3_free_mem32l( var_param );
	}
        else
          switch ( type_finaltype( actuals->tree.expr ) )
          {
              case REAL : a3_add_op( A3_ASSIGN, oVFLOAT_IX, var_adr_base, offset / 8,
              				        oVFLOAT, var_param );
			  a3_free_mem32f( var_param );
			  break;
	      case ARRAY : var_adr = a3_get_mem32l();
	      		   a3_add_op( A3_BINARY_OP, oVLONG, var_adr, oVLONG, var_adr_base,
	      		   	      oCLONG, offset, ADD );
	       		   cogen_copy_array( var_adr, var_param,
	                                     actuals->tree.expr->type );
			   a3_free_mem32l( var_param );
			   a3_free_mem32l( var_adr );
	       	           break;
	      case STRING : printf( "string arguments not implemented\n" );
	       		    break;
	      case INTEGER : a3_add_op( A3_ASSIGN, oVLONG_IX, var_adr_base, offset / 4,
	      					   oVLONG, var_param );
			     a3_free_mem32l( var_param );
			     break;
	      case BOOLEAN : a3_add_op( A3_ASSIGN, oVBYTE_IX, var_adr_base, offset,
	                                           oVLONG, var_param );
			     a3_free_mem32l( var_param );
	       		     break;
	      default : printf( "switch not handled\n" );
	  }
    }
    
    if ( var_adr_base >= 0 )  a3_free_mem32l( var_adr_base );

    if ( ! object->label )
        object->label = a3_get_label();

    a3_add_op( A3_JSR, oLABEL, object->label );
    
    return TRUE;
}



PRIVATE BOOL cogen_copy_array( label_type dst_adr,
			       label_type src_adr,
			       atn_type *type )
{
    label_type var_data_len = a3_get_mem32l(),
               var_unit_len = a3_get_mem32l();
    label_type label1;
               
    a3_add_op( A3_ASSIGN, oVLONG, var_data_len,
                          oCLONG, - typ_length( type ) );
    
    label1 = a3_get_label();

    switch ( type_arraytype( type ) )
    {
        case INTEGER : a3_add_op( A3_ASSIGN, oVLONG, var_unit_len, oCLONG, 4 );
		       a3_set_label( label1 );
		       a3_add_op( A3_ASSIGN, oVLONG_IND, dst_adr, oVLONG_IND, src_adr );
            	       break;
        case REAL : a3_add_op( A3_ASSIGN, oVLONG, var_unit_len, oCLONG, 8 );
		    a3_set_label( label1 );
		    a3_add_op( A3_ASSIGN, oVFLOAT_IND, dst_adr, oVFLOAT_IND, src_adr );
                    break;
        case BOOLEAN : a3_add_op( A3_ASSIGN, oVLONG, var_unit_len, oCLONG, 1 );
        	       a3_set_label( label1 );
        	       a3_add_op( A3_ASSIGN, oVBYTE_IND, dst_adr, oVBYTE_IND, src_adr );
            	       break;
        default : printf( "error in cogen_copy_array\n" );
    }
    
    a3_add_op( A3_BINARY_OP, oVLONG, src_adr, oVLONG, src_adr, oVLONG, var_unit_len, ADD );
    a3_add_op( A3_BINARY_OP, oVLONG, dst_adr, oVLONG, dst_adr, oVLONG, var_unit_len, ADD );
    a3_add_op( A3_BINARY_OP, oVLONG, var_data_len, oVLONG, var_data_len, oVLONG, var_unit_len, ADD );
    a3_add_op( A3_COND, oVLONG, var_data_len, oCLONG, 0, oLABEL, label1, R_LOWER );
    a3_free_mem32l( var_data_len );
    a3_free_mem32l( var_unit_len );
    return TRUE;
}


PRIVATE label_type cogen_index( atn_index *node )
{
    atn_type *type;
    atn_index *index;
    label_type label1;
    label_type var_result, var_offset, var_expr, var_index, var_tmp;
    UINT8 refdepth;
    int i;
    
    for ( index = node;
          index->tag == INDEX;
	  index = index->tree.index.index ) ;
    
    for ( type = index->tree.name->object->tree.type, refdepth = 0;
          type->tag == REF;
          type = type->tree.reftype, refdepth ++ );

    
    var_offset = a3_get_mem32l();
    a3_add_op( A3_ASSIGN, oVLONG, var_offset, oCLONG, 0 ); /* initial offset is zero */

    while ( node->tag == INDEX )
    {
        var_expr = cogen_expr( node->tree.index.expr );
	/* substract lower bound */
	var_index = a3_get_mem32l();
	a3_add_op( A3_BINARY_OP, oVLONG, var_index, oVLONG, var_expr,
				 oCLONG, - type->tree.array.lwb, ADD );
	a3_free_mem32l( var_expr );

	/* result should be >= 0 */
	
	if ( range_check_flag )
	{
	    if ( string_range == -1 )
	    {
	        ctab_insert( (UINT8 *)"runtime error: range check\n", 28,
	                     (UINT32 *)&string_range, TRUE );
	        range_check_label = a3_get_label();
	        label1 = a3_get_label();  /* range check 1 ok */
		
		a3_add_op( A3_COND, oVLONG, var_index, oCLONG, 0,
		                    oLABEL, label1, R_GEQ );
		
		/* only generate this code once: RANGE CHECK ERROR */

		a3_set_label( range_check_label );
		a3_add_op( A3_FRAME, oCLONG, 0, oCLONG, 1 );
		
		var_tmp = a3_get_mem32l();
		a3_add_op( A3_BINARY_OP, oVLONG, var_tmp, oREG, 29,
					 oCLONG, -4, ADD );
		a3_add_op( A3_ASSIGN, oVLONG_IND, var_tmp,
				      oSTRING_ID, string_range );
		a3_free_mem32l( var_tmp );
		a3_add_op( A3_JSR, oCLONG, -20 );
		a3_add_op( A3_HALT, oCLONG, 1 );
		a3_set_label( label1 );
	    }
	    else
	    {
	        a3_add_op( A3_COND, oVLONG, var_index, oCLONG, 0,
	        		    oLABEL, range_check_label, R_LOWER );
	    }
	    
	    a3_add_op( A3_COND, oVLONG, var_index, oCLONG,
	                        type->tree.array.upb - type->tree.array.lwb,
	    	 		oLABEL, range_check_label, R_GREATER );
	}
	
        /* the real index is in var_index now... multiply with the size of the elements */
	var_tmp = a3_get_mem32l();
	a3_add_op( A3_BINARY_OP, oVLONG, var_tmp, oVLONG, var_index,
                   oCLONG, typ_length( type->tree.array.type ), MULT );
        a3_free_mem32l( var_index );
	var_index = a3_get_mem32l();
	a3_add_op( A3_BINARY_OP, oVLONG, var_index, oVLONG, var_tmp,
		   		 oVLONG, var_offset, ADD );
	a3_free_mem32l( var_tmp );
	a3_free_mem32l( var_offset );
        var_offset = var_index;
        
        node = node->tree.index.index;
        type = type->tree.array.type;
    }

    /* the offset is in var_offset */
    
    /* now calc the address of the (base) variable */
    
    var_index = a3_get_mem32l();
    a3_add_op( A3_ASSIGN, oVLONG, var_index, oREG, 30 );

    for ( i = 1 + depth - node->tree.name->object->depth; i > 0; i -- )
    {
        var_tmp = a3_get_mem32l();
	a3_add_op( A3_ASSIGN, oVLONG, var_tmp, oVLONG_IND, var_index );
	a3_free_mem32l( var_index );
	var_index = var_tmp;
    }

    var_tmp = a3_get_mem32l();
    a3_add_op( A3_BINARY_OP, oVLONG, var_tmp, oVLONG, var_index,
               oCLONG, 4 + 4 + node->tree.name->object->location, ADD );
    a3_free_mem32l( var_index );
    var_index = var_tmp;

    for ( i = 1; i < refdepth; i ++ )
    {
        var_tmp = a3_get_mem32l();
        a3_add_op( A3_ASSIGN, oVLONG, var_tmp, oVLONG_IND, var_index );
	a3_free_mem32l( var_index );
        var_index = var_tmp;
    }

    var_result = a3_get_mem32l();
    a3_add_op( A3_BINARY_OP, oVLONG, var_result, oVLONG, var_offset,
    			     oVLONG, var_index, ADD );
    a3_free_mem32l( var_offset );
    a3_free_mem32l( var_index );

    return var_result;
}


/* ------------------ type alignment and sizes -------------------- */

PRIVATE UINT8 typ_align( atn_type *node )
{
    if ( node->tag == REF )
	node = node->tree.reftype;

    switch ( node->tag )
    {
	case INTEGER : return 4;
	case REAL    : return 8;
	case BOOLEAN : return 1;
	case STRING  : return 4;
	case ARRAY   : return typ_align( node->tree.array.type );
	case REF     : return 4;
	default : log_error( ERR_FATAL, SYSTEM_ERROR, E_ILLEGAL_TAG, "type", 0 );
    }

    return 8;
}


PRIVATE UINT16 typ_length( atn_type *node )
{
    if ( node->tag == REF )
	node = node->tree.reftype;

    switch ( node->tag )
    {
	case INTEGER : return 4;
	case REAL    : return 8;
	case BOOLEAN : return 1;
	case STRING  : return 4;
	case ARRAY   : return typ_length( node->tree.array.type ) *
			 ( node->tree.array.upb - node->tree.array.lwb + 1 );
	case REF     : return 4;
	default : log_error( ERR_FATAL, SYSTEM_ERROR, E_ILLEGAL_TAG, "type", 0 );
    }

    return 0;
}



/* ---------- functions to map variables to stack locations ---------- */
/*   simple version, since there are no records and stuff in MINILAX   */

#define MAX_VARMEM 65536/8

PRIVATE INT32 seg_max = 0;     /* actual memory usage (bytes) */
PRIVATE UINT8 seg_used[MAX_VARMEM];  /* max. 64k of variables */


PRIVATE INT32 seg_new( void )
{
    memset( seg_used, 0, MAX_VARMEM );
    seg_max = 0;
    return seg_max;
}

PRIVATE INT32 seg_length( void )
{
    return seg_max * 8;
}

#define MBITSOFNSET( m,n ) ( ( 0xff >> (8-m) ) << (n-m) )

PRIVATE INT32 seg_insert( UINT8 align, UINT16 length )
{
    INT32 i, j;
    INT32 result;

    /* first try to insert into left space in memory, if align < 8 */

    if ( ( align < 8 ) && ( length <= 4 ) )
    {
	for ( i = 0; i < seg_max; i ++ )
	{
	    if ( seg_used[i] != 255 )
	    {
		for ( j = 8-align; j >= length; j -= align )
		{
		    if ( ! ( seg_used[i] & MBITSOFNSET( length, j ) ) )
		    {
			seg_used[i] |= MBITSOFNSET( length, j );
			return 8 * i + 8-j;
		    }
		}
	    }
	}
    }

    /* now see if we can start the new variable in the previous 8 bytes */

    if ( ( align < 8 ) && ( seg_max > 0 ) )
    {
	for ( i = 8-align; i > 0; i -= align )
	{
	    if ( ! ( seg_used[seg_max-1] & MBITSOFNSET( i, i ) ) )
	    {
		seg_used[seg_max-1] |= MBITSOFNSET( i, i );
		length -= i;
		break;
	    }
	}
	result = 8 * seg_max - i;
    }
    else
	result = 8 * seg_max;



    for ( i = 0; i < length/8; i ++ )
	seg_used[seg_max + i] = 0xff;

    seg_max += length/8;
    length &= 7;

    if ( length )
    {
	seg_used[seg_max] = MBITSOFNSET( length, 8 );
	seg_max ++;
    }

    return result;
}

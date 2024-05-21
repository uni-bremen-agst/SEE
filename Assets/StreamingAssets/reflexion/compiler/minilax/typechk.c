#include "macros.h"

#include "stdarg.h"

#include "import.h"
#include "xmem.h"
#include "symtable.h"
#include "abstree.h"
#include "error.h"
#include "semantic.h"

#include "export.h"
#include "typechk.h"

/* ------------- macros --------------- */

#define RETURN_ERR( F ) if ( ! ( F ) )  return FALSE

/* -------- private functions --------- */

PRIVATE BOOL type_decl( atn_decl *node );
PRIVATE BOOL type_decls( atn_decls *node );

PRIVATE BOOL type_stats( atn_stats *node );
PRIVATE BOOL type_expr( atn_expr *node );
PRIVATE BOOL type_name( atn_name *node );
PRIVATE BOOL type_actuals( atn_actuals *node );
PRIVATE BOOL type_index( atn_index *node );

PRIVATE BOOL type_expr_post( atn_expr *node, atn_type *dsttype );

PRIVATE coercion_type type_coercion( atn_type *srctype,
				     atn_type *dsttype );
PRIVATE BOOL          type_sametype( atn_type *type1,
				     atn_type *type2 );
/* PRIVATE UINT8       type_indexdepth( atn_index *index ); */
PRIVATE UINT8       type_arraydepth( atn_type *type );
PRIVATE BOOL       type_checkparams( atn_formals *formals,
				     atn_actuals *actuals,
				     UINT32          line );
PRIVATE coercion_type type_assignarray( atn_index *left, atn_expr *right );
PRIVATE BOOL              type_is_a( atn_expr *node, ... );
PRIVATE atn_type *makeref( atn_type *type, UINT8 refdepth );


/* ---------- private variables -------- */

PRIVATE atn_decl *actual_block;

PRIVATE atn_type *StringType;
PRIVATE atn_type *IntType;
PRIVATE atn_type *RealType;
PRIVATE atn_type *BoolType;



PUBLIC BOOL type_check()
{
    /* --- create default types --- */
    StringType = XALLOCTYPE( atn_type );
    StringType->tag = STRING;
    IntType = XALLOCTYPE( atn_type );
    IntType->tag = INTEGER;
    RealType = XALLOCTYPE( atn_type );
    RealType->tag = REAL;
    BoolType = XALLOCTYPE( atn_type );
    BoolType->tag = BOOLEAN;

    return type_decl( root );
}



PRIVATE BOOL type_decl( atn_decl *node )
{
    atn_decl *old_actual;

    if ( node->tag == VAR )  return TRUE;

    old_actual = actual_block;
    actual_block = node;

    switch ( node->tag )
    {
	case PROC : RETURN_ERR( type_decls( node->tree.proc.decls ) );
		    RETURN_ERR( type_stats( node->tree.proc.stats ) );
		    break;

	case FUNC : RETURN_ERR( type_decls( node->tree.func.decls ) );
		    RETURN_ERR( type_stats( node->tree.func.stats ) );
		    break;

	default :  log_error( ERR_FATAL, SYSTEM_ERROR, E_ILLEGAL_TAG,
			      "decl", 0 );
    }

    actual_block = old_actual;

    return TRUE;
}


PRIVATE BOOL type_decls( atn_decls *node )
{
    do
    {
	RETURN_ERR( type_decl( node->tree.decl ) );
	node = node->tree.decls;
    } while ( node );

    return TRUE;
}


PRIVATE BOOL type_stats( atn_stats *node )
{
    atn_stat        *stat;

    while ( node )
    {
	stat = node->tree.stat;

	if ( ! stat )  return TRUE;

	switch ( stat->tag )
	{
	    case ASSIGN : RETURN_ERR( type_index( stat->tree.assign.index ) );
			  RETURN_ERR( type_expr( stat->tree.assign.expr ) );

			  /* index->type = variable or array index? */
			  /*         yes (semantic) */

			  /* expr->type convertable to index->type? */
			  /*    (that is, equal or int->real) */
			  
			  /*  => everything is done in a seperate proc! */

			  stat->tree.assign.expr->coercion =
				  type_assignarray( stat->tree.assign.index,
						    stat->tree.assign.expr );
			  break;

	    case CALL :   RETURN_ERR( type_name( stat->tree.call.name ) );
			  RETURN_ERR( type_actuals( stat->tree.call.actuals ) );

			  /* argument types coercible to formals? */
			  /* attention with var parameters! */
			  if ( stat->tree.call.name->object->tag != DECL )
			  {
			      log_error( ERR_ERROR, SEMANTIC_ERROR, E_NO_FUNC_OR_PROC, NULL, stat->line );
			  }
			  else
			  {
			      RETURN_ERR( type_checkparams( stat->tree.call.name->object->tree.formals,
							    stat->tree.call.actuals, stat->line ) );
			  }
			  break;

	    case WRITELN :
	    case WRITE :  RETURN_ERR( type_actuals( stat->tree.call.actuals ) );
			  /* argument types allowed? */
			  if ( ! type_is_a( stat->tree.call.actuals->tree.expr,
						StringType, NULL ) )
			  {
			      log_error( ERR_ERROR, TYPE_ERROR, E_PARAM_TYPE,
					 "1=STRING", stat->line );
			  }
			  if ( stat->tree.call.actuals->tree.actuals != NULL )
			  {
			      log_error( ERR_ERROR, TYPE_ERROR, E_PARAM_COUNT,
					 NULL, stat->line );
			  }
			  break;

	    case READ :   RETURN_ERR( type_actuals( stat->tree.call.actuals ) );
			  /* argument types allowed? */
			  if ( stat->tree.call.actuals->tree.expr->tag != EINDEX )
			  {
			      log_error( ERR_ERROR, TYPE_ERROR, E_NO_SIMPLE_TYPE_ACTUAL, NULL, stat->line );
			  }
			  else
			  {
			      if ( ! type_is_a( stat->tree.call.actuals->tree.expr,
						StringType, IntType, RealType, BoolType, NULL ) )
			      {
				  log_error( ERR_ERROR, TYPE_ERROR, E_NO_SIMPLE_TYPE_ACTUAL, NULL, stat->line );
			      }
			  }
			  if ( stat->tree.call.actuals->tree.actuals != NULL )
			  {
			      log_error( ERR_ERROR, TYPE_ERROR, E_PARAM_COUNT,
					 NULL, stat->line );
			  }
			  break;

	    case IF :     RETURN_ERR( type_expr( stat->tree.if_.expr ) );
			  RETURN_ERR( type_stats( stat->tree.if_.stats_then ) );
			  RETURN_ERR( type_stats( stat->tree.if_.stats_else ) );
			  /* if_.expr->type = BOOLEAN? */
			  if ( ! type_is_a( stat->tree.if_.expr, BoolType, NULL ) )
			  {
			      log_error( ERR_ERROR, TYPE_ERROR, E_BOOLEAN_NEEDED, NULL, stat->line );
			  }
			  break;

	    case WHILE :  RETURN_ERR( type_expr( stat->tree.while_.expr ) );
			  RETURN_ERR( type_stats( stat->tree.while_.stats ) );
			  /* while_.expr->type = BOOLEAN? */
			  if ( ! type_is_a( stat->tree.while_.expr, BoolType, NULL ) )
			  {
			      log_error( ERR_ERROR, TYPE_ERROR, E_BOOLEAN_NEEDED, NULL, stat->line );
			  }
			  break;

	    case RETURN : if ( stat->tree.returnexpr )
			  {
			      if ( actual_block->tag != FUNC )
			      {
				  log_error( ERR_ERROR, SEMANTIC_ERROR,
					     E_PARAM_IN_PROC_RETURN, NULL, stat->line );
			      }
			      else
			      {
				  /* it's a function */
				  RETURN_ERR( type_expr( stat->tree.returnexpr ) );
				  if ( ( stat->tree.returnexpr->coercion =
					    type_coercion( stat->tree.returnexpr->type,
							   actual_block->tree.func.type ) ) == CO_ERROR )
				  {
				      log_error( ERR_ERROR, TYPE_ERROR,
						 E_WRONG_TYPE, NULL, stat->line );
				  }
			      }
			  }
			  else
			  {
			      if ( actual_block->tag != PROC )
			      {
				  log_error( ERR_ERROR, SEMANTIC_ERROR,
					     E_NO_PARAM_IN_FUNC_RETURN,
					     NULL, stat->line );
			      }
			  }
			  break;

	    case FAIL :   if ( stat->tree.failexpr )
			  {
			      RETURN_ERR( type_expr( stat->tree.failexpr ) );

			      if ( ! type_is_a( stat->tree.failexpr, IntType, NULL ) )
			      {
				  log_error( ERR_ERROR, TYPE_ERROR, E_PARAM_TYPE, "#1=INTEGER", stat->line );
			      }
			  }
			  else
			      log_error( ERR_WARNING, TYPE_ERROR, E_PARAM_COUNT, NULL, stat->line );
			  break;
	}

	node = node->tree.stats;
    }

    return TRUE;
}


PRIVATE BOOL type_expr( atn_expr *node )
{
    coercion_type coer;

    if ( ! node )  return TRUE;

    switch ( node->tag )
    {
	case EXPR :       RETURN_ERR( type_expr( node->tree.expr.expr1 ) );
			  RETURN_ERR( type_expr( node->tree.expr.expr2 ) );

			  switch ( node->tree.expr.operator )
			  {
			      case REL_EQUAL : if ( ! type_is_a( node->tree.expr.expr1,
								 IntType, RealType, BoolType, StringType, NULL ) )
					       {
						   log_error( ERR_ERROR, TYPE_ERROR, E_WRONG_LHS_TYPE,
							      "REAL, INTEGER, BOOLEAN or STRING", node->line );
					       }
					       if ( ! type_is_a( node->tree.expr.expr2,
								 IntType, RealType, BoolType, StringType, NULL ) )
					       {
						   log_error( ERR_ERROR, TYPE_ERROR, E_WRONG_RHS_TYPE,
							      "REAL, INTEGER, BOOLEAN or STRING", node->line );
					       }
					       coer = type_coercion( node->tree.expr.expr1->type,
								       node->tree.expr.expr2->type );
					       if ( coer != CO_OK )
					       {
						   if ( coer == CO_INTTOREAL )
						   {
						       node->tree.expr.expr1->coercion = coer;
						       node->tree.expr.op_type = REAL;
						       RETURN_ERR( type_expr_post( node->tree.expr.expr1, RealType ) );
						   }
						   else
						   {
						       if ( ( node->tree.expr.expr2->coercion =
			 			         	    type_coercion( node->tree.expr.expr2->type,
			 			         			   node->tree.expr.expr1->type ) ) != CO_INTTOREAL )
			 			       {
			 			           log_error( ERR_ERROR, TYPE_ERROR, E_WRONG_TYPE, NULL, node->line );
			 			       }
			 			       else
			 			       {
			 			           node->tree.expr.op_type = REAL;
			 			           RETURN_ERR( type_expr_post( node->tree.expr.expr2, RealType ) );
						       }
			 		           }
			 		       }
			 		       else
						   node->tree.expr.op_type = type_realtype( node->tree.expr.expr1 );

					       node->type = BoolType;
					       break;
			      case REL_LOWER :
			      case REL_LEQ :
			      case REL_GEQ :
			      case REL_GREATER : if ( ! type_is_a( node->tree.expr.expr1,
			      					   IntType, RealType, BoolType, NULL ) )
			  			 {
			  			     log_error( ERR_ERROR, TYPE_ERROR, E_WRONG_LHS_TYPE,
			  			                "REAL, INTEGER or BOOLEAN", node->line );
			  			 }
			  			 if ( ! type_is_a( node->tree.expr.expr2,
			  			 		   IntType, RealType, BoolType, NULL ) )
			 			 {
			 			     log_error( ERR_ERROR, TYPE_ERROR, E_WRONG_RHS_TYPE,
								"REAL, INTEGER or BOOLEAN", node->line );
						 }
						 coer = type_coercion( node->tree.expr.expr1->type,
								       node->tree.expr.expr2->type );
						 if ( coer != CO_OK )
						 {
						     if ( coer == CO_INTTOREAL )
						     {
							 node->tree.expr.expr1->coercion = coer;
							 node->tree.expr.op_type = REAL;
			 			         RETURN_ERR( type_expr_post( node->tree.expr.expr1, RealType ) );
			 			     }
			 			     else
			 			     {
			 			         if ( ( node->tree.expr.expr2->coercion =
			 			         	    type_coercion( node->tree.expr.expr2->type,
			 			         			   node->tree.expr.expr1->type ) ) != CO_INTTOREAL )
			 			         {
			 			             log_error( ERR_ERROR, TYPE_ERROR, E_WRONG_TYPE, NULL, node->line );
			 			         }
			 			         else
			 			         {
			 			             node->tree.expr.op_type = REAL;
			 			             RETURN_ERR( type_expr_post( node->tree.expr.expr2, RealType ) );
			 			         }
			 			     }
						 }
			 			 else
			 			     node->tree.expr.op_type = type_realtype( node->tree.expr.expr1 );
			 			 node->type = BoolType;
			 			 break;
			      case OP_MOD : if ( ! type_is_a( node->tree.expr.expr1, IntType, NULL ) )
			      		    {
			      		        log_error( ERR_ERROR, TYPE_ERROR, E_WRONG_LHS_TYPE, "INTEGER", node->line );
			      		    }
			      		    if ( ! type_is_a( node->tree.expr.expr2, IntType, NULL ) )
			      		    {
			      		        log_error( ERR_ERROR, TYPE_ERROR, E_WRONG_RHS_TYPE, "INTEGER", node->line );
			      		    }
			      		    node->tree.expr.op_type = INTEGER;  /* was sonst... */
			      		    node->type = IntType;
			      		    break;
			      case OP_CONCAT : if ( ! type_is_a( node->tree.expr.expr1, StringType, NULL ) )
			      		       {
			      		           log_error( ERR_ERROR, TYPE_ERROR, E_WRONG_LHS_TYPE, "STRING", node->line );
			      		       }
					       if ( ! type_is_a( node->tree.expr.expr2, StringType, NULL ) )
			      		       {
			      		           log_error( ERR_ERROR, TYPE_ERROR, E_WRONG_RHS_TYPE, "STRING", node->line );
			      		       }
			      		       node->tree.expr.op_type = STRING;  /* was sonst... */
			      		       node->type = StringType;
			      		       break;
			      case OP_ADD :
			      case OP_MINUS :
			      case OP_MULT :
			      case OP_DIV :   if ( ! type_is_a( node->tree.expr.expr1, RealType, IntType, NULL ) )
			      		      {
			      		          log_error( ERR_ERROR, TYPE_ERROR, E_WRONG_LHS_TYPE, "INTEGER or REAL", node->line );
			      		      }
			      		      if ( ! type_is_a( node->tree.expr.expr2, RealType, IntType, NULL ) )
			      		      {
			      		          log_error( ERR_ERROR, TYPE_ERROR, E_WRONG_RHS_TYPE, "INTEGER or REAL", node->line );
			      		      }
			      		      coer = type_coercion( node->tree.expr.expr1->type,
			      		      			    node->tree.expr.expr2->type );
			      		      if ( coer != CO_OK )
			      		      {
			      		          node->tree.expr.op_type = REAL;
			      		          node->type = RealType;
			      		          if ( coer == CO_INTTOREAL )
			      		          {
			      		              node->tree.expr.expr1->coercion = coer;
			      		              type_expr_post( node->tree.expr.expr1, RealType );
			      		          }
			      		          else
			      		          {
			      		              if ( ( node->tree.expr.expr2->coercion =
			      		              		 type_coercion( node->tree.expr.expr2->type,
			      		              				node->tree.expr.expr1->type ) ) != CO_INTTOREAL )
			      		              {
			      		                  log_error( ERR_ERROR, TYPE_ERROR, E_WRONG_TYPE, NULL, node->line );
			 			      }
			 			      else
			 			          RETURN_ERR( type_expr_post( node->tree.expr.expr2, RealType ) );
			      		          }
			      		      }
			      		      else
			      		      {
			      		          node->tree.expr.op_type = type_realtype( node->tree.expr.expr1 );
			      		          node->type = node->tree.expr.expr1->type;
			      		      }
			      		      
			      		      break;
			      case OP_NOT : if ( node->tree.expr.expr2 )
			                    {
			                        log_error( ERR_ABORT, SYSTEM_ERROR, E_ILLEGAL_POINTER, NULL, node->line );
			                    }
			                    if ( ! type_is_a( node->tree.expr.expr1, BoolType, NULL ) )
			                    {
			                        log_error( ERR_ERROR, TYPE_ERROR, E_WRONG_RHS_TYPE, "BOOLEAN", node->line );
			                    }
			                    node->tree.expr.op_type = BOOLEAN;  /* was sonst... */
			                    node->type = BoolType;
			                    break;
			      case OP_ERROR : log_error( ERR_ABORT, SYSTEM_ERROR, E_ILLEGAL_TAG, "expr", node->line );
			  }
  
			  break;

	case IFTHENELSE : RETURN_ERR( type_expr( node->tree.ifthenelse.expr_if ) );
			  /* ifthenelse.expr_if->type = BOOLEAN? */
			  RETURN_ERR( type_expr( node->tree.ifthenelse.expr_then ) );
			  RETURN_ERR( type_expr( node->tree.ifthenelse.expr_else ) );
			  coer = type_coercion( node->tree.ifthenelse.expr_then->type,
			  			node->tree.ifthenelse.expr_else->type );
			  if ( coer != CO_OK )
			  {
			      node->type = RealType;
			      if ( coer == CO_INTTOREAL )
			      {
			          node->tree.ifthenelse.expr_then->coercion = coer;
			          RETURN_ERR( type_expr_post( node->tree.ifthenelse.expr_then, RealType ) );
			      }
			      else
			      {
			          if ( ( node->tree.ifthenelse.expr_else->coercion =
			          		type_coercion( node->tree.ifthenelse.expr_else->type,
			          			       node->tree.ifthenelse.expr_then->type ) ) != CO_INTTOREAL )
			          {
			              log_error( ERR_ERROR, TYPE_ERROR, E_WRONG_TYPE, NULL, node->line );
			              node->type = node->tree.ifthenelse.expr_then->type;
			          }
			          else
			              RETURN_ERR( type_expr_post( node->tree.ifthenelse.expr_else, RealType ) );
			      }
			  }
			  else
			      node->type = node->tree.ifthenelse.expr_then->type;
			  break;

	case FUNCALL :    RETURN_ERR( type_name( node->tree.funcall.name ) );
			  RETURN_ERR( type_actuals( node->tree.funcall.actuals ) );
			  if ( node->tree.funcall.name->object->tag != DECL )
			  {
			      log_error( ERR_ERROR, SEMANTIC_ERROR, E_NO_FUNC_OR_PROC, NULL, node->line );
			  }
			  else
			  {
			      node->type = node->tree.funcall.name->object->tree.type;

			      /* now we have to verify if the actuals match the formals */
			      /* and check VARs etc. */
			      RETURN_ERR( type_checkparams( node->tree.funcall.name->object->tree.formals,
							    node->tree.funcall.actuals, node->line ) );
			  }
			  break;

	case FORMAT :     RETURN_ERR( type_expr( node->tree.formatexpr ) );
			  /* any input type allowed! */
			  node->type = StringType;
			  break;

	case EINDEX :     RETURN_ERR( type_index( node->tree.index ) );
			  node->type = node->tree.index->type;
			  break;

	case INTCONST :    node->type = IntType;
			   break;
	case REALCONST :   node->type = RealType;
			   break;
	case BOOLCONST :   node->type = BoolType;
			   break;
	case STRINGCONST : node->type = StringType;
			   break;

	default :         ;
    }

    return TRUE;
}


PRIVATE BOOL type_actuals( atn_actuals *node )
{
    for ( ;  node != NULL;  node = node->tree.actuals )
    {
	RETURN_ERR( type_expr( node->tree.expr ) );
    }

    return TRUE;
}


PRIVATE BOOL type_index( atn_index *node )
{
    UINT8    depth;
    INT8      diff;
    atn_index *ptr;
    atn_type *type;
    INT8  refdepth;
    INT8 i, j;

    for ( ptr = node,  depth = 0;
          ptr->tag == INDEX;
          ptr = ptr->tree.index.index,  depth ++ )
    {
	RETURN_ERR( type_expr( ptr->tree.index.expr ) );
	
	if ( ! type_is_a( ptr->tree.index.expr, IntType, NULL ) )
	{
	    log_error( ERR_ERROR, TYPE_ERROR, E_NOT_INDEX_TYPE, NULL, ptr->line );
	}

        ptr->type = NULL;
    }

    RETURN_ERR( type_name( ptr->tree.name ) );

    if ( ptr->tree.name->object->tag != VARI )
    {
	log_error( ERR_ERROR, SEMANTIC_ERROR, E_NO_VARIABLE, NULL, ptr->line );
    }

    ptr->type = ptr->tree.name->object->tree.type;
    diff = type_arraydepth( ptr->type ) - depth;
    refdepth = type_refdepth( ptr->type );
    
    if ( diff >= 0 )
    {
        type = type_simplify( ptr->type );
	
        for ( j = depth-1; j >= 0; j -- )
        {
            for ( i = 0,  ptr = node;
                  i < j;
                  i ++,   ptr = ptr->tree.index.index );
            
            type = type->tree.array.type;
            ptr->type = type;
        }
        
        node->type = makeref( type, refdepth );
    }
    else if ( diff < 0 )
    {
	log_error( ERR_ERROR, TYPE_ERROR, E_TOO_MANY_INDICES, NULL, node->line );
	gencode_flag = FALSE;
    }
    
    return TRUE;
}


PRIVATE BOOL type_name( atn_name *node )
{
    struct tEnv     *env_ptr;
    struct tDecls *decls_ptr;
    UINT32 ident;

    if ( ! node )  return TRUE;

    ident = node->ident;

    for ( env_ptr = actual_block->env;
	  env_ptr != NoEnv;
	  env_ptr = sem_resolvehidden( env_ptr->next ) )
    {
	for ( decls_ptr = env_ptr->decls;
	      decls_ptr != NULL;
	      decls_ptr = decls_ptr->next )
	{
	    if ( decls_ptr->object->ident == ident )
	    {
		node->object = decls_ptr->object;
		return TRUE;
	    }
	}
    }

    for ( env_ptr = sem_resolvehidden( &( actual_block->hidden ) );
	  env_ptr != NoEnv;
	  env_ptr = sem_resolvehidden( env_ptr->next ) )
    {
	for ( decls_ptr = env_ptr->decls;
	      decls_ptr != NULL;
	      decls_ptr = decls_ptr->next )
	{
	    if ( decls_ptr->object->ident == ident )
	    {
		node->object = decls_ptr->object;
		return TRUE;
	    }
	}
    }

    log_error( ERR_ERROR, SEMANTIC_ERROR, E_UNDECLARED, NULL, node->line );
    gencode_flag = FALSE;

    return FALSE;
}


PRIVATE BOOL type_checkparams( atn_formals *formals,
			       atn_actuals *actuals,
			       UINT32       defline )
{
    UINT8 countparam = 0;
    char *errstr;
    UINT32 line = defline;

    while ( formals && actuals )
    {
	countparam ++;

	if ( ( actuals->tree.expr->coercion =
			type_coercion( actuals->tree.expr->type,
				     formals->tree.formal->tree.type ) ) == CO_ERROR )
	{
            RETURN_ERR( errstr = (char *)xmalloc( 4 ) );
	    sprintf( errstr, "%d", countparam );
	    log_error( ERR_ERROR, TYPE_ERROR, E_PARAM_TYPE, errstr, actuals->tree.expr->line );
	}
	else
	{
	    if ( ( type_refdepth( formals->tree.formal->tree.type ) == 2 ) &&
	         ( actuals->tree.expr->tag != EINDEX ) )
	    {
		RETURN_ERR( errstr = (char *)xmalloc( 4 ) );
	        sprintf( errstr, "%d", countparam );
	        log_error( ERR_ERROR, TYPE_ERROR, E_NO_SIMPLE_TYPE_ACTUAL,
	        	   errstr, actuals->tree.expr->line );
	    }
	    else
	    {
		RETURN_ERR( type_expr_post( actuals->tree.expr,
					    formals->tree.formal->tree.type ) );
	    }
	}
	formals = formals->tree.formals;
	line = actuals->tree.expr->line;
	actuals = actuals->tree.actuals;
    }

    if ( formals && !actuals )
	log_error( ERR_ERROR, TYPE_ERROR, E_PARAM_COUNT, NULL, line );
    else if ( !formals && actuals )
	log_error( ERR_ERROR, TYPE_ERROR, E_PARAM_COUNT, NULL, actuals->tree.expr->line );

    return TRUE;
}



/* ================== coercion routines ======================= */

/*
PUBLIC BOOL type_coercable( atn_type *srctype, atn_type *dsttype )
{
    srctype = type_simplify( srctype );
    dsttype = type_simplify( dsttype );

    if ( type_sametype( srctype, dsttype ) )  return TRUE;

    if ( ( srctype->tag == INTEGER ) && ( dsttype->tag == REAL ) )
	return TRUE;

    return FALSE;
}
*/

PRIVATE coercion_type type_coercion( atn_type *srctype, atn_type *dsttype )
{
    srctype = type_simplify( srctype );
    dsttype = type_simplify( dsttype );
    
    if ( type_sametype( srctype, dsttype ) )  return CO_OK;
    
    if ( ( srctype->tag == INTEGER ) && ( dsttype->tag == REAL ) )
        return CO_INTTOREAL;
    
    return CO_ERROR;
}


PRIVATE BOOL type_sametype( atn_type *type1, atn_type *type2 )
{
    type1 = type_simplify( type1 );
    type2 = type_simplify( type2 );

    if ( type1 == type2 )  return TRUE;

    if ( type1->tag != type2->tag )  return FALSE;

    if ( type1->tag == ARRAY )
	return ( type1->tree.array.upb - type1->tree.array.lwb ==
		 type2->tree.array.upb - type2->tree.array.lwb ) &&
	       type_sametype( type1->tree.array.type, type2->tree.array.type );

    return TRUE;
}


PUBLIC atn_type *type_simplify( atn_type *type )
{
    while ( type->tag == REF )
        type = type->tree.reftype;
    return type;
}


PUBLIC UINT8 type_refdepth( atn_type *type )
{
    UINT8 result = 0;

    while ( type->tag == REF )
    {
        type = type->tree.reftype;
        result ++;
    }
    return result;
}


PRIVATE UINT8 type_arraydepth( atn_type *type )
{
    UINT8 result = 0;
    
    type = type_simplify( type );
    while ( type->tag == ARRAY )
    {
        type = type->tree.array.type;
        result ++;
    }
    return result;
}

/*
PRIVATE UINT8 type_indexdepth( atn_index *index )
{
    UINT8 result = 0;
    
    while ( index->tag == INDEX )
    {
        index = index->tree.index.index;
        result ++;
    }
    return result;
}
*/

PUBLIC type_tag type_realtype( atn_expr *node )
{
    atn_type *type = type_simplify( node->type );
    return type->tag;
}


PUBLIC type_tag type_finaltype( atn_expr *node )
{
    if ( node->coercion == CO_INTTOREAL )
        return REAL;
    else
        return type_realtype( node );
}


PUBLIC type_tag type_arraytype( atn_type *type )
{
    type = type_simplify( type );
    while ( type->tag == ARRAY )
        type = type->tree.array.type;
    return type->tag;
}


PRIVATE BOOL type_expr_post( atn_expr *node, atn_type *dsttype )
{
    return TRUE;
}


PRIVATE coercion_type type_assignarray( atn_index *left, atn_expr *expr )
{
    coercion_type coercion = CO_OK;


    atn_type  *left_type = type_simplify( left->type );
    atn_type *right_type = type_simplify( expr->type );

    
    if ( type_arraydepth( left_type ) == 0 )
    {
        if ( ( coercion = type_coercion( right_type, left_type ) ) == CO_ERROR )
	{
	    log_error( ERR_ERROR, TYPE_ERROR, E_WRONG_TYPE, NULL, left->line );
	}
	else
	{
/*	    RETURN_ERR( type_expr_post( stat->tree.assign.expr,            */
/*	      		  		stat->tree.assign.index ->type ) ); */
        }
    }
    else
    {
        if ( ! type_sametype( left_type, right_type ) )
        {
            log_error( ERR_ERROR, TYPE_ERROR, E_WRONG_TYPE, NULL, left->line );
            coercion = CO_ERROR;
        }
/*      else
           coercion = CO_OK; */
    }

    return coercion;
}


PRIVATE BOOL type_is_a( atn_expr *node, ... )
{
    BOOL    result = FALSE;
    va_list     ap;
    atn_type  *arg;
    atn_type *type = type_simplify( node->type );


    va_start( ap, node );

    for ( arg = va_arg( ap, atn_type * );
          ( arg != NULL ) && !result;
          arg = va_arg( ap, atn_type * ) )
    {
        result |= type_sametype( type, arg );
    }
    
    return result;
}


PRIVATE atn_type *makeref( atn_type *type, UINT8 refdepth )
{
    atn_type *result;
    
    if ( refdepth == 0 )  return type;

    result = XALLOCTYPE( atn_type );
    result->tag = REF;
    result->tree.reftype = type;
    result->line = type->line;

    return makeref( result, refdepth - 1 );
}

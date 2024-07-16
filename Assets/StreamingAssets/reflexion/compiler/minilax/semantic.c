#include "macros.h"

#include "import.h"
#include "xmem.h"
#include "error.h"
#include "symtable.h"
#include "abstree.h"

#include "export.h"
#include "semantic.h"


/* ------------------- macros ----------------- */

#define RETURN_ERR( F ) if ( ! ( F ) )  return FALSE

/* -------------- local variables ------------- */

PUBLIC struct tEnv     *NoEnv = NULL;
PUBLIC struct tDecls *NoDecls = NULL;

PRIVATE UINT16 depth = 0;

/* -------------- local functions ------------- */

PRIVATE BOOL sem_decl( atn_decl *node );
PRIVATE BOOL sem_formals( atn_formals *node );
PRIVATE BOOL sem_formal( atn_formal *node );
PRIVATE BOOL sem_decls( atn_decls *node );
PRIVATE BOOL sem_type( atn_type *node );

PRIVATE struct tObject *makeprocdecl( UINT32 ident, atn_formals *formals, atn_type *type );
PRIVATE struct tEnv    *makeenv( struct tDecls *decls, struct tEnvExt *hidden );
PRIVATE struct tObject *makevardecl( UINT32 ident, atn_type *type, BOOL ref );
PRIVATE struct tDecls  *makedecls( UINT32 ident, struct tObject *object, struct tDecls *decls );

PRIVATE BOOL isdeclared( UINT32 ident, struct tDecls *decls );
/* PRIVATE BOOL issimpletype( atn_type *type );
   PRIVATE atn_type *reduce1( atn_type *type ); */


/* ---------------------- implementation ----------------------- */

PUBLIC BOOL sem_analysis()
{
    char func[] = "sem_analysis";

    if ( ! root )
    {
	log_error( ERR_ABORT, SYSTEM_ERROR, E_NO_ROOT, func, 0 );
	return FALSE;
    }

    NoEnv = XALLOCTYPE( struct tEnv );
    NoEnv->decls = NoDecls;
    NoEnv->next  = NULL;

    root->hidden.tag = PTRTOENV;
    root->hidden.tree.envptr = &NoEnv;

    return sem_decl( root );
}


PUBLIC struct tEnv *sem_resolvehidden( struct tEnvExt *hidden )
{
    if ( ! hidden->tree.envptr )  return NULL;  /* bei erster Ausgabe! */

    while ( hidden->tag == PTRTOHIDDEN )
	hidden = hidden->tree.hiddenptr;

    return *( hidden->tree.envptr );
}


PRIVATE BOOL sem_decl( atn_decl *node )
{
    switch ( node->tag )
    {
	case PROC : node->ident = node->tree.proc.name->ident;
		    node->object = makeprocdecl( node->tree.proc.name->ident,
						 node->tree.proc.formals,
						 NULL );
		    node->tree.proc.name->object = node->object;
		    
		    depth ++;

		    if ( node->tree.proc.formals )
		    {
			node->tree.proc.formals->decls_in = NoDecls;
			RETURN_ERR( sem_formals( node->tree.proc.formals ) );
			node->tree.proc.decls->decls_in = node->tree.proc.formals->decls_out;
		    }
		    else
			node->tree.proc.decls->decls_in = NoDecls;

		    RETURN_ERR( sem_decls( node->tree.proc.decls ) );

		    node->env = makeenv( node->tree.proc.decls->decls_out, &( node->hidden ) );

		    node->tree.proc.decls->hidden.tag = PTRTOENV;
		    node->tree.proc.decls->hidden.tree.envptr = &( node->env );
		    
		    depth --;
		    
		    break;

	case FUNC : node->ident = node->tree.func.name->ident;
                    RETURN_ERR( sem_type( node->tree.func.type ) );
		    node->object = makeprocdecl( node->tree.func.name->ident,
						 node->tree.func.formals,
		    				 node->tree.func.type );
		    node->tree.func.name->object = node->object;

		    depth ++;

		    if ( node->tree.func.formals )
		    {
			node->tree.func.formals->decls_in = NoDecls;
                        RETURN_ERR( sem_formals( node->tree.func.formals ) );
                        node->tree.func.decls->decls_in = node->tree.func.formals->decls_out;
		    }
		    else
		        node->tree.func.decls->decls_in = NoDecls;

                    RETURN_ERR( sem_decls( node->tree.func.decls ) );
                    
                    node->env = makeenv( node->tree.func.decls->decls_out, &( node->hidden ) );
                    
                    node->tree.func.decls->hidden.tag = PTRTOENV;
		    node->tree.func.decls->hidden.tree.envptr = &( node->env );

		    depth --;

                    break;
                    
        case VAR :  node->ident = node->tree.var.name->ident;
                    node->object = makevardecl( node->tree.var.name->ident,
                    				node->tree.var.type, TRUE );
                    node->tree.var.name->object = node->object;
                    node->env = NoEnv;
                    RETURN_ERR( sem_type( node->tree.var.type ) );
		    break;
    }
    
    return TRUE;
}


PRIVATE struct tObject *makeprocdecl( UINT32         ident,
				      atn_formals *formals,
				      atn_type       *type )
{
    struct tObject *result;
    
    result = XALLOCTYPE( struct tObject );
    result->tag = DECL;
    result->tree.formals = formals;
    result->tree.type = type;
    result->ident = ident;
    result->depth = depth;
    
    return result;
}


PRIVATE struct tEnv *makeenv( struct tDecls *decls, struct tEnvExt *hidden )
{
    struct tEnv *result;
    
    result = XALLOCTYPE( struct tEnv );
    result->decls = decls;
    result->next  = hidden;
    
    return result;
}


PRIVATE BOOL sem_formals( atn_formals *node )
{
    RETURN_ERR( sem_formal( node->tree.formal ) );
    
    if ( node->tree.formals )
    {
        node->tree.formals->decls_in = makedecls( node->tree.formal->ident,
					  	  node->tree.formal->object,
					  	  node->decls_in );
        RETURN_ERR( sem_formals( node->tree.formals ) );
        node->decls_out = node->tree.formals->decls_out;
    }
    else
    {
        node->decls_out = makedecls( node->tree.formal->ident,
        			     node->tree.formal->object,
        			     node->decls_in );
    }
    
    if ( isdeclared( node->tree.formal->ident, node->decls_in ) )
    {
        log_error( ERR_ERROR, SEMANTIC_ERROR, E_DECLARED_TWICE, NULL, node->tree.formal->line );
	gencode_flag = FALSE;
    }
    
    return TRUE;
}


PRIVATE BOOL isdeclared( UINT32 ident, struct tDecls *decls )
{
    struct tDecls *ptr;
    
    ptr = decls;

    while ( ptr && ( ptr->object->ident != ident ) )
        ptr = ptr->next;
        
    return ( ptr != NULL );
}


PRIVATE BOOL sem_formal( atn_formal *node )
{
    node->ident = node->tree.name->ident;
    node->object = makevardecl( node->tree.name->ident, node->tree.type, FALSE );
    node->tree.name->object = node->object;
    
/*  this seems to be no longer required: ARRAYS may now be passed as arguments! */

/*  if ( ! issimpletype( reduce1( node->tree.type ) ) )
    {
	log_error( ERR_ERROR, SEMANTIC_ERROR, E_NO_SIMPLE_TYPE_FORMAL, NULL, node->line );
	return FALSE;
    } */

    RETURN_ERR( sem_type( node->tree.type ) );
    
    return TRUE;
}


PRIVATE struct tObject *makevardecl( UINT32 ident, atn_type *type, BOOL ref )
{
    struct tObject *result;
    
    result = XALLOCTYPE( struct tObject );
    result->tag = VARI;
    if ( ref )
    {
        result->tree.type = XALLOCTYPE( atn_type );
        result->tree.type->line = type->line;
        result->tree.type->tag = REF;
        result->tree.type->tree.reftype = type;
    }
    else
        result->tree.type = type;
    result->ident = ident;
    result->depth = depth;
    
    return result;
}

/*
PRIVATE BOOL issimpletype( atn_type *type )
{
    while ( type->tag == REF )
        type = type->tree.reftype;
    return ! ( type->tag == ARRAY );
}


PRIVATE atn_type *reduce1( atn_type *type )
{
    while ( type->tag == REF )
        type = type->tree.reftype;
        
    return type;
}
*/

PRIVATE BOOL sem_decls( atn_decls *node )
{
    RETURN_ERR( sem_decl( node->tree.decl ) );
    
    if ( node->tree.decls )
    {
        node->tree.decls->decls_in = makedecls( node->tree.decl->ident,
						node->tree.decl->object,
						node->decls_in );
        RETURN_ERR( sem_decls( node->tree.decls ) );
        node->decls_out = node->tree.decls->decls_out;

        node->tree.decls->hidden.tag = PTRTOHIDDEN;
        node->tree.decls->hidden.tree.hiddenptr = &( node->hidden );
    }
    else
    {
        node->decls_out = makedecls( node->tree.decl->ident,
        			     node->tree.decl->object,
        			     node->decls_in );
    }
    
    node->tree.decl->hidden.tag = PTRTOHIDDEN;
    node->tree.decl->hidden.tree.hiddenptr = &( node->hidden );

    if ( isdeclared( node->tree.decl->ident, node->decls_in ) )
    {
        log_error( ERR_ERROR, SEMANTIC_ERROR, E_DECLARED_TWICE, NULL, node->tree.decl->line );
	gencode_flag = FALSE;
    }
    
    return TRUE;
}


PRIVATE struct tDecls *makedecls( UINT32           ident,
                                  struct tObject *object,
                                  struct tDecls   *decls )
{
    struct tDecls *result;
    
    result = XALLOCTYPE( struct tDecls );
    result->object = object;
    result->next   = decls;
    
    return result;
}


PRIVATE BOOL sem_type( atn_type *node )
{
    if ( node->tag == ARRAY )
    {
        if ( node->tree.array.lwb > node->tree.array.upb )
        {
	    log_error( ERR_ERROR, SEMANTIC_ERROR, E_LWB_GREATER_UPB, NULL, node->line );
	    gencode_flag = FALSE;
        }
        
        RETURN_ERR( sem_type( node->tree.array.type ) );
    }
    
    if ( node->tag == REF )
        RETURN_ERR( sem_type( node->tree.reftype ) );

    return TRUE;
}

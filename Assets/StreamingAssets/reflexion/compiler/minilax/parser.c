#include "macros.h"

/* -------------------------- import -------------------------- */

#include "import.h"
#include "xmem.h"
#include "error.h"
#include "symtable.h"
#include "scanner.h"
#include "abstree.h"

/* -------------------------- export -------------------------- */

#include "export.h"
#include "parser.h"

/* ---------------------------- macros ------------------------ */

#define GET_NEXT_SYMBOL scan_get( &symbol, &merkmal, &line )
#define SYMNAME( SYM ) sym_keytable[SYM].lexem
#define RETURN_ERR( F ) { if ( ! ( F ) ) { xfree(result); return NULL; } }

/* ------------------------ global variables ------------------ */

PRIVATE Symbol        symbol;
PRIVATE Merkmal      merkmal;
PRIVATE UINT32          line;

/* --- local functions --- */

PRIVATE BOOL parse_symbol( Symbol expect );

PRIVATE atn_decl    *parse_prog( void );
PRIVATE atn_decls   *parse_decls( void );
PRIVATE atn_decl    *parse_decl( void );
PRIVATE atn_formals *parse_formals( void );
PRIVATE atn_formals *parse_innerformals( void );
PRIVATE atn_formal  *parse_formal( void );
PRIVATE atn_type    *parse_type( void );
PRIVATE atn_stats   *parse_stats( void );
PRIVATE atn_stat    *parse_stat( void );
PRIVATE atn_stat    *parse_assignorcall( atn_name *name );
PRIVATE atn_actuals *parse_actuals( void );
PRIVATE atn_expr    *parse_expr( void );
PRIVATE atn_expr    *parse_expr2( void );
PRIVATE atn_expr    *parse_expr3( void );
PRIVATE atn_expr    *parse_term( void );
PRIVATE atn_expr    *parse_factor( void );
PRIVATE atn_expr    *parse_varorfunc( atn_name *name );
PRIVATE atn_index   *parse_var( void );
PRIVATE atn_index   *parse_index( atn_index *index_so_far );
PRIVATE atn_name    *parse_name( void );
PRIVATE comptype     parse_addopr( void );
PRIVATE comptype     parse_mulopr( void );
PRIVATE comptype     parse_newopr( void );
PRIVATE comptype     parse_relopr( void );

PRIVATE BOOL         parse_eocmd( void );
PRIVATE atn_type    *makeref( atn_type *refd );
PRIVATE atn_expr    *makeexpr( atn_index *index );
PRIVATE atn_actuals *makeactuals( atn_expr *expr );
PRIVATE atn_index   *makeindex( atn_name *name );


PUBLIC BOOL parse( void )
{
    if ( ! GET_NEXT_SYMBOL )
    {
        log_error( ERR_FATAL, FILE_ERROR, E_FILE_EMPTY, NULL, 0 );
        return FALSE;
    }

    if ( ! ( root = parse_prog() ) )  return FALSE;

    return TRUE;
}


PRIVATE BOOL parse_symbol( Symbol expect )
{
    char func[] = "parse_symbol";

    if ( debug_flag )
    {
        printf( "%s ", sym_keytable[symbol].lexem );

        if ( sym_keytable[symbol].sk != symbol )  /* Konsistenz pruefen */
        {
            log_error( ERR_FATAL, SYSTEM_ERROR, E_SYM_KEYTABLE, func, 0 );
            return FALSE;
        }

        if ( symbol == tok_eocmd )  putchar( '\n' );
    }

    if ( symbol != expect )
    {
        log_error( ERR_ERROR, SEMANTIC_ERROR, E_SYMBOL_EXPECTED, SYMNAME(expect), line );
        return FALSE;
    }
    else
    {                /* --> was tun bei unerwartetem EOF ?! <-- */
    
        GET_NEXT_SYMBOL;
    }

    return TRUE;
}


/* === akzeptieren eines Programms === */

PRIVATE atn_decl *parse_prog( void )
{
    atn_decl *result;

    result = XALLOCTYPE( atn_decl );
    result->line = line;
    result->tag = PROC;
    result->tree.proc.formals = NULL;

    RETURN_ERR( parse_symbol( tok_program ) )
    RETURN_ERR( result->tree.proc.name = parse_name() )
    RETURN_ERR( parse_eocmd() )
    RETURN_ERR( parse_symbol( tok_decl ) )
    RETURN_ERR( result->tree.proc.decls = parse_decls() )
    RETURN_ERR( parse_symbol( tok_begin ) )
    RETURN_ERR( result->tree.proc.stats = parse_stats() )
    
    RETURN_ERR( parse_symbol( tok_end ) )
    RETURN_ERR( parse_symbol( tok_eoprog ) )

    /* hier evtl. Warnung, falls nach END. noch was kommt!!! */

    return result;
}


/* === akzeptieren von Deklarationen === */

PRIVATE atn_decls *parse_decls( void )
{
    atn_decls *result;
    
    result = XALLOCTYPE( atn_decls );

    RETURN_ERR( result->tree.decl = parse_decl() )

    if ( symbol == tok_eocmd )
    {
        RETURN_ERR( parse_eocmd() )
        RETURN_ERR( result->tree.decls = parse_decls() )
    }

    return result;
}


/* === akzeptieren einer einzelnen Deklaration === */

PUBLIC atn_decl *parse_decl( void )
{
    atn_decl *result;
    
    switch ( symbol )
    {
        case tok_ident : /* accept Ident, expect ':' Type */

                result = XALLOCTYPE( atn_decl );
                result->line = line;
                result->tag = VAR;
                RETURN_ERR( result->tree.var.name = parse_name() )
                RETURN_ERR( parse_symbol( tok_istype ) )
                RETURN_ERR( result->tree.var.type = parse_type() )
                break;

        case tok_proc :  /* accept PROCEDURE, expect Name Formals ';' Block */
                         
                result = XALLOCTYPE( atn_decl );
                result->line = line;
                result->tag = PROC;
                RETURN_ERR( parse_symbol( tok_proc ) )
                RETURN_ERR( result->tree.proc.name = parse_name() )
                result->tree.proc.formals = parse_formals(); /* NULL = keine Parameter! */
                RETURN_ERR( parse_eocmd() )
    		RETURN_ERR( parse_symbol( tok_decl ) )
    		RETURN_ERR( result->tree.proc.decls = parse_decls() )
    		RETURN_ERR( parse_symbol( tok_begin ) )
    		RETURN_ERR( result->tree.proc.stats = parse_stats() )
    		RETURN_ERR( parse_symbol( tok_end ) )
                break;

        case tok_func :  /* accept FUNCTION, expect
					Name Formals ':' Type ';' Block */
                result = XALLOCTYPE( atn_decl );
                result->line = line;
                result->tag = FUNC;
                RETURN_ERR( parse_symbol( tok_func ) )
                RETURN_ERR( result->tree.func.name = parse_name() )
                result->tree.func.formals = parse_formals(); /* NULL = keine Parameter! */
                RETURN_ERR( parse_symbol( tok_istype ) )
                RETURN_ERR( result->tree.func.type = parse_type() )
                RETURN_ERR( parse_eocmd() )
    		RETURN_ERR( parse_symbol( tok_decl ) )
    		RETURN_ERR( result->tree.func.decls = parse_decls() )
    		RETURN_ERR( parse_symbol( tok_begin ) )
    		RETURN_ERR( result->tree.func.stats = parse_stats() )
    		RETURN_ERR( parse_symbol( tok_end ) )
                break;
        
        default : log_error( ERR_ERROR, SEMANTIC_ERROR, E_SYMBOL_EXPECTED,
        		     "PROCEDURE, FUNCTION or identifier", line );
                  return NULL;
    }

    return result;
}


/* === akzeptieren von Parametern === */

PRIVATE atn_formals *parse_formals( void )
{
    atn_formals *result = NULL;

    if ( symbol != tok_lbrack )  return NULL;   /* keine Parameter mehr */

    RETURN_ERR( parse_symbol( tok_lbrack ) )
    RETURN_ERR( result = parse_innerformals() )
    RETURN_ERR( parse_symbol( tok_rbrack ) )

    return result;
}

/* --- akzeptieren von Parametern OHNE KLAMMERN --- */

PRIVATE atn_formals *parse_innerformals( void )
{
    atn_formals *result;

    result = XALLOCTYPE( atn_formals );

    RETURN_ERR( result->tree.formal = parse_formal() )

    if ( symbol == tok_eocmd )
    {
        RETURN_ERR( parse_eocmd() )
        RETURN_ERR( result->tree.formals = parse_innerformals() )
    }

    return result;
}


/* === akzeptieren eines Parameters bei der Definition === */

PRIVATE atn_formal *parse_formal( void )
{
    atn_formal *result;
    
    switch ( symbol )
    {
        case tok_ident : /* accept identifier, expect ':' Type */
    
                result = XALLOCTYPE( atn_formal );
                result->line = line;
                RETURN_ERR( result->tree.name = parse_name() )
                RETURN_ERR( parse_symbol( tok_istype ) )
                RETURN_ERR( result->tree.type = makeref( parse_type() ) )
                break;

        case tok_var :   /* accept VAR, expect Name ':' Type */

                result = XALLOCTYPE( atn_formal );
                result->line = line;
                RETURN_ERR( parse_symbol( tok_var ) )
                RETURN_ERR( result->tree.name = parse_name() )
                RETURN_ERR( parse_symbol( tok_istype ) )
                RETURN_ERR( result->tree.type = makeref( makeref( parse_type() ) ) )
                break;

        default: log_error( ERR_ERROR, SEMANTIC_ERROR, E_SYMBOL_EXPECTED,
                            "identifier or VAR", line );
                 return NULL;
    }

    return result;
}

/* --- erstellen einer Referenz auf eine Variable --- */

PRIVATE atn_type *makeref( atn_type *refd )
{
    atn_type *result;

    if ( ! refd )  return NULL;

    result = XALLOCTYPE( atn_type );
    result->line = line;
    result->tag = REF;
    result->tree.reftype = refd;

    return result;
}

/* === akzeptieren eines Typs === */

PRIVATE atn_type *parse_type( void )
{
    atn_type *result;

    switch ( symbol )
    {
        case tok_int:   /* accept INTEGER */
                        result = XALLOCTYPE( atn_type );
                        result->line = line;
                        result->tag = INTEGER;
                        RETURN_ERR( parse_symbol( tok_int ) )
                        break;

        case tok_real:  /* accept REAL */
                        result = XALLOCTYPE( atn_type );
                        result->line = line;
                        result->tag = REAL;
                        RETURN_ERR( parse_symbol( tok_real ) )
                        break;

        case tok_bool:  /* accept BOOLEAN */
                        result = XALLOCTYPE( atn_type );
                        result->line = line;
                        result->tag = BOOLEAN;
                        RETURN_ERR( parse_symbol( tok_bool ) )
                        break;

        case tok_string: /* accept STRING */
                         result = XALLOCTYPE( atn_type );
                         result->line = line;
                         result->tag = STRING;
                         RETURN_ERR( parse_symbol( tok_string ) )
                         break;

        case tok_array: /* accept ARRAY, expect '[' IntConst '..' IntConst ']'
                                                         OF Type */
                        result = XALLOCTYPE( atn_type );
                        result->line = line;
                        result->tag = ARRAY;
                        RETURN_ERR( parse_symbol( tok_array ) )
                        RETURN_ERR( parse_symbol( tok_lidx ) )
                        result->tree.array.lwb = merkmal;
                        RETURN_ERR( parse_symbol( tok_intconst ) )
                        RETURN_ERR( parse_symbol( tok_range ) )
                        result->tree.array.upb = merkmal;
                        RETURN_ERR( parse_symbol( tok_intconst ) )
                        RETURN_ERR( parse_symbol( tok_ridx ) )
                        RETURN_ERR( parse_symbol( tok_of ) )
                        RETURN_ERR( result->tree.array.type = parse_type() )
                        break;

        default:        log_error( ERR_ERROR, SEMANTIC_ERROR, E_SYMBOL_EXPECTED,
                                   "Type (INTEGER, REAL, BOOLEAN or STRING)", line );
                        return NULL;
    }

    return result;
}


/* === akzeptieren von Befehlsfolgen === */

PRIVATE atn_stats *parse_stats( void )
{
    atn_stats *result;

    result = XALLOCTYPE( atn_stats );
    
    RETURN_ERR( result->tree.stat = parse_stat() )

    if ( symbol == tok_eocmd )
    {
        RETURN_ERR( parse_eocmd() )
        RETURN_ERR( result->tree.stats = parse_stats() )
    }

    return result;
}


/* === akzeptieren eines Befehls === */

PRIVATE atn_stat *parse_stat( void )
{
    atn_stat *result = NULL;
    atn_name *tmpname;

    switch ( symbol )
    {
        case tok_ident : /* accept identifier, expect AssignOrCall */
                         RETURN_ERR( tmpname = parse_name() )
                         RETURN_ERR( result = parse_assignorcall( tmpname ) )
                         break;

        case tok_if :    /* accept IF, expect Expr THEN Stats ELSE Stats END */
                         result = XALLOCTYPE( atn_stat );
                         result->line = line;
                         result->tag = IF;
                         RETURN_ERR( parse_symbol( tok_if ) )
                         RETURN_ERR( result->tree.if_.expr = parse_expr() )
                         RETURN_ERR( parse_symbol( tok_then ) )
                         RETURN_ERR( result->tree.if_.stats_then = parse_stats() )
                         RETURN_ERR( parse_symbol( tok_else ) )
                         RETURN_ERR( result->tree.if_.stats_else = parse_stats() )
                         RETURN_ERR( parse_symbol( tok_end ) )
                         break;

        case tok_while : /* accept WHILE, expect Expr DO Stats END */
                         result = XALLOCTYPE( atn_stat );
                         result->line = line;
                         result->tag = WHILE;
                         RETURN_ERR( parse_symbol( tok_while ) )
                         RETURN_ERR( result->tree.while_.expr = parse_expr() )
                         RETURN_ERR( parse_symbol( tok_do ) )
                         RETURN_ERR( result->tree.while_.stats = parse_stats() )
                         RETURN_ERR( parse_symbol( tok_end ) )
                         break;

        case tok_read :  /* accept READ, expect '(' Var ')' */
                         result = XALLOCTYPE( atn_stat );
                         result->line = line;
                         result->tag = READ;
                         RETURN_ERR( parse_symbol( tok_read ) )
                         RETURN_ERR( parse_symbol( tok_lbrack ) )
                         RETURN_ERR( result->tree.call.actuals = makeactuals(makeexpr(parse_var())) )
                         RETURN_ERR( parse_symbol( tok_rbrack ) )
                         break;

        case tok_write : /* accept WRITE(LN), expect '(' Expr ')' */
        case tok_writeln :
                         result = XALLOCTYPE( atn_stat );
                         result->line = line;
                         if ( symbol == tok_write )
                         {
                             result->tag = WRITE;
                             RETURN_ERR( parse_symbol( tok_write ) )
                         }
                         else
                         {
                             result->tag = WRITELN;
                             RETURN_ERR( parse_symbol( tok_writeln ) )
                         }
                         RETURN_ERR( parse_symbol( tok_lbrack ) )
                         RETURN_ERR( result->tree.call.actuals = makeactuals(parse_expr()) )
			 RETURN_ERR( parse_symbol( tok_rbrack ) )
			 break;

	case tok_return: /* accept RETURN, expect [ '(' [ Expr ] ')' ] */
			 result = XALLOCTYPE( atn_stat );
			 result->line = line;
			 result->tag = RETURN;
			 RETURN_ERR( parse_symbol( tok_return ) )
			 if ( symbol == tok_lbrack )
			 {
			     RETURN_ERR( parse_symbol( tok_lbrack ) )
			     if ( symbol != tok_rbrack )
			     {
				 RETURN_ERR( result->tree.returnexpr = parse_expr() )
			     }
			     RETURN_ERR( parse_symbol( tok_rbrack ) )
			 }
			 else
			     log_error( ERR_WARNING, SEMANTIC_ERROR, E_SYMBOL_EXPECTED, "'('", line );
			 break;

	case tok_fail :  /* accept FAIL, expect '(' [ Expr ] ')' */
			 result = XALLOCTYPE( atn_stat );
			 result->line = line;
			 result->tag = FAIL;
			 RETURN_ERR( parse_symbol( tok_fail ) )
			 RETURN_ERR( parse_symbol( tok_lbrack ) )
			 if ( symbol != tok_rbrack )
			 {
			     RETURN_ERR( result->tree.failexpr = parse_expr() )
			 }
			 RETURN_ERR( parse_symbol( tok_rbrack ) )
			 break;

	default :        log_error( ERR_ERROR, SEMANTIC_ERROR, E_SYMBOL_EXPECTED,
				    "IF, WHILE, READ, WRITE, WRITELN, "
				    "RETURN, FAIL or identifier", line );
			 return NULL;
    }

    return result;
}

/* --- makes actals from expression --- */

PRIVATE atn_actuals *makeactuals( atn_expr *expr )
{
    atn_actuals *result;

    if ( ! expr )  return NULL;

    result = XALLOCTYPE( atn_actuals );
    result->tree.expr = expr;
    result->tree.actuals = NULL;
    
    return result;
}

/* --- makes expression from index --- */

PRIVATE atn_expr *makeexpr( atn_index *index )
{
    atn_expr *result;

    if ( ! index )  return NULL;

    result = XALLOCTYPE( atn_expr );
    result->line = line;
    result->tag = EINDEX;
    result->tree.index = index;

    return result;
}


/* === akzeptieren von dem, was auf einen Identifier am Zeilenanfang folgt === */

PRIVATE atn_stat *parse_assignorcall( atn_name *name )
{
    atn_stat *result;

    switch ( symbol )
    {
        case tok_lidx :    /* Index => Zuweisung an Feldvariable:  Index := Expr */

                result = XALLOCTYPE( atn_stat );
                result->line = line;
                result->tag = ASSIGN;
                RETURN_ERR( result->tree.assign.index = parse_index( makeindex( name ) ) )
                RETURN_ERR( parse_symbol( tok_assign ) )
                RETURN_ERR( result->tree.assign.expr = parse_expr() )
                break;

        case tok_assign :  /* kein Index => Zuweisung an Nicht-Feldvariable:  := Expr */
       
                result = XALLOCTYPE( atn_stat );
                result->line = line;
                result->tag = ASSIGN;
                result->tree.assign.index = makeindex( name );
                RETURN_ERR( parse_symbol( tok_assign ) )
                RETURN_ERR( result->tree.assign.expr = parse_expr() )
                break;

	case tok_lbrack :  /* '(' => Prozeduraufruf mit Parametern:  '(' [Actuals] ')' */

		result = XALLOCTYPE( atn_stat );
		result->line = line;
		result->tag = CALL;
		result->tree.call.name = name;
		RETURN_ERR( parse_symbol( tok_lbrack ) )
		if ( symbol != tok_rbrack )
		{
		    RETURN_ERR( result->tree.call.actuals = parse_actuals() )
		}
		RETURN_ERR( parse_symbol( tok_rbrack ) )

		break;

	default :  /* Prozeduraufruf ohne Parameter => hier kein Fehler! */

		result = XALLOCTYPE( atn_stat );
		result->line = line;
		result->tag = CALL;
		result->tree.call.name = name;
		result->tree.call.actuals = NULL;

		/* nicht mehr erlaubt => Fehlermeldung! */
		log_error( ERR_WARNING, SEMANTIC_ERROR, E_SYMBOL_EXPECTED, "'('", line );
    }

    return result;
}

/* --- makes an index from a name --- */

PRIVATE atn_index *makeindex( atn_name *name )
{
    atn_index *result;

    if ( ! name )  return NULL;

    result = XALLOCTYPE( atn_index );
    result->line = line;
    result->tag = NAME;
    result->tree.name = name;

    return result;
}


/* === akzeptieren der Parameter beim Prozeduraufruf === */

PRIVATE atn_actuals *parse_actuals( void )
{
    atn_actuals *result;
    
    result = XALLOCTYPE( atn_actuals );
    
    RETURN_ERR( result->tree.expr = parse_expr() )

    if ( symbol == tok_comma )
    {
        RETURN_ERR( parse_symbol( tok_comma ) )
        RETURN_ERR( result->tree.actuals = parse_actuals() )
    }
    else
        result->tree.actuals = NULL;

    return result;
}


/* === akzeptieren eines Ausdrucks === */

PRIVATE atn_expr *parse_expr( void )
{
    atn_expr *result, *new_expr;

    if ( symbol == tok_if )  /* Sonderfall IF ... THEN ... ELSE ... END */
    {
	result = XALLOCTYPE( atn_expr );
        result->line = line;
        result->tag = IFTHENELSE;
        RETURN_ERR( parse_symbol( tok_if ) )
        RETURN_ERR( result->tree.ifthenelse.expr_if = parse_expr() )
        RETURN_ERR( parse_symbol( tok_then) )
        RETURN_ERR( result->tree.ifthenelse.expr_then = parse_expr() )
        RETURN_ERR( parse_symbol( tok_else) )
        RETURN_ERR( result->tree.ifthenelse.expr_else = parse_expr() )
        RETURN_ERR( parse_symbol( tok_end ) )
        return result;
    }

    /* ansonsten ist es ein "normaler" Ausdruck */

    if ( ! ( result = parse_expr2() ) )  return NULL;

    if ( symbol == tok_relop )
    {                                 /* accept RelOpr, expect Expr2 */
        new_expr = XALLOCTYPE( atn_expr );
        new_expr->line = line;
        new_expr->tag = EXPR;
        new_expr->tree.expr.expr1 = result;
        RETURN_ERR( new_expr->tree.expr.operator = parse_relopr() )
        RETURN_ERR( new_expr->tree.expr.expr2 = parse_expr2() )
        result = new_expr;
    }

    return result;  /* keine Relation, also wird Expr2 zurueckgegeben! */
}


/* === akzeptieren eines einfachen Ausdrucks mit ++ und % === */

PRIVATE atn_expr *parse_expr2( void )
{
    atn_expr *result, *new_expr;

    RETURN_ERR( result = parse_expr3() )

    while ( symbol == tok_newopr )
    {
        new_expr = XALLOCTYPE( atn_expr );
        new_expr->line = line;
        new_expr->tag = EXPR;
        new_expr->tree.expr.expr1 = result;
        RETURN_ERR( new_expr->tree.expr.operator = parse_newopr() )
        RETURN_ERR( new_expr->tree.expr.expr2 = parse_expr3() )
        result = new_expr;
    }

    return result;
}


/* === akzeptieren eines Ausdrucks mit + und - === */

PRIVATE atn_expr *parse_expr3( void )
{
    atn_expr *result, *new_expr;

    RETURN_ERR( result = parse_term() )

    while ( symbol == tok_addopr )
    {
	new_expr = XALLOCTYPE( atn_expr );
        new_expr->line = line;
        new_expr->tag = EXPR;
        new_expr->tree.expr.expr1 = result;
        RETURN_ERR( new_expr->tree.expr.operator = parse_addopr() )
        RETURN_ERR( new_expr->tree.expr.expr2 = parse_term() )
        result = new_expr;
    }

    return result;
}


/* === akzeptieren eines Terms === */

PRIVATE atn_expr *parse_term( void )
{
    atn_expr *result, *new_expr;

    RETURN_ERR( result = parse_factor() )

    while ( symbol == tok_multopr )
    {
        new_expr = XALLOCTYPE( atn_expr );
        new_expr->line = line;
        new_expr->tag = EXPR;
	new_expr->tree.expr.expr1 = result;
        RETURN_ERR( new_expr->tree.expr.operator = parse_mulopr() )
        RETURN_ERR( new_expr->tree.expr.expr2 = parse_factor() )
        result = new_expr;
    }

    return result;
}


/* === akzeptieren eines Faktors === */

PRIVATE atn_expr *parse_factor( void )
{
    atn_expr *result = NULL;
    atn_name *name;

    switch ( symbol )
    {
        case tok_not :       /* accept NOT, expect Factor */
			     result = XALLOCTYPE( atn_expr );
			     result->line = line;
			     result->tag = EXPR;
			     result->tree.expr.operator = OP_NOT;
			     result->tree.expr.expr2    = NULL;
                             RETURN_ERR( parse_symbol( tok_not ) )
			     RETURN_ERR( result->tree.expr.expr1 = parse_factor() )
                             break;

        case tok_lbrack :    /* accept '(', expect Expr ')' */
                             if ( ! parse_symbol( tok_lbrack ) )  return NULL;
                             RETURN_ERR( result = parse_expr() )
                             RETURN_ERR( parse_symbol( tok_rbrack ) )
                             break;

        case tok_ident :     /* accept identifier -> function or variable */
        		     RETURN_ERR( name = parse_name() )
        		     RETURN_ERR( result = parse_varorfunc( name ) )
        		     break;

        case tok_lidx :   /* Variable */
			  result->tag = EINDEX;
        		  RETURN_ERR( result->tree.index = parse_index( makeindex( name ) ) )
        		  break;
        		     
	case tok_intconst :  /* accept integer constant */
			     result = XALLOCTYPE( atn_expr );
			     result->line = line;
			     result->tag = INTCONST;
			     result->tree.intconst = merkmal;
			     RETURN_ERR( parse_symbol( tok_intconst ) )
			     break;

	case tok_realconst : /* accept real constant */
			     result = XALLOCTYPE( atn_expr );
			     result->line = line;
			     result->tag = REALCONST;
			     result->tree.realconst = merkmal;
			     RETURN_ERR( parse_symbol( tok_realconst ) )
			     break;

	case tok_stringconst : /* accept string constant */
			     result = XALLOCTYPE( atn_expr );
			     result->line = line;
			     result->tag = STRINGCONST;
			     result->tree.stringconst = merkmal;
			     RETURN_ERR( parse_symbol( tok_stringconst ) )
			     break;

	case tok_false :     /* accept FALSE */
			     result = XALLOCTYPE( atn_expr );
			     result->line = line;
			     result->tag = BOOLCONST;
			     result->tree.boolconst = FALSE;
			     RETURN_ERR( parse_symbol( tok_false ) )
			     break;

	case tok_true :      /* accept TRUE */
			     result = XALLOCTYPE( atn_expr );
			     result->line = line;
			     result->tag = BOOLCONST;
			     result->tree.boolconst = TRUE;
                             RETURN_ERR( parse_symbol( tok_true ) )
                             break;

        case tok_format :    /* accept FORMAT, expect '(' Expr ')' */
                             result = XALLOCTYPE( atn_expr );
                             result->line = line;
                             result->tag = FORMAT;
                             RETURN_ERR( parse_symbol( tok_format ) )
			     RETURN_ERR( parse_symbol( tok_lbrack ) )
			     RETURN_ERR( result->tree.formatexpr = parse_expr() )
                             RETURN_ERR( parse_symbol( tok_rbrack ) )
                             break;

        default :            log_error( ERR_ERROR, SEMANTIC_ERROR, E_SYMBOL_EXPECTED,
        				"NOT, '(', constant or identifier", line );
                             return NULL;
    }

    return result;
}


/* === akzeptieren einer Variablen ODER einer Funktion (Rest) === */

PRIVATE atn_expr *parse_varorfunc( atn_name *name )
{
    atn_expr *result;
    
    result = XALLOCTYPE( atn_expr );
    result->line = line;

    if ( symbol == tok_lbrack )
    {
        /* function */
	result->tag = FUNCALL;
	result->tree.funcall.name = name;
        RETURN_ERR( parse_symbol( tok_lbrack ) )
        if ( symbol != tok_rbrack )
            RETURN_ERR( result->tree.funcall.actuals = parse_actuals() )
        else
            result->tree.funcall.actuals = NULL;
        RETURN_ERR( parse_symbol( tok_rbrack ) )
    }
    else
    {
        /* variable */
	result->tag = EINDEX;
	RETURN_ERR( result->tree.index = parse_index( makeindex( name ) ) )
    }

    return result;
}


/* === akzeptieren einer Variablen === */

PRIVATE atn_index *parse_var( void )
{
    atn_index *result;
    
    result = XALLOCTYPE( atn_index );
    result->line = line;
    result->tag = NAME;
    
    RETURN_ERR( result->tree.name = parse_name() )
    RETURN_ERR( result = parse_index( result ) )

    return result;
}


/* === akzeptieren einer Indizierung === */

PRIVATE atn_index *parse_index( atn_index *index_so_far )
{
    atn_index *result;

    if ( symbol == tok_lidx )
    {
        result = XALLOCTYPE( atn_index );
        result->line = line;
        result->tag = INDEX;
        result->tree.index.index = index_so_far;
        
        RETURN_ERR( parse_symbol( tok_lidx ) )
        RETURN_ERR( result->tree.index.expr = parse_expr() )
	RETURN_ERR( parse_symbol( tok_ridx ) )

        if ( symbol == tok_lidx )
        {
            RETURN_ERR( result = parse_index( result ) );
        }
    }
    else
        result = index_so_far;

    return result;
}


/* === akzeptieren eines Namens === */

PRIVATE atn_name *parse_name( void )
{
    atn_name *result;
    Merkmal m;
    UINT32  l;

    m = merkmal;
    l = line;

    if ( ! parse_symbol( tok_ident ) )  return FALSE;

    result = XALLOCTYPE( atn_name );
    result->line = l;
    result->ident = m;

    return result;
}


/* === akzeptieren eines '+' oder '-' === */

PRIVATE comptype parse_addopr( void )
{
    Merkmal m = merkmal;

    if ( ! parse_symbol( tok_addopr ) )  return OP_ERROR;
    
    return m;
}


/* === akzeptieren eines '*' oder '/' === */

PRIVATE comptype parse_mulopr( void )
{
    Merkmal m = merkmal;

    if ( ! parse_symbol( tok_multopr ) )  return OP_ERROR;
    
    return m;
}


/* === akzeptieren eines '++' oder '%' === */

PRIVATE comptype parse_newopr( void )
{
    Merkmal m = merkmal;
    
    if ( ! parse_symbol( tok_newopr ) )  return OP_ERROR;

    return m;
}


/* ===  akzeptieren einer Relation === */

PRIVATE comptype parse_relopr( void )
{
    Merkmal m = merkmal;

    if ( ! parse_symbol( tok_relop ) )  return OP_ERROR;

    return m;
}


PRIVATE BOOL parse_eocmd( void )
{
    if ( symbol != tok_eocmd )
        log_error( ERR_WARNING, SEMANTIC_ERROR, E_MISSING_SEMICOLON, NULL, line );
    else
        if ( ! parse_symbol( tok_eocmd ) )  return FALSE;

    return TRUE;
}

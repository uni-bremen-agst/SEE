#ifndef ABSTREE_H
#define ABSTREE_H

/* === Types of the attributed grammar === */

typedef enum { VARI, DECL } object_tag;

struct tObject
{   object_tag tag;
    struct
    {
        struct atn_formals *formals;
        struct atn_type       *type;
    } tree;
    UINT32   ident;
    INT32 location;  /* stack position in VARI, local stack size in DECL */
    INT32    label;  /* label of a DECL */
    UINT16   depth;
};

struct tDecls
{
    struct tObject *object;
    struct tDecls    *next;
};

struct tEnv
{
    struct tDecls  *decls;
    struct tEnvExt  *next;
};

typedef enum { PTRTOENV, PTRTOHIDDEN } envext_tag;

struct tEnvExt
{
    envext_tag tag;
    union
    {
        struct tEnv **envptr;
        struct tEnvExt *hiddenptr;
    } tree;
};


/* === the Decl node === */

typedef enum { PROC, FUNC, VAR } decl_tag;

typedef struct atn_decl
{   decl_tag tag;
    union
    {   struct
        {   struct atn_name    *name;
            struct atn_formals *formals;
            struct atn_decls   *decls;
            struct atn_stats   *stats;
        } proc;
        struct
        {   struct atn_name    *name;
            struct atn_formals *formals;
            struct atn_type    *type;
            struct atn_decls   *decls;
            struct atn_stats   *stats;
        } func;
        struct
        {   struct atn_name    *name;
            struct atn_type    *type;
        } var;
    } tree;

    UINT32           ident;
    struct tObject *object;
    struct tEnv       *env;
    struct tEnvExt  hidden;
    
    UINT32 line;

} atn_decl;
        

/* === the Formals node === */

typedef struct atn_formals
{   struct
    {   struct atn_formal  *formal;
        struct atn_formals *formals;
    } tree;
    
    struct tDecls  *decls_in;
    struct tDecls *decls_out;
} atn_formals;


/* === the Formal node === */

typedef struct atn_formal
{   struct
    {   struct atn_name *name;
        struct atn_type *type;
    } tree;

    UINT32           ident;
    struct tObject *object;
    
    UINT32 line;

} atn_formal;


/* === the Decls node === */

typedef struct atn_decls
{   struct
    {   struct atn_decl  *decl;
        struct atn_decls *decls;
    } tree;

    struct tDecls  *decls_in;
    struct tDecls *decls_out;
    
    struct tEnvExt    hidden;

} atn_decls;


/* === the Type node === */

typedef enum { ERROR, INTEGER, REAL, BOOLEAN, STRING, ARRAY, REF } type_tag;

typedef struct atn_type
{   type_tag tag;
    union
    {   struct
        {   INT16        lwb, upb;
            struct atn_type *type;
        } array;
        struct atn_type *reftype;
    } tree;

    UINT32 line;

} atn_type;


/* === the Stats node === */

typedef struct atn_stats
{   struct
    {   struct atn_stat   *stat;
        struct atn_stats  *stats;
    } tree;
} atn_stats;

/* === the Stat node === */

typedef enum { ASSIGN, CALL, IF, WHILE, RETURN, FAIL,
               READ, WRITE, WRITELN } stat_tag;

typedef struct atn_stat
{   stat_tag tag;
    union
    {   struct
        {   struct atn_index *index;
            struct atn_expr  *expr;
        } assign;
        struct
        {   struct atn_name    *name;
            struct atn_actuals *actuals;
        } call;
        struct
        {   struct atn_expr   *expr;
            struct atn_stats  *stats_then;
            struct atn_stats  *stats_else;
        } if_;
        struct
        {   struct atn_expr  *expr;
            struct atn_stats *stats;
        } while_;
        struct atn_expr *returnexpr;
        struct atn_expr *failexpr;
    } tree;

    UINT32 line;
} atn_stat;

/* === the Actuals node === */

typedef struct atn_actuals
{   struct
    {   struct atn_expr    *expr;
        struct atn_actuals *actuals;
    } tree;
} atn_actuals;

/* === the Expression node === */

typedef enum { CO_OK, CO_ERROR, CO_INTTOREAL } coercion_type;

typedef enum { EXPR, IFTHENELSE, FUNCALL, INTCONST, REALCONST,
               BOOLCONST, STRINGCONST, EINDEX, FORMAT } expr_tag;

typedef struct atn_expr
{   expr_tag tag;
    union
    {   struct
        {   comptype operator;
            type_tag op_type;
            struct atn_expr *expr1;
            struct atn_expr *expr2;
        } expr;
        struct
        {   struct atn_expr *expr_if;
            struct atn_expr *expr_then;
            struct atn_expr *expr_else;
        } ifthenelse;
        struct
        {   struct atn_name    *name;
            struct atn_actuals *actuals;
        } funcall;
        INT32  intconst;
        Merkmal realconst;
        BOOL   boolconst;
        Merkmal  stringconst;
        struct atn_expr *formatexpr;
        struct atn_index *index;
    } tree;

    atn_type *type;
    coercion_type coercion; 
    UINT32 line;
} atn_expr;

/* === the Index node === */

typedef enum { INDEX, NAME } index_tag;

typedef struct atn_index
{   index_tag tag;
    union
    {   struct
        {   struct atn_index *index;
            struct atn_expr  *expr;
        } index;
        struct atn_name *name;
    } tree;

    struct atn_type *type;
    coercion_type coercion;
    UINT32 line;
} atn_index;

/* === the Name node === */

typedef struct atn_name
{   UINT32 ident;

    struct tObject *object;
    UINT32 line;
} atn_name;

/* ------------------ global variables ------------------- */

extern atn_decl *root;

extern struct tEnv     *NoEnv;
extern struct tDecls *NoDecls;

#endif

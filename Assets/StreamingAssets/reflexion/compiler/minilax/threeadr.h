#ifndef THREEADR_H
#define THREEADR_H

typedef enum
{   A3_NO_OP,
    
    A3_ASSIGN,
    A3_UNARY_OP,
    A3_BINARY_OP,
    A3_GOTO,
    A3_COND,

    A3_FRAME,
    A3_JSR,
    A3_RTS,
    A3_HALT,
    A3_POPL,
    A3_POPF,
    A3_PUSHL,
    A3_PUSHF
} a3_stat_type;

typedef enum
{   oNONE       = -1,
    oCLONG  = 0,
    oCFLOAT = 1,
    oVBYTE  = 2,
    oVLONG  = 3,
    oVFLOAT = 4,
    oLABEL  = 5,
    oREG    = 6,
    oSTRING_ID = 7,
    oCLONG_IND  = 8,
    oCFLOAT_IND = 9,
    oVBYTE_IND  = 10,
    oVLONG_IND  = 11,
    oVFLOAT_IND = 12,
    oLABEL_IND  = 13,
    oREG_IND    = 14,
    oSTRING_ID_IND = 15,
/*  oCLONG_IX  = 16, */
/*  oCFLOAT_IX = 17, */
    oVBYTE_IX  = 18,
    oVLONG_IX  = 19,
    oVFLOAT_IX = 20,
/*  oLABEL_IX  = 21, */
    oREG_IX    = 22,
/*  oSTRING_ID_IX = 23, */
/*  oCLONG_IX_IND  = 24, */
/*  oCFLOAT_IX_IND = 25, */
    oVBYTE_IX_IND  = 26,
    oVLONG_IX_IND  = 27,
    oVFLOAT_IX_IND = 28,
/*  oLABEL_IX_IND  = 29, */
    oREG_IX_IND    = 30
/*  oSTRING_ID_IX_IND = 31 */
} a3_argument_type;

typedef struct
{   a3_argument_type type;
    union
    {
	UINT32  l;
	INT32  f[2];
    } val;
} a3_operator;

typedef enum
{   NONE, ADD, SUB, MULT, DIV, MOD, NEG, LNOT, SHL, SHR,
    BAND, BOR, BNOT, INT2FLOAT,
    R_LOWER, R_LEQ, R_EQ, R_GEQ, R_GREATER
} a3_operation_type;

typedef INT32 label_type;

typedef struct multiLabelStruct
{   label_type label;
    struct multiLabelStruct *next;
} multiLabelType;


typedef struct
{   multiLabelType   *labelPtr;
    a3_stat_type     stat_type;
    a3_operator          op[3];
    a3_operation_type  op_type;
} a3_operation;


typedef struct
{   UINT32 firstUse;
    UINT32 lastUse;
} mem_usage_range;


PUBLIC BOOL a3_add_op( a3_stat_type stat_type, ... );

PUBLIC label_type a3_get_label( void );
PUBLIC BOOL a3_set_label( label_type label );

PUBLIC label_type a3_get_mem32l( void );
PUBLIC label_type a3_get_mem32f( void );

PUBLIC BOOL a3_free_mem32l( label_type nr );
PUBLIC BOOL a3_free_mem32f( label_type nr );

PUBLIC BOOL a3_print( void );
PUBLIC BOOL a3_optimize( void );
PUBLIC BOOL a3_codegen( void );

#endif

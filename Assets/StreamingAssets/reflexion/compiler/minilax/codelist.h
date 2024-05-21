#ifndef CODELIST_H
#define CODELIST_H

#ifndef THREEADR_H

typedef INT32 label_type;

typedef struct multiLabelStruct
{   label_type label;
    struct multiLabelStruct *next;
} multiLabelType;

#endif

typedef union
{   UINT32 address;
    INT32 *backpatch;
} backpatch_type;


typedef enum optype_enum { otERROR, otVAL, otREG, otLABEL, otSTRING_ID }  optype;

typedef struct operand
{   enum mode_enum    adrmode;
    enum optype_enum  valtype;
    INT32            value[2];
} operand;

typedef struct operation
{   multiLabelType     *labelPtr;
    enum opword_enum    operator;
    operand           operand[3];
} operation;


PUBLIC BOOL cl_add_op( opword operator, ... );
PUBLIC BOOL cl_add_operator( opword operator );
PUBLIC BOOL cl_add_operand( mode adrmode, ... );

PUBLIC BOOL set_label( label_type label, int codeCount );
PUBLIC label_type cl_get_label( void );
PUBLIC BOOL cl_set_label( label_type label );
PUBLIC BOOL cl_set_label_count( label_type newLabelCount );

PUBLIC UINT32 cl_get_mem32l( void );
PUBLIC UINT32 cl_get_mem32f( void );

PUBLIC BOOL cl_optimize( void );
PUBLIC BOOL cl_dump_code( FILE *out );
PUBLIC BOOL cl_print( void );

#endif

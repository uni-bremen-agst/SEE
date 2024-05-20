#include "macros.h"

#include <stdarg.h>
#include <string.h>

#include "import.h"
#include "xmem.h"
#include "error.h"
#include "constab.h"
#include "cbam.h"

#include "export.h"
#include "codelist.h"

/* ------------------------- variables -------------------------- */

PRIVATE operation     codeList[10000];
PRIVATE UINT32          codeCount = 0;
PRIVATE label_type     labelCount = 0;
PRIVATE UINT32           codeSize = 0;
PRIVATE UINT8             opCount = 0;

char *opnames[] = { "HALT", "MOVB", "MOVS", "MOVL", "MOVF",
		    "PUSHB", "PUSHS", "PUSHL", "PUSHF", "POPL", "POPF",
		    "ADDS", "ADDL", "NEGS", "NEGL", "MATHOP", "NOT",
		    "CPL", "CPF", "SHLL", "SHRL", "BANDL", "BORL", "BNOTL",
		    "JMP", "BR", "BSALL", "BSANY", "BL", "FRAME", "JSR", "RTS" };


/* -------------------- private functions ----------------------- */

PRIVATE BOOL cl_resolve_labels( void );
/*
PRIVATE UINT16 cl_succ( UINT16 start, UINT16 offset );
PRIVATE BOOL cl_append_labels( UINT16 from, UINT16 to );
*/

/* ------------------------- functions -------------------------- */


/* this function is only used by the old stack scheme! */

PUBLIC BOOL cl_add_op( opword operator, ... )
{
    va_list ap;
    UINT8 opCount = 0;
    operation *thisOp;


    thisOp = &codeList[codeCount];
    thisOp->operator = operator;

    va_start( ap, operator );
    thisOp->operand[opCount].adrmode = va_arg( ap, mode );
    
    codeSize += 4;

    while ( ( opCount < 3 ) && ( thisOp->operand[opCount].adrmode != mNONE ) )
    {
        thisOp->operand[opCount].valtype  = va_arg( ap, optype );
        thisOp->operand[opCount].value[0] = va_arg( ap, INT32 );
        if ( ( ( thisOp->operand[opCount].adrmode == mLIT ) &&
             ( ( codeList[codeCount].operator == MOVF ) ||
               ( codeList[codeCount].operator == PUSHF ) ) ) ||
             ( ( thisOp->operand[opCount].adrmode == mIDX ) ||
               ( thisOp->operand[opCount].adrmode == mPOSTIDX_IND ) ||
               ( thisOp->operand[opCount].adrmode == mPREIDX_IND ) ||
               ( thisOp->operand[opCount].adrmode == mIDX_DBLIND ) ) )
        {
            thisOp->operand[opCount].value[1] = va_arg( ap, INT32 );
            codeSize += 4;
        }
        if ( thisOp->operand[opCount].valtype != otREG )
            codeSize += 4;
        opCount ++;
        if ( opCount < 3 )
            thisOp->operand[opCount].adrmode = va_arg( ap, mode );
    }
    
    codeCount++;
    codeList[codeCount].labelPtr = NULL;

    if ( thisOp->operand[0].valtype == otLABEL )
    switch ( operator )   /* work around the 127 jump limit */
    {
        case BR : thisOp->operator = JMP;
        	  break;
        case BSALL :
        case BSANY : codeList[codeCount] = *thisOp;
        	     codeList[codeCount].labelPtr = NULL;
        	     codeList[codeCount].operand[0].valtype = otVAL;
        	     codeList[codeCount].operand[0].value[0] = 8;
        	     codeList[codeCount+1] = *thisOp;
        	     codeList[codeCount+1].operator = JMP;
        	     codeList[codeCount+1].operand[1].adrmode = mNONE;
        	     codeList[codeCount+1].labelPtr = NULL;
        	     thisOp->operator = NOT;
        	     thisOp->operand[0].adrmode = mNONE;
        	     codeCount += 2;
        	     codeSize += 12;
        	     break;
        default : ;
    }
    
    return TRUE;
}


PUBLIC BOOL cl_add_operator( opword operator )
{
    operation *thisOp;
    

    opCount = 0;

    thisOp = &codeList[codeCount];
    thisOp->operator = operator;
    thisOp->operand[0].adrmode = mNONE;
    thisOp->operand[1].adrmode = mNONE;
    thisOp->operand[2].adrmode = mNONE;

    codeSize += 4;
    codeCount ++;
    codeList[codeCount].labelPtr = NULL;

    return TRUE;
}


PUBLIC BOOL cl_add_operand( mode adrmode, ... )
{
    operation *thisOp;
    va_list ap;
    
    thisOp = &codeList[codeCount-1];
    
    va_start( ap, adrmode );

    thisOp->operand[opCount].adrmode = adrmode;
    thisOp->operand[opCount].valtype  = va_arg( ap, optype );
    thisOp->operand[opCount].value[0] = va_arg( ap, INT32 );

    if ( ( ( adrmode == mLIT ) &&
           ( ( codeList[codeCount-1].operator == MOVF ) ||
             ( codeList[codeCount-1].operator == PUSHF ) ) ) ||
         ( ( adrmode == mIDX ) || ( adrmode == mPOSTIDX_IND ) ||
           ( adrmode == mPREIDX_IND ) || ( adrmode == mIDX_DBLIND ) ) )
    {
        thisOp->operand[opCount].value[1] = va_arg( ap, INT32 );
        codeSize += 4;
    }

    if ( thisOp->operand[opCount].valtype != otREG )
        codeSize += 4;

    opCount ++;
    
    return TRUE;
    

/*
    if ( thisOp->operand[0].valtype == otLABEL )
    switch ( operator )   // work around the 127 jump limit
    {
        case BR : thisOp->operator = JMP;
        	  break;
        case BSALL :
        case BSANY : codeList[codeCount] = *thisOp;
        	     codeList[codeCount].labelPtr = NULL;
        	     codeList[codeCount].operand[0].valtype = otVAL;
        	     codeList[codeCount].operand[0].value[0] = 8;
        	     codeList[codeCount+1] = *thisOp;
        	     codeList[codeCount+1].operator = JMP;
        	     codeList[codeCount+1].operand[1].adrmode = mNONE;
        	     codeList[codeCount+1].labelPtr = NULL;
        	     thisOp->operator = NOT;
        	     thisOp->operand[0].adrmode = mNONE;
        	     codeCount += 2;
        	     codeSize += 12;
        	     break;
        default : ;
    }
    
    return TRUE; */
}


PUBLIC BOOL cl_print( void )
{
    operand *op;
    UINT32 i;
    UINT8  j;
    UINT32 currentAddress = 0;

    cl_resolve_labels();

    for ( i = 0; i < codeCount; i ++ )
    {
        if ( codeList[i].operator != NO_OP )
        {
            printf( "%lu : %s ", currentAddress, opnames[codeList[i].operator] );
	    currentAddress += 4;

            for ( j = 0; ( j < 3 ) &&
                         ( codeList[i].operand[j].adrmode != mNONE );
                  j ++ )
            {
                op = &codeList[i].operand[j];
                if ( op->valtype != otREG )  currentAddress += 4;
                if ( ( op->valtype != otVAL ) && ( op->valtype != otREG ) )
                    printf( "[ERROR!] " );
                else
                {
                    switch ( op->adrmode )
                    {
                        case mLIT :    if ( op->valtype == otVAL )
                   		       {
                   		           if ( ( codeList[i].operator == MOVF ) ||
                  		                ( codeList[i].operator == PUSHF ) )
                   		               printf( "#%ld*10^%ld ", op->value[0], op->value[1] );
                   		           else
                   		               printf( "#%ld ", op->value[0] );
                    		       }
                    		       else
                    		           printf( "[mLIT_ERROR!] ");
                    		       break;
                        case mDIRECT : if ( op->valtype == otVAL )
                    		           printf( "%ld ", op->value[0] );
                    		       else
                    		           printf( "R%ld ", op->value[0] );
                    		       break;
                        case mIND : if ( op->valtype == otREG )
                                        printf( "R%ld* ", op->value[0] );
                                    else
                                        printf( "[mIND_ERROR!] ");
                                    break;
                        case mIDX : if ( op->valtype == otREG )
                        	        printf( "R%ld[%ld] ", op->value[0], op->value[1] );
                        	    else
                        	        printf( "[mIDX ERROR!] ");
                        	    currentAddress += 4;
                        	    break;
                        case mPOSTIDX_IND : if ( op->valtype == otREG )
                        		        printf( "R%ld*[%ld] ", op->value[0], op->value[1] );
                        		    else
                        		        printf( "[mPOSTIDX_IND_ERROR!] " );
                        		    currentAddress += 4;
                        		    break;
                        case mPREIDX_IND : if ( op->valtype == otREG )
                        		       printf( "R%ld[%ld]* ", op->value[0], op->value[1] );
                        		   else
                        		       printf( "[mPREIDX_IND_ERROR!] " );
                        		   currentAddress += 4;
                        		   break;
                        case mDBLIND : if ( op->valtype == otREG )
                        		   printf( "R%ld** ", op->value[0] );
                        	       else
                        	           printf( "[mDBLIND_ERROR!] " );
                        	       break;
                        case mIDX_DBLIND : if ( op->valtype == otREG )
                        		       printf( "R%ld*[%ld]* ", op->value[0], op->value[1] );
                        		   else
                                               printf( "[mIDX_DBLIND_ERROR!] " );
                                           currentAddress += 4;
                    		           break;
                        default : printf( "[ERROR!] " );
                    }
                }
            }
            printf( "\n" );
        }
    }

    return TRUE;
}


PUBLIC BOOL cl_dump_code( FILE *out )
{
    UINT32 i;
    UINT8 j;
    UINT32 codeword;
    operand *op;
    
    cl_resolve_labels();

    for ( i = 0; i < codeCount; i ++ )
    {
        if ( codeList[i].operator != NO_OP )
        {
            codeword = (UINT32)(codeList[i].operator) << 27;

            for ( j = 0; ( j < 3 ) &&
                         ( codeList[i].operand[j].adrmode != mNONE );
                  j ++ )
            {
                op = &codeList[i].operand[j];
                if ( ( op->valtype != otVAL ) && ( op->valtype != otREG ) )
                    log_error( ERR_ABORT, SYSTEM_ERROR, E_ILLEGAL_OPERAND, NULL, 0 );
                else
                {
		    codeword |= (UINT32)(op->adrmode) << ( 5 + 8*(2-j) );

		    if ( op->valtype == otREG )
                    {
                        codeword |= (UINT32)1 << ( 24 + 2-j );
                        codeword |= (UINT32)(op->value[0]) << ( 8 * (2-j) );
                    }
                }
            }

	    fprintf( out, "%lu\n", codeword );

	    if ( ( codeList[i].operator == PUSHF )
	         && ( codeList[i].operand[0].adrmode == mLIT ) )
	    {
	        fprintf( out, "F%ldE%ld\n", (INT32)codeList[i].operand[0].value[0],
	                                    (INT32)codeList[i].operand[0].value[1] );
	    }
	    else if ( ( codeList[i].operator == MOVF ) &&
	              ( codeList[i].operand[1].adrmode == mLIT ) )
	    {
	        op = &codeList[i].operand[0];
	        if ( op->valtype == otVAL )
	            fprintf( out, "%lu\n", (UINT32)codeList[i].operand[0].value[0] );
                if ( ( op->adrmode == mIDX ) ||
	             ( op->adrmode == mPOSTIDX_IND ) ||
	             ( op->adrmode == mPREIDX_IND ) ||
	             ( op->adrmode == mIDX_DBLIND ) )
	        {
	            fprintf( out, "%lu\n", (UINT32)codeList[i].operand[0].value[1] );
	        }

	        fprintf( out, "F%ldE%ld\n", (INT32)codeList[i].operand[1].value[0],
	    				    (INT32)codeList[i].operand[1].value[1] );
	    }
	    else
	    {
                for ( j = 0; ( j < 3 ) &&
                             ( codeList[i].operand[j].adrmode != mNONE );
                      j ++ )
                {
	            op = &codeList[i].operand[j];
	            if ( op->valtype == otVAL )
	                fprintf( out, "%lu\n", (UINT32)op->value[0] );

	            if ( ( op->adrmode == mIDX ) ||
	                 ( op->adrmode == mPOSTIDX_IND ) ||
	                 ( op->adrmode == mPREIDX_IND ) ||
	                 ( op->adrmode == mIDX_DBLIND ) )
	            {
	                fprintf( out, "%lu\n", (UINT32)op->value[1] );
	            }
	        }
            }
        }
    }

    return TRUE;
}


PUBLIC BOOL cl_set_label( label_type label )
{
  return set_label (label, codeCount);
}

PUBLIC BOOL set_label( label_type label, int codeCount )
{
    multiLabelType *newLabel;
    
    newLabel = XALLOCTYPE( multiLabelType );
    newLabel->label = label;
    newLabel->next = codeList[codeCount].labelPtr;
    codeList[codeCount].labelPtr = newLabel;

    return TRUE;
}


PUBLIC label_type cl_get_label( void )
{
    labelCount ++;
    return labelCount;
}


PUBLIC BOOL cl_set_label_count( label_type newLabelCount )
{
    labelCount = newLabelCount;
    return TRUE;
}


PRIVATE BOOL cl_resolve_labels( void )
{
    backpatch_type *labelList;
    multiLabelType *labelPtr;
    UINT32 i;
    UINT8 j;

    UINT32 currentAddress = 0;


    if ( ! labelCount )  return TRUE;

    labelList = (backpatch_type *)xcalloc( labelCount+1, sizeof( backpatch_type ) );

    for ( i = 0; i <= codeCount; i ++ )   /* also treat the stringLabel! */
    {
        if ( ( codeList[i].operator != NO_OP ) || ( i == codeCount ) )
        {
            /* first treat the label for this address */

            for ( labelPtr = codeList[i].labelPtr;
                  labelPtr;
                  labelPtr = labelPtr->next )
            {
                if ( labelList[labelPtr->label].backpatch )
                    *( labelList[labelPtr->label].backpatch ) = currentAddress;
                labelList[labelPtr->label].address = currentAddress;
            }

            currentAddress += 4;
        
            /* now treat labels in the operands */
        
            for ( j = 0;  ( j < 3 ) &&
                          ( codeList[i].operand[j].adrmode != mNONE );
                  j ++ )
            {
                if ( codeList[i].operand[j].valtype == otLABEL )
                {
                    if ( labelList[codeList[i].operand[j].value[0]].address )
                    {
                        codeList[i].operand[j].value[0] =
                            labelList[codeList[i].operand[j].value[0]].address;
                    }
                    else
                    {
                        labelList[codeList[i].operand[j].value[0]].backpatch
                            = &codeList[i].operand[j].value[0];
                    }
                    codeList[i].operand[j].valtype = otVAL;
                }
                else if ( codeList[i].operand[j].valtype == otSTRING_ID )
		{
                    codeList[i].operand[j].value[0]
                        = codeSize + ctab_get_stroff( codeList[i].operand[j].value[0] );
                    codeList[i].operand[j].valtype = otVAL;
                }
                if ( codeList[i].operand[j].valtype != otREG )
                    currentAddress += 4;
                if ( ( codeList[i].operand[j].adrmode == mIDX ) ||
                     ( codeList[i].operand[j].adrmode == mPOSTIDX_IND ) ||
                     ( codeList[i].operand[j].adrmode == mPREIDX_IND ) ||
                     ( codeList[i].operand[j].adrmode == mIDX_DBLIND ) )
		{
                    currentAddress += 4;
                }
                if ( ( ( codeList[i].operator == MOVF ) ||
                       ( codeList[i].operator == PUSHF ) ) &&
                     ( codeList[i].operand[j].adrmode == mLIT ) )
                {
                    currentAddress += 4;
                }
            }
        }
    }

    xfree( labelList );

    labelCount = 0;
    
    return TRUE;
}


PRIVATE UINT16 cl_succ( UINT16 start, UINT16 offset )
{
    if ( offset <= 0 )  return start;

    start ++;

    do
    {
        while ( codeList[start].operator == NO_OP )
            start ++;
        offset --;
    } while ( offset && ( start < codeCount ));

    if ( start >= codeCount )  return 0;
    
    return start;
}


PRIVATE BOOL cl_append_labels( UINT16 from, UINT16 to )
{
    multiLabelType *multiLabel;

    if ( codeList[from].labelPtr )
    {
        for ( multiLabel = codeList[from].labelPtr;
              multiLabel->next;
              multiLabel = multiLabel->next );
        
        multiLabel->next = codeList[to].labelPtr;
        codeList[to].labelPtr = codeList[from].labelPtr;
    }
    
    return TRUE;
}


#define OP_EQUAL( X, Y ) ( ( (X).adrmode == (Y).adrmode ) && \
                           ( (X).valtype == (Y).valtype ) && \
                           ( (X).value   == (Y).value ) )

PUBLIC BOOL cl_optimize( void )
{
    int this;
    int next;
    int next2, next3, next4, next5;
    BOOL success;
    
do
{
    success = FALSE;
    if ( debug_flag )  printf( "-> starting loop\n" );

    for ( this = 0; this < codeCount-1; this ++ )
    {
        if ( ( codeList[this].operator == BR ) ||
             ( codeList[this].operator == BSALL ) ||
             ( codeList[this].operator == BSANY ) )
        {
            this += 2;
        }
        
        while ( codeList[this].operator == NO_OP )
            this ++;

	next = cl_succ( this, 1 );
        
        if ( ( codeList[this].operator == NOT ) &&
             ( codeList[next].operator == NOT ) &&
             ( !codeList[this].labelPtr ) &&
             ( !codeList[next].labelPtr ) )
        {
	    if ( debug_flag )  printf( "Opt: NOT NOT -> -8 bytes\n" );
            codeList[this].operator = NO_OP;
	    codeList[next].operator = NO_OP;
	    codeSize -= 8;
        }
        
        if ( ( codeList[this].operator == MOVL ) &&
             ( ! codeList[next].labelPtr ) )
        {
            if ( OP_EQUAL( codeList[this].operand[0], codeList[next].operand[1] ) )
            {
		if ( debug_flag )  printf( "Opt: MOVL type 1 -> -4 bytes\n" );
                codeList[next].operand[1] = codeList[this].operand[1];
                codeList[next].labelPtr = codeList[this].labelPtr;
	        codeList[this].operator = NO_OP;
	        codeSize -= 4;
	        success = TRUE;
            }
            else if ( OP_EQUAL( codeList[this].operand[0], codeList[next].operand[2] ) )
            {
                if ( debug_flag )  printf( "Opt: MOVL type 2 -> -4 bytes\n" );
                codeList[next].operand[2] = codeList[this].operand[1];
                codeList[next].labelPtr = codeList[this].labelPtr;
                codeList[this].operator = NO_OP;
                codeSize -= 4;
                success = TRUE;
            }
/*            else if ( ( codeList[next].operator == PUSHL ) &&
                      ( OP_EQUAL( codeList[this].operand[0], codeList[next].operand[0] ) ) )
            {
                if ( debug_flag)  printf( "Opt: PUSHL -> -4 bytes\n" );
                codeList[next].operand[0] = codeList[this].operand[1];
                codeList[next].labelPtr = codeList[this].labelPtr;
	        codeList[this].operator = NO_OP;
	        codeSize -= 4;
	        success = TRUE;
            }
*/            else if ( OP_EQUAL( codeList[this].operand[0], codeList[this].operand[1] ) )
            {
                codeList[this].operator = NO_OP;
                if ( codeList[this].operand[0].valtype == otREG )
		{
                    codeSize -= 4;
                    if ( debug_flag )  printf( "Opt: MOVL x x -> -4 bytes\n" );
                }
                else
                {
                    codeSize -= 12;
                    if ( debug_flag )  printf( "Opt: MOVL x x -> -12 bytes\n" );
                }
		cl_append_labels( this, next );
            }
        }
        
        if ( codeList[this].operator == ADDL )
        {
            if ( ( codeList[this].operand[1].adrmode == mLIT ) &&
                 ( codeList[this].operand[1].valtype == otVAL ) &&
                 ( codeList[this].operand[1].value[0] == 0 ) )
            {
                if ( debug_flag )  printf( "Opt: ADD #0 -> -4 bytes\n" );
                codeList[this].operator = MOVL;
                codeList[this].operand[1] = codeList[this].operand[2];
                codeList[this].operand[2].adrmode = mNONE;
                codeSize -= 4;
                success = TRUE;
            }
            else if ( ( codeList[this].operand[2].adrmode == mLIT ) &&
                      ( codeList[this].operand[2].valtype == otVAL ) &&
                      ( codeList[this].operand[2].value[0] == 0 ) )
            {
                if ( debug_flag )  printf( "Opt: ADD #0 -> -4 bytes\n" );
                codeList[this].operator = MOVL;
                codeList[this].operand[2].adrmode = mNONE;
                codeSize -= 4;
                success = TRUE;
            }
            else if ( ( codeList[this].operand[1].adrmode == mLIT ) &&
                      ( codeList[this].operand[1].valtype == otVAL ) &&
                      ( codeList[this].operand[2].adrmode == mLIT ) &&
                      ( codeList[this].operand[2].valtype == otVAL ) )
            {
                if ( debug_flag )  printf( "Opt: ADD # # -> -4 bytes\n" );
                codeList[this].operator = MOVL;
                codeList[this].operand[1].value[0] += codeList[this].operand[2].value[0];
                codeList[this].operand[2].adrmode = mNONE;
                codeSize -= 4;
                success = TRUE;
            }
        }
        
        if ( ( codeList[this].operator == PUSHL ) &&
             ( codeList[next].operator == PUSHL ) )
        {
                                         /* PUSHL R0 */
            next2 = cl_succ( next, 1 );  /* something */
            next3 = cl_succ( next2, 1 );  /* POPL */
            next4 = cl_succ( next3, 1 );  /* POPL */
            next5 = cl_succ( next4, 1 );  /* xxx ...R29*... */
            
            if ( next5 && ( codeList[next3].operator == POPL ) &&
                 ( codeList[next4].operator == POPL ) )
            {
		/* test for labels */
		
		if ( ( codeList[next].labelPtr ) || ( codeList[next2].labelPtr ) ||
		     ( codeList[next3].labelPtr ) || ( codeList[next4].labelPtr ) ||
		     ( codeList[next5].labelPtr ) )
		    break;

                if ( debug_flag )  printf("Opt: PUSHL - POPL -> -12 bytes\n" );
                
                if ( ( codeList[next5].operand[1].adrmode == mIND ) &&
                     ( codeList[next5].operand[1].valtype == otREG ) &&
                     ( codeList[next5].operand[1].value[0]   == 29 ) )
                {
                    codeList[next5].operand[1].adrmode = mDIRECT;
                    codeList[next5].operand[1].valtype = otREG;
                    codeList[next5].operand[1].value[0]   = 23;
                }
                else if ( ( codeList[next5].operand[2].adrmode == mIND ) &&
                          ( codeList[next5].operand[2].valtype == otREG ) &&
                          ( codeList[next5].operand[2].value[0]   == 29 ) )
                {
                    codeList[next5].operand[2].adrmode = mDIRECT;
                    codeList[next5].operand[2].valtype = otREG;
                    codeList[next5].operand[2].value[0]   = 23;
                }
                else
                {
                    printf( "-------ERROR!!!!!!!!!!!!-------\n" );
                    printf( "-----> %s \n", opnames[codeList[next5].operator] );
                }
                
                codeList[this].operator = MOVL;
                codeList[this].operand[1] = codeList[this].operand[0];
                codeList[this].operand[0].adrmode = mDIRECT;
                codeList[this].operand[0].valtype = otREG;
                codeList[this].operand[0].value[0]   = 23;
                codeList[this].operand[2].adrmode = mNONE;
                
                codeList[next].operator = NO_OP; 
                codeList[next3].operator = NO_OP;
                codeList[next4].operator = NO_OP;
                
                codeSize -= 12;
                success = TRUE;
            }
        }
    }
} while ( success );

    return TRUE;
}

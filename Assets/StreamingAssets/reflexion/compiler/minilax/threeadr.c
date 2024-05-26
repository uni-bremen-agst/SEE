#include "macros.h"

#include <stdarg.h>
#include <string.h>

#include "import.h"
#include "xmem.h"
#include "error.h"

#include "export.h"
#include "threeadr.h"
#include "cbam.h"
#include "codelist.h"

#define MAX_CODESIZE 100000

/* ------------------------- variables -------------------------- */

PRIVATE a3_operation  codeList[MAX_CODESIZE];
PRIVATE UINT32      codeCount = 0;  /* number of three address statements */
PRIVATE label_type labelCount = 0;  /* number of labels */
PRIVATE UINT32      memLcount = 0;  /* number of temporary long memory units */
PRIVATE UINT32      memFcount = 0;  /* number of temporary float memory units */
PRIVATE mem_usage_range  longMem[MAX_CODESIZE];
PRIVATE mem_usage_range floatMem[MAX_CODESIZE];
PRIVATE UINT8          *registerL;  /* numbers of the registers to use for var. */
PRIVATE UINT8          *registerF;

/* -------------------- private functions ----------------------- */

PRIVATE BOOL a3_print_op( a3_operator op );
PRIVATE BOOL a3_add_operand_code( a3_operator op );

PRIVATE BOOL a3_used_once_and_only_here( INT32 varNr, INT32 start, INT32 stop );
PRIVATE BOOL a3_append_labels( UINT32 from, UINT32 to );


/* ----------------------- implementation ----------------------- */

PUBLIC BOOL a3_add_op( a3_stat_type stat_type, ... )
{
    va_list ap;
    a3_operation  *thisOp;

    if ( codeCount >= MAX_CODESIZE )
    {
        if ( codeCount == MAX_CODESIZE )
            printf( "MAXIMUM CODESIZE EXCEEDED!!!\n" );
        codeCount ++;
    }

    thisOp = &codeList[codeCount];
    thisOp->stat_type = stat_type;
    
    va_start( ap, stat_type );

    /* special treatment for POPL and POPF... */

    if ( ( stat_type == A3_POPL ) || ( stat_type == A3_POPF ) )
    {
        thisOp->op[0].type = oNONE;
        thisOp->op[1].type = oNONE;
        thisOp->op[2].type = oNONE;
        codeCount ++;
        return TRUE;
    }

    /* read operand #0 (always present) */

    thisOp->op[0].type = va_arg( ap, a3_argument_type );
    
    if ( ( thisOp->op[0].type == oCFLOAT ) ||
         ( thisOp->op[0].type & 16 ) )  /* indexed */
    {
        thisOp->op[0].val.f[0] = va_arg( ap, INT32 );
        thisOp->op[0].val.f[1] = va_arg( ap, INT32 );
    }
    else
        thisOp->op[0].val.l = va_arg( ap, INT32 );
    
    /* read operand #1 (sometimes present) */

    if ( ( stat_type != A3_GOTO ) &&
         ( stat_type != A3_JSR ) &&
         ( stat_type != A3_HALT ) )
    {
        thisOp->op[1].type = va_arg( ap, a3_argument_type );
        
        if ( ( thisOp->op[1].type == oCFLOAT ) ||
             ( thisOp->op[1].type & 16 ) )
        {
            thisOp->op[1].val.f[0] = va_arg( ap, INT32 );
            thisOp->op[1].val.f[1] = va_arg( ap, INT32 );
        }
        else
            thisOp->op[1].val.l = va_arg( ap, INT32 );
    }
    else
        thisOp->op[1].type = oNONE;
    
    /* read operand #2 (binary op only) */

    if ( ( stat_type == A3_BINARY_OP ) ||
         ( stat_type == A3_COND ) )
    {
        thisOp->op[2].type = va_arg( ap, a3_argument_type );
        
        if ( ( thisOp->op[2].type == oCFLOAT ) ||
             ( thisOp->op[2].type & 16 ) )
        {
            thisOp->op[2].val.f[0] = va_arg( ap, INT32 );
            thisOp->op[2].val.f[1] = va_arg( ap, INT32 );
        }
        else
            thisOp->op[2].val.l = va_arg( ap, INT32 );
    }
    else
        thisOp->op[2].type = oNONE;
    
    /* read operator, if required */

    if ( ( stat_type == A3_UNARY_OP ) ||
         ( stat_type == A3_BINARY_OP ) ||
         ( stat_type == A3_COND ) )
    {
        thisOp->op_type = va_arg( ap, a3_operation_type );
    }
    else
        thisOp->op_type = NONE;


    codeCount++;
    codeList[codeCount].labelPtr = NULL;

    return TRUE;
}


PUBLIC BOOL a3_set_label( label_type label )
{
  return set_label (label, codeCount);
}


PUBLIC label_type a3_get_label( void )
{
    labelCount ++;
    return labelCount;
}


PUBLIC label_type a3_get_mem32l( void )
{
    memLcount ++;
/*  printf( "L%ld allocated\n", memLcount ); */
    longMem[memLcount].firstUse = codeCount;
    return memLcount;
}


PUBLIC label_type a3_get_mem32f( void )
{
    memFcount ++;
/*  printf( "F%ld allocated\n", memFcount ); */
    floatMem[memFcount].firstUse = codeCount;
    return memFcount;
}


PRIVATE BOOL a3_free ( label_type nr, mem_usage_range Mem[])
{
    if ( Mem[nr].lastUse )
        printf( "ERROR freeing %ld!\n", nr );
    else
    {
        Mem[nr].lastUse = codeCount;
    }
    return TRUE;
}

PUBLIC BOOL a3_free_mem32l( label_type nr )
{
  return a3_free (nr, longMem);
}


PUBLIC BOOL a3_free_mem32f( label_type nr )
{
  return a3_free (nr, floatMem);
}


PRIVATE handle (a3_operation *thisOp) {
  cl_add_operand( mDIRECT, otREG, 24 );
  
  if ( thisOp->op_type == R_EQ )
    cl_add_operator( NOT );
  
  cl_add_operator( BSANY );
  cl_add_operand( mLIT, otVAL, 8 );
  
  switch ( thisOp->op_type )
    {
    case R_LOWER :
    case R_GREATER : cl_add_operand( mLIT, otVAL, 6 );
      cl_add_operand( mLIT, otVAL, 6 );
      break;
    case R_LEQ : 
    case R_GEQ : cl_add_operand( mLIT, otVAL, 4 );
      cl_add_operand( mLIT, otVAL, 4 );
      break;
      
    case R_EQ : cl_add_operand( mLIT, otVAL, 2 );
      cl_add_operand( mLIT, otVAL, 2 );
      break;
    default : printf( "illegal tag - no relation\n" );
    }
  
  cl_add_operator( JMP );
  a3_add_operand_code( thisOp->op[2] );
}

PUBLIC BOOL a3_codegen( void )
{
    INT32 i, j, jsrPos;
    multiLabelType *labelPtr;
    a3_operation *thisOp;
    UINT32        memPosL = 1;  /* current position in the long_mem and */
    UINT32        memPosF = 1;  /*   float_mem memory usage range arrays */
    UINT32        reg_exp[24];  /* address of expiration of registers */
    UINT8 numsave;  UINT8 regsave[24];
    
/*  for ( i = 1; i <= memLcount; i ++ )
        printf( "%lu : %ld - %ld\n", i, longMem[i].firstUse, longMem[i].lastUse );
*/
    cl_set_label_count( labelCount );
    registerL = (UINT8 *)xmalloc( memLcount+1 );
    registerF = (UINT8 *)xmalloc( memFcount+1 );
    memset( registerL, 0, memLcount+1 );
    memset( registerF, 0, memFcount+1 );
    memset( reg_exp, 0, 24 * 4 );

    for ( i = 0; i < codeCount; i ++ )
    {
        thisOp = &codeList[i];
        
        for ( labelPtr = thisOp->labelPtr;
	      labelPtr;
	      labelPtr = labelPtr->next )
	{
	    cl_set_label( labelPtr->label );
	}
	
	/* determine allocated and freed memory */
	
	while ( ( memPosL <= memLcount ) &&
	        ( ( longMem[memPosL].firstUse == i ) ||
	          ( !longMem[memPosL].firstUse ) ) )
	{
	    if ( longMem[memPosL].firstUse )
	    {
	        for ( j = 0; j < 24; j ++ )
	            if ( reg_exp[j] <= i )  /* register no longer used */
	            {
	                registerL[memPosL] = j;
	                reg_exp[j] = longMem[memPosL].lastUse;
	                j = 25;
	            }
	        if ( j == 24 )  printf( "ERROR: ZU WENIG REGISTER!!!!!!!!\n" );
	    }
	    memPosL ++;
	}
	
	while ( ( memPosF <= memFcount ) &&
	        ( floatMem[memPosF].firstUse == i ) )
	{
	    for ( j = 0; j < 24; j += 2 )
	        if ( ( reg_exp[j] <= i ) && ( reg_exp[j+1] <= i ) )
	        {
	            registerF[memPosF] = j;
	            reg_exp[j] = floatMem[memPosF].lastUse;
	            reg_exp[j+1] = floatMem[memPosF].lastUse;
	            j = 25;
	        }
	    if ( j == 24 )  printf( "ERROR: ZU WENIG REGISTER FUER FLOAT!!!!!\n" );
	    memPosF ++;
	}
	
	/* generate code */

        switch ( thisOp->stat_type )
        {
            case A3_ASSIGN :
                if ( thisOp->op[0].type == oVBYTE )
                    cl_add_operator( MOVB );
                else if ( ( thisOp->op[0].type == oVFLOAT ) ||
                          ( thisOp->op[1].type == oVFLOAT ) )
                    cl_add_operator( MOVF );
                else
                    cl_add_operator( MOVL );

                a3_add_operand_code( thisOp->op[0] );
                a3_add_operand_code( thisOp->op[1] );
                break;
                
            case A3_UNARY_OP :
                switch ( thisOp->op_type )
                {
                    case NEG  : if ( ( thisOp->op[0].type & 7 ) != oVFLOAT )
                    	        {
                    	            cl_add_operator( NEGL );
                    		    a3_add_operand_code( thisOp->op[0] );
                    		    a3_add_operand_code( thisOp->op[1] );
                    		}
                    		else
                    		{
                    		    cl_add_operator( MOVF );
                    		    cl_add_operand( mDIRECT, otREG, 24 );
                    		    a3_add_operand_code( thisOp->op[1] );
                    		    cl_add_operator( MATHOP );
                    		    cl_add_operand( mLIT, otVAL, 131 );
                    		    a3_add_operand_code( thisOp->op[0] );
                    		}
                    		break;
                    case BNOT : cl_add_operator( BNOTL );
                    		a3_add_operand_code( thisOp->op[0] );
                    		a3_add_operand_code( thisOp->op[1] );
                    		break;
                    case LNOT : cl_add_operator( BNOTL );
                    		a3_add_operand_code( thisOp->op[0] );
                    		a3_add_operand_code( thisOp->op[1] );
                    		cl_add_operator( BANDL );
                    		a3_add_operand_code( thisOp->op[0] );
                    		a3_add_operand_code( thisOp->op[0] );
                    		cl_add_operand( mLIT, otVAL, 1 );
                    		break;
                    case INT2FLOAT : cl_add_operator( MOVL );
                    		     cl_add_operand( mDIRECT, otREG, 24 );
                    		     a3_add_operand_code( thisOp->op[1] );
                    		     cl_add_operator( MATHOP );
                    		     cl_add_operand( mLIT, otVAL, 193 );
                    		     a3_add_operand_code( thisOp->op[0] );
                    		     break;
                    default : printf( "illegal tag...\n" );
                }
                break;
            
            case A3_BINARY_OP :
                if ( ( thisOp->op[0].type & 7 ) != oVFLOAT )
                {
		    switch ( thisOp->op_type )
		    {
            	        case ADD : cl_add_operator( ADDL );
            	        	   a3_add_operand_code( thisOp->op[0] );
            	        	   a3_add_operand_code( thisOp->op[1] );
            	        	   a3_add_operand_code( thisOp->op[2] );
            	        	   break;
            	        case SUB : cl_add_operator( NEGL );
            	        	   a3_add_operand_code( thisOp->op[0] );
            	        	   a3_add_operand_code( thisOp->op[2] );
            	        	   cl_add_operator( ADDL );
            	        	   a3_add_operand_code( thisOp->op[0] );
            	        	   a3_add_operand_code( thisOp->op[1] );
            	        	   a3_add_operand_code( thisOp->op[0] );
            	        	   break;
            	        case MULT : if ( ( thisOp->op[2].type == oCLONG ) &&
            	                         ( ( thisOp->op[2].val.l == 2 ) ||
            	                           ( thisOp->op[2].val.l == 4 ) ||
            	                           ( thisOp->op[2].val.l == 8 ) ) )
            	        	    {
            	        	        cl_add_operator( MOVL );
            	        	        a3_add_operand_code( thisOp->op[0] );
            	        	        a3_add_operand_code( thisOp->op[1] );
            	        	        for ( j = thisOp->op[2].val.l; j > 1; j >>= 1 )
            	        	        {
            	        	            cl_add_operator( SHLL );
            	        	            a3_add_operand_code( thisOp->op[0] );
            	        	        }
            	        	    }
            	                    else if ( ( thisOp->op[1].type == oCLONG ) &&
            	                              ( ( thisOp->op[1].val.l == 2 ) ||
            	                                ( thisOp->op[1].val.l == 4 ) ||
            	                                ( thisOp->op[1].val.l == 8 ) ) )
            	        	    {
            	        	        cl_add_operator( MOVL );
            	        	        a3_add_operand_code( thisOp->op[0] );
            	        	        a3_add_operand_code( thisOp->op[2] );
            	        	        for ( j = thisOp->op[1].val.l; j > 1; j >>= 1 )
            	        	        {
            	        	            cl_add_operator( SHLL );
            	        	            a3_add_operand_code( thisOp->op[0] );
            	        	        }
            	        	    }
            	        	    else
            	        	    {
            	        	        cl_add_operator( MOVL );
            	        	        cl_add_operand( mDIRECT, otREG, 24 );
            	        	        a3_add_operand_code( thisOp->op[1] );
            	        	        cl_add_operator( MOVL );
            	        	        cl_add_operand( mDIRECT, otREG, 25 );
            	        	        a3_add_operand_code( thisOp->op[2] );
            	        	        cl_add_operator( MATHOP );
            	        	        cl_add_operand( mLIT, otVAL, 3 );
            	        	        a3_add_operand_code( thisOp->op[0] );
            	        	    }
            	        	    break;
            	        case DIV : if ( ( thisOp->op[2].type == oCLONG ) &&
            	                        ( ( thisOp->op[2].val.l == 2 ) ||
            	                          ( thisOp->op[2].val.l == 4 ) ||
            	                           ( thisOp->op[2].val.l == 8 ) ) )
            	        	   {
            	        	       cl_add_operator( MOVL );
            	        	       a3_add_operand_code( thisOp->op[0] );
            	        	       a3_add_operand_code( thisOp->op[1] );
            	        	       for ( j = thisOp->op[2].val.l; j > 1; j >>= 1 )
            	        	       {
            	        	           cl_add_operator( SHRL );
            	        	           a3_add_operand_code( thisOp->op[0] );
            	        	       }
            	        	   }
            	        	   else
            	        	   {
            	        	       cl_add_operator( MOVL );
            	        	       cl_add_operand( mDIRECT, otREG, 24 );
            	        	       a3_add_operand_code( thisOp->op[1] );
            	        	       cl_add_operator( MOVL );
            	        	       cl_add_operand( mDIRECT, otREG, 25 );
            	        	       a3_add_operand_code( thisOp->op[2] );
            	        	       cl_add_operator( MATHOP );
            	        	       cl_add_operand( mLIT, otVAL, 4 );
            	        	       a3_add_operand_code( thisOp->op[0] );
            	        	   }
            	        	   break;
            	        case MOD : cl_add_operator( MOVL );
            	        	   cl_add_operand( mDIRECT, otREG, 24 );
            	        	   a3_add_operand_code( thisOp->op[1] );
            	        	   cl_add_operator( MOVL );
            	        	   cl_add_operand( mDIRECT, otREG, 25 );
            	        	   a3_add_operand_code( thisOp->op[2] );
            	        	   cl_add_operator( MATHOP );
            	        	   cl_add_operand( mLIT, otVAL, 5 );
            	        	   a3_add_operand_code( thisOp->op[0] );
            	        	   break;
            	        case SHL : cl_add_operator( MOVL );
            	        	   a3_add_operand_code( thisOp->op[0] );
            	        	   a3_add_operand_code( thisOp->op[1] );
            	        	   for ( j = thisOp->op[2].val.l; j > 0; j -- )
            	        	   {
            	        	       cl_add_operator( SHLL );
            	        	       a3_add_operand_code( thisOp->op[0] );
            	        	   }
            	        	   break;
            	        case SHR : cl_add_operator( MOVL );
            	        	   a3_add_operand_code( thisOp->op[0] );
            	        	   a3_add_operand_code( thisOp->op[1] );
            	        	   for ( j = thisOp->op[2].val.l; j > 0; j -- )
            	        	   {
            	        	       cl_add_operator( SHRL );
            	        	       a3_add_operand_code( thisOp->op[2] );
            	        	   }
            	        	   break;
            	        case BAND : cl_add_operator( BANDL );
            	        	    a3_add_operand_code( thisOp->op[0] );
            	        	    a3_add_operand_code( thisOp->op[1] );
            	        	    a3_add_operand_code( thisOp->op[2] );
            	        	    break;
            	        case BOR : cl_add_operator( BORL );
            	        	   a3_add_operand_code( thisOp->op[0] );
            	        	   a3_add_operand_code( thisOp->op[1] );
            	        	   a3_add_operand_code( thisOp->op[2] );
            	        	   break;
            	        default : printf( " [illegal binary func] ");
             	    }
             	}
             	else
                {
                    cl_add_operator( MOVF );
            	    cl_add_operand( mDIRECT, otREG, 24 );
            	    a3_add_operand_code( thisOp->op[1] );
            	    cl_add_operator( MOVF );
            	    cl_add_operand( mDIRECT, otREG, 26 );
            	    a3_add_operand_code( thisOp->op[2] );
            	    cl_add_operator( MATHOP );
            	    
            	    switch ( thisOp->op_type )
            	    {
            	        case ADD :  cl_add_operand( mLIT, otVAL, 129 ); break;
            	        case SUB :  cl_add_operand( mLIT, otVAL, 130 ); break;
            	        case MULT : cl_add_operand( mLIT, otVAL, 132 ); break;
            	        case DIV :  cl_add_operand( mLIT, otVAL, 133 ); break;
            	        default : printf( "illegal binary float function!\n" );
            	    }
            	    
 	            a3_add_operand_code( thisOp->op[0] );
 	        }
             	break;
            
            case A3_GOTO :
                cl_add_operator( JMP );
                a3_add_operand_code( thisOp->op[0] );
                break;

            case A3_COND :
                if ( ( ( thisOp->op[0].type & 7 ) == oCFLOAT ) ||
                     ( ( thisOp->op[0].type & 7 ) == oVFLOAT ) ||
                     ( ( thisOp->op[1].type & 7 ) == oCFLOAT ) ||
                     ( ( thisOp->op[1].type & 7 ) == oVFLOAT ) )
                {
                    /* float comparison */
                    
                    if ( ( thisOp->op_type == R_LOWER ) ||
                         ( thisOp->op_type == R_LEQ ) )
                    {
                        cl_add_operator( MOVF );
                        cl_add_operand( mDIRECT, otREG, 24 );
                        a3_add_operand_code( thisOp->op[1] );
                        cl_add_operator( MOVF );
                        cl_add_operand( mDIRECT, otREG, 25 );
                        a3_add_operand_code( thisOp->op[0] );
		    }
		    else
		    {
                        cl_add_operator( MOVF );
                        cl_add_operand( mDIRECT, otREG, 24 );
                        a3_add_operand_code( thisOp->op[0] );
                        cl_add_operator( MOVF );
                        cl_add_operand( mDIRECT, otREG, 25 );
                        a3_add_operand_code( thisOp->op[1] );
                    }
                    cl_add_operator( MATHOP );
                    cl_add_operand( mLIT, otVAL, 130 );

		    handle(thisOp);
                }
                else
                {
                    /* long int comparison */
                    
                    cl_add_operator( NEGL );
                    cl_add_operand( mDIRECT, otREG, 24 );
                    
                    switch ( thisOp->op_type )
                    {
                        case R_LOWER :
                        case R_LEQ :
                        case R_EQ : a3_add_operand_code( thisOp->op[0] );
                    		    cl_add_operator( ADDL );
                    		    cl_add_operand( mDIRECT, otREG, 25 );
                    		    a3_add_operand_code( thisOp->op[1] );
                                    break;
                        case R_GEQ :
                        case R_GREATER : a3_add_operand_code( thisOp->op[1] );
                    		         cl_add_operator( ADDL );
                    		         cl_add_operand( mDIRECT, otREG, 25 );
                    			 a3_add_operand_code( thisOp->op[0] );
                        		 break;
                        default : printf( "illegal tag\n" );
                    }

		    handle(thisOp);
                }
                
           	break;
            
            case A3_FRAME : /* save values */
            		    numsave = 0;
            		    for ( jsrPos = i+1;
            		          codeList[jsrPos].stat_type != A3_JSR;
            		          jsrPos ++ );
            		    
            		    if ( (INT32)codeList[jsrPos].op[0].val.l >= 0 )
            		    {
            		        for ( j = 0; j < 24; j ++ )
            		            if ( reg_exp[j] > jsrPos )
            		            {
            		                cl_add_operator( PUSHL );
            		                cl_add_operand( mDIRECT, otREG, j );
            		                regsave[numsave] = j;
            		                numsave ++;
            		            }
            		        if ( numsave & 1 )
            		        {
            		            cl_add_operator( PUSHL );
            		            cl_add_operand( mDIRECT, otREG, 0 );
            		        }
            		    }
            		    
            		    cl_add_operator( FRAME );
                	    a3_add_operand_code( thisOp->op[0] );
                	    a3_add_operand_code( thisOp->op[1] );
                	    break;
            
            case A3_JSR : cl_add_operator( JSR );
                	  a3_add_operand_code( thisOp->op[0] );
                	  
                	  /* restore values */
            		  if ( numsave > 0 )
            		  {
            		      if ( numsave & 1 )
            		      {
            		          cl_add_operator( POPL );
            		      }
          		      for ( j = numsave-1; j >= 0; j -- )
          		      {
          		          cl_add_operator( POPL );
          		          cl_add_operator( MOVL );
          		          cl_add_operand( mDIRECT, otREG, regsave[j] );
          		          cl_add_operand( mIND, otREG, 29 );
          		      }
          		  }
          		  break;
            
            case A3_RTS : cl_add_operator( RTS );
                	  a3_add_operand_code( thisOp->op[0] );
                	  break;

            case A3_HALT : cl_add_operator( HALT );
            		   a3_add_operand_code( thisOp->op[0] );
            		   break;
            		   
            case A3_POPL : cl_add_operator( POPL );
            		   break;
            		   
            case A3_POPF : cl_add_operator( POPF );
            		   break;
            		   
            case A3_PUSHL : cl_add_operator( PUSHL );
            		    a3_add_operand_code( thisOp->op[0] );
            		    break;
            		    
            case A3_PUSHF : cl_add_operator( PUSHF );
            		    a3_add_operand_code( thisOp->op[0] );
            		    break;
            		  
            case A3_NO_OP : ;
        }
    }

    xfree( registerL );
    xfree( registerF );
    
    return TRUE;
}


PRIVATE BOOL a3_add_operand_code( a3_operator op )
{
    switch ( op.type )
    {
        case oCLONG :  cl_add_operand( mLIT, otVAL, op.val.l ); break;
        case oCFLOAT : cl_add_operand( mLIT, otVAL, op.val.f[0], op.val.f[1] ); break;
        case oVBYTE :
        case oVLONG :  cl_add_operand( mDIRECT, otREG, registerL[op.val.l] ); break;
        case oVFLOAT : cl_add_operand( mDIRECT, otREG, registerF[op.val.l] ); break;
        case oLABEL :  cl_add_operand( mLIT, otLABEL, op.val.l ); break;
        case oREG :    cl_add_operand( mDIRECT, otREG, op.val.l ); break;
        case oSTRING_ID : cl_add_operand( mLIT, otSTRING_ID, op.val.l ); break;

        case oVBYTE_IND :
        case oVLONG_IND :
        case oVFLOAT_IND : cl_add_operand( mIND, otREG, registerL[op.val.l] ); break;
        case oREG_IND :    cl_add_operand( mIND, otREG, op.val.l ); break;

	case oVBYTE_IX :
	case oVLONG_IX :
	case oVFLOAT_IX : cl_add_operand( mIDX, otREG, registerL[op.val.f[0]], op.val.f[1] ); break;
	case oREG_IX :    cl_add_operand( mIDX, otREG, op.val.f[0], op.val.f[1] ); break;
	
	case oVBYTE_IX_IND :
	case oVLONG_IX_IND :
	case oVFLOAT_IX_IND : cl_add_operand( mPREIDX_IND, otREG, registerL[op.val.f[0]], op.val.f[1] ); break;
	case oREG_IX_IND :    cl_add_operand( mPREIDX_IND, otREG, op.val.f[0], op.val.f[1] ); break;
	
	default : printf( "error - illegal operand\n" );
    }
    
    return TRUE;
}


PUBLIC BOOL a3_print( void )
{
    UINT32 i;
    multiLabelType *labelPtr;
    a3_operation *thisOp;
    

    for ( i = 0; i < codeCount; i ++ )
    {
        thisOp = &codeList[i];
        
    if ( thisOp->stat_type != A3_NO_OP )
    {
	if ( thisOp->labelPtr )
	{
	    labelPtr = thisOp->labelPtr;
	    do
	    {
	        printf( "%ld ", labelPtr->label );
	        labelPtr = labelPtr->next;
	    }
	    while ( labelPtr );
	}

        printf( "\t" );

        switch ( thisOp->stat_type )
        {
            case A3_ASSIGN : a3_print_op( thisOp->op[0] );
            		     printf( " := " );
            		     a3_print_op( thisOp->op[1] );
            		     break;
            case A3_UNARY_OP : a3_print_op( thisOp->op[0] );
            		       printf( " := " );
            		       switch ( thisOp->op_type )
            		       {
            		           case NEG : printf( "-( " ); break;
            		           case LNOT : printf( "LNOT( " ); break;
            		           case BNOT : printf( "BNOT( " ); break;
            		           case INT2FLOAT : printf( "INT2FLOAT( " ); break;
            		           default : printf( "[illegal unary func]( " );
            		       }
            		       a3_print_op( thisOp->op[1] );
            		       printf( " )" );
            		       break;
            case A3_BINARY_OP : a3_print_op( thisOp->op[0] );
            			printf( " := " );
            			a3_print_op( thisOp->op[1] );
            			switch ( thisOp->op_type )
            			{
            			    case ADD : printf( " + " ); break;
            			    case SUB : printf( " - " ); break;
            			    case MULT : printf( " * " ); break;
            			    case DIV : printf( " / " ); break;
            			    case MOD : printf( " %% " ); break;
            			    case SHL : printf( " << " ); break;
            			    case SHR : printf( " >> " ); break;
            			    case BAND : printf( " & " ); break;
            			    case BOR : printf( " | " ); break;
            			    default : printf( " [illegal binary func] ");
            			}
            			a3_print_op( thisOp->op[2] );
            			break;
            case A3_GOTO : printf( "goto " );
            		   a3_print_op( thisOp->op[0] );
            		   break;
            case A3_COND : printf( "if " );
            		   a3_print_op( thisOp->op[0] );
            		   switch ( thisOp->op_type )
            		   {
            		       case R_LOWER : printf( " < " ); break;
            		       case R_LEQ : printf( " <= " ); break;
            		       case R_EQ : printf( " == " ); break;
            		       case R_GEQ : printf( " >= " ); break;
            		       case R_GREATER : printf( " > " ); break;
            		       default : printf( " [illegal relation] ");
            		   }
            		   a3_print_op( thisOp->op[1] );
            		   printf( " then goto " );
            		   a3_print_op( thisOp->op[2] );
            		   break;
            case A3_FRAME : printf( "FRAME " );
            		    a3_print_op( thisOp->op[0] );
            		    printf( " " );
            		    a3_print_op( thisOp->op[1] );
            		    break;
            case A3_JSR : printf( "JSR " );
            		  a3_print_op( thisOp->op[0] );
            		  break;
            case A3_RTS : printf( "RTS " );
            		  a3_print_op( thisOp->op[0] );
            		  break;
            case A3_HALT : printf( "HALT " );
            		   a3_print_op( thisOp->op[0] );
            		   break;
            case A3_POPL : printf( "POPL" );
            		   break;
            case A3_POPF : printf( "POPF" );
            		   break;
            case A3_PUSHL : printf( "PUSHL " );
            		    a3_print_op( thisOp->op[0] );
            		    break;
            case A3_PUSHF : printf( "PUSHF " );
            		    a3_print_op( thisOp->op[0] );
            		    break;
            case A3_NO_OP : printf( "ERROR:NO_OP ") ;
        }
        printf( "\n" );
    }
  }
    return TRUE;
}


PRIVATE BOOL a3_print_op( a3_operator op )
{
    if ( op.type & 8 )  /* it's indirected */
        printf( "*" );
    
    switch ( op.type & 7 )
    {
        case oCLONG :  printf( "#%ld", (INT32)op.val.l ); break;
        case oCFLOAT : printf( "#(%ld*10^%ld)", op.val.f[0], op.val.f[1] ); break;
        case oVBYTE :  printf( "Byte(l%ld)", op.val.l ); break;
        case oVLONG :  printf( "l%ld", op.val.l ); break;
        case oVFLOAT : if ( op.type & 24 )  printf( "l%ld", op.val.l );
                                       else printf( "f%ld", op.val.l ); break;
        case oLABEL :  printf( "LABEL_%ld", op.val.l ); break;
        case oREG :    printf( "R%ld", op.val.l ); break;
        case oSTRING_ID : printf( "STRING_%ld", op.val.l ); break;
    }
    
    if ( op.type & 16 )
        printf( "[%ld]", op.val.f[1] );
    
    return TRUE;
}


PUBLIC BOOL a3_optimize( void )
{
    UINT32 i, j, k, index;
    UINT32 start, stop;  /* bounds of a baseblock */
    BOOL found;

    for ( i = 0; i < codeCount; i ++ )
    {
        start = i;
        while ( ( !codeList[i+1].labelPtr ) &&
        	( codeList[i].stat_type != A3_GOTO ) &&
        	( codeList[i].stat_type != A3_COND ) &&
        	( codeList[i].stat_type != A3_JSR ) &&
        	( codeList[i].stat_type != A3_RTS ) &&
        	( codeList[i].stat_type != A3_HALT ) )
        {
            i ++;
        }
        stop = i;
        
        if ( stop > start )
        {
/*          printf( "optimizing %ld - %ld\n", start, stop ); */
            
          do
          {
            found = FALSE;
            
	    /* step one: insert constants and registers directly */

            for ( j = start; j < stop; j ++ )
            {
                if ( ( codeList[j].stat_type == A3_ASSIGN ) &&
                     ( codeList[j].op[0].type == oVLONG ) &&
                     ( ( codeList[j].op[1].type == oCLONG ) ||
                       ( codeList[j].op[1].type == oVLONG ) ||
                       ( codeList[j].op[1].type == oREG ) ||
                       ( codeList[j].op[1].type == oSTRING_ID ) ||
                       ( codeList[j].op[1].type == oLABEL ) ) &&
		     ( a3_used_once_and_only_here( codeList[j].op[0].val.l, j, stop ) ) )
                {
                    /* replace all following occurences by the right side */
                    
                    index = codeList[j].op[0].val.l;
                    
/*                  printf( "found something at %ld: l%ld\n", j, index ); */
                    
                    for ( k = j + 1;
                          k < longMem[index].lastUse;
                          k ++ )
                    {
                        if ( ( codeList[k].op[0].val.l == index ) &&
                             ( ( ( codeList[k].op[0].type & 7 ) == oVBYTE ) ||
                               ( ( codeList[k].op[0].type & 7 ) == oVLONG ) ||
                               ( ( codeList[k].op[0].type == oVFLOAT_IND ) ) ) )
                        {
                            codeList[k].op[0].type = ( codeList[k].op[0].type & 24 )
                                                     | codeList[j].op[1].type;
                            codeList[k].op[0].val.l = codeList[j].op[1].val.l;
                        }

                        if ( ( codeList[k].op[1].val.l == index ) &&
                             ( ( ( codeList[k].op[1].type & 7 ) == oVBYTE ) ||
                               ( ( codeList[k].op[1].type & 7 ) == oVLONG ) ||
                               ( ( codeList[k].op[1].type == oVFLOAT_IND ) ) ) )
                        {
                            codeList[k].op[1].type = ( codeList[k].op[1].type & 24 )
                                                     | codeList[j].op[1].type;
                            codeList[k].op[1].val.l = codeList[j].op[1].val.l;
                        }

                        if ( ( codeList[k].op[2].val.l == index ) &&
                             ( ( ( codeList[k].op[2].type & 7 ) == oVBYTE ) ||
                               ( ( codeList[k].op[2].type & 7 ) == oVLONG ) ||
                               ( ( codeList[k].op[2].type == oVFLOAT_IND ) ) ) )
                        {
                            codeList[k].op[2].type = ( codeList[k].op[1].type & 24 )
                                                     | codeList[j].op[1].type;
                            codeList[k].op[2].val.l = codeList[j].op[1].val.l;
                        }
                    }
                    
                    if ( codeList[j].op[1].type == oVLONG )
                    {
                        longMem[codeList[j].op[1].val.l].lastUse = MAX(
                        	longMem[codeList[j].op[1].val.l].lastUse,
                        	longMem[codeList[j].op[0].val.l].lastUse );
                    }
                    
                    codeList[j].stat_type = A3_NO_OP;
                    codeList[j].op[0].type = oNONE;
                    codeList[j].op[1].type = oNONE;
                    longMem[index].firstUse = 0;
                    longMem[index].lastUse = 0;

                    if ( codeList[j].labelPtr )
                    {
                        a3_append_labels( j, j + 1 );
                        codeList[j].labelPtr = NULL;
                    }
                    
                    found = TRUE;
                }
                
                /* same for floats... */
/*
                if ( ( codeList[j].stat_type == A3_ASSIGN ) &&
                     ( codeList[j].op[0].type == oVFLOAT ) &&
                     ( ( codeList[j].op[1].type == oCFLOAT ) ||
                       ( codeList[j].op[1].type == oVFLOAT ) ) &&
		     ( a3_used_once_and_only_hereF( codeList[j].op[0].val.l, j, stop ) ) )
                {
                    // replace all following occurences by the right side
                    
                    index = codeList[j].op[0].val.l;
                    
                    printf( "found FLOAT at %ld: l%ld\n", j, index );
                    
                    for ( k = j + 1;
                          k < floatMem[index].lastUse;
                          k ++ )
                    {
                        if ( ( codeList[k].op[1].val.l == index ) &&
                             ( codeList[k].op[1].type == oVFLOAT ) )
                        {
                            codeList[k].op[1].type = ( codeList[k].op[1].type & 24 )
                                                     | codeList[j].op[1].type;
                            codeList[k].op[1].val.l = codeList[j].op[1].val.l;
                        }

			if ( ( codeList[k].op[2].val.l == index ) &&
                             ( codeList[k].op[2].type == oVFLOAT ) )
                        {
                            codeList[k].op[2].type = ( codeList[k].op[1].type & 24 )
                                                     | codeList[j].op[1].type;
                            codeList[k].op[2].val.l = codeList[j].op[1].val.l;
                        }
                    }
                    
                    codeList[j].stat_type = A3_NO_OP;
                    codeList[j].op[0].type = oNONE;
                    codeList[j].op[1].type = oNONE;
                    floatMem[index].firstUse = 0;
                    floatMem[index].lastUse = 0;

                    if ( codeList[j].labelPtr )
                    {
                        a3_append_labels( j, j + 1 );
                        codeList[j].labelPtr = NULL;
                    }
                    
                    found = TRUE;
                }
*/            }

	    /* second step: eliminate additions of zero and constants */

            for ( j = start; j < stop; j ++ )
            {
                /* operation on two constants */
                
                if ( ( codeList[j].stat_type == A3_BINARY_OP ) &&
                     ( codeList[j].op[0].type == oVLONG ) &&
                     ( codeList[j].op[1].type == oCLONG ) &&
                     ( codeList[j].op[2].type == oCLONG ) )
                {
                    /* replace by ASSIGN */
                    
                    codeList[j].stat_type = A3_ASSIGN;
                    
                    switch ( codeList[j].op_type )
                    {
                        case ADD : codeList[j].op[1].val.l = codeList[j].op[1].val.l
                                                           + codeList[j].op[2].val.l;
                                   break;
                        case SUB : codeList[j].op[1].val.l = codeList[j].op[1].val.l
                                                           - codeList[j].op[2].val.l;
                                   break;
                        case MULT :  codeList[j].op[1].val.l = codeList[j].op[1].val.l
                                                             * codeList[j].op[2].val.l;
                                    break;
                        case DIV :  codeList[j].op[1].val.l = codeList[j].op[1].val.l
                                                            / codeList[j].op[2].val.l;
                                    break;
                        case MOD : codeList[j].op[1].val.l = codeList[j].op[1].val.l
                                                           % codeList[j].op[2].val.l;
                                    break;
                        case SHL : codeList[j].op[1].val.l = codeList[j].op[1].val.l
                                                          << codeList[j].op[2].val.l;
                                    break;
                        case SHR : codeList[j].op[1].val.l = codeList[j].op[1].val.l
                                                          >> codeList[j].op[2].val.l;
                                   break;
                        case BAND : codeList[j].op[1].val.l = codeList[j].op[1].val.l
                                                            & codeList[j].op[2].val.l;
                                    break;
                        case BOR : codeList[j].op[1].val.l = codeList[j].op[1].val.l
                                                           | codeList[j].op[2].val.l;
                                   break;
                        default : printf( "illegal tag\n" );
                    }
                        
                    codeList[j].op[2].type = oNONE;
                    
                    found = TRUE;
                }
                
                if ( ( codeList[j].stat_type == A3_UNARY_OP ) &&
                     ( codeList[j].op_type == NEG ) &&
                     ( codeList[j].op[0].type == oVLONG ) &&
                     ( codeList[j].op[2].type == oCLONG ) )
                {
                    codeList[j].stat_type = A3_ASSIGN;
                    codeList[j].op_type = NONE;
                    codeList[j].op[1].val.l = - codeList[j].op[1].val.l;
                }
                     
                /* addition of zero */
                
                if ( ( codeList[j].stat_type == A3_BINARY_OP ) &&
                     ( codeList[j].op[0].type == oVLONG ) &&
                     ( codeList[j].op_type == ADD ) &&
                     ( ( ( codeList[j].op[1].type == oCLONG ) &&
                         ( codeList[j].op[1].val.l == 0 ) ) ||
                       ( ( codeList[j].op[2].type == oCLONG ) &&
                         ( codeList[j].op[2].val.l == 0 ) ) ) )
                {
                    /* replace by ASSIGN */
                    
                    codeList[j].stat_type = A3_ASSIGN;
                    
                    if ( ( codeList[j].op[1].type == oCLONG ) &&
                         ( codeList[j].op[1].val.l == 0 ) )
                    {
                        codeList[j].op[1] = codeList[j].op[2];
                    }
                    codeList[j].op[2].type = oNONE;
                    
                    found = TRUE;
                }
            
                /* multiplication by one */
                
                if ( ( codeList[j].stat_type == A3_BINARY_OP ) &&
                     ( codeList[j].op[0].type == oVLONG ) &&
                     ( codeList[j].op_type == MULT ) &&
                     ( ( ( codeList[j].op[1].type == oCLONG ) &&
                         ( codeList[j].op[1].val.l == 1 ) ) ||
                       ( ( codeList[j].op[2].type == oCLONG ) &&
                         ( codeList[j].op[2].val.l == 1 ) ) ) )
                {
                    /* replace by ASSIGN */
                    
                    codeList[j].stat_type = A3_ASSIGN;
                    
                    if ( ( codeList[j].op[1].type == oCLONG ) &&
                         ( codeList[j].op[1].val.l == 1 ) )
                    {
                        codeList[j].op[1] = codeList[j].op[2];
                    }
                    codeList[j].op[2].type = oNONE;
                    
                    found = TRUE;
                }
            }
            
            /* third step: simplify things like   l1 := Rx + #y
            					  l2 := *l1	l2 := Rx[y/4]
            				      or  *l1 := l2	Rx[y/4] := l2
            				      or  *l1 := #z     Rx[y/4] := #z */
            
            for ( j = start; j < stop; j ++ )
            {
                if ( ( codeList[j].stat_type == A3_BINARY_OP ) &&
                     ( codeList[j].op_type == ADD ) &&
                     ( codeList[j].op[0].type == oVLONG ) &&
                     ( codeList[j].op[1].type == oREG ) &&
                     ( codeList[j].op[2].type == oCLONG ) &&
		     ( a3_used_once_and_only_here( codeList[j].op[0].val.l, j, stop ) ) )
                {
                    index = codeList[j].op[0].val.l;
                
                    for ( k = longMem[index].lastUse - 1;
                          codeList[k].stat_type == A3_NO_OP;
                          k -- );
                    
                    if ( ( codeList[k].stat_type == A3_ASSIGN ) &&
                         ( ( codeList[k].op[0].type == oVLONG ) ||
                           ( codeList[k].op[0].type == oVFLOAT ) ) &&
                         ( codeList[k].op[1].type == oVLONG_IND ) &&
                         ( codeList[k].op[1].val.l == index ) )
                    {
                        codeList[k].op[1].type = oREG_IX;
                        codeList[k].op[1].val.f[0] = codeList[j].op[1].val.l;
                        codeList[k].op[1].val.f[1] = codeList[j].op[2].val.l /
                                   ( codeList[k].op[0].type == oVFLOAT ? 8 : 4 );

                        codeList[j].stat_type = A3_NO_OP;
                        codeList[j].op[0].type = oNONE;
                        codeList[j].op[1].type = oNONE;
                        codeList[j].op[2].type = oNONE;
                        longMem[index].firstUse = 0;
                        longMem[index].lastUse = 0;

                        if ( codeList[j].labelPtr )
                        {
                            a3_append_labels( j, j + 1 );
                            codeList[j].labelPtr = NULL;
                        }
                        
                        found = TRUE;
                    }
                    else if ( ( codeList[k].stat_type == A3_ASSIGN ) &&
                              ( codeList[k].op[0].type == oVLONG_IND ) &&
                              ( codeList[k].op[0].val.l == index ) )
                    {
                        codeList[k].op[0].type = oREG_IX;
                        codeList[k].op[0].val.f[0] = codeList[j].op[1].val.l;
                        codeList[k].op[0].val.f[1] = codeList[j].op[2].val.l /
                                 ( ( codeList[k].op[1].type == oVFLOAT ) ||
                                   ( codeList[k].op[1].type == oCFLOAT ) ? 8 : 4 );

                        codeList[j].stat_type = A3_NO_OP;
                        codeList[j].op[0].type = oNONE;
                        codeList[j].op[1].type = oNONE;
                        codeList[j].op[2].type = oNONE;
                        longMem[index].firstUse = 0;
                        longMem[index].lastUse = 0;

                        if ( codeList[j].labelPtr )
                        {
                            a3_append_labels( j, j + 1 );
                            codeList[j].labelPtr = NULL;
                        }
                        
                        found = TRUE;
                    }
                }
            }
            
          } while ( found );
        }
    }
    
    return TRUE;
}


PRIVATE INT32 a3_next_useL( INT32 varNr, INT32 start )
{
    BOOL occurence = FALSE;

    do
    {
        start ++;
        occurence |= ( ( ( ( codeList[start].op[0].type & 7 ) == oVBYTE ) ||
                         ( ( codeList[start].op[0].type & 7 ) == oVLONG ) ||
                         ( codeList[start].op[0].type == oVFLOAT_IND ) ) &&
                       ( codeList[start].op[0].val.l == varNr ) );
        occurence |= ( ( ( ( codeList[start].op[1].type & 7 ) == oVBYTE ) ||
                         ( ( codeList[start].op[1].type & 7 ) == oVLONG ) ||
                         ( codeList[start].op[1].type == oVFLOAT_IND ) ) &&
                       ( codeList[start].op[1].val.l == varNr ) );
        occurence |= ( ( ( ( codeList[start].op[2].type & 7 ) == oVBYTE ) ||
                         ( ( codeList[start].op[2].type & 7 ) == oVLONG ) ||
                         ( codeList[start].op[2].type == oVFLOAT_IND ) ) &&
                     ( codeList[start].op[2].val.l == varNr ) );
    } while ( ! occurence );

    return start;
}


PRIVATE BOOL a3_used_once_and_only_here( INT32 varNr, INT32 start, INT32 stop )
{
    if ( longMem[varNr].lastUse > stop+1 )  return FALSE;
    return ( a3_next_useL( varNr, start ) >= longMem[varNr].lastUse-1 );
}

/*
PRIVATE INT32 a3_next_useF( INT32 varNr, INT32 start )
{
    BOOL occurence = FALSE;

    do
    {
        start ++;
        occurence |= ( ( codeList[start].op[1].type == oVFLOAT ) &&
                       ( codeList[start].op[1].val.l == varNr ) );
        occurence |= ( ( codeList[start].op[2].type == oVFLOAT ) &&
                       ( codeList[start].op[2].val.l == varNr ) );
    } while ( ! occurence );

    return start;
}


PRIVATE BOOL a3_used_once_and_only_hereF( INT32 varNr, INT32 start, INT32 stop )
{
    if ( floatMem[varNr].lastUse > stop+1 )  return FALSE;
    return ( a3_next_useF( varNr, start ) >= floatMem[varNr].lastUse-1 );
}
*/

PRIVATE BOOL a3_append_labels( UINT32 from, UINT32 to )
{
    multiLabelType *multiLabel;

    while ( codeList[to].stat_type == A3_NO_OP )
        to ++;

    if ( codeList[from].labelPtr )
    {
        for ( multiLabel = codeList[from].labelPtr;
              multiLabel->next;
              multiLabel = multiLabel->next );
        
        multiLabel->next = codeList[to].labelPtr;
        codeList[to].labelPtr = codeList[from].labelPtr;
        codeList[from].labelPtr = NULL;
    }
    
    return TRUE;
}

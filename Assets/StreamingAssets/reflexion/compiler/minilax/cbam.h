#ifndef CBAM_H
#define CBAM_H

/* Declarations */
#define MO1  24
#define MO1a 24
#define MO1b 25
#define MO2  26
#define MO2a 26
#define MO2b 27
#define PSW  28
#define SP   29
#define AP   30
#define PC   31

/* Filters for the processor status word. */
#define PSWhalt_FILTER 2147483648
#define PSWhalt_SHIFT  31

#define PSWcode_FILTER 2147418112
#define PSWcode_SHIFT  16

#define PSWbits_FILTER 65535
#define PSWbits_SHIFT  0

/* Some machine parameters */ 
#define FREGISTER_COUNT	16
#define REGISTER_COUNT	32
#define MEMORY_SIZE	((unsigned)(1 << 20))
#define SMEMORY_SIZE	((unsigned)(1 << 19))
#define LMEMORY_SIZE	((unsigned)(1 << 18))
#define FMEMORY_SIZE	((unsigned)(1 << 17))

/* How big are the various machine types? */
#define SIZEOF_BYTE	1
#define SIZEOF_SHORT	2
#define SIZEOF_LONG	4
#define SIZEOF_ADDR    	4
#define SIZEOF_FLOAT	8

/* How many registers do the various types fill ? */
#define REG_SIZEOF_INT   1
#define REG_SIZEOF_FLOAT 2

/* Given an address into memory (or a register number), how many bytes to the right
   do we shift that address to get an index for the type-specific array? */
#define  BYTE_ADDR_SHIFT 0
#define SHORT_ADDR_SHIFT 1
#define  LONG_ADDR_SHIFT 2
#define  ADDR_ADDR_SHIFT 2
#define FLOAT_ADDR_SHIFT 3
#define FLOAT_REG_SHIFT 1

/* Given an address into memory (or a register number), which bytes must be
   clear for the address to be valid? */
#define  BYTE_ADDR_CLEAR 0
#define SHORT_ADDR_CLEAR 1
#define  LONG_ADDR_CLEAR 3
#define  ADDR_ADDR_CLEAR 3
#define FLOAT_ADDR_CLEAR 7
#define FLOAT_REG_CLEAR 1

/* Given a part of the instruction longword, how many bytes to the
   left do we shift it for insertion into the longword?  */
#define IWORD_SHIFT_OPCODE	27
#define IWORD_SHIFT_REG_BIT_1	26
#define IWORD_SHIFT_REG_BIT_2	25
#define IWORD_SHIFT_REG_BIT_3	24
#define IWORD_SHIFT_MODE1	21
#define IWORD_SHIFT_REG1_NUM	16
#define IWORD_SHIFT_MODE2	13
#define IWORD_SHIFT_REG2_NUM	 8
#define IWORD_SHIFT_MODE3	 5
#define IWORD_SHIFT_REG3_NUM	 0
/* For the register bits, something to bit-check */
#define IWORD_REG_BIT_1_FILTER	(1 << IWORD_SHIFT_REG_BIT_1)
#define IWORD_REG_BIT_2_FILTER	(1 << IWORD_SHIFT_REG_BIT_2)
#define IWORD_REG_BIT_3_FILTER	(1 << IWORD_SHIFT_REG_BIT_3)
/* Filters for the shifted stuff */
#define IWORD_SHIFTED_MODE_FILTER 7
#define IWORD_SHIFTED_REG_NUM_FILTER 31

#define ACCESS_LIT  1
#define ACCESS_REG  1
#define ACCESS_MEM 50


/** Machine code structures **/
typedef enum opword_enum
{ OPWORD_error = -1,
  NO_OP = -2,
  HALT	=  0,
  MOVB	=  1,
  MOVS	=  2,
  MOVL	=  3,
  MOVF	=  4,
  PUSHB	=  5,
  PUSHS	=  6,
  PUSHL	=  7,
  PUSHF	=  8,
  POPL	=  9,
  POPF	= 10,
  ADDS	= 11,
  ADDL	= 12,
  NEGS	= 13,
  NEGL	= 14,
  MATHOP= 15,
  NOT	= 16,
  CPL	= 17,
  CPF	= 18,
  SHLL	= 19,
  SHRL	= 20,
  BANDL	= 21,
  BORL	= 22,
  BNOTL	= 23,
  JMP	= 24,
  BR	= 25,
  BSALL	= 26,
  BSANY	= 27,
  BL	= 28,
  FRAME	= 29,
  JSR	= 30,
  RTS	= 31
} opword;

typedef enum mode_enum
{ mNONE	= -1,
  mLIT		= 6, /* #n     */
  mDIRECT	= 0, /* L      */
  mIND		= 1, /* L*     */
  mIDX		= 2, /* L[n]   */
  mPOSTIDX_IND	= 3, /* L*[n]  */
  mPREIDX_IND	= 5, /* L[n]*  */
  mDBLIND	= 4, /* L**    */
  mIDX_DBLIND	= 7  /* L*[n]* */
} mode;

#endif

#ifndef TYPECHK_H
#define TYPECHK_H

PUBLIC BOOL         type_check( void );
PUBLIC atn_type *type_simplify( atn_type *node );
PUBLIC UINT8     type_refdepth( atn_type *type );
PUBLIC type_tag  type_realtype( atn_expr *node );
PUBLIC type_tag type_finaltype( atn_expr *node );
PUBLIC type_tag type_arraytype( atn_type *type );

#endif

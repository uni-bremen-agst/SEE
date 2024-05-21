#ifndef PARSER_H
#define PARSER_H

typedef struct anchorelem
{
    Symbol    sym;
    UINT16  depth;
} anchorelem;

PUBLIC BOOL parse( void );

#endif

#include "symtable.h"

delim   [ \t\n\r]
ws      {delim}+
letter  [A-Za-z]
digit   [0-9]
id      {letter}({letter}|{digit})*
intconst  {digit}+
scalefact E["+-"]?{intconst}
realconst {intconst}?"."{intconst}{scalefact}?

%%

{ws}    { /* do nothing */ }
ARRAY   {return(tok_array);}
BEGIN   {return(tok_begin);}
BOOLEAN {return(tok_bool);}
DECLARE {return(tok_decl);}
DO      {return(tok_do);}
ELSE    {return(tok_else);}
END     {return(tok_end);}
FALSE   {return(tok_false);}
IF      {return(tok_if);}
INT     {return(tok_int);}
NOT     {return(tok_not);}
OF      {return(tok_of);}
PROCEDURE {return(tok_proc);}
READ    {return(tok_read);}
REAL    {return(tok_real);}
THEN    {return(tok_then);}
TRUE    {return(tok_true);}
VAR     {return(tok_var);}
WHILE   {return(tok_while);}
WRITE   {return(tok_write);}
":"     {return(tok_istype);}
";"     {return(tok_eocmd);}
"."     {return(tok_eoprog);}
","     {return(tok_comma);}
":="    {return(tok_assign);}
"("     {return(tok_lbrack);}
")"     {return(tok_rbrack);}
"["     {return(tok_lidx);}
"]"     {return(tok_ridx);}
".."    {return(tok_range);}
"+"     {return(tok_add);}
"*"     {return(tok_mult);}
"<"     {return(tok_less);}
"<="    {return(tok_leq);}
">"     {return(tok_greater);}
">="    {return(tok_geq);}
"="     {return(tok_eq);}
"!="    {return(tok_neq);}
{id}    {yylval=install_id(); return(tok_ident);}
{intconst}  {yylval=install_int(); return(tok_intconst);}
{realconst} {yylval=install_real(); return(tok_realconst);}

%%

#include <string.h>

static char       tmpstr[255];
static SymIndex            ix;

install_id()
{
    strncpy(tmpstr,yytext,yyleng);
    tmpstr[yyleng] = 0;

    ix = sym_lookup(tmpstr);

    if (ix == ERROR)
    {
        ix = sym_insert(tmpstr, tok_ident);
    }

    return ix;
}


install_int()
{
    strncpy(tmpstr,yytext,yyleng);
    tmpstr[yyleng] = 0;

    // lege Konstante an und gib Index zurÅck! ...
}


install_real()
{
    strncpy(tmpstr,yytext,yyleng);
    tmpstr[yyleng] = 0;

    // lege Konstante an und gib Index zurÅck! ...
}


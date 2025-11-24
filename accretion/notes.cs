using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/*
 * precedence in grammar - need to specify what operations take "precedence" over other ops
 * 
 * without precedence, our grammar is
 * 
 * expression   -> literal | unary | binary | grouping
 * literal      -> NUMBER | STRING | "true" | "false" | "nil"
 * grouping     -> "(" expression ")"
 * unary        -> ( "-" | "!" ) expression
 * binary       -> expression operator expression
 * operator     -> "==" | "!=" | "<" | "<=" | ">" | ">=" | "+" | "-" | "*" | "/"
 * 
 * 
 * but the resolution of rules for 6 / 3 - 1 is ambiguous, which can lead to undefined behaviour
 *      expression -> binary (6, /, binary (3, -, 1))
 *      expression -> binary (binary(6, /, 3), -, 1)
 *      
 *      or in post order
 *      / 6 (- 3 1) = 3
 *      - (\ 6 3) 1 = 1
 * 
 * 
 * precedence solves this -> determines which operator is evaluated first
 * assossiativity determines which operated is evaluated first in a series of the same operator. - and ! are left-associative, = is right-associative
 * 
 * 
 * we need to create a separate rule for each precedence level, where each rule only matches expression at its precendence level or higher
 * 
 * from lowest to highest precedence:
 * expression   -> ternary
 * ternary      -> equality ( "?" ternary ":" ternary )?
 * equality     -> comparison ( ( "!=" | "==" ) comparison )*
 * comparison   -> term ( (">" | ">=" | "<" | "<=" ) term)*
 * term         -> factor ( ( "-" | "+ ) factor )*
 * factor       -> unary ( ( "/" | "*" ) unary)*
 * unary        -> ( "!" | "-" ) unary | primary
 * primary      -> NUMBER | STRING | "true" | "false" | "nil" | "(" expression ")"
 * 
 * 
 * so here, 6 / 3 - 1 would resolve to:
 * 
 * start with equality -> comparison -> term
 *      term (LHS) -> factor 
 *          factor (LHS) -> unary -> primary -> NUMBER(6)
 *          factor (RHS) -> \ unary -> \ primary -> \ NUMBER(3)
 *      term (RHS) -> - factor -> - unary -> - primary -> - NUMBER(1)
 *      
 * here, 6 / 3 had to match *something*, and the only way it could match that is by using factor, which does not allow for 3 - 1 to be done first
 *      
 * to translate grammar into recursive descent top-down parser
 * terminal becomes code to match & consume token
 * nonterminal calls a rule's function
 * | becomes a switch
 * * or + becomes while or for loop
 * ? becomes an if statement
 * 
 * 
 * 
 * 
 * 
 */

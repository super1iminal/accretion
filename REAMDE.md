# Accretion
> The process of growth or enlargement by a gradual buildup


Accretion is a user-first, statically typed, interpreted programming language designed for use in C# education and game applications. Accretion is a statically typed language. You may not even know what a type is. That's okay. I forgive you. You probably *like* not knowing. A friend of mine once said that they hated Python for the sole reason that it wasn't typed. 

## Language Features/Support
- Static typing
- Built-in functions and constants with external API support
- Overloadable functions
  - Merged across scopes
  - Nearest-function search used for matching
- Compiling errors and warnings
  - Line number and error/warning lexeme
  - Unused variables
  - Shadowed variables
  - Incorrect function calls
  - Cascading error suppression
- Ternary operator


### `.acc` snippet
```
int foo(int a) { return a; }

void bar() {
    int foo(int a) { return a * 2; }
    print foo(5) * PI;
}

bar();
```
**Accretion output:**
```
[line 1] Warning at 'foo': Variable/Function is never used
31.41592653589793
```

## Language Specification
### Primitives
The language cosists of several basic primitive types.
* `int` (4 bytes)
* `double` (8 bytes)
* `string`

### Builtins
#### Functions
`double clock()`
- returns the current time in seconds
```
// trig
double sin(double val)
double sin(int val)

double cos(double val)
double cos(int val)

double tan(double val)
double tan(int val)

double asin(double val)

double acos(int val)

double atan(double val)

double atan2(double y, double x)


// math
// exponential/logarithmic
double sqrt(double val)
double sqrt(int val)

double pow(double val, double exp)

double pow(int val, int exp)

double exp(double val)

double log(double val)

double log10(double val)

double log2(double val)

// rounding
double abs(double val)
int abs(int val)

int floor(double val)

int ceil(double val)

int round(double val)

// mix/max
double min(double a, double b)
int min(int a, int b)

double max(double a, double b)
int max(int a, int b)

// sign/mod
int sign(double) // -1, 0, or 1
int sign(int)

double mod(double dividend, double divisor)

int mod(int dividend, int divisor)
```

#### Constants
```
PI // 3.1415...

E // 2.718...

INF

NINF // negative infinity
```

### Grammar (EBNF)
In general, statement and declarations are code that don't return a value, whereas an expression is code that does. 
```
program         -> [declaration] EOF
declaration     -> ( identifier identifier ( varDecl | funDecl ) ) | statement
funDecl         -> "(" ( [ parameter "," ] parameter ) ")" "{" block
parameter       -> identifier identifier
varDecl         -> { "=" expression } ";"

statement       -> jmpStmt | ifStmt | whileStmt | forStmt | printStmt | returnStmt | ( "{" block ) | exprStmt 
block           -> [ declaration ] "}"

jmpStmt         -> "break" | "continue" ";"
ifStmt          -> "if" "(" expression ")" statement { "else" statement }
whileStmt       -> "while" "(" expression ")" statement
forStmt         -> "for" "(" ( ";" | varDecl | exprStmt ) ( ";" | expression) { expression } ")" statement
printStmt       -> "print" expression ";"
returnStmt      -> "return" { expression } ";"
exprStmt        -> expression ";"

expression      -> AssignmentRule
AssignmentRule  -> OrRule { "=" AssignmentRule }
OrRule          -> AndRule [ "or" AndRule ]
AndRule         -> TernaryRule [ "and" TernaryRule ]
TernaryRule     -> EqualityRule { "?" TernaryRule ":" TernaryRule }
EqualityRule    -> ComparisonRule [ ( "!=" | "==" ) ComparisonRule ]
ComparisonRule  -> TermRule [ ( ">" | ">=" | "<" | "<=" ) TermRule ]
TermRule        -> FactorRule [ ( "-" | "+" ) FactorRule ]
FactorRule      -> UnaryRule [ ( "/" | "*" ) UnaryRule ]
UnaryRule       -> ( ( "!" | "-" ) UnaryRule ) | CallRule
CallRule        -> PrimaryRule [ "(" { [ expression "," ] expression } ")" ]
PrimaryRule     -> IDENTIFIER | NUMBER | STRING | "true" | "false" | "void" | "(" expression ")"
```
#### Notes
* `IDENTIFIER` matches both types and variable names.
* At the beginning of an expression, the token "falls through" to `PrimaryRule`, as trivially matches every condition. The rule chosen for the next token therefore goes *up* the chain of rules. So, higher priority rules sit *lower* on the chain (see PEMDAS)
* The call rule can contain arbitrary expressions as its parameters, as expressions **always** return a value
* `UnaryRule` is interesting, as it is the only rule that matches its own operators before falling through. This is to avoid collisions with `FactorRule`. Therefore, `-x - y` results in `(-x) - y`
* The value of an assignment is the assigned value (e.g., `2 == (x = 2)`)
* The maximum number of parameters a function can have is 255
* `if`, `for`, and `while` statements allow for a direct follow-up statement (i.e., `if (x < 3) if (y > 5) for (int i = 0; i < 5; i+++) x = x + 1;` is a valid statement)
* In practice, a `for` statement desugars into a `while` statement

### Function Declaration
```
type funcName(type param1, type param2, ...) {
  {body}
  return type;
  // or, if type == void
  return;
}
```
For example,
```
bool fooBar(int x) {
  return x < 15.4;
}
```

#### Notes
* Parameters not required
* If function return type is `void`, then return statement can be written as `return;`

### Variable Declaration
```
int x = 5;

bool t1 = bool t2 = !5; // t1 == t2 == false 


int y;
y = 41;

int z = 23.1; // this works
```
#### First-Class Functions
Accretion does not currently support first-class functions (i.e., you cannot assign a function to a variable), but I am definitely exploring the idea. It wouldn't require too much detangling to allow function signatures to be declared as types, and to add lambdas. I do like the idea.


#### Implicit Casting (Promotion)
The current implicit casts are:
```
int --> double

int, double, string -> bool

int, double, bool -> string
```
Promotion applies when the type used does not match the expected type. The typer warns the user when an implicit cast has taken place. 

The following is a list of promotions when the types don't match
* Variable declaration (initializer): Always if possible
* Variable assignment: Always if possible
* Binary expressions: 
  * Boolean operations: Always to `boolean`
  * Divison, multiplication, subtraction: Always to `double`
  * Addition: 
    * `string` always if one of the two operands is a string
    * `double` otherwise
* Unary expressions:
  * Boolean operations: Always to `boolean`
  * Negation: no promotion required
* Function arguments:
  * First, functions from local to global scopes are searched for an **exact** match (function `identifier` and `parameter` types)
  * If no match is found, then the search is repeated on functions with a matching `identifier` and promotion-compatible values
    * Functions are again evaluated from local to global scope. If two compatible functions are found (e.g., `void foo(string x, string y))` and `void foo(bool x, bool y)`), then **no function is chosen**, and the typer reports an error.
* Return value:
  * Always if possible

### If Statements
You know these.
```
if (x < 5) {
  print 1;
} 
else if (x == 5) {
  print 2;
}
else {
  // this triggers when x > 5
  print 3;
}
```

### While Statements
```
int x = 2;
while (x < 5) {
  // do something
  x = x + 1;
}
```

### For Statements
```
for (int x = 2; x < 5; x = x + 1) {
  // do something
}
```

### Jump Statements
`break` and `continue` in loops:
```
int counter = 0;
while (true) {

  // skips printing 3
  if (counter == 3) {
    continue;
  }

  // stops printing after 4
  if (counter >= 5) {
    break;
  }

  print counter;
}
```

### Print Statement
Printing is built-in to the language as a statement. It is not a standard library, although I may make this change soon to allow for hooking printing up into an external console.
```
print 5;

print "hello" + "world";

print 5 < 3 and false;

print foo("bar");
```

### Scopes
Accretion has generous scoping rules. A "scope" is defined by curly braces (`{}`), and can be created pretty much anywhere, although certain statements, such as function calls, require them. In the grammar, scopes are called `block`s. 

Both variables and functions are limited to the scope they are created in, and that scope's children. For example,

```
{
  void foo() {
    print "hello world";
  }
}

foo(); // would cause a compiler error
```

Additionally, if a global variable is defined *after* a function, that function cannot reference that global variable. For example,

```
void foo() {
  print a;
}

string a = "hello world";

foo(); // would return a compiler error ('a' does no exist from foo's perspective)
```
However,
```
string b = "world, hello";

void bar() {
  print b;
}

bar(); // this would pass compiler checks

```

### Overloading & Shadowing
The same name can be used for two functions, given they have different arguments. This rule applies only if they can be reached from the same scope. For example:
```
void foo(int a) {
  print a;
}

void foo(string a) {
  print a + "!";
}

void foo(double b) {
  print b * 2;
}

foo(5);
foo("5");
foo(5.0);

prints "5", then "5!", then "10.0"
```

In some languages, like C#, if you create an overloaded function within the scope of a function with the same name, that high-level function is "shadowed". In other words, within the scope of the child function, the parent function **cannot be accessed or called**. Accretion differs in that functions are **never shadowed** if they have the same name with **different parameters**.
```
int foo(int a, bool b) {
  int foo() {
	print a;
    if (b) {
      foo(4, false); // can still access outer foo()
    }
  }

  foo();
}

foo(5, true); // prints 5, then 4. 
```

However, both functions and variables can be shadowed if they share a name with a function in an inner scope. 
Example of a function being shadowed:
```
int foo(int a) { return a; } 

void bar() {
    int foo(int a) { return a * 2; } // shadows global foo
    print foo(5.0) * PI;
}

bar(); // prints 31.4159.. (inner foo) instead of 15.707.. (global foo)
```

```
int a = 5;

void bar(int a) {
  print a; // shadows global a
}

bar(10); // prints 10
```

## Developer's Guide [IN PROGRESS]
### Preamble
There's a lot of ground to cover here, so I'm going to assume you have some baseline knowledge of how interpreters work[^1]. An interpreter is split into several phases, and one of them is *also* called an interpreter. I know, it's dumb and it can be a little confusing. I'll explain the codebase in the phases of the interpreter pipeline (scanning, parsing, resolving/typing), referencing the Appendix for details on miscellaneous files and code. 

### Scanner
To start, you have the **scanner**. The scanner takes as input a raw text file, and spits out tokens, which are matched pieces of that text file that contain one or more characters. For example, in Accretion, `(` is a token, `print` is a token, and so are `and`, `while`, `for`, `{`, `void`, `"hello world"`, `true`, `*`, `+`, `=`, and `\\`. The scanner needs to be good at its job, because it creates the basement-level foundation that we build a strucutred representation of the code upon. It can catch basic errors, such as token sequences that start but never terminate in an expected way (e.g., `"hello world [...] EOF`), and unexpected characters, but not much more. Its job is to be robust. 


### Parser
The **parser** is the next step, and it's a big one. You ever see those videos of cool geometric bismuth crystals being pulled out of a flat plane of liquid bizmuth? That's basicically what a parser does. It takes the one-dimensional sequence of tokens that the scanner gave us, combines it with a grammar definition, and builds from them a tree structure called an Abstract Syntax Tree (AST). 

Take a look at Accretion's grammar I defined above. `program` is the highest-level node of the tree, and it's comprised of several `declaration`s. Our `declaration` rule is comprised of variable and function declarations, and `statements`. You can image the grammar forms a tree downward to the very bottom `PrimaryRule`. However, the AST is not a tree representing the grammar. It's a tree using the grammar rules as, well, rules on how to build it. Below is a basic program with a corresponding `AST`. Note that I'm not going to go into much further detail on the basics of interpreters, but you need to know this if you are to understand the rest of this README.

```
int x = 5;

if (x > 10) {
  print "Rango is the best movie"
}
```


You may have noticed we've assigned names to the elements in the grammar that were previously unnamed. Now, the expression that is the initializer has a name: Aptly, Initializer. Also note that we no longer care about trivialities such as semicolons. The parser handles the bulk of the error processing and "reformats" the data in a different way, so once we get past it, we no longer care about the original representation of the code, with all of it's . You could say it's now in a different "intermediate" language that is agnostic to its actual textual source. That `metadata` you see is fields added from the **scanning** phase. They include the line number of the token, the token type (`IDENTIFIER`, `while`, `if`, etc.), and other important data relevant to the interpreter and outputting warnings and errors.


### Typer
Section in progress.



[^1]: Read my [article](https://onetwelvethree.com/blog/interpreted-languages/) on interpreted language for a more beginner-friendly introduction to interpreted lanauges


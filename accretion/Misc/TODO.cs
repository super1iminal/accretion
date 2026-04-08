using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace accretion.Misc
{
    /*
     * alright there's a lot to do before the language is mostly functional:
     *
     * 1. classes. oof.
     * 1.5. overload functionality for functions -> might have to move resolution to typer to tell which scope the correct typed func is in
     * 2. enums. double oof.
     *
     *
     * CRITICAL: `continue` inside `for` loops is broken.
     *   For loops desugar into while loops (Parser.cs, ForStatement()), and the increment
     *   is appended inside the body block. When `continue` throws a Jump, it exits the
     *   entire block — skipping the increment — so the loop variable never changes and
     *   you get an infinite loop.
     *   Fix: either wrap the user's body in a try/catch for continue so the increment
     *   always executes, or introduce a proper Stmt.For AST node with its own visitor
     *   that handles continue before the increment.
     *
     *
     * warn users when they shadow out a local variable
     *
     *
     *
     */



}

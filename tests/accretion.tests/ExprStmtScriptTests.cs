using System.Collections.Generic;
using Xunit;

namespace accretion.tests
{
    /// <summary>
    /// Scripts under TestScripts/expr_stmt/ — expressions, statements, closures,
    /// scoping, and resolver/type errors.
    /// </summary>
    public class ExprStmtScriptTests
    {
        public static IEnumerable<object[]> TestCases() => ScriptTestRunner.DiscoverCases("expr_stmt");

        [Theory]
        [MemberData(nameof(TestCases))]
        public void RunScript(string relativeTestFilePath) => ScriptTestRunner.Run(relativeTestFilePath);
    }
}

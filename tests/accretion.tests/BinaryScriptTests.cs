using System.Collections.Generic;
using Xunit;

namespace accretion.tests
{
    /// <summary>
    /// Scripts under TestScripts/binary/ — binary operator behavior across types.
    /// </summary>
    public class BinaryScriptTests
    {
        public static IEnumerable<object[]> TestCases() => ScriptTestRunner.DiscoverCases("binary");

        [Theory]
        [MemberData(nameof(TestCases))]
        public void RunScript(string relativeTestFilePath) => ScriptTestRunner.Run(relativeTestFilePath);
    }
}

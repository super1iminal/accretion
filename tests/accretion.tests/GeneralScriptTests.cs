using System.Collections.Generic;
using Xunit;

namespace accretion.tests
{
    /// <summary>
    /// Scripts under TestScripts/general/ — broad end-to-end language coverage.
    /// </summary>
    public class GeneralScriptTests
    {
        public static IEnumerable<object[]> TestCases() => ScriptTestRunner.DiscoverCases("general");

        [Theory]
        [MemberData(nameof(TestCases))]
        public void RunScript(string relativeTestFilePath) => ScriptTestRunner.Run(relativeTestFilePath);
    }
}

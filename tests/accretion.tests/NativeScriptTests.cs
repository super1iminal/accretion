using System.Collections.Generic;
using Xunit;

namespace accretion.tests
{
    /// <summary>
    /// Scripts under TestScripts/natives/ — native functions and constants.
    /// </summary>
    public class NativeScriptTests
    {
        public static IEnumerable<object[]> TestCases() => ScriptTestRunner.DiscoverCases("natives");

        [Theory]
        [MemberData(nameof(TestCases))]
        public void RunScript(string relativeTestFilePath) => ScriptTestRunner.Run(relativeTestFilePath);
    }
}

using System.Text.Json.Serialization;

namespace accretion.Tests
{
    /// <summary>
    /// Represents a test case loaded from a JSON file.
    /// </summary>
    public class TestCase
    {
        /// <summary>
        /// Name of the test case for identification in output.
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = "Unnamed Test";

        /// <summary>
        /// Description of what the test is testing.
        /// </summary>
        [JsonPropertyName("description")]
        public string Description { get; set; } = "";

        /// <summary>
        /// The Accretion source code to execute.
        /// </summary>
        [JsonPropertyName("script")]
        public string Script { get; set; } = "";

        /// <summary>
        /// Expected output when execution succeeds (no errors).
        /// </summary>
        [JsonPropertyName("expectedOutput")]
        public string ExpectedOutput { get; set; } = "";

        /// <summary>
        /// Whether a compiler error is expected.
        /// </summary>
        [JsonPropertyName("expectCompilerError")]
        public bool ExpectCompilerError { get; set; } = false;

        /// <summary>
        /// Expected compiler error output (if expectCompilerError is true).
        /// Can be a substring to match against.
        /// </summary>
        [JsonPropertyName("expectedCompilerError")]
        public string ExpectedCompilerError { get; set; } = "";

        /// <summary>
        /// Whether a compiler warning is expected.
        /// </summary>
        [JsonPropertyName("expectCompilerWarning")]
        public bool ExpectCompilerWarning { get; set; } = false;

        /// <summary>
        /// Expected compiler warning output (if expectCompilerWarning is true).
        /// Can be a substring to match against.
        /// </summary>
        [JsonPropertyName("expectedCompilerWarning")]
        public string ExpectedCompilerWarning { get; set; } = "";

        /// <summary>
        /// Whether a runtime error is expected.
        /// </summary>
        [JsonPropertyName("expectRuntimeError")]
        public bool ExpectRuntimeError { get; set; } = false;

        /// <summary>
        /// Expected runtime error output (if expectRuntimeError is true).
        /// Can be a substring to match against.
        /// </summary>
        [JsonPropertyName("expectedRuntimeError")]
        public string ExpectedRuntimeError { get; set; } = "";

        /// <summary>
        /// Whether to use exact matching for output comparison.
        /// If false, uses contains/substring matching.
        /// </summary>
        [JsonPropertyName("exactMatch")]
        public bool ExactMatch { get; set; } = true;

        /// <summary>
        /// Whether to trim whitespace when comparing outputs.
        /// </summary>
        [JsonPropertyName("trimWhitespace")]
        public bool TrimWhitespace { get; set; } = true;

        /// <summary>
        /// Tags for categorizing tests (e.g., "parser", "interpreter", "types").
        /// </summary>
        [JsonPropertyName("tags")]
        public string[] Tags { get; set; } = System.Array.Empty<string>();
    }
}

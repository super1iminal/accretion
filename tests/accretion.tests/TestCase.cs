using System.Text.Json.Serialization;

namespace accretion.tests
{
    /// <summary>
    /// Represents a test case loaded from a JSON file.
    /// </summary>
    public class TestCase
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = "Unnamed Test";

        [JsonPropertyName("description")]
        public string Description { get; set; } = "";

        [JsonPropertyName("script")]
        public string Script { get; set; } = "";

        [JsonPropertyName("expectedOutput")]
        public string ExpectedOutput { get; set; } = "";

        [JsonPropertyName("expectCompilerError")]
        public bool ExpectCompilerError { get; set; } = false;

        [JsonPropertyName("expectedCompilerError")]
        public string ExpectedCompilerError { get; set; } = "";

        [JsonPropertyName("expectCompilerWarning")]
        public bool ExpectCompilerWarning { get; set; } = false;

        [JsonPropertyName("expectedCompilerWarning")]
        public string ExpectedCompilerWarning { get; set; } = "";

        [JsonPropertyName("expectRuntimeError")]
        public bool ExpectRuntimeError { get; set; } = false;

        [JsonPropertyName("expectedRuntimeError")]
        public string ExpectedRuntimeError { get; set; } = "";

        /// <summary>
        /// Whether to use exact matching for output comparison.
        /// If false, uses contains/substring matching.
        /// </summary>
        [JsonPropertyName("exactMatch")]
        public bool ExactMatch { get; set; } = true;

        [JsonPropertyName("trimWhitespace")]
        public bool TrimWhitespace { get; set; } = true;

        [JsonPropertyName("tags")]
        public string[] Tags { get; set; } = System.Array.Empty<string>();
    }
}

# Accretion Test Framework

A comprehensive test framework for the Accretion programming language that allows you to define test cases in JSON format.

## Directory Structure

```
Tests/
├── AccretionTests.csproj     # Test project file
├── TestCase.cs               # JSON test case model
├── TestErrorManager.cs       # Error manager for capturing errors/warnings
├── TestLogger.cs             # Logger for capturing output
├── TestProgram.cs            # Entry point for running tests
├── TestResult.cs             # Test result models
├── TestRunner.cs             # Main test runner logic
├── README.md                 # This file
└── TestScripts/              # JSON test files go here
    ├── 01_simple_print.json
    ├── 02_variable_declaration.json
    └── ...
```

## JSON Test File Format

Each test case is defined in a JSON file with the following structure:

```json
{
    "name": "Test Name",
    "description": "Description of what this test is testing",
    "script": "int x = 42;\nprint x;",
    "expectedOutput": "42",
    "expectCompilerError": false,
    "expectedCompilerError": "",
    "expectCompilerWarning": false,
    "expectedCompilerWarning": "",
    "expectRuntimeError": false,
    "expectedRuntimeError": "",
    "exactMatch": true,
    "trimWhitespace": true,
    "tags": ["variables", "print"]
}
```

### Fields

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `name` | string | Yes | Name of the test case |
| `description` | string | No | Description of what the test is testing |
| `script` | string | Yes | The Accretion source code to execute |
| `expectedOutput` | string | No | Expected output when execution succeeds |
| `expectCompilerError` | bool | No | Whether a compiler error is expected |
| `expectedCompilerError` | string | No | Expected compiler error message (substring match) |
| `expectCompilerWarning` | bool | No | Whether a compiler warning is expected |
| `expectedCompilerWarning` | string | No | Expected compiler warning message (substring match) |
| `expectRuntimeError` | bool | No | Whether a runtime error is expected |
| `expectedRuntimeError` | string | No | Expected runtime error message (substring match) |
| `exactMatch` | bool | No | If true, output must match exactly; if false, uses substring match |
| `trimWhitespace` | bool | No | Whether to trim whitespace when comparing outputs |
| `tags` | string[] | No | Tags for categorizing and filtering tests |

## Running Tests

### Command Line Options

```bash
# Run all tests
dotnet run

# Run tests from a custom directory
dotnet run -- -d /path/to/test/scripts

# Run only tests with a specific tag
dotnet run -- -t parser

# Run tests with verbose output
dotnet run -- -v

# Combine options
dotnet run -- -d ./custom-tests -t functions -v
```

### Exit Codes

- `0`: All tests passed
- `1`: One or more tests failed

## Test Case Behavior

### Compiler Errors
When `expectCompilerError` is `true`:
- The test passes if a compiler error occurs
- If `expectedCompilerError` is provided, the actual error message must contain this substring
- Output comparison is skipped

When `expectCompilerError` is `false`:
- Any compiler error causes the test to fail

### Compiler Warnings
When `expectCompilerWarning` is `true`:
- The test passes if a compiler warning occurs
- If `expectedCompilerWarning` is provided, the actual warning must contain this substring
- Code execution continues (unlike compiler errors)
- Output is still compared to `expectedOutput`

### Runtime Errors
When `expectRuntimeError` is `true`:
- The test passes if a runtime error occurs
- If `expectedRuntimeError` is provided, the actual error must contain this substring
- Partial output up to the error point is compared with `expectedOutput`

### Output Matching
- By default (`exactMatch: true`), output must match exactly
- With `exactMatch: false`, the expected output just needs to be contained in actual output
- With `trimWhitespace: true` (default), leading/trailing whitespace is ignored
- Line endings are normalized for comparison

## Example Test Cases

### Basic Print Test
```json
{
    "name": "Simple Print",
    "description": "Tests basic print statement",
    "script": "print \"Hello, World!\";",
    "expectedOutput": "Hello, World!",
    "tags": ["basic", "print"]
}
```

### Expected Error Test
```json
{
    "name": "Undefined Variable",
    "description": "Tests that undefined variable produces error",
    "script": "print x;",
    "expectCompilerError": true,
    "expectedCompilerError": "no declared variable",
    "exactMatch": false,
    "tags": ["error", "variables"]
}
```

### Warning Test (code still runs)
```json
{
    "name": "Unused Variable Warning",
    "description": "Tests unused variable warning",
    "script": "{ int unused = 42; }\nprint \"done\";",
    "expectedOutput": "done",
    "expectCompilerWarning": true,
    "expectedCompilerWarning": "unused",
    "tags": ["warning", "variables"]
}
```

## Setup Instructions

1. **Place test files in your project:**
   Copy the `Tests/` directory into your Accretion project root.

2. **Update your .csproj or add reference:**
   Either compile the test project separately or include the test files in your main project.

3. **Create test scripts:**
   Add JSON test files to the `Tests/TestScripts/` directory.

4. **Run tests:**
   ```bash
   cd Tests
   dotnet run
   ```

## Extending the Framework

### Custom TestLogger
The `TestLogger` class captures all `print` output. You can extend it for custom output handling:

```csharp
public class MyTestLogger : TestLogger
{
    public override void Log(string message)
    {
        base.Log(message);
        // Add custom handling
    }
}
```

### Custom TestErrorManager
The `TestErrorManager` class captures all compiler and runtime errors/warnings:

```csharp
public class MyTestErrorManager : TestErrorManager
{
    public override void CompilerError(Token token, string message)
    {
        base.CompilerError(token, message);
        // Add custom handling
    }
}
```

## Tags Reference

Suggested tags for categorizing tests:

| Tag | Description |
|-----|-------------|
| `basic` | Basic functionality tests |
| `variables` | Variable declaration and usage |
| `functions` | Function definition and calls |
| `loops` | While and for loops |
| `conditionals` | If/else statements |
| `operators` | Arithmetic, logical, comparison operators |
| `types` | Type checking tests |
| `scope` | Variable scoping tests |
| `error` | Expected compiler error tests |
| `warning` | Expected compiler warning tests |
| `runtime` | Runtime behavior tests |
| `native` | Native function/constant tests |
| `recursion` | Recursive function tests |

# NINA.Sequencer.Generators Architecture

## Purpose

`NINA.Sequencer.Generators` is the Roslyn source-generator project used by `NINA.Sequencer`. It removes repetitive boilerplate for expression-backed sequence properties.

Build shape from `NINA.Sequencer.Generators.csproj`:

- Target framework: `netstandard2.0`
- Output type: analyzer/source-generator library

## What It Generates

The project currently contains a single generator:

- `ExpressionGenerator.cs`

This generator scans for:

- partial properties
- annotated with `[IsExpression]`

and generates partial class code that adds:

- the backing `Expression` object
- generated property accessors
- optional validator hooks
- clone support for the generated expression fields
- proxy/default/range handling encoded in attribute arguments

## Discovery Rules

The generator is explicit about what it accepts:

- the syntax node must be a partial property declaration
- the property must use `get;` / `set;` style accessors without bodies
- the property must carry `NINA.Sequencer.Generators.IsExpressionAttribute`

It also requires the containing class to have `[UsesExpressions]`. If that attribute is missing, the generator emits diagnostic `EXP0001` and skips generation for that property.

## Generated Contract

The generated code assumes a class-level pattern used in `NINA.Sequencer`:

- a generated `Clone()` method that copies expression state
- optional partial validator methods such as `PropertyExpressionValidator(...)`
- optional partial `AfterClone(...)` hooks

This means the generator is part of the sequencer entity programming model, not just a build convenience.

## Dependency Position

The project is referenced by `NINA.Sequencer.csproj` as:

- `ProjectReference ... OutputItemType="Analyzer"`

No runtime project should instantiate or call this library directly. Its output only exists at compile time.

## Contribution Notes

- If you change the generated shape, inspect the sequence entities that rely on `Clone()`, validator hooks, and generated expression properties.
- Keep diagnostics specific and conservative; generator failures should not silently produce invalid runtime behavior.
- Add new attributes or generation modes here only when the sequencer project truly needs a repeated compile-time pattern.

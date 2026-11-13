using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Imprint.Analyzers;

/// <summary>
/// IMP0001 and IMP0002 (ADR-0028 in kennel.canine.dev): on the websites a CAI score becomes text only through
/// <c>Imprint.Publishing.CaiScoreText</c>, and a score keeps a name that says what it is, so the first rule can follow it.
/// <para>★ IMPRINT'S COPY of the app's <c>Kennel.Analyzers.CaiScoreTextAnalyzer</c> (KNL0201/KNL0202), like the engine's
/// CHA0007/CHA0008: a separate repository cannot share the app's source. Only the settings differ (the formatter, the
/// attributes, the ids, the collect property, the assemblies it reads). Change the copies together.</para>
/// </summary>
/// <remarks>
/// <para><b>What a score is.</b> A number (a <c>double</c>, an <c>int</c>, a nullable one) held by a member, local or
/// parameter whose name says so (<see cref="ScoreNames"/>), or marked <c>[CaiScore]</c>; and what is done to one:
/// <c>Math.Round</c>, a cast, <c>+</c> and <c>-</c> (a change), <c>?:</c>, <c>??</c>, <c>?.</c>, <c>.Value</c>, an element of
/// a collection of scores, an average or maximum over one, and a local or lambda parameter that was given one. Scaling
/// (<c>*</c>, <c>/</c>) is not followed: that is how a chart turns a score into a coordinate, and a coordinate is not a reading.</para>
/// <para><b>IMP0001 (the app's KNL0201), what text is.</b> A hole in an interpolated string, <c>ToString</c> (and <c>Convert.ToString</c>),
/// <c>string.Format/Concat/Join</c>, string concatenation, <c>StringBuilder.Append*/Insert</c>, <c>TextWriter.Write*</c>,
/// <c>Console.Write*</c>, and what a compiled Razor component writes: <c>AddContent</c>, <c>AddAttribute</c>,
/// <c>AddMarkupContent</c> and <c>BindConverter.FormatValue</c>. A score kept as a number (a JSON field, a logger's template
/// argument, a comparison) is not text and is never flagged.</para>
/// <para><b>IMP0002 (the app's KNL0202), the closure.</b> A rule that follows names can be escaped by a name: <c>new TrendPoint(name,
/// Math.Round(p.Cai!.Value))</c> made the sprint e-mail's score a <c>Value</c>. So passing a score into a parameter, storing
/// it in a property or field, or returning it from a member of the product's own code is an error unless that target's
/// name says score, or it is marked <c>[CaiScore]</c> (or <c>[NotACaiScore("why")]</c>). The runtime's own members
/// (<c>Math.Max</c>, <c>List.Add</c>), anonymous payloads and the formatter itself are outside it.</para>
/// <para><b>The exemption</b> is <c>[NotACaiScore("why")]</c>, reviewed like <c>[ArtifactDownload]</c>: on a member whose
/// name says score but whose number is not one, or on a method or type whose text no reader sees. A blank reason is an error.</para>
/// <para>Razor components are generated code and are analysed (KNL0001 does the same); other generated code is not.</para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class CaiScoreTextAnalyzer : DiagnosticAnalyzer
{
    public const string TextId = "IMP0001";

    public const string NameId = "IMP0002";

    public const string FormatterMetadataName = "Imprint.Publishing.CaiScoreText";

    public const string ScoreAttributeMetadataName = "Imprint.Publishing.CaiScoreAttribute";

    public const string NotAScoreAttributeMetadataName = "Imprint.Publishing.NotACaiScoreAttribute";

    private const string HelpLink = "https://github.com/CanineCC/kennel.canine.dev/blob/main/docs/adr/0028-a-cai-score-is-written-with-one-decimal.md";

    private static readonly DiagnosticDescriptor TextRule = new(
        TextId,
        "A score becomes text only through CaiScoreText",
        "{0} writes the score '{1}' as text itself, but a CAI score is written only by CaiScoreText, with one decimal, always (ADR-0028), so pass it to CaiScoreText.Shown or Change, or mark machine text no reader sees [NotACaiScore(\"why\")]",
        "Presentation",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Owner decision 12 November 2026: the CAI score is always shown with one decimal, on every page, e-mail, chat message, export, report, PDF and text for an AI model. One formatter writes it, so the rounding, the point and the decimal are the same everywhere.",
        helpLinkUri: HelpLink);

    private static readonly DiagnosticDescriptor NameRule = new(
        NameId,
        "A score keeps its name",
        "The score '{0}' goes into {1}, whose name does not say it holds a score, so IMP0001 cannot follow it there (ADR-0028); name it for the score (…Score, …Cai, …Headline), mark it [CaiScore], or mark it [NotACaiScore(\"why\")] if it is not one",
        "Presentation",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "A score stored or passed under a name like Value or Trend is out of the formatting rule's sight. Naming or marking the member keeps every score visible to the build.",
        helpLinkUri: HelpLink);

    private static readonly DiagnosticDescriptor BlankReasonRule = new(
        NameId,
        "An exemption gives its reason",
        "[NotACaiScore] on {0} gives no reason, so it exempts nothing (ADR-0028); say why the number is not a CAI or dimension score, or why no reader sees the text",
        "Presentation",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: HelpLink);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(TextRule, NameRule, BlankReasonRule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        // Razor components ARE generated code, and they are where most scores are written.
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.RegisterCompilationStartAction(Start);
    }

    private static void Start(CompilationStartAnalysisContext start)
    {
        var severity = Collecting(start.Options) ? DiagnosticSeverity.Warning : DiagnosticSeverity.Error;
        var flow = new ScoreFlow(start.Compilation);

        start.RegisterOperationAction(c => Sink(c, flow, severity), OperationKind.Interpolation, OperationKind.InterpolatedStringAppendFormatted,
            OperationKind.Invocation, OperationKind.Binary);
        start.RegisterOperationAction(c => Names(c, flow, severity), OperationKind.Argument, OperationKind.SimpleAssignment,
            OperationKind.Return, OperationKind.YieldReturn);
        start.RegisterSymbolAction(s => BlankReasons(s, severity), SymbolKind.NamedType, SymbolKind.Method, SymbolKind.Property, SymbolKind.Field);
    }

    /// <summary>-p:ImpCollectScoreText=true demotes IMP0001/IMP0002 to warnings for the one build that lists them all.</summary>
    private static bool Collecting(AnalyzerOptions options) =>
        options.AnalyzerConfigOptionsProvider.GlobalOptions.TryGetValue("build_property.ImpCollectScoreText", out var raw)
        && string.Equals(raw, "true", StringComparison.OrdinalIgnoreCase);

    // ── IMP0001: where a score becomes text ─────────────────────────────────────────────────────

    private static void Sink(OperationAnalysisContext context, ScoreFlow flow, DiagnosticSeverity severity)
    {
        if (OutOfScope(context, flow))
        {
            return;
        }

        switch (context.Operation)
        {
            case IInterpolationOperation hole when flow.IsScore(hole.Expression):
                ReportText(context, hole.Syntax, "An interpolated string", hole.Expression, severity);
                break;

            case IInterpolatedStringAppendOperation append
                when append.AppendCall is IInvocationOperation call && call.Arguments.Length > 0 && flow.IsScore(call.Arguments[0].Value):
                ReportText(context, append.Syntax, "An interpolated string", call.Arguments[0].Value, severity);
                break;

            case IBinaryOperation { OperatorKind: BinaryOperatorKind.Add } concat when concat.Type?.SpecialType == SpecialType.System_String:
                var operand = flow.IsScore(concat.LeftOperand) ? concat.LeftOperand : flow.IsScore(concat.RightOperand) ? concat.RightOperand : null;
                if (operand is not null)
                {
                    ReportText(context, concat.Syntax, "String concatenation", operand, severity);
                }

                break;

            case IInvocationOperation invocation:
                if (TextSink(invocation, flow) is { } found)
                {
                    ReportText(context, invocation.Syntax, found.What, found.Score, severity);
                }

                break;
        }
    }

    /// <summary>The call that turns a score into text, and the score it turns: null when the call writes none.</summary>
    private static (string What, IOperation Score)? TextSink(IInvocationOperation invocation, ScoreFlow flow)
    {
        var method = invocation.TargetMethod;
        if (method.Name == "AppendFormatted")
        {
            return null;   // an interpolation hole: reported once, by the append operation
        }

        if (method.Name == "ToString")
        {
            if (invocation.Instance is { } instance && flow.IsScore(instance))
            {
                return ("ToString", instance);
            }

            return FirstScore(invocation, flow, collections: false) is { } converted ? ("Convert.ToString", converted) : null;
        }

        var type = ScoreFlow.FullName(method.ContainingType);
        switch (type)
        {
            case "string" when method.Name is "Format" or "Concat" or "Join":
                return FirstScore(invocation, flow, collections: true) is { } s ? ("string." + method.Name, s) : null;

            case "System.Text.StringBuilder" when method.Name is "Append" or "AppendLine" or "Insert" or "AppendFormat" or "AppendJoin":
                return FirstScore(invocation, flow, collections: true) is { } b ? ("StringBuilder." + method.Name, b) : null;

            case "System.Console" when method.Name is "Write" or "WriteLine":
                return FirstScore(invocation, flow, collections: false) is { } w ? ("Console." + method.Name, w) : null;

            case "Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder":
                var index = method.Name switch { "AddContent" => 1, "AddMarkupContent" => 1, "AddAttribute" => 2, _ => -1 };
                return index >= 0 && invocation.Arguments.Length > index && flow.IsScore(invocation.Arguments[index].Value)
                    ? ("A Razor render (" + method.Name + ")", invocation.Arguments[index].Value)
                    : null;

            case "Microsoft.AspNetCore.Components.BindConverter" when method.Name == "FormatValue":
                return FirstScore(invocation, flow, collections: false) is { } f ? ("A Razor binding", f) : null;
        }

        if (method.Name is "Write" or "WriteLine" && ScoreFlow.DerivesFrom(method.ContainingType, "System.IO.TextWriter"))
        {
            return FirstScore(invocation, flow, collections: false) is { } t ? ("TextWriter." + method.Name, t) : null;
        }

        return null;
    }

    /// <summary>The first argument (looking inside an implicit params array) that is a score, or a collection of scores.</summary>
    private static IOperation? FirstScore(IInvocationOperation invocation, ScoreFlow flow, bool collections)
    {
        foreach (var argument in invocation.Arguments)
        {
            foreach (var value in ScoreFlow.Values(argument))
            {
                if (flow.IsScore(value) || (collections && flow.IsScoreCollection(value)))
                {
                    return value;
                }
            }
        }

        return null;
    }

    private static void ReportText(OperationAnalysisContext context, SyntaxNode at, string what, IOperation score, DiagnosticSeverity severity) =>
        context.ReportDiagnostic(Diagnostic.Create(TextRule, at.GetLocation(), severity, additionalLocations: null, properties: null, what, Snippet(score)));

    // ── IMP0002: a score keeps its name ─────────────────────────────────────────────────────────

    private static void Names(OperationAnalysisContext context, ScoreFlow flow, DiagnosticSeverity severity)
    {
        if (OutOfScope(context, flow))
        {
            return;
        }

        switch (context.Operation)
        {
            case IArgumentOperation { Parameter: { } parameter } argument when argument.ArgumentKind != ArgumentKind.DefaultValue:
                foreach (var value in ScoreFlow.Values(argument))
                {
                    if (Escapes(value, parameter, flow, out var collection))
                    {
                        ReportName(context, value, Describe(parameter, collection), severity);
                        break;
                    }
                }

                break;

            case ISimpleAssignmentOperation assignment:
                var member = assignment.Target switch
                {
                    IPropertyReferenceOperation p when !p.Property.IsIndexer => (ISymbol)p.Property,
                    IFieldReferenceOperation f => f.Field,
                    _ => null,
                };
                if (member is not null && Escapes(assignment.Value, member, flow, out var isCollection))
                {
                    ReportName(context, assignment.Value, Describe(member, isCollection), severity);
                }

                break;

            case IReturnOperation { ReturnedValue: { } returned } when context.ContainingSymbol is IMethodSymbol method && !ScoreFlow.IsLambda(method):
                var named = method.AssociatedSymbol ?? method;
                if (Escapes(returned, named, flow, out var returnsCollection, returnOf: method))
                {
                    ReportName(context, returned, (returnsCollection ? "the scores returned by " : "the return value of ") + Quote(named), severity);
                }

                break;
        }
    }

    /// <summary>Whether a score (or a collection of scores) flows into a target of the product's own code that does not say so.</summary>
    private static bool Escapes(IOperation value, ISymbol target, ScoreFlow flow, out bool collection, IMethodSymbol? returnOf = null)
    {
        collection = false;
        if (!flow.IsOurs(target) || (returnOf is not null && !ScoreFlow.IsNumberOrNumbers(returnOf.ReturnType)))
        {
            return false;
        }

        var scalar = flow.IsScore(value);
        collection = !scalar && flow.IsScoreCollection(value);
        if (!scalar && !collection)
        {
            return false;
        }

        return !flow.SaysScore(target, returnOf);
    }

    private static void ReportName(OperationAnalysisContext context, IOperation value, string into, DiagnosticSeverity severity) =>
        context.ReportDiagnostic(Diagnostic.Create(NameRule, value.Syntax.GetLocation(), severity, additionalLocations: null, properties: null, Snippet(value), into));

    private static string Describe(ISymbol target, bool collection)
    {
        var what = target switch
        {
            IParameterSymbol p => "parameter '" + p.Name + "' of " + Quote(p.ContainingSymbol),
            IPropertySymbol p => "property " + Quote(p),
            IFieldSymbol f => "field " + Quote(f),
            _ => Quote(target),
        };
        return collection ? what + " (a collection of scores)" : what;
    }

    private static string Quote(ISymbol symbol)
    {
        var owner = symbol is IMethodSymbol { MethodKind: MethodKind.Constructor } ctor ? ctor.ContainingType : symbol;
        var name = owner.ContainingType is { } type && owner is not INamedTypeSymbol ? type.Name + "." + owner.Name : owner.Name;
        return "'" + name + "'";
    }

    // ── The exemption's reason ──────────────────────────────────────────────────────────────────

    private static void BlankReasons(SymbolAnalysisContext context, DiagnosticSeverity severity)
    {
        var symbol = context.Symbol;
        var carriers = new List<ImmutableArray<AttributeData>> { symbol.GetAttributes() };
        if (symbol is IMethodSymbol method)
        {
            carriers.Add(method.GetReturnTypeAttributes());
            carriers.AddRange(method.Parameters.Select(p => p.GetAttributes()));
        }

        foreach (var attribute in carriers.SelectMany(a => a))
        {
            if (attribute.AttributeClass?.ToDisplayString() == NotAScoreAttributeMetadataName && ScoreFlow.BlankReason(attribute))
            {
                var at = attribute.ApplicationSyntaxReference?.GetSyntax(context.CancellationToken).GetLocation() ?? symbol.Locations.FirstOrDefault() ?? Location.None;
                context.ReportDiagnostic(Diagnostic.Create(BlankReasonRule, at, severity, additionalLocations: null, properties: null, Quote(symbol)));
            }
        }
    }

    // ── Scope ───────────────────────────────────────────────────────────────────────────────────

    /// <summary>The formatter itself, a member or type exempted with its reason, and generated code that is not Razor.</summary>
    private static bool OutOfScope(OperationAnalysisContext context, ScoreFlow flow)
    {
        var tree = context.Operation.Syntax.SyntaxTree;
        if (IsGeneratedNotRazor(tree.FilePath))
        {
            return true;
        }

        for (var symbol = context.ContainingSymbol; symbol is not null and not INamespaceSymbol; symbol = symbol.ContainingSymbol)
        {
            if (flow.IsFormatter(symbol as INamedTypeSymbol) || flow.Exempt(symbol) || (symbol is IMethodSymbol { AssociatedSymbol: { } property } && flow.Exempt(property)))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsGeneratedNotRazor(string? path) =>
        path is not null
        && (path.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".generated.cs", StringComparison.OrdinalIgnoreCase))
        && path.IndexOf("razor", StringComparison.OrdinalIgnoreCase) < 0;

    private static string Snippet(IOperation operation)
    {
        var text = string.Join(" ", operation.Syntax.ToString().Split(['\r', '\n', '\t'], StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()));
        return text.Length <= 80 ? text : text.Substring(0, 77) + "...";
    }

    // ── What a score is ─────────────────────────────────────────────────────────────────────────

    /// <summary>Follows a score through one compilation: names and marks, what is done to a score, and locals and lambda
    /// parameters given one. Thread-safe; its caches are per compilation.</summary>
    private sealed class ScoreFlow(Compilation compilation)
    {
        private const int MaxDepth = 16;

        private readonly ConcurrentDictionary<(ISymbol, bool), bool> _locals = new(new LocalKeyComparer());

        private readonly ConcurrentDictionary<SyntaxNode, Dictionary<ILocalSymbol, List<IOperation>>> _assignments = new();

        private readonly IAssemblySymbol _assembly = compilation.Assembly;

        public static string FullName(ITypeSymbol? type) => type?.OriginalDefinition.ToDisplayString() ?? "";

        public static bool IsLambda(IMethodSymbol method) => method.MethodKind is MethodKind.AnonymousFunction or MethodKind.LambdaMethod;

        public bool IsFormatter(INamedTypeSymbol? type)
        {
            for (var t = type; t is not null; t = t.ContainingType)
            {
                if (t.ToDisplayString() == FormatterMetadataName)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>The values an argument carries: itself, or the elements of an implicit params array.</summary>
        public static IEnumerable<IOperation> Values(IArgumentOperation argument)
        {
            if (argument.ArgumentKind == ArgumentKind.ParamArray && argument.Value is IArrayCreationOperation { Initializer: { } init })
            {
                return init.ElementValues;
            }

            return new[] { argument.Value };
        }

        public static bool DerivesFrom(INamedTypeSymbol? type, string fullName)
        {
            for (var t = type; t is not null; t = t.BaseType)
            {
                if (FullName(t) == fullName)
                {
                    return true;
                }
            }

            return false;
        }

        // ── Types ───────────────────────────────────────────────────────────────────────────────

        public static bool IsNumber(ITypeSymbol? type)
        {
            if (type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } nullable)
            {
                type = nullable.TypeArguments[0];
            }

            return type?.SpecialType is SpecialType.System_Double or SpecialType.System_Single or SpecialType.System_Decimal
                or SpecialType.System_Int32 or SpecialType.System_Int64 or SpecialType.System_Int16 or SpecialType.System_Byte
                or SpecialType.System_SByte or SpecialType.System_UInt16 or SpecialType.System_UInt32 or SpecialType.System_UInt64;
        }

        public static bool IsNumbers(ITypeSymbol? type) => ElementOf(type) is { } element && IsNumber(element);

        public static bool IsNumberOrNumbers(ITypeSymbol? type) => IsNumber(type) || IsNumbers(type);

        private static ITypeSymbol? ElementOf(ITypeSymbol? type)
        {
            switch (type)
            {
                case null:
                    return null;
                case IArrayTypeSymbol array:
                    return array.ElementType;
                case INamedTypeSymbol { SpecialType: SpecialType.System_String }:
                    return null;
                case INamedTypeSymbol named when named.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T:
                    return named.TypeArguments[0];
            }

            return type.AllInterfaces.FirstOrDefault(i => i.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T)?.TypeArguments[0];
        }

        // ── Names and marks ─────────────────────────────────────────────────────────────────────

        /// <summary>A symbol of the product's own code, which can be renamed or marked: not the runtime's, not a library's,
        /// not an anonymous payload's, not the formatter's.</summary>
        public bool IsOurs(ISymbol target)
        {
            var ns = target.ContainingNamespace?.ToDisplayString() ?? "";
            if (ns == "System" || ns.StartsWith("System.", StringComparison.Ordinal) || ns == "Microsoft" || ns.StartsWith("Microsoft.", StringComparison.Ordinal))
            {
                return false;
            }

            var type = target as INamedTypeSymbol ?? target.ContainingType ?? (target.ContainingSymbol as IMethodSymbol)?.ContainingType;
            if (type is { IsAnonymousType: true } || IsFormatter(type) || target.ContainingSymbol is IMethodSymbol { MethodKind: MethodKind.DelegateInvoke })
            {
                return false;
            }

            var assembly = target.ContainingAssembly;
            return assembly is not null
                && (SymbolEqualityComparer.Default.Equals(assembly, _assembly) || assembly.Name.StartsWith("Imprint", StringComparison.Ordinal));
        }

        /// <summary>Whether a target says it holds a score: its name, [CaiScore], or a reasoned [NotACaiScore] (a declared
        /// non-score is a declaration too). A constructor parameter and the property it initialises speak for each other.</summary>
        public bool SaysScore(ISymbol target, IMethodSymbol? returnOf = null)
        {
            if (returnOf is not null && ReturnsNotAScore(returnOf))
            {
                return true;
            }

            foreach (var symbol in Linked(target))
            {
                if (ScoreNames.IsScoreName(NameOf(symbol)) || Marked(symbol, returnOf) || Exempt(symbol))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Whether a symbol holding a number (or numbers) is a score: by its name or [CaiScore], unless exempted.</summary>
        public bool IsScoreSymbol(ISymbol symbol, ITypeSymbol? type, bool collection)
        {
            if (collection ? !IsNumbers(type) : !IsNumber(type))
            {
                return false;
            }

            var linked = Linked(symbol);
            if (linked.Any(Exempt) || (symbol is IMethodSymbol method && ReturnsNotAScore(method)))
            {
                return false;
            }

            return linked.Any(s => Marked(s, s as IMethodSymbol) || ScoreNames.IsScoreName(NameOf(s)));
        }

        /// <summary>[NotACaiScore("why")] with a reason, on the symbol itself. On a method or type it also exempts the
        /// code inside; <c>[return: NotACaiScore]</c> says only that the result is not a score (<see cref="ReturnsNotAScore"/>).</summary>
        public bool Exempt(ISymbol symbol) =>
            symbol.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString() == NotAScoreAttributeMetadataName && !BlankReason(a));

        public static bool BlankReason(AttributeData attribute) =>
            attribute.ConstructorArguments.Length == 0 || string.IsNullOrWhiteSpace(attribute.ConstructorArguments[0].Value as string);

        private static bool Marked(ISymbol symbol, IMethodSymbol? returnOf) =>
            AttributesOf(symbol, returnOf).Any(a => a.AttributeClass?.ToDisplayString() == ScoreAttributeMetadataName);

        private static IEnumerable<AttributeData> AttributesOf(ISymbol symbol, IMethodSymbol? method)
        {
            foreach (var attribute in symbol.GetAttributes())
            {
                yield return attribute;
            }

            if (method is not null)
            {
                foreach (var attribute in method.GetReturnTypeAttributes())
                {
                    yield return attribute;
                }
            }
        }

        private static string NameOf(ISymbol symbol) => symbol is IMethodSymbol { AssociatedSymbol: { } property } ? property.Name : symbol.Name;

        /// <summary>The symbol, and the one that speaks for it: a constructor parameter and the property or field of the same name.</summary>
        private static List<ISymbol> Linked(ISymbol symbol)
        {
            var linked = new List<ISymbol> { symbol };
            switch (symbol)
            {
                case IParameterSymbol { ContainingSymbol: IMethodSymbol { MethodKind: MethodKind.Constructor } ctor } parameter:
                    linked.AddRange(ctor.ContainingType.GetMembers()
                        .Where(m => m is IPropertySymbol or IFieldSymbol && string.Equals(m.Name, parameter.Name, StringComparison.OrdinalIgnoreCase)));
                    break;
                case IParameterSymbol { ContainingSymbol: IMethodSymbol method } when method.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString() == NotAScoreAttributeMetadataName && !BlankReason(a)):
                    linked.Add(method);   // a helper exempted for its numbers declares its parameters too
                    break;
                case IPropertySymbol or IFieldSymbol when symbol.ContainingType is { } type:
                    linked.AddRange(type.InstanceConstructors.SelectMany(c => c.Parameters)
                        .Where(p => string.Equals(p.Name, symbol.Name, StringComparison.OrdinalIgnoreCase)));
                    break;
                case IMethodSymbol { AssociatedSymbol: { } property }:
                    linked.Add(property);
                    break;
            }

            return linked;
        }

        // ── Scores ──────────────────────────────────────────────────────────────────────────────

        public bool IsScore(IOperation? operation) => IsScore(operation, 0);

        public bool IsScoreCollection(IOperation? operation) => IsScoreCollection(operation, 0);

        private bool IsScore(IOperation? operation, int depth)
        {
            operation = Unwrap(operation);
            if (operation is null || depth > MaxDepth)
            {
                return false;
            }

            var next = depth + 1;
            switch (operation)
            {
                case IPropertyReferenceOperation p when p.Property.ContainingType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T:
                    return p.Property.Name == "Value" && IsScore(p.Instance, next);
                case IPropertyReferenceOperation { Property.IsIndexer: true } indexer:
                    return IsNumber(indexer.Type) && (IsScoreCollection(indexer.Instance, next) || IsScoreSymbol(indexer.Property, indexer.Type, collection: false));
                case IPropertyReferenceOperation p:
                    return IsScoreSymbol(p.Property, p.Type, collection: false);
                case IFieldReferenceOperation f:
                    return IsScoreSymbol(f.Field, f.Type, collection: false);
                case ILocalReferenceOperation l:
                    return IsScoreSymbol(l.Local, l.Type, collection: false) || LocalHolds(l, collection: false, next);
                case IParameterReferenceOperation p:
                    return IsScoreSymbol(p.Parameter, p.Type, collection: false) || LambdaElement(p, next);
                case IInvocationOperation i:
                    return InvocationIsScore(i, next);
                case IBinaryOperation { OperatorKind: BinaryOperatorKind.Add or BinaryOperatorKind.Subtract } b when IsNumber(b.Type):
                    return IsScore(b.LeftOperand, next) || IsScore(b.RightOperand, next);
                case IUnaryOperation { OperatorKind: UnaryOperatorKind.Minus or UnaryOperatorKind.Plus } u:
                    return IsScore(u.Operand, next);
                case IConditionalOperation { WhenFalse: { } whenFalse } c when IsNumber(c.Type):
                    return IsScore(c.WhenTrue, next) || IsScore(whenFalse, next);
                case ICoalesceOperation c:
                    return IsScore(c.Value, next) || IsScore(c.WhenNull, next);
                case IConditionalAccessOperation c:
                    return IsScore(c.WhenNotNull, next);
                case IConditionalAccessInstanceOperation instance:
                    return IsScore(ConditionalTarget(instance), next);
                case IArrayElementReferenceOperation a:
                    return IsNumber(a.Type) && IsScoreCollection(a.ArrayReference, next);
                case IImplicitIndexerReferenceOperation ii:
                    return IsNumber(ii.Type) && IsScoreCollection(ii.Instance, next);
                case ISwitchExpressionOperation s when IsNumber(s.Type):
                    return s.Arms.Any(arm => IsScore(arm.Value, next));
                default:
                    return false;
            }
        }

        private bool IsScoreCollection(IOperation? operation, int depth)
        {
            operation = Unwrap(operation);
            if (operation is null || depth > MaxDepth)
            {
                return false;
            }

            var next = depth + 1;
            switch (operation)
            {
                case IPropertyReferenceOperation { Property.IsIndexer: false } p:
                    return IsScoreSymbol(p.Property, p.Type, collection: true);
                case IFieldReferenceOperation f:
                    return IsScoreSymbol(f.Field, f.Type, collection: true);
                case ILocalReferenceOperation l:
                    return IsScoreSymbol(l.Local, l.Type, collection: true) || LocalHolds(l, collection: true, next);
                case IParameterReferenceOperation p:
                    return IsScoreSymbol(p.Parameter, p.Type, collection: true);
                case IInvocationOperation i:
                    return InvocationIsScoreCollection(i, next);
                case ICollectionExpressionOperation c when IsNumbers(c.Type):
                    return c.Elements.Any(e => e is ISpreadOperation spread ? IsScoreCollection(spread.Operand, next) : IsScore(e, next));
                case IArrayCreationOperation { Initializer: { } init } a when IsNumbers(a.Type):
                    return init.ElementValues.Any(e => IsScore(e, next));
                case IObjectCreationOperation o when IsNumbers(o.Type):
                    return o.Arguments.Any(a => IsScoreCollection(a.Value, next));
                case IConditionalOperation { WhenFalse: { } whenFalse } c:
                    return IsScoreCollection(c.WhenTrue, next) || IsScoreCollection(whenFalse, next);
                case ICoalesceOperation c:
                    return IsScoreCollection(c.Value, next) || IsScoreCollection(c.WhenNull, next);
                case IConditionalAccessOperation c:
                    return IsScoreCollection(c.WhenNotNull, next);
                case IConditionalAccessInstanceOperation instance:
                    return IsScoreCollection(ConditionalTarget(instance), next);
                default:
                    return false;
            }
        }

        private static IOperation? Unwrap(IOperation? operation)
        {
            while (operation is IConversionOperation or IParenthesizedOperation)
            {
                operation = operation switch
                {
                    IConversionOperation c => c.Operand,
                    IParenthesizedOperation p => p.Operand,
                    _ => operation,
                };
            }

            return operation;
        }

        private static IOperation? ConditionalTarget(IConditionalAccessInstanceOperation instance)
        {
            for (var parent = instance.Parent; parent is not null; parent = parent.Parent)
            {
                if (parent is IConditionalAccessOperation access)
                {
                    return access.Operation;
                }
            }

            return null;
        }

        private static readonly HashSet<string> MathFollows = new(StringComparer.Ordinal)
        {
            "Round", "Floor", "Ceiling", "Truncate", "Abs", "Clamp", "Max", "Min", "CopySign",
        };

        private static readonly HashSet<string> LinqElement = new(StringComparer.Ordinal)
        {
            "Average", "Sum", "Min", "Max", "First", "FirstOrDefault", "Last", "LastOrDefault", "Single", "SingleOrDefault",
            "ElementAt", "ElementAtOrDefault", "Aggregate", "MinBy", "MaxBy",
        };

        private static readonly HashSet<string> LinqPassThrough = new(StringComparer.Ordinal)
        {
            "Where", "OrderBy", "OrderByDescending", "ThenBy", "ThenByDescending", "Order", "OrderDescending", "Take", "Skip",
            "TakeLast", "SkipLast", "TakeWhile", "SkipWhile", "Reverse", "Distinct", "ToList", "ToArray", "ToHashSet", "AsEnumerable",
            "Append", "Prepend", "Concat", "DefaultIfEmpty", "Chunk",
        };

        private bool InvocationIsScore(IInvocationOperation invocation, int depth)
        {
            var method = invocation.TargetMethod;
            if (!IsNumber(method.ReturnType))
            {
                return false;
            }

            if (IsFormatter(method.ContainingType))
            {
                return true;   // CaiScoreText.Rounded: still a score, so writing it is still the formatter's job
            }

            if (ReturnsNotAScore(method))
            {
                return false;   // [return: NotACaiScore("why")]: a percentile rank, a share, a count
            }

            if (method.ContainingType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
            {
                return method.Name == "GetValueOrDefault" && IsScore(invocation.Instance, depth);
            }

            switch (FullName(method.ContainingType))
            {
                case "System.Math" when MathFollows.Contains(method.Name):
                case "System.Convert" when method.Name.StartsWith("To", StringComparison.Ordinal):
                    return invocation.Arguments.Any(a => IsScore(a.Value, depth));
                case "System.Linq.Enumerable" when LinqElement.Contains(method.Name):
                    return IsScoreCollection(Source(invocation), depth) || invocation.Arguments.Skip(1).Any(a => SelectsScore(a.Value, depth));
            }

            // A statistic of the product's own over a collection of scores (a median, a percentile, a mean) is a score.
            if (IsOurs(method) && invocation.Arguments.Any(a => IsScoreCollection(a.Value, depth)))
            {
                return true;
            }

            return IsScoreSymbol(method, method.ReturnType, collection: false);
        }

        /// <summary>A method whose RESULT is declared not a score: <c>[return: NotACaiScore("why")]</c>.</summary>
        private static bool ReturnsNotAScore(IMethodSymbol method) =>
            method.GetReturnTypeAttributes().Any(a => a.AttributeClass?.ToDisplayString() == NotAScoreAttributeMetadataName && !BlankReason(a));

        private bool InvocationIsScoreCollection(IInvocationOperation invocation, int depth)
        {
            var method = invocation.TargetMethod;
            if (!IsNumbers(method.ReturnType))
            {
                return false;
            }

            if (FullName(method.ContainingType) == "System.Linq.Enumerable")
            {
                if (method.Name is "Select" or "SelectMany")
                {
                    return invocation.Arguments.Skip(1).Any(a => SelectsScore(a.Value, depth));
                }

                if (LinqPassThrough.Contains(method.Name))
                {
                    return IsScoreCollection(Source(invocation), depth);
                }
            }

            if (method.Name is "ToList" or "ToArray" && invocation.Instance is { } instance)
            {
                return IsScoreCollection(instance, depth);
            }

            return IsScoreSymbol(method, method.ReturnType, collection: true);
        }

        private static IOperation? Source(IInvocationOperation invocation) =>
            invocation.Instance ?? (invocation.Arguments.Length > 0 ? invocation.Arguments[0].Value : null);

        /// <summary>Whether a selector (a lambda, or a method group) returns a score.</summary>
        private bool SelectsScore(IOperation? selector, int depth)
        {
            selector = Unwrap(selector);
            if (selector is IDelegateCreationOperation creation)
            {
                selector = creation.Target;
            }

            return selector switch
            {
                IAnonymousFunctionOperation lambda => lambda.Body.Descendants().OfType<IReturnOperation>().Any(r => IsScore(r.ReturnedValue, depth)),
                IMethodReferenceOperation group => IsFormatter(group.Method.ContainingType) ? IsNumber(group.Method.ReturnType) : IsScoreSymbol(group.Method, group.Method.ReturnType, collection: false),
                _ => false,
            };
        }

        /// <summary>A lambda's element parameter over a collection of scores: <c>scores.Select(s => …)</c>.</summary>
        private bool LambdaElement(IParameterReferenceOperation reference, int depth)
        {
            var parameter = reference.Parameter;
            if (parameter.Ordinal != 0 || !IsNumber(parameter.Type) || parameter.ContainingSymbol is not IMethodSymbol lambda || !IsLambda(lambda))
            {
                return false;
            }

            for (var parent = reference.Parent; parent is not null; parent = parent.Parent)
            {
                if (parent is IAnonymousFunctionOperation function && SymbolEqualityComparer.Default.Equals(function.Symbol, lambda))
                {
                    var invocation = Ancestor<IInvocationOperation>(function);
                    return invocation is not null && IsScoreCollection(Source(invocation), depth);
                }
            }

            return false;
        }

        private static T? Ancestor<T>(IOperation operation)
            where T : class, IOperation
        {
            for (var parent = operation.Parent; parent is not null; parent = parent.Parent)
            {
                if (parent is T found)
                {
                    return found;
                }
            }

            return null;
        }

        /// <summary>A local that was given a score, wherever its name: its initializer, a foreach over scores, a pattern
        /// over a score, or any assignment to it in the member that declares it.</summary>
        private bool LocalHolds(ILocalReferenceOperation reference, bool collection, int depth)
        {
            var local = reference.Local;
            var key = ((ISymbol)local, collection);
            if (_locals.TryGetValue(key, out var known))
            {
                return known;
            }

            // A local assigned from itself (v = v + 1) must not make itself a score. The guard is per thread, so another
            // thread never reads a half-computed answer: only finished answers go into the shared cache.
            var visiting = _visiting ??= new HashSet<(ISymbol, bool)>(new LocalKeyComparer());
            if (!visiting.Add(key))
            {
                return false;
            }

            try
            {
                var holds = Compute(reference, local, collection, depth);
                _locals[key] = holds;
                return holds;
            }
            finally
            {
                visiting.Remove(key);
            }
        }

        [ThreadStatic]
        private static HashSet<(ISymbol, bool)>? _visiting;

        private bool Compute(ILocalReferenceOperation reference, ILocalSymbol local, bool collection, int depth)
        {
            var holds = false;
            var root = Root(reference);
            var model = reference.SemanticModel;

            foreach (var declaration in local.DeclaringSyntaxReferences.Select(r => r.GetSyntax()))
            {
                if (model is null || declaration.SyntaxTree != model.SyntaxTree)
                {
                    continue;
                }

                switch (declaration)
                {
                    case VariableDeclaratorSyntax { Initializer: { } init }:
                        holds |= Holds(model.GetOperation(init.Value), collection, depth);
                        break;
                    case ForEachStatementSyntax loop when !collection:
                        holds |= IsScoreCollection(model.GetOperation(loop.Expression), depth);
                        break;
                    case SingleVariableDesignationSyntax designation when !collection && PatternMember(designation, model) is { } member:
                        // `r is { Cai: { } c }`: c is the member the subpattern names, not the value r.
                        holds |= IsScoreSymbol(member, member switch { IPropertySymbol p => p.Type, IFieldSymbol f => f.Type, _ => null }, collection: false);
                        break;
                    case SingleVariableDesignationSyntax designation when !collection && PatternInput(designation) is { } input:
                        holds |= IsScore(model.GetOperation(input), depth);
                        break;
                }
            }

            if (!holds && Assignments(root).TryGetValue(local, out var values))
            {
                holds = values.Any(v => Holds(v, collection, depth));
            }

            return holds;
        }

        private bool Holds(IOperation? value, bool collection, int depth) => collection ? IsScoreCollection(value, depth) : IsScore(value, depth);

        /// <summary>The member a property subpattern binds a pattern variable to: <c>Cai</c> in <c>r is { Cai: { } c }</c>; null
        /// when the variable is not inside a named subpattern (then <see cref="PatternInput"/> says what it was matched against).</summary>
        private static ISymbol? PatternMember(SyntaxNode designation, SemanticModel model)
        {
            for (var node = designation.Parent; node is not null; node = node.Parent)
            {
                switch (node)
                {
                    case SubpatternSyntax { ExpressionColon.Expression: { } named }:
                        return model.GetSymbolInfo(named).Symbol is IPropertySymbol or IFieldSymbol ? model.GetSymbolInfo(named).Symbol : null;
                    case IsPatternExpressionSyntax or SwitchExpressionSyntax or SwitchStatementSyntax or StatementSyntax or MemberDeclarationSyntax:
                        return null;
                }
            }

            return null;
        }

        /// <summary>What a pattern variable was matched against: <c>x</c> in <c>x is { } v</c>, a switch's governing value.</summary>
        private static ExpressionSyntax? PatternInput(SyntaxNode designation)
        {
            for (var node = designation.Parent; node is not null; node = node.Parent)
            {
                switch (node)
                {
                    case IsPatternExpressionSyntax isPattern:
                        return isPattern.Expression;
                    case SwitchExpressionSyntax switchExpression:
                        return switchExpression.GoverningExpression;
                    case SwitchStatementSyntax switchStatement:
                        return switchStatement.Expression;
                    case StatementSyntax or MemberDeclarationSyntax:
                        return null;
                }
            }

            return null;
        }

        private static IOperation Root(IOperation operation)
        {
            while (operation.Parent is { } parent)
            {
                operation = parent;
            }

            return operation;
        }

        /// <summary>Every assignment to a local in one member body, collected once per body.</summary>
        private Dictionary<ILocalSymbol, List<IOperation>> Assignments(IOperation root) =>
            _assignments.GetOrAdd(root.Syntax, _ =>
            {
                var map = new Dictionary<ILocalSymbol, List<IOperation>>(SymbolEqualityComparer.Default);
                foreach (var operation in root.Descendants())
                {
                    if (operation is ISimpleAssignmentOperation { Target: ILocalReferenceOperation target } assignment)
                    {
                        if (!map.TryGetValue(target.Local, out var list))
                        {
                            map[target.Local] = list = new List<IOperation>();
                        }

                        list.Add(assignment.Value);
                    }
                }

                return map;
            });

        private sealed class LocalKeyComparer : IEqualityComparer<(ISymbol Symbol, bool Collection)>
        {
            public bool Equals((ISymbol Symbol, bool Collection) x, (ISymbol Symbol, bool Collection) y) =>
                x.Collection == y.Collection && SymbolEqualityComparer.Default.Equals(x.Symbol, y.Symbol);

            public int GetHashCode((ISymbol Symbol, bool Collection) obj) =>
                (SymbolEqualityComparer.Default.GetHashCode(obj.Symbol) * 397) ^ obj.Collection.GetHashCode();
        }
    }
}

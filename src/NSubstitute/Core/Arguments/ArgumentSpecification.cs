using System;

namespace NSubstitute.Core.Arguments
{
    public class ArgumentSpecification : IArgumentSpecification
    {
        private static readonly Action<object?> NoOpAction = x => { };
 
        private readonly IArgumentMatcher _matcher;
        private readonly Action<object?> _action;
        public Type ForType { get; }
        public bool HasAction => _action != NoOpAction;

        public ArgumentSpecification(Type forType, IArgumentMatcher matcher) : this(forType, matcher, NoOpAction) { }

        public ArgumentSpecification(Type forType, IArgumentMatcher matcher, Action<object?> action)
        {
            ForType = forType;
            _matcher = matcher;
            _action = action;
        }

        public bool IsSatisfiedBy(object? argument)
        {
            if (!IsCompatibleWith(argument)) return false;
            try { return _matcher.IsSatisfiedBy(argument); }
            catch { return false; }
        }

        public string DescribeNonMatch(object? argument)
        {
            var describable = _matcher as IDescribeNonMatches;
            if (describable == null) return string.Empty;

            return IsCompatibleWith(argument) ? describable.DescribeFor(argument) : GetIncompatibleTypeMessage(argument);
        }

        public string FormatArgument(object? argument)
        {
            var isSatisfiedByArg = IsSatisfiedBy(argument);
            var matcherFormatter = _matcher as IArgumentFormatter;
            return matcherFormatter == null
                ? ArgumentFormatter.Default.Format(argument, !isSatisfiedByArg)
                : matcherFormatter.Format(argument, isSatisfiedByArg);
        }

        public override string ToString() => _matcher.ToString() ?? string.Empty;

        public IArgumentSpecification CreateCopyMatchingAnyArgOfType(Type requiredType)
        {
            // Don't pass RunActionIfTypeIsCompatible method if no action is present.
            // Otherwise, unnecessary closure will keep reference to this and will keep it alive.
            return new ArgumentSpecification(
                requiredType,
                new AnyArgumentMatcher(requiredType),
                _action == NoOpAction ? NoOpAction : RunActionIfTypeIsCompatible);
        }

        public void RunAction(object? argument)
        {
            _action(argument);
        }

        private void RunActionIfTypeIsCompatible(object? argument)
        {
            if (!argument.IsCompatibleWith(ForType)) return;
            _action(argument);
        }

        private bool IsCompatibleWith(object? argument)
        {
            return argument.IsCompatibleWith(ForType);
        }

        private string GetIncompatibleTypeMessage(object? argument)
        {
            var argumentType = argument == null ? typeof(object) : argument.GetType();
            return string.Format("Expected an argument compatible with type {0}. Actual type was {1}.", ForType, argumentType);
        }
    }
}
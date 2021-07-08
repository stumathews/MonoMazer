using System;
using LanguageExt;

namespace ImmutableTests
{
    public partial class ValueStackTests
    {
        public class StackDescriptor
        {
            public StackDescriptor(object valueStack, Type stackType, Option<StackOptions> options)
            {
                this.valueStack = valueStack;
                this.stackType = stackType;
                _options = options;
            }
            public object valueStack;
            public Type stackType;
            public Option<StackOptions> _options;
        }
    }
}
using System;

namespace WhiteDependencyInjection
{
    public static class SourceGeneratorBuilderExtensions
    {
        private const char OpenBracket = '{';
        private const string CloseBracket = "}";

        public static SourceGeneratorBuilder AppendOpenBracket(this SourceGeneratorBuilder builder) =>
            builder.Append(OpenBracket);

        public static SourceGeneratorBuilder AppendCloseBracket(this SourceGeneratorBuilder builder) =>
            builder.Append(CloseBracket);

        public static SourceGeneratorBuilder AppendMethod(this SourceGeneratorBuilder builder, string modifier,
            string name,
            string returnType, Action<SourceGeneratorBuilder> setBody, params string[] parameters)
        {
            var paramList = string.Join(", ", parameters);
            builder.Append($"{modifier} {returnType} {name}({paramList})").AppendOpenBracket();
            setBody(builder);
            builder.AppendCloseBracket();
            return builder;
        }
        
        public static SourceGeneratorBuilder AppendMethod(this SourceGeneratorBuilder builder, string modifier,
            string name, string genericType,
            string returnType, Action<SourceGeneratorBuilder> setBody, params string[] parameters)
        {
            var paramList = string.Join(", ", parameters);
            builder.Append($"{modifier} {returnType} {name}({paramList}) where T: {genericType}").AppendOpenBracket();
            setBody(builder);
            builder.AppendCloseBracket();
            return builder;
        }

        public static SourceGeneratorBuilder AppendClass(this SourceGeneratorBuilder builder, string name,
            string baseType,
            Action<SourceGeneratorBuilder> setBody)
        {
            builder.Append($"public sealed class {name}: {baseType}").AppendOpenBracket();
            setBody(builder);
            builder.AppendCloseBracket();
            return builder;
        }
    }
}
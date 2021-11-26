﻿using System;
using System.Runtime.CompilerServices;
using BindingMode = Microsoft.UI.Xaml.Data.BindingMode;
using IValueConverter = Microsoft.UI.Xaml.Data.IValueConverter;
using UpdateSourceTrigger = Microsoft.UI.Xaml.Data.UpdateSourceTrigger;

namespace Microsoft.UI.Markup
{
    public interface IUI<TUI> where TUI : Xaml.DependencyObject
    {
        TUI UI { get; }
    }

#if !WINDOWS
    public partial class DependencyObject : IUI<Xaml.DependencyObject>
    {
        public Xaml.DependencyObject UI { get; protected set; }

        protected DependencyObject() { }
    }
#endif


    /// <summary>Optional <typeparamref name="TValue"/> parameter</summary>
    public struct O<TValue>
    {
        internal TValue Value;
        internal bool HasValue;
        public static implicit operator O<TValue>(TValue value) => new O<TValue> { Value = value, HasValue = true };
    }

    // Since DependencyObject is the root object of the UI hierarchy, this is a good place to explain why and how a single static instance of a chain is safe:

    // Chains have a separate static instance per class; that instance is also a separate instance of all the ancestor classes of that class.
    // The UI property on that instance is also a separate instance per class. E.g. Button and Brush are separate chain instances which are separate DepencyObject instances.
    // So you can nest those chains without breaking them.
    // However what would break a chain is if you try to nest multiple chains of the same class; then the effect would be that you replace the UI of the parent chain with the child UI.

    // But because we use helper parameters for composition == nesting (of views or of property values), and we are executing on a single thread,
    // we prevent ever nesting chains of the same class.
    // TODO: In debug mode, throw an exception if you start a chain within an Invoke() to prevent nesting. Is that enough? Or can we detect the end of a chain through an assignment? Maybe optional end method only present in unit tests?

    // The language guarantuees that all parameter chains are completed before the parent chain is started, and that only a single parameter chain is evaluated at the same time.
    // So even if we e.g. put a StackPanel in a StackPanel with StackPanel(StackPanel()), this does not break a chain.

    public static partial class DependencyObjectExtensions
    {
        const string bindingContextPath = ""; // TODO: Find framework static var for this
        // TODO: Check how much overhead having all these generics introduces (app size binary bloat in eg wasm?)

        /// <summary>Bind to a specified property</summary>
        public static TDependencyObject Bind<TDependencyObject, TPropertyValue>(
            this DependencyProperty<TDependencyObject, TPropertyValue> property,
            object pathExpression = null,
            BindingMode mode = BindingMode.OneWay,
            IValueConverter converter = null,
            object converterParameter = null,
            string converterLanguage = null,
            UpdateSourceTrigger updateSourceTrigger = UpdateSourceTrigger.Default,
            object source = null,
            TPropertyValue targetNullValue = default,
            TPropertyValue fallbackValue = default,
            [CallerArgumentExpression("pathExpression")] string pathExpressionString = default
        ) where TDependencyObject : DependencyObject
        => property.BindWithString(
            StripExpressionToPath(pathExpressionString),
            mode,
            converter,
            converterParameter,
            converterLanguage,
            updateSourceTrigger,
            source,
            targetNullValue,
            fallbackValue
        );

        /// <remarks>This overload allows to pass a string <paramref name="path"/> instead of a pathExpression. A pathExpression only uses the part after the last '.', while <paramref name="path"/> allows to specify paths that contain '.'</remarks>
        public static TDependencyObject BindWithString<TDependencyObject, TPropertyValue>(
            this DependencyProperty<TDependencyObject, TPropertyValue> property,
            string path = bindingContextPath,
            BindingMode mode = BindingMode.OneWay,
            IValueConverter converter = null,
            object converterParameter = null,
            string converterLanguage = null,
            UpdateSourceTrigger updateSourceTrigger = UpdateSourceTrigger.Default,
            object source = null,
            TPropertyValue targetNullValue = default,
            TPropertyValue fallbackValue = default
        ) where TDependencyObject : DependencyObject
        => property.SetBinding(new Microsoft.UI.Xaml.Data.Binding
        {
            Path = new Xaml.PropertyPath(path),
            Mode = mode,
            Converter = converter,
            ConverterParameter = converterParameter,
            //ConverterLanguage = converterLanguage, // TODO: figure out correct default; can't be null
            UpdateSourceTrigger = updateSourceTrigger,
            Source = source,
            // TODO: RelativeSource
            TargetNullValue = targetNullValue,
            FallbackValue = fallbackValue,
        });

        /// <summary>Bind to a specified property with inline conversion</summary>
        public static TDependencyObject Bind<TDependencyObject, TPropertyValue, TSource>(
            this DependencyProperty<TDependencyObject, TPropertyValue> property,
            object pathExpression = null,
            BindingMode mode = BindingMode.OneWay,
            Func<TSource, TPropertyValue> convert = null,
            Func<TPropertyValue, TSource> convertBack = null,
            string converterLanguage = null,
            UpdateSourceTrigger updateSourceTrigger = UpdateSourceTrigger.Default,
            object source = null,
            TPropertyValue targetNullValue = default,
            [CallerArgumentExpression("pathExpression")] string pathExpressionString = default
        ) where TDependencyObject : DependencyObject
        => property.BindWithString(
            StripExpressionToPath(pathExpressionString),
            mode,
            new FuncConverter<TSource, TPropertyValue, object>(convert, convertBack),
            null,
            converterLanguage,
            updateSourceTrigger,
            source,
            targetNullValue
        );

        /// <summary>Bind to a specified property with inline conversion and conversion parameter</summary>
        public static TDependencyObject Bind<TDependencyObject, TPropertyValue, TSource, TParam>(
            this DependencyProperty<TDependencyObject, TPropertyValue> property,
            object pathExpression = null,
            BindingMode mode = BindingMode.OneWay,
            Func<TSource, TParam, TPropertyValue> convert = null,
            Func<TPropertyValue, TParam, TSource> convertBack = null,
            TParam converterParameter = default,
            string converterLanguage = null,
            UpdateSourceTrigger updateSourceTrigger = UpdateSourceTrigger.Default,
            object source = null,
            TPropertyValue targetNullValue = default,
            [CallerArgumentExpression("pathExpression")] string pathExpressionString = default
        ) where TDependencyObject : DependencyObject
        => property.BindWithString(
            StripExpressionToPath(pathExpressionString),
            mode,
            new FuncConverter<TSource, TPropertyValue, TParam>(convert, convertBack),
            converterParameter,
            converterLanguage,
            updateSourceTrigger,
            source,
            targetNullValue
        );

        /// <summary>Bind to <typeparamref name="TDependencyObject"/>.DefaultBindProperty</summary>
        public static TDependencyObject Bind<TDependencyObject>(
            this TDependencyObject target,
            object pathExpression = null,
            BindingMode mode = BindingMode.OneWay,
            IValueConverter converter = null,
            object converterParameter = null,
            string converterLanguage = null,
            UpdateSourceTrigger updateSourceTrigger = UpdateSourceTrigger.Default,
            object source = null,
            object targetNullValue = default,
            object fallbackValue = default,
            [CallerArgumentExpression("pathExpression")] string pathExpressionString = default
        ) where TDependencyObject : DependencyObject, IDefaultBindProperty
        => target.BindWithString(
            StripExpressionToPath(pathExpressionString),
            mode,
            converter,
            converterParameter,
            converterLanguage,
            updateSourceTrigger,
            source,
            targetNullValue,
            fallbackValue
        );

        /// <remarks>This overload allows to pass a string <paramref name="path"/> instead of a pathExpression. A pathExpression only uses the part after the last '.', while <paramref name="path"/> allows to specify paths that contain '.'</remarks>
        public static TDependencyObject BindWithString<TDependencyObject>(
            this TDependencyObject target,
            string path = bindingContextPath,
            BindingMode mode = BindingMode.OneWay,
            IValueConverter converter = null,
            object converterParameter = null,
            string converterLanguage = null,
            UpdateSourceTrigger updateSourceTrigger = UpdateSourceTrigger.Default,
            object source = null,
            object targetNullValue = default,
            object fallbackValue = default
        ) where TDependencyObject : DependencyObject, IDefaultBindProperty
        {
            Xaml.Data.BindingOperations.SetBinding(
                target.UI,
                target.DefaultBindProperty,
                new Microsoft.UI.Xaml.Data.Binding
                {
                    Path = new Xaml.PropertyPath(path),
                    Mode = mode,
                    Converter = converter,
                    ConverterParameter = converterParameter,
                    //ConverterLanguage = converterLanguage, // TODO: figure out correct default; can't be null
                    UpdateSourceTrigger = updateSourceTrigger,
                    Source = source,
                    // TODO: RelativeSource
                    TargetNullValue = targetNullValue,
                    FallbackValue = fallbackValue,
                }
            );
            return target;
        }

        /// <summary>Bind to the default property with inline conversion</summary>
        public static TDependencyObject Bind<TDependencyObject, TPropertyValue, TSource>(
            this TDependencyObject target,
            object pathExpression = null,
            BindingMode mode = BindingMode.OneWay,
            Func<TSource, TPropertyValue> convert = null,
            Func<TPropertyValue, TSource> convertBack = null,
            string converterLanguage = null,
            UpdateSourceTrigger updateSourceTrigger = UpdateSourceTrigger.Default,
            object source = null,
            TPropertyValue targetNullValue = default,
            [CallerArgumentExpression("pathExpression")] string pathExpressionString = default
        ) where TDependencyObject : DependencyObject, IDefaultBindProperty
        => target.BindWithString(
            StripExpressionToPath(pathExpressionString),
            mode,
            new FuncConverter<TSource, TPropertyValue, object>(convert, convertBack),
            null,
            converterLanguage,
            updateSourceTrigger,
            source,
            targetNullValue
        );

        /// <summary>Bind to the default property with inline conversion and conversion parameter</summary>
        public static TDependencyObject Bind<TDependencyObject, TPropertyValue, TSource, TParam>(
            this TDependencyObject target,
            object pathExpression = null,
            BindingMode mode = BindingMode.OneWay,
            Func<TSource, TParam, TPropertyValue> convert = null,
            Func<TPropertyValue, TParam, TSource> convertBack = null,
            TParam converterParameter = default,
            string converterLanguage = null,
            UpdateSourceTrigger updateSourceTrigger = UpdateSourceTrigger.Default,
            object source = null,
            TPropertyValue targetNullValue = default,
            [CallerArgumentExpression("pathExpression")] string pathExpressionString = default
        ) where TDependencyObject : DependencyObject, IDefaultBindProperty
        => target.BindWithString(
            StripExpressionToPath(pathExpressionString),
            mode,
            new FuncConverter<TSource, TPropertyValue, TParam>(convert, convertBack),
            converterParameter,
            converterLanguage,
            updateSourceTrigger,
            source,
            targetNullValue
        );

        /// <summary>Bind to the <typeparamref name="TDependencyObject"/>'s default Command and CommandParameter properties </summary>
        /// <param name="parameterPathExpression">If omitted, the CommandParameter property is bound to the binding context</param>
        public static TDependencyObject BindCommand<TDependencyObject>(
            this TDependencyObject target,
            object pathExpression = null,
            object source = null,
            object parameterPathExpression = null,
            object parameterSource = null,
            [CallerArgumentExpression("pathExpression")] string pathExpressionString = default,
            [CallerArgumentExpression("parameterPathExpression")] string parameterPathExpressionString = default
        ) where TDependencyObject : DependencyObject, IDefaultBindCommandProperties
        => target.BindCommandWithString(
            StripExpressionToPath(pathExpressionString),
            source,
            StripExpressionToPath(parameterPathExpressionString),
            parameterSource
        );

        /// <param name="parameterPath">If omitted, the CommandParameter property is bound to the binding context</param>
        /// <remarks>This overload allows to pass strings to <paramref name="path"/> and <paramref name="parameterPath"/> instead of path expressions. A path expression only uses the part after the last '.', while <paramref name="path"/> and <paramref name="parameterPath"/> allow to specify paths that contain '.'</remarks>
        public static TDependencyObject BindCommandWithString<TDependencyObject>(
            this TDependencyObject target,
            string path = bindingContextPath,
            object source = null,
            string parameterPath = bindingContextPath,
            object parameterSource = null
        ) where TDependencyObject : DependencyObject, IDefaultBindCommandProperties
        {
            Xaml.Data.BindingOperations.SetBinding(
                target.UI,
                target.DefaultBindCommandProperty,
                new Microsoft.UI.Xaml.Data.Binding
                {
                    Path = new Xaml.PropertyPath(path),
                    Mode = BindingMode.OneWay,
                    Source = source
                    // TODO: RelativeSource
                }
            );

            if (parameterPath != null)
            {
                if (target.DefaultBindCommandParameterProperty == null)
                    throw new ArgumentException($"{typeof(TDependencyObject).Name} does not have a default CommandParameterProperty", nameof(parameterPath));

                Xaml.Data.BindingOperations.SetBinding(
                    target.UI,
                    target.DefaultBindCommandParameterProperty,
                    new Microsoft.UI.Xaml.Data.Binding
                    {
                        Path = new Xaml.PropertyPath(parameterPath),
                        Mode = BindingMode.OneWay,
                        Source = parameterSource
                        // TODO: RelativeSource
                    }
                );
            }

            return target;
        }

        public static TDependencyObject Assign<TDependencyObject, TUI>(this TDependencyObject bindable, out TUI ui)
            where TDependencyObject : DependencyObject, IUI<TUI>
            where TUI : Xaml.DependencyObject
        {
            ui = (TUI)bindable.UI;
            return bindable;
        }

        public static TDependencyObject Invoke<TDependencyObject, TUI>(this TDependencyObject bindable, Action<TUI> action)
            where TDependencyObject : DependencyObject, IUI<TUI>
            where TUI : Xaml.DependencyObject
        {
            action?.Invoke((TUI)bindable.UI);
            return bindable;
        }

        static string StripExpressionToPath(string pathExpressionString)
        {
            if (string.IsNullOrWhiteSpace(pathExpressionString))
                return bindingContextPath;

            pathExpressionString = pathExpressionString.Replace("?", ""); // Allow .Bind(tweet?.Title) where tweet is a dummy null field used for binding only

            if (pathExpressionString.Contains('('))
                return pathExpressionString.Split(')')[0].Split('(')[^1]; // Allow .Bind(nameof(Person.Address.Zip)) to specify a path containing '.'

            return pathExpressionString.Split('.')[^1]; // The part after the last '.', just like nameof() returns.
        }
    }
}
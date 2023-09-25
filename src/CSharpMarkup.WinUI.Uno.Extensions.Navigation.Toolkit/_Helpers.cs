﻿using System;
using Xaml = Microsoft.UI.Xaml;
#if GENERATE
using CSharpMarkup.Generate.WinUI;
using Controls = Microsoft.UI.Xaml.Controls;

[assembly: MarkupHelpers(
    markupHelpersType: typeof(CSharpMarkup.WinUI.Uno.Toolkit.Helpers)
)]
#endif

namespace CSharpMarkup.WinUI.Uno.Toolkit
{
    public static partial class Helpers
    {
#if GENERATE
        /// <summary>Used by codegen to generate a <see cref="IDefaultBindProperty"/> implementation on markup types. Not used at runtime.</summary>
        /// <remarks>Types must be fully specified for codegen to work</remarks>
        static Xaml.DependencyProperty[] DefaultBindProperties => new Xaml.DependencyProperty[]
        {
            // Microsoft.UI.Xaml.Controls.AutoSuggestBox.TextProperty,
        };

        /// <summary>
        /// Used by codegen to identify which assemblies to scan for UI types. 
        /// Markup types will be generated for these UI types (e.g. controls).
        /// </summary>
        static Type[] UIAssemblies => new Type[] // Assemblies in NuGet Uno.Toolkit.WinUI that contain UI types
        {
            typeof(global::Uno.Extensions.Navigation.Toolkit.Controls.ModalFlyout)
        };

        public static Microsoft.UI.Xaml.DependencyObject dependencyObject;
#endif
    }
}
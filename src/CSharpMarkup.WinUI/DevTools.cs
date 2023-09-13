﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using Xaml = Microsoft.UI.Xaml;

[assembly: MetadataUpdateHandler(typeof(CSharpMarkup.WinUI.HotReloadService))]

namespace CSharpMarkup.WinUI;

/// <summary>(Re)build the UI completely</summary>
public interface IBuildUI
{
    /// <summary>(Re)build the UI completely</summary>
    void BuildUI();
}

/// <summary>Update the UI for specific changed types</summary>
public interface IUpdateUI
{
    /// <summary>(Re)build the UI for specific changed types</summary>
    /// <param name="updatedTypes">if null, any type may have been updated</param>
    void UpdateUI(Type[]? updatedTypes = null);
}

public static class HotReloadService
{
    public static event Action<Type[]?>? UpdateApplicationEvent;

    internal static void UpdateApplication(Type[]? types) => UpdateApplicationEvent?.Invoke(types);
}

public static partial class Helpers
{
    public static UIElement DevToolsOverlay(
        this Frame rootFrame,

        bool addInDebugBuildOnly = true,
        Func<Xaml.Controls.Frame, IEnumerable<Type>> getPageTypes = null,
        Action<Xaml.Controls.Frame, Type>? navigateToPageType = null, 
        Action<Xaml.Controls.Frame>? rebuildUI = null) 
    {
        var pagesAssembly = Assembly.GetCallingAssembly();
        if (addInDebugBuildOnly && !IsDebugBuild(pagesAssembly))
            return rootFrame;

        getPageTypes ??= (Xaml.Controls.Frame _) => 
            pagesAssembly.GetTypes().Where(t =>
                t.IsAssignableTo(typeof(Xaml.Controls.Page)) &&
                (t.IsAssignableTo(typeof(IBuildUI)) || t.IsAssignableTo(typeof(IUpdateUI))));

        navigateToPageType ??= (f, pageType) => f.Navigate(pageType);

        rebuildUI ??= f =>
        {
            switch (f?.Content)
            {
                case IBuildUI buildable: buildable.BuildUI(); break;
                case IUpdateUI updateable: updateable.UpdateUI(); break;
            }
        };

        var frame = rootFrame.UI;
        Dictionary<string, Type> buildablePageTypes = new();
        Xaml.Controls.Button? hotReloadButton;

        return MonochromaticOverlayPresenter(
            rootFrame,

            HStack (
                Button("\U0001F525")
                   .BorderThickness(0) .Style(null) 
                   .Assign(out hotReloadButton) .Invoke(b => b.Tapped += (s, e) => rebuildUI(frame)), // Manual hot reload in case the HotReloadService does not work (reliably) on all platforms / IDE's

                ComboBox()
                   .MinWidth(150) .Style(null) 
                   .Invoke(Initialize)
            )  .Top() .Right(),

            TextBlock("Built with C# Markup 2") .FontSize(16) .FontStyle().Italic() .Foreground(Microsoft.UI.Colors.Gold)
               .Bottom() .HCenter()
        );

        void Initialize(Xaml.Controls.ComboBox pageSelector)
        {
            var pageTypes = getPageTypes(frame);

            string prefix = null;
            foreach (var pageType in pageTypes) prefix = CommonPrefix(prefix, pageType.FullName);
            int prefixLength = prefix?.Length ?? 0;

            buildablePageTypes.Clear();
            pageSelector.Items.Clear();
            foreach (var pageType in pageTypes)
            {
                string name = pageType.FullName[prefixLength..];
                buildablePageTypes.Add(name, pageType);
                pageSelector.Items.Add(name);
                if (frame?.CurrentSourcePageType == pageType)
                    pageSelector.SelectedItem = name;
            }

            UpdateForFrameContent();

            if (frame is not null)
                frame.Navigated += (s, e) => UpdateForFrameContent();

            pageSelector.SelectionChanged += PageSelector_SelectionChanged;

            void UpdateForFrameContent()
            {
                var pageType = frame?.CurrentSourcePageType;
                pageSelector.SelectedItem = pageType?.FullName[prefixLength..];
                hotReloadButton.Visibility = frame?.Content switch { IBuildUI or IUpdateUI => Xaml.Visibility.Visible, _ => Xaml.Visibility.Collapsed };
            }
        }

        string CommonPrefix(string? prefix, string text)
        {
            if (prefix is null) return text;
            if (text.StartsWith(prefix, StringComparison.Ordinal)) return prefix;

            int i = 0;
            while (i < prefix.Length && i < text.Length && prefix[i] == text[i]) i++;
            return prefix[..i];
        }

        void PageSelector_SelectionChanged(object sender, Xaml.Controls.SelectionChangedEventArgs e)
        {
            string? name = e.AddedItems.SingleOrDefault()?.ToString();
            if (frame is null || name is null) return;

            var pageType = buildablePageTypes[name];

            if (frame.CurrentSourcePageType == pageType) return;

            if (frame.BackStack.Any(entry => entry.SourcePageType == pageType))
            {
                while (frame.CurrentSourcePageType != pageType && frame.CanGoBack)
                    frame.GoBack(new Xaml.Media.Animation.SuppressNavigationTransitionInfo());
                return;
            }

            if (frame.ForwardStack.Any(entry => entry.SourcePageType == pageType))
            {
                while (frame.CurrentSourcePageType != pageType && frame.CanGoForward)
                    frame.GoForward();
                return;
            }

            navigateToPageType(frame, pageType);
        }

        static bool IsDebugBuild(Assembly assembly) => (assembly.GetCustomAttributes(typeof(DebuggableAttribute), false).FirstOrDefault() as DebuggableAttribute)?.IsJITTrackingEnabled ?? false;
    }
}
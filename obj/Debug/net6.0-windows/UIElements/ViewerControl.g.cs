﻿#pragma checksum "..\..\..\..\UIElements\ViewerControl.xaml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "51EA234B5D94D840252A3CF5F4B153A4A2F61DF3"
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using ImageViewer.UIElements;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Controls.Ribbon;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Media.TextFormatting;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Shell;


namespace ImageViewer.UIElements {
    
    
    /// <summary>
    /// ViewerControl
    /// </summary>
    public partial class ViewerControl : System.Windows.Controls.UserControl, System.Windows.Markup.IComponentConnector {
        
        
        #line 11 "..\..\..\..\UIElements\ViewerControl.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Canvas MainCanvas;
        
        #line default
        #line hidden
        
        
        #line 25 "..\..\..\..\UIElements\ViewerControl.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Image MainImage;
        
        #line default
        #line hidden
        
        private bool _contentLoaded;
        
        /// <summary>
        /// InitializeComponent
        /// </summary>
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "8.0.2.0")]
        public void InitializeComponent() {
            if (_contentLoaded) {
                return;
            }
            _contentLoaded = true;
            System.Uri resourceLocater = new System.Uri("/ImageViewer;component/uielements/viewercontrol.xaml", System.UriKind.Relative);
            
            #line 1 "..\..\..\..\UIElements\ViewerControl.xaml"
            System.Windows.Application.LoadComponent(this, resourceLocater);
            
            #line default
            #line hidden
        }
        
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "8.0.2.0")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes")]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily")]
        void System.Windows.Markup.IComponentConnector.Connect(int connectionId, object target) {
            switch (connectionId)
            {
            case 1:
            this.MainCanvas = ((System.Windows.Controls.Canvas)(target));
            
            #line 12 "..\..\..\..\UIElements\ViewerControl.xaml"
            this.MainCanvas.MouseLeftButtonDown += new System.Windows.Input.MouseButtonEventHandler(this.CanvasMouseLeftButtonDown);
            
            #line default
            #line hidden
            
            #line 13 "..\..\..\..\UIElements\ViewerControl.xaml"
            this.MainCanvas.MouseLeftButtonUp += new System.Windows.Input.MouseButtonEventHandler(this.CanvasMouseLeftButtonUp);
            
            #line default
            #line hidden
            
            #line 14 "..\..\..\..\UIElements\ViewerControl.xaml"
            this.MainCanvas.MouseMove += new System.Windows.Input.MouseEventHandler(this.CanvasMouseMove);
            
            #line default
            #line hidden
            return;
            case 2:
            this.MainImage = ((System.Windows.Controls.Image)(target));
            return;
            }
            this._contentLoaded = true;
        }
    }
}


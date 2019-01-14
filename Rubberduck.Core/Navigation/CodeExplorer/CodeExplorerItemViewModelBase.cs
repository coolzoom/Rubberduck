﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Windows;
using Rubberduck.Parsing;
using Rubberduck.Parsing.Symbols;
using Rubberduck.Resources;
using Rubberduck.UI;
using Rubberduck.VBEditor;

namespace Rubberduck.Navigation.CodeExplorer
{
    public abstract class CodeExplorerItemViewModelBase : ViewModelBase, ICodeExplorerNode
    {
        protected CodeExplorerItemViewModelBase(ICodeExplorerNode parent, Declaration declaration)
        {
            Parent = parent;
            _declaration = declaration;
        }

        private Declaration _declaration;
        public Declaration Declaration
        {
            get => _declaration;
            protected set
            {
                _declaration = value;

                if (_declaration is null)
                {
                    // No need to call OnPropertyChanged - the node's being removed.
                    return;
                }

                OnPropertyChanged();
            }
        }

        public ICodeExplorerNode Parent { get; }

        public abstract string Name { get; }

        public abstract string NameWithSignature { get; }

        public virtual string PanelTitle
        {
            get
            {
                if (Declaration is null)
                {
                    return string.Empty;
                }

                var nameWithDeclarationType =
                    $"{Declaration.IdentifierName} - ({RubberduckUI.ResourceManager.GetString("DeclarationType_" + Declaration.DeclarationType, CultureInfo.CurrentUICulture)})";

                if (string.IsNullOrEmpty(Declaration.AsTypeName))
                {
                    return nameWithDeclarationType;
                }

                var typeName = Declaration.HasTypeHint
                    ? SymbolList.TypeHintToTypeName[Declaration.TypeHint]
                    : Declaration.AsTypeName;

                return $"{nameWithDeclarationType}: {typeName}";
            }
        }

        public virtual string Description => Declaration?.DescriptionString ?? string.Empty;

        [SuppressMessage("ReSharper", "ExplicitCallerInfoArgument")]
        protected void OnNameChanged()
        {
            OnPropertyChanged("Name");
            OnPropertyChanged("NameWithSignature");
            OnPropertyChanged("PanelTitle");
            OnPropertyChanged("Description");
        }

        public virtual QualifiedSelection? QualifiedSelection => Declaration?.QualifiedSelection;

        protected bool UnfilteredIsExpanded { get; private set; }

        private bool _isExpanded = true;
        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                _isExpanded = value;

                if (!Filtered)
                {
                    UnfilteredIsExpanded = _isExpanded;
                }

                OnPropertyChanged();
            }
        }

        private bool _selected;
        public bool IsSelected
        {
            get => _selected;
            set
            {
                _selected = value;
                OnPropertyChanged();
            }
        }

        public virtual bool IsDimmed
        {
            get => false;
            set { }
        }

        public virtual bool IsObsolete => false;

        public abstract bool IsErrorState { get; set; }

        public virtual string ToolTip => NameWithSignature;

        public virtual FontWeight FontWeight => FontWeights.Normal;

        public ObservableCollection<ICodeExplorerNode> Children { get; } = new ObservableCollection<ICodeExplorerNode>();

        public void AddChild(ICodeExplorerNode child)
        {
            if (Children.Contains(child))
            {
                return;
            }

            var before = Children.FirstOrDefault(existing => existing.SortComparer.Compare(existing, child) > 0);
            if (before is null)
            {
                Children.Add(child);
                return;
            }

            Children.Insert(Children.IndexOf(before), child);
        }

        public void AddChildren(IEnumerable<ICodeExplorerNode> children)
        {
            foreach (var child in children)
            {
                AddChild(child);
            }
        }

        public void RemoveChild(ICodeExplorerNode child)
        {
            Children.Remove(child);
        }

        public void RemoveChildren(IEnumerable<ICodeExplorerNode> children)
        {
            foreach (var child in children)
            {
                RemoveChild(child);
            }
        }

        private CodeExplorerSortOrder _order;
        public CodeExplorerSortOrder SortOrder
        {
            get => _order;
            set
            {
                if (_order == value)
                {
                    return;
                }

                _order = value;

                foreach (var child in Children)
                {
                    child.SortOrder = _order;
                }
                Sort();
            }
        }

        public abstract Comparer<ICodeExplorerNode> SortComparer { get; }

        private void Sort()
        {
            if (Children.Count == 0)
            {
                return;
            }

            var ordered = new List<ICodeExplorerNode>(Children);
            ordered.Sort(Children.First().SortComparer);

            for (var index = 0; index < ordered.Count; index++)
            {
                var position = Children.IndexOf(ordered[index]);
                if (position != index)
                {
                    Children.Move(position, index);
                }              
            }
        }

        private string _filter = string.Empty;
        public string Filter
        {
            get => _filter;
            set
            {
                var input = value ?? string.Empty;
                if (_filter.Equals(input))
                {
                    return;
                }

                _filter = input;
                foreach (var child in Children)
                {
                    child.Filter = input;
                }
                
                OnPropertyChanged();
                // ReSharper disable once ExplicitCallerInfoArgument
                OnPropertyChanged("Filtered");
            }
        }

        public virtual bool Filtered => !string.IsNullOrEmpty(Filter) &&
                                        Name.IndexOf(Filter, StringComparison.OrdinalIgnoreCase) < 0 &&
                                        Children.All(node => node.Filtered);
    }
}

﻿using Elementary.Hierarchy;
using Elementary.Hierarchy.Collections;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Treesor.PSDriveProvider
{
    public class TreesorService : ITreesorService
    {
        private static readonly Logger log = LogManager.GetCurrentClassLogger();

        public static Func<string, ITreesorService> Factory { get; set; } = DefaultFactoryDelegate;

        private static ITreesorService DefaultFactoryDelegate(string type)
        {
            var hierarchy = new MutableHierarchy<string, ValueReference<Guid>>();
            ValueReference<Guid> id;
            if (!hierarchy.TryGetValue(HierarchyPath.Create<string>(), out id))
                hierarchy.Add(HierarchyPath.Create<string>(), new ValueReference<Guid>(Guid.NewGuid()));
            return new TreesorService(hierarchy);
        }

        #region Construction and initialization of this instance

        private readonly IHierarchy<string, ValueReference<Guid>> hierarchy;

        private readonly IDictionary<string, TreesorColumn> columns;

        public TreesorService(IHierarchy<string, ValueReference<Guid>> hierarchy)
        {
            this.hierarchy = hierarchy;
            this.columns = new Dictionary<string, TreesorColumn>();
        }

        #endregion Construction and initialization of this instance

        #region IDisposable Support

        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~TreesorService() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }

        #endregion IDisposable Support

        public bool ItemExists(TreesorNodePath treesorNodePath)
        {
            ValueReference<Guid> id;
            return this.hierarchy.TryGetValue(treesorNodePath.HierarchyPath, out id);
        }

        public TreesorItem GetItem(TreesorNodePath rootPath)
        {
            ValueReference<Guid> id;
            if (this.hierarchy.TryGetValue(rootPath.HierarchyPath, out id))
                return new TreesorItem(rootPath, id);
            else return null;
        }

        public void SetItem(TreesorNodePath treesorNodePath, object value)
        {
            throw new NotSupportedException($"A value for node {treesorNodePath} is not allowed");
        }

        public void ClearItem(TreesorNodePath rootPath)
        {
            // currently function isn't implmented.
            return;
        }

        public TreesorItem NewItem(TreesorNodePath treesorNodePath, object newItemValue)
        {
            // currently a value is not accepted.
            // a data model of the nodes is not yet decided

            if (newItemValue != null)
                throw new NotSupportedException($"A value for node {treesorNodePath} is not allowed");

            ValueReference<Guid> id;
            this.hierarchy.Add(treesorNodePath.HierarchyPath, id = new ValueReference<Guid>(Guid.NewGuid()));

            return new TreesorItem(treesorNodePath, id);
        }

        public void RemoveItem(TreesorNodePath treesorNodePath, bool recurse)
        {
            this.hierarchy.RemoveNode(treesorNodePath.HierarchyPath, recurse);
        }

        public bool HasChildItems(TreesorNodePath treesorNodePath)
        {
            // Result of Travesr is never null.
            // hierachy throws KeyNotFindException in this case.
            return this.hierarchy.Traverse(treesorNodePath.HierarchyPath).HasChildNodes;
        }

        public IEnumerable<TreesorItem> GetChildItemsByWildcard(TreesorNodePath treesorNodePath)
        {
            throw new InvalidOperationException();
        }

        public IEnumerable<TreesorItem> GetChildItems(TreesorNodePath treesorNodePath)
        {
            return this.hierarchy
                .Traverse(treesorNodePath.HierarchyPath)
                .Children()
                .Select(n => new TreesorItem(TreesorNodePath.Create(n.Path), n.Value));
        }

        public IEnumerable<TreesorItem> GetDescendants(TreesorNodePath treesorNodePath)
        {
            return this.hierarchy
                .Traverse(treesorNodePath.HierarchyPath)
                .Descendants()
                .Select(n => new TreesorItem(TreesorNodePath.Create(n.Path), n.Value));
        }

        public void CopyItem(TreesorNodePath path, TreesorNodePath destinationPath, bool recurse)
        {
            ValueReference<Guid> id;
            if (this.hierarchy.TryGetValue(path.HierarchyPath, out id))
            {
                ValueReference<Guid> destinationId;
                if (this.hierarchy.TryGetValue(destinationPath.HierarchyPath, out destinationId))
                {
                    // try create new item under existing destination
                    if (!this.hierarchy.TryGetValue(destinationPath.HierarchyPath.Join(path.HierarchyPath.Leaf()), out destinationId))
                        this.hierarchy.Add(destinationPath.HierarchyPath.Join(path.HierarchyPath.Leaf()), destinationId = new ValueReference<Guid>(Guid.NewGuid()));
                }
                else
                {
                    // create new item at destiontionPath
                    this.hierarchy.Add(destinationPath.HierarchyPath, destinationId = new ValueReference<Guid>(Guid.NewGuid()));
                    if (recurse)
                    {
                        // copy child nodes of source to destination
                        foreach (var source in this.hierarchy.Traverse(path.HierarchyPath).Descendants(depthFirst: false))
                            this.hierarchy.Add(destinationPath.HierarchyPath.Join(source.Path.RelativeToAncestor(path.HierarchyPath)), new ValueReference<Guid>(Guid.NewGuid()));
                    }
                }
            }
        }

        public void RenameItem(TreesorNodePath treesorNodePath, string newName)
        {
            ValueReference<Guid> id;
            if (this.hierarchy.TryGetValue(treesorNodePath.HierarchyPath, out id))
                if (!this.hierarchy.TryGetValue(treesorNodePath.HierarchyPath.Parent().Join(newName), out id))
                    if (this.hierarchy.Remove(treesorNodePath.HierarchyPath))
                        this.hierarchy.Add(treesorNodePath.HierarchyPath.Parent().Join(newName), id);
        }

        public void MoveItem(TreesorNodePath path, TreesorNodePath destinationPath)
        {
            ValueReference<Guid> id, destinationId;
            if (this.hierarchy.TryGetValue(path.HierarchyPath, out id))
                if (this.hierarchy.TryGetValue(destinationPath.HierarchyPath, out destinationId))
                {
                    // try create item under existing destination item
                    if (!this.hierarchy.TryGetValue(destinationPath.HierarchyPath.Join(path.HierarchyPath.Leaf()), out destinationId))
                    {
                        this.hierarchy.Add(destinationPath.HierarchyPath.Join(path.HierarchyPath.Leaf()), id);

                        // finally remove the source
                        this.hierarchy.Remove(path.HierarchyPath);
                    }
                }
                else
                {
                    this.hierarchy.Add(destinationPath.HierarchyPath, id);
                    // move item and its descendants to the destination
                    foreach (var source in this.hierarchy.Traverse(path.HierarchyPath).Descendants(depthFirst: false))
                        this.hierarchy.Add(destinationPath.HierarchyPath.Join(source.Path.RelativeToAncestor(path.HierarchyPath)), source.Value);

                    // finally remove the source
                    this.hierarchy.Remove(path.HierarchyPath);
                }
        }

        #region Column Handling

        public TreesorColumn CreateColumn(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));

            var tmp = new TreesorColumn(name);
            this.columns.Add(name, tmp);
            return tmp;
        }
        
        public void SetPropertyValue(TreesorNodePath path, string name, object value)
        {
            TreesorColumn column = null;
            if(!this.columns.TryGetValue(name, out column))
                column = this.CreateColumn(name);

            column.SetValue(this.GetItem(path), value);
        }

        public object GetPropertyValue(TreesorNodePath rootPath, string name)
        {
            throw new NotImplementedException();
        }

        public void ClearPropertyValue(TreesorNodePath treesorNodePath, string name)
        {
            throw new NotImplementedException();
        }

        #endregion Column Handling
    }
}
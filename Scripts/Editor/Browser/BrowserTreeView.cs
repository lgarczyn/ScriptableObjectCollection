using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace BrunoMikoski.ScriptableObjectCollections.Browser
{
    public class BrowserTreeView : TreeView
    {
        public delegate void ItemClickedDelegate(BrowserTreeViewItem item);

        public event ItemClickedDelegate ItemClicked;


        public BrowserTreeView(TreeViewState state)
            : base(state)
        {
            Initialize();
        }

        public BrowserTreeView(TreeViewState state, MultiColumnHeader multiColumnHeader)
            : base(state, multiColumnHeader)
        {
            Initialize();
        }

        private void Initialize()
        {
            showAlternatingRowBackgrounds = true;
            Reload();
        }

        protected override TreeViewItem BuildRoot()
        {
            TreeViewItem root = new(0, -1);
            int id = 1;

            string[] scriptableObjectCollections = AssetDatabase.FindAssets("t:ScriptableObjectCollection");
            foreach (string guid in scriptableObjectCollections)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                ScriptableObjectCollection collection =
                    AssetDatabase.LoadAssetAtPath<ScriptableObjectCollection>(assetPath);

                BrowserTreeViewItem parentItem = new(id++, 0, collection);

                foreach (ScriptableObject item in collection.Items)
                {
                    BrowserTreeViewItem childItem = new(id++, 1, item);
                    parentItem.AddChild(childItem);
                }

                root.AddChild(parentItem);
            }

            return root;
        }

        protected override bool CanMultiSelect(TreeViewItem item)
        {
            return false;
        }

        protected override void SelectionChanged(IList<int> selectedIds)
        {
            if (selectedIds.Count != 1)
                return;

            TreeViewItem item = FindItem(selectedIds[0], rootItem);

            if (item is BrowserTreeViewItem treeViewItem)
            {
                ItemClicked?.Invoke(treeViewItem);
            }
        }

        protected override void SingleClickedItem(int id)
        {
            TreeViewItem item = FindItem(id, rootItem);

            if (item is BrowserTreeViewItem treeViewItem)
            {
                ItemClicked?.Invoke(treeViewItem);
            }
        }

        protected override void DoubleClickedItem(int id)
        {
            TreeViewItem item = FindItem(id, rootItem);

            if (item is BrowserTreeViewItem treeViewItem)
            {
                EditorGUIUtility.PingObject(treeViewItem.ScriptableObject);
            }
        }

        protected override void ContextClickedItem(int id)
        {
            TreeViewItem item = FindItem(id, rootItem);

            if (item is not BrowserTreeViewItem treeViewItem)
                return;

            GenericMenu menu = new();

            menu.AddItem(new GUIContent("Show In Project Window"),
                false,
                () => EditorGUIUtility.PingObject(treeViewItem.ScriptableObject));

            menu.ShowAsContext();
        }

    }
}

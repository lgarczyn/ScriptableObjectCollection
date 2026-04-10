using System;
using System.Collections.Generic;
using System.Linq;
using BrunoMikoski.ScriptableObjectCollections.Popup;
using UnityEditor;
using UnityEngine;
using PopupWindow = UnityEditor.PopupWindow;

namespace BrunoMikoski.ScriptableObjectCollections.Picker
{
    [CustomPropertyDrawer(typeof(CollectionItemPicker<>), true)]
    public class CollectionItemPickerPropertyDrawer : PropertyDrawer
    {
        private const string ITEMS_PROPERTY_NAME = "itemReferences";
        private const string ASSET_GUID_PROPERTY = "m_AssetGUID";

        private static GUIStyle labelStyle;
        private static GUIStyle buttonStyle;
        private float buttonHeight = EditorGUIUtility.singleLineHeight;
        private List<ScriptableObjectCollection> possibleCollections;
        private List<ScriptableObject> availableItems = new();

        private readonly HashSet<string> initializedPropertiesPaths = new();

        private readonly Dictionary<string, PopupList<PopupItem>> propertyPathToPopupList = new();

        private readonly struct PopupItem : IPopupListItem
        {
            private readonly string name;
            public string Name => name;

            public PopupItem(ScriptableObject scriptableObject)
            {
                name = scriptableObject.name;
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return Mathf.Max(buttonHeight,
                EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            Initialize(property);

            PopupList<PopupItem> popupList = propertyPathToPopupList[property.propertyPath];

            position = EditorGUI.PrefixLabel(position, label);

            Rect totalPosition = position;
            Rect buttonRect = position;

            totalPosition.height = buttonHeight;
            buttonRect.height = buttonHeight;

            float buttonWidth = 20f;
            Rect plusButtonRect = new Rect(position.xMax - buttonWidth, position.y, buttonWidth, buttonHeight);

            totalPosition.width -= buttonWidth;

            if (!popupList.IsOpen)
            {
                SetSelectedValuesOnPopup(popupList, property);
            }

            if (GUI.Button(totalPosition, "", buttonStyle))
            {
                EditorWindow inspectorWindow = EditorWindow.focusedWindow;

                popupList.OnClosedEvent += () =>
                {
                    GetValuesFromPopup(popupList, property);
                    SetSelectedValuesOnPopup(popupList, property);
                };
                popupList.OnItemSelectedEvent += (x, y) => { inspectorWindow.Repaint(); };
                PopupWindow.Show(buttonRect, popupList);
            }

            using (new EditorGUI.DisabledScope(possibleCollections.Count > 1))
            {
                if (GUI.Button(plusButtonRect, "+"))
                {
                    CreatAndAddNewItems(property);
                }
            }

            buttonRect.width = 0;

            Rect labelRect = buttonRect;

            labelRect.y += 2;
            labelRect.height -= 4;

            float currentLineWidth = position.x + 4;
            float maxHeight = 0;
            float inspectorWidth = EditorGUIUtility.currentViewWidth - 88;
            float currentLineMaxHeight = 0;

            Color originalColor = GUI.backgroundColor;
            for (int i = 0; i < popupList.Count; i++)
            {
                if (!popupList.GetSelected(i))
                    continue;

                ScriptableObject collectionItem = availableItems[i];
                GUIContent labelContent = new GUIContent(collectionItem.name);
                Vector2 size = labelStyle.CalcSize(labelContent);

                if (currentLineWidth + size.x + 4 > inspectorWidth)
                {
                    labelRect.y += currentLineMaxHeight + 4;
                    maxHeight += currentLineMaxHeight + 4;
                    currentLineWidth = position.x + 4;
                    currentLineMaxHeight = 0;
                }

                currentLineMaxHeight = Mathf.Max(currentLineMaxHeight, size.y);

                labelRect.x = currentLineWidth;
                labelRect.width = size.x;

                currentLineWidth += size.x + 4;

                if (collectionItem is ISOCColorizedItem coloredItem)
                    GUI.backgroundColor = coloredItem.LabelColor;
                else
                    GUI.backgroundColor = Color.black;

                GUI.Label(labelRect, labelContent, labelStyle);
            }

            GUI.backgroundColor = originalColor;

            maxHeight += currentLineMaxHeight;

            buttonHeight = Mathf.Max(maxHeight + EditorGUIUtility.standardVerticalSpacing * 3,
                EditorGUIUtility.singleLineHeight);

            EditorGUI.EndProperty();
        }

        private void CreatAndAddNewItems(SerializedProperty property)
        {
            ScriptableObjectCollection collection = possibleCollections.First();

            ScriptableObject newItem = CollectionCustomEditor.AddNewItem(collection, collection.GetItemType());
            SerializedProperty itemsProperty = property.FindPropertyRelative(ITEMS_PROPERTY_NAME);
            itemsProperty.arraySize++;

            string assetGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(newItem));
            SerializedProperty newElement = itemsProperty.GetArrayElementAtIndex(itemsProperty.arraySize - 1);
            newElement.FindPropertyRelative(ASSET_GUID_PROPERTY).stringValue = assetGuid;

            itemsProperty.serializedObject.ApplyModifiedProperties();
        }

        private void GetValuesFromPopup(PopupList<PopupItem> popupList, SerializedProperty property)
        {
            SerializedProperty itemsProperty = property.FindPropertyRelative(ITEMS_PROPERTY_NAME);
            itemsProperty.ClearArray();

            int selectedCount = 0;
            for (int i = 0; i < popupList.Count; i++)
            {
                if (popupList.GetSelected(i))
                    selectedCount++;
            }

            itemsProperty.arraySize = selectedCount;

            int propertyArrayIndex = 0;

            for (int i = 0; i < popupList.Count; i++)
            {
                if (popupList.GetSelected(i))
                {
                    SerializedProperty newProperty = itemsProperty.GetArrayElementAtIndex(propertyArrayIndex);
                    string assetGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(availableItems[i]));
                    newProperty.FindPropertyRelative(ASSET_GUID_PROPERTY).stringValue = assetGuid;
                    propertyArrayIndex++;
                }
            }

            itemsProperty.serializedObject.ApplyModifiedProperties();
        }

        private void SetSelectedValuesOnPopup(PopupList<PopupItem> popupList, SerializedProperty property)
        {
            popupList.DeselectAll();

            SerializedProperty itemsProperty = property.FindPropertyRelative(ITEMS_PROPERTY_NAME);

            int arraySize = itemsProperty.arraySize;
            for (int i = arraySize - 1; i >= 0; i--)
            {
                SerializedProperty elementProperty = itemsProperty.GetArrayElementAtIndex(i);
                string assetGuid = elementProperty.FindPropertyRelative(ASSET_GUID_PROPERTY).stringValue;

                if (string.IsNullOrEmpty(assetGuid))
                {
                    itemsProperty.DeleteArrayElementAtIndex(i);
                    continue;
                }

                string assetPath = AssetDatabase.GUIDToAssetPath(assetGuid);
                ScriptableObject asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);

                if (asset != null)
                {
                    int indexOf = availableItems.IndexOf(asset);
                    if (indexOf >= 0)
                        popupList.SetSelected(indexOf, true);
                    else
                        itemsProperty.DeleteArrayElementAtIndex(i);
                }
                else
                {
                    itemsProperty.DeleteArrayElementAtIndex(i);
                }
            }

            itemsProperty.serializedObject.ApplyModifiedProperties();
        }

        private void Initialize(SerializedProperty property)
        {
            if (initializedPropertiesPaths.Contains(property.propertyPath))
                return;

            Type arrayOrListType = fieldInfo.FieldType.GetArrayOrListType();
            Type itemType = arrayOrListType ?? fieldInfo.FieldType;

            if (itemType.IsGenericType)
                itemType = itemType.GetGenericArguments()[0];

            possibleCollections = ScriptableObjectCollection.FindByItemTypeInEditor(itemType);
            if (possibleCollections.Count == 0)
                throw new Exception($"No collection found for item type {itemType}");

            propertyPathToPopupList.Add(property.propertyPath, new PopupList<PopupItem>());

            availableItems.Clear();
            for (int i = 0; i < possibleCollections.Count; i++)
            {
                for (int j = 0; j < possibleCollections[i].Count; j++)
                {
                    ScriptableObject scriptableObject = possibleCollections[i][j];
                    Type scriptableObjectType = scriptableObject.GetType();

                    if (scriptableObjectType != itemType && !scriptableObjectType.IsSubclassOf(itemType))
                        continue;

                    availableItems.Add(scriptableObject);
                    propertyPathToPopupList[property.propertyPath].AddItem(new PopupItem(scriptableObject), false);
                }
            }

            buttonStyle = EditorStyles.textArea;
            GUIStyle assetLabelStyle = new GUIStyle(EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector).FindStyle("AssetLabel"));
            labelStyle = assetLabelStyle;
            initializedPropertiesPaths.Add(property.propertyPath);
        }
    }
}

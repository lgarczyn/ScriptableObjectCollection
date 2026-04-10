# Addressables Migration Plan

## Summary

Replace the current Resources-based, eagerly-loaded, serialized-list architecture with
an Addressables-backed, folder-based, lazy-loaded system. Addressables becomes a hard
dependency. Collections no longer serialize a `List<ScriptableObject> items`; instead,
a collection **is** a folder (and subfolders) of items. Addressable labels auto-link
items to their collection. Runtime access uses `WaitForCompletion()` for synchronous
convenience. SmartAddresser-inspired automation manages groups, labels, and addresses.

---

## Architecture Overview

### Current

```
CollectionsRegistry (Resources singleton)
  └── List<ScriptableObjectCollection> collections  (direct references)
        └── List<ScriptableObject> items             (direct references, all in memory)
```

### Target

```
CollectionsRegistry (Addressable asset, loaded on init)
  └── List<CollectionMetadata> collections           (lightweight: GUID, label, folder path)

Collection asset (Addressable, loaded on demand)
  └── No serialized item list
  └── Items discovered by Addressable label at runtime
  └── Items discovered by folder at edit time

Item assets (individually Addressable, loaded on demand)
  └── Loaded via WaitForCompletion() on first access
  └── Released when collection is unloaded
```

---

## Phase 1: Core Data Model Changes

### 1.1 Remove serialized item list from ScriptableObjectCollection

**File:** `Scripts/Runtime/Core/ScriptableObjectCollection.cs`

- Remove `[SerializeField] List<ScriptableObject> items`
- Remove `IList` implementation (iteration over a live list of loaded items)
- Replace with a runtime-populated list backed by Addressables:

```csharp
public abstract class ScriptableObjectCollection : ScriptableObject
{
    [SerializeField, HideInInspector]
    private LongGuid guid;

    // No more serialized item list. Items are discovered at edit time
    // via folder scanning, and at runtime via Addressable labels.

    // The Addressable label used to tag items belonging to this collection.
    // Auto-generated: "soc_{guid}" or "soc_{CollectionName}"
    public string AddressableLabel => $"soc_{GUID.ToBase64()}";

    // Runtime: populated by loading all assets with our label
    [NonSerialized] private List<ScriptableObject> loadedItems;
    [NonSerialized] private bool isLoaded;

    // Sync load all items (called lazily on first access)
    public IReadOnlyList<ScriptableObject> Items
    {
        get
        {
            if (!isLoaded) LoadSync();
            return loadedItems;
        }
    }

    private void LoadSync()
    {
        var handle = Addressables.LoadAssetsAsync<ScriptableObject>(
            AddressableLabel, null);
        loadedItems = new List<ScriptableObject>(handle.WaitForCompletion());
        isLoaded = true;
        // Register each item's back-reference
        foreach (var item in loadedItems)
            if (item is ISOCItem soc) soc.SetCollectionRuntime(this);
    }

    public void Unload()
    {
        if (!isLoaded) return;
        Addressables.Release(loadedItemsHandle);
        loadedItems = null;
        isLoaded = false;
    }
}
```

### 1.2 Collection metadata for registry

Instead of the registry holding direct references to all collection assets (which
forces them all into memory), it holds lightweight metadata:

```csharp
[Serializable]
public struct CollectionMetadata
{
    public LongGuid guid;
    public string collectionName;
    public string addressableAddress; // address of the collection asset itself
    public string itemLabel;          // label for items: "soc_{guid}"
    public string assetGuid;          // Unity asset GUID (for editor)
}
```

### 1.3 CollectionsRegistry changes

**File:** `Scripts/Runtime/Core/CollectionsRegistry.cs`

- No longer extends `ResourceScriptableObjectSingleton`
- Becomes an Addressable asset (or a simple ScriptableObject with a known address)
- Stores `List<CollectionMetadata>` instead of `List<ScriptableObjectCollection>`
- Collections are loaded on demand, cached in a dictionary

```csharp
public class CollectionsRegistry : ScriptableObject
{
    private const string RegistryAddress = "SOC_Registry";

    [SerializeField]
    private List<CollectionMetadata> collectionEntries = new();

    // Runtime cache of loaded collections
    [NonSerialized]
    private Dictionary<LongGuid, ScriptableObjectCollection> loadedCollections = new();

    private static CollectionsRegistry instance;
    public static CollectionsRegistry Instance
    {
        get
        {
            if (instance == null)
            {
                var handle = Addressables.LoadAssetAsync<CollectionsRegistry>(RegistryAddress);
                instance = handle.WaitForCompletion();
            }
            return instance;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        // Pre-load registry (lightweight, just metadata)
        _ = Instance;
    }

    // Load a collection on demand
    public ScriptableObjectCollection GetOrLoadCollection(LongGuid guid)
    {
        if (loadedCollections.TryGetValue(guid, out var cached))
            return cached;

        var meta = collectionEntries.Find(e => e.guid == guid);
        var handle = Addressables.LoadAssetAsync<ScriptableObjectCollection>(meta.addressableAddress);
        var collection = handle.WaitForCompletion();
        loadedCollections[guid] = collection;
        return collection;
    }
}
```

### 1.4 ScriptableObjectCollectionItem changes

**File:** `Scripts/Runtime/Core/ScriptableObjectCollectionItem.cs`

Minor changes:
- `Collection` property now triggers collection loading via registry if not cached
- Add `SetCollectionRuntime()` for the loading path (doesn't dirty the asset)
- The item still serializes its `collectionGUID` (set at edit time)

```csharp
public void SetCollectionRuntime(ScriptableObjectCollection collection)
{
    cachedCollection = collection;
    hasCachedCollection = true;
    // No SetDirty - this is runtime only
}
```

### 1.5 Remove ResourceScriptableObjectSingleton

**File:** `Scripts/Runtime/Utils/ResourceScriptableObjectSingleton.cs`

- Delete this file entirely (or keep as deprecated)
- Registry no longer uses Resources.Load

---

## Phase 2: Editor - Folder-Based Item Discovery

### 2.1 Collection = Folder concept

A collection's items are all `ISOCItem` assets living in the same folder (and
subfolders) as the collection asset. This replaces the serialized list.

**Editor item discovery:**
```csharp
// In an editor utility class
public static List<ScriptableObject> GetItemsInCollectionFolder(
    ScriptableObjectCollection collection)
{
    string assetPath = AssetDatabase.GetAssetPath(collection);
    string folder = Path.GetDirectoryName(assetPath);
    Type itemType = collection.GetItemType();

    string[] guids = AssetDatabase.FindAssets($"t:{itemType.Name}", new[] { folder });
    var items = new List<ScriptableObject>();
    foreach (string guid in guids)
    {
        var item = AssetDatabase.LoadAssetAtPath<ScriptableObject>(
            AssetDatabase.GUIDToAssetPath(guid));
        if (item is ISOCItem) items.Add(item);
    }
    return items;
}
```

This is essentially what `RefreshCollection()` already does today, but now it
becomes the **primary** item discovery mechanism rather than a repair tool.

### 2.2 AddNew / Remove in editor

`AddNew()` still creates the item asset in the Items/ subfolder, but it no longer
calls `items.Add()`. The folder structure is the source of truth.

`Remove()` deletes the asset file. The item disappears from the collection
because it's no longer in the folder.

### 2.3 CollectionCustomEditor changes

**File:** `Scripts/Editor/CustomEditors/CollectionCustomEditor.cs`

- `filteredItems` populated via folder scan (`AssetDatabase.FindAssets`) instead
  of `collection.Items`
- No change to ListView UX - still shows all items, renameable, expandable
- Items loaded via `AssetDatabase.LoadAssetAtPath` (editor only, fine for inspector)
- Sorting/ordering: stored in the collection asset as a serialized order list
  (list of LongGuids defining display order), or just alphabetical

---

## Phase 3: Automated Addressable Management (SmartAddresser-inspired)

### 3.1 Auto-labeling via AssetPostprocessor

**New file:** `Scripts/Editor/Addressables/SOCAddressablePostprocessor.cs`

An `AssetPostprocessor` that watches for:
- New/moved/deleted `ISOCItem` assets
- New/moved/deleted `ScriptableObjectCollection` assets

On change:
1. Find the parent collection (by scanning up from the item's folder)
2. Ensure the item is in an Addressable group
3. Apply the collection's label (`soc_{collectionGUID}`)
4. Set the item's address to its asset GUID (stable across renames)

```csharp
public class SOCAddressablePostprocessor : AssetPostprocessor
{
    static void OnPostprocessAllAssets(
        string[] importedAssets, string[] deletedAssets,
        string[] movedAssets, string[] movedFromAssetPaths)
    {
        foreach (string path in importedAssets)
        {
            var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
            if (asset is ISOCItem item)
                SOCAddressableUtility.EnsureItemAddressable(item, path);
            else if (asset is ScriptableObjectCollection collection)
                SOCAddressableUtility.EnsureCollectionAddressable(collection, path);
        }
        // Handle moves and deletes similarly
    }
}
```

### 3.2 SOCAddressableUtility

**New file:** `Scripts/Editor/Addressables/SOCAddressableUtility.cs`

Core utility for managing Addressable entries:

```csharp
public static class SOCAddressableUtility
{
    private const string SOCGroupName = "ScriptableObjectCollections";

    // Ensure an item is properly addressable with the right label
    public static void EnsureItemAddressable(ISOCItem item, string assetPath)
    {
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        string assetGuid = AssetDatabase.AssetPathToGUID(assetPath);

        // Find or create the SOC group
        var group = GetOrCreateSOCGroup(settings);

        // Add or update the entry
        var entry = settings.CreateOrMoveEntry(assetGuid, group, readOnly: false);
        entry.address = assetGuid; // Use Unity GUID as address for stability

        // Find parent collection and apply its label
        if (item.Collection != null)
        {
            string label = item.Collection.AddressableLabel;
            if (!entry.labels.Contains(label))
            {
                settings.AddLabel(label);
                entry.labels.Add(label);
            }
        }
    }

    public static void EnsureCollectionAddressable(
        ScriptableObjectCollection collection, string assetPath)
    {
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        string assetGuid = AssetDatabase.AssetPathToGUID(assetPath);
        var group = GetOrCreateSOCGroup(settings);
        var entry = settings.CreateOrMoveEntry(assetGuid, group);
        entry.address = $"soc_collection_{collection.GUID.ToBase64()}";
    }

    // Ensure registry is addressable
    public static void EnsureRegistryAddressable(CollectionsRegistry registry)
    {
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        string assetPath = AssetDatabase.GetAssetPath(registry);
        string assetGuid = AssetDatabase.AssetPathToGUID(assetPath);
        var group = GetOrCreateSOCGroup(settings);
        var entry = settings.CreateOrMoveEntry(assetGuid, group);
        entry.address = CollectionsRegistry.RegistryAddress; // "SOC_Registry"
    }

    // Bulk operation: re-label all items in a collection's folder
    public static void RelabelCollectionItems(ScriptableObjectCollection collection)
    {
        // Find all items in folder, ensure each has the right label
    }

    private static AddressableAssetGroup GetOrCreateSOCGroup(
        AddressableAssetSettings settings)
    {
        var group = settings.FindGroup(SOCGroupName);
        if (group == null)
        {
            group = settings.CreateGroup(SOCGroupName, false, false, true,
                null, typeof(BundledAssetGroupSchema));
        }
        return group;
    }
}
```

### 3.3 Registry auto-update

**New file or extension of SOCAddressablePostprocessor**

When collections are added/removed/changed, automatically update the
`CollectionsRegistry` asset's `collectionEntries` list:

```csharp
public static void RebuildRegistryMetadata()
{
    var registry = CollectionsRegistry.Instance; // editor load
    registry.collectionEntries.Clear();

    string[] guids = AssetDatabase.FindAssets($"t:{nameof(ScriptableObjectCollection)}");
    foreach (string guid in guids)
    {
        string path = AssetDatabase.GUIDToAssetPath(guid);
        var collection = AssetDatabase.LoadAssetAtPath<ScriptableObjectCollection>(path);
        if (collection == null) continue;

        registry.collectionEntries.Add(new CollectionMetadata
        {
            guid = collection.GUID,
            collectionName = collection.name,
            addressableAddress = $"soc_collection_{collection.GUID.ToBase64()}",
            itemLabel = collection.AddressableLabel,
            assetGuid = guid,
        });
    }

    EditorUtility.SetDirty(registry);
}
```

### 3.4 Validation / "Sync Addressables" button

Add a menu item and inspector button: **"SOC > Sync Addressables"** that:
1. Scans all collections
2. Ensures all items have correct labels
3. Removes stale labels from deleted items
4. Updates registry metadata
5. Reports any issues

---

## Phase 4: Code Generation Changes

### 4.1 Generated static accessor class

**File:** `Scripts/Editor/Core/CodeGenerationUtility.cs`

Generated code changes significantly. Instead of caching individual items by GUID
lookup from a loaded collection, items are loaded individually via Addressables.

**New generated pattern:**

```csharp
// MyCollection.g.cs (generated)
public partial class MyItem
{
    private static MyItemCollection cachedValues;
    private static AsyncOperationHandle<IList<ScriptableObject>> itemsHandle;

    public static MyItemCollection Values
    {
        get
        {
            if (cachedValues == null)
            {
                cachedValues = CollectionsRegistry.Instance
                    .GetOrLoadCollection(new LongGuid(123, 456)) as MyItemCollection;
            }
            return cachedValues;
        }
    }

    // Individual item accessors - load via collection
    private static bool hasCachedSword;
    private static MyItem cachedSword;
    public static MyItem Sword
    {
        get
        {
            if (!hasCachedSword)
            {
                hasCachedSword = Values.TryGetItemByGUID(
                    new LongGuid(789, 012), out cachedSword);
            }
            return cachedSword;
        }
    }

    // Loading / unloading
    public static bool IsCollectionLoaded => cachedValues != null && cachedValues.IsLoaded;

    public static async Task EnsureLoadedAsync()
    {
        // Async preload for performance-sensitive code
        await Addressables.LoadAssetsAsync<ScriptableObject>(
            Values.AddressableLabel, null).Task;
    }

    public static void UnloadCollection()
    {
        cachedValues?.Unload();
        cachedValues = null;
        hasCachedSword = false;
        cachedSword = null;
        // ... all item caches
    }
}
```

The key difference: `Values` triggers loading the collection, and accessing the
collection's `Items` triggers `WaitForCompletion()` to load all items with the
collection's label.

Individual item properties (`Sword`, etc.) go through `Values.TryGetItemByGUID()`
which in turn triggers the lazy load.

### 4.2 Remove the old `WriteNonAutomaticallyLoadedCollectionItems` distinction

The distinction between auto-loaded and manually-loaded collections disappears.
ALL collections are Addressable and loaded on demand. The `automaticallyLoaded`
field is removed.

### 4.3 Indirect reference changes

`CollectionItemIndirectReference<T>.Ref` already resolves via registry - this
path remains similar but now triggers lazy loading:

```csharp
public TObject Ref
{
    get
    {
        if (cachedRef != null) return cachedRef;
        // This will trigger collection + items loading via WaitForCompletion
        var collection = CollectionsRegistry.Instance.GetOrLoadCollection(CollectionGUID);
        if (collection != null && collection.TryGetItemByGUID(CollectionItemGUID, out ScriptableObject item))
            cachedRef = item as TObject;
        return cachedRef;
    }
}
```

---

## Phase 5: Build Pipeline Integration

### 5.1 Pre-build validation

**File:** `Scripts/Editor/Addressables/SOCBuildProcessor.cs`

An `IPreprocessBuildWithReport` that:
1. Ensures all collections and items are in Addressable groups
2. Validates labels are correct
3. Updates registry metadata
4. Fails the build if items are orphaned (in folder but not addressable)

### 5.2 Remove Resources folder dependency

- `CollectionsRegistry` no longer lives in Resources/
- Can live anywhere (e.g., `Assets/SOC/CollectionsRegistry.asset`)
- Loaded via its Addressable address `"SOC_Registry"`
- Pre/post build collection removal logic is no longer needed

---

## Phase 6: Assembly & Package Changes

### 6.1 package.json

- Add `com.unity.addressables` as a hard dependency (minimum version `1.19.0`
  or later for `WaitForCompletion()` support)
- Bump package version to `3.0.0` (breaking change)

```json
{
  "dependencies": {
    "com.unity.addressables": "1.19.0"
  }
}
```

### 6.2 Assembly definitions

**Files:** `Scripts/Runtime/BrunoMikoski.ScriptableObjectCollection.asmdef`,
`Scripts/Editor/BrunoMikoski.ScriptableObjectCollection.Editor.asmdef`

- Add `Unity.Addressables` and `Unity.ResourceManager` as references
- Remove the `versionDefines` for `ADDRESSABLES_ENABLED` (no longer conditional)
- Remove all `#if ADDRESSABLES_ENABLED` guards throughout the codebase

### 6.3 Remove `#if ADDRESSABLES_ENABLED` conditionals

All code that was conditional on addressables is now unconditional. Grep for
`ADDRESSABLES_ENABLED` and remove all `#if` blocks, keeping the addressable path.

---

## File Change Summary

### New files
| File | Purpose |
|------|---------|
| `Scripts/Editor/Addressables/SOCAddressablePostprocessor.cs` | Auto-label items on import/move |
| `Scripts/Editor/Addressables/SOCAddressableUtility.cs` | Addressable group/label management |
| `Scripts/Editor/Addressables/SOCBuildProcessor.cs` | Pre-build validation |
| `Scripts/Runtime/Core/CollectionMetadata.cs` | Lightweight collection descriptor |

### Modified files
| File | Changes |
|------|---------|
| `Scripts/Runtime/Core/ScriptableObjectCollection.cs` | Remove serialized items list, add Addressables loading, folder-based discovery |
| `Scripts/Runtime/Core/CollectionsRegistry.cs` | Store metadata instead of direct refs, load via Addressables |
| `Scripts/Runtime/Core/ScriptableObjectCollectionItem.cs` | Add `SetCollectionRuntime()`, adjust `Collection` property |
| `Scripts/Runtime/Core/CollectionItemIndirectReference.cs` | Adjust resolution to trigger lazy loading |
| `Scripts/Editor/Core/CodeGenerationUtility.cs` | New generated code patterns, remove old addressables toggle |
| `Scripts/Editor/Core/SOCSettings.cs` | Remove `WriteAddressableLoadingMethods` (now always), remove `AutomaticallyLoaded` |
| `Scripts/Editor/Core/CollectionSettings.cs` | Remove addressable toggle, add group/label settings |
| `Scripts/Editor/CustomEditors/CollectionCustomEditor.cs` | Folder-based item list, remove auto-load toggle |
| `Scripts/Runtime/*.asmdef` | Add Addressables dependency, remove version defines |
| `Scripts/Editor/*.asmdef` | Add Addressables dependency, remove version defines |
| `package.json` | Add addressables dependency, bump version |

### Deleted files
| File | Reason |
|------|--------|
| `Scripts/Runtime/Utils/ResourceScriptableObjectSingleton.cs` | No longer needed |

---

## Migration Path for Existing Users

### Automatic migration tool

**New file:** `Scripts/Editor/Migration/SOCMigrationWindow.cs`

An editor window that:
1. Finds the existing `Resources/CollectionsRegistry.asset`
2. For each registered collection:
   a. Creates Addressable entries for the collection and all its items
   b. Applies the correct label to each item
   c. Populates `CollectionMetadata` in the new registry
3. Moves the registry out of Resources/
4. Makes it Addressable with address `"SOC_Registry"`
5. Regenerates all static accessor classes
6. Reports what was migrated

### Breaking changes checklist
- `ScriptableObjectCollection.Items` is no longer a mutable `List<>`, it's `IReadOnlyList<>`
- `IList` interface removed from collection
- `AutomaticallyLoaded` property removed
- Direct manipulation of items list removed (Add/Remove still work but affect files)
- First access to items may cause a frame hitch (WaitForCompletion)
- `ResourceScriptableObjectSingleton` deleted
- Package now requires Addressables

---

## Ordering & Sorting

Since items are no longer in a serialized list, ordering needs a new home:

- The collection asset stores `[SerializeField] List<LongGuid> itemOrder`
- Editor uses this to sort the display list
- Runtime can use this to sort loaded items
- If an item is not in the order list, it goes at the end (alphabetical)
- `ShouldProtectItemOrder` still works against this order list

---

## Performance Considerations

### WaitForCompletion caveats
- First access to any collection's items will block the main thread
- For large collections, recommend calling `EnsureLoadedAsync()` during loading screens
- Generated code includes both sync (property) and async (`EnsureLoadedAsync`) paths
- Consider logging a warning on first sync load in development builds

### Addressable group strategy
- Default: one group `ScriptableObjectCollections` for all SOC assets
- Can be configured per-collection if needed (e.g., DLC content in separate groups)
- Labels used for logical grouping, groups used for bundle boundaries

### Memory
- Items only loaded when a collection is first accessed
- Can be explicitly unloaded via `UnloadCollection()`
- Editor no longer holds all items in memory via serialized references
- GC can reclaim items that aren't referenced after collection unload

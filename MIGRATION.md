# Migration Guide: v2.x to v3.0.0

This document covers everything that changed between the original
[brunomikoski/ScriptableObjectCollection](https://github.com/brunomikoski/ScriptableObjectCollection)
(v2.4.x and earlier) and v3.0.0. Version 3.0 is a ground-up architectural
change: the entire loading, discovery, and code-generation pipeline has been
rebuilt on top of **Unity Addressables**.

---

## At a Glance

| Area | v2.x (original) | v3.0.0 |
|------|-----------------|--------|
| **Asset loading** | Resources folder + direct references | Pure Addressables |
| **Collection discovery** | `CollectionsRegistry` singleton in Resources | Addressables label query (`soc_collections`) |
| **Item storage** | Serialized `List<ScriptableObject>` on collection | Folder-based: items live in an `Items/` subfolder |
| **Indirect references** | `CollectionItemIndirectReference` system | Removed (use `AssetReference` or generated static accessors) |
| **Runtime identity** | Two GUIDs (collection + item) stored on item | `LongGuid` on collection only; items identified by asset GUID |
| **Code generation output** | Static class + IndirectReference `.g.cs` files | Static class `.g.cs` only, loads via Addressables |
| **Minimum Unity** | 2018.4 | 2022.2 |
| **Addressables** | Optional (conditional `#if`) | Required hard dependency (1.19.0+) |
| **CollectionItemPicker** | Stored direct ScriptableObject references | Stores `List<AssetReference>`, lazy-resolved |
| **Editor automation** | Manual sync button | `AssetPostprocessor` auto-labels on import/move/delete |
| **Browser window** | Standalone IMGUI TreeView window | Removed |
| **Settings storage** | Per-collection ScriptableObject fields | `ProjectSettings/ScriptableObjectCollection.json` + asset `.meta` userData |

---

## Breaking Changes

### 1. CollectionsRegistry is gone

**Before:** A `ResourceScriptableObjectSingleton<CollectionsRegistry>` lived in a
Resources folder, held references to every collection, and was the entry point
for all runtime lookups.

```csharp
// v2.x
CollectionsRegistry.Instance.TryGetCollectionByGUID(guid, out var collection);
CollectionsRegistry.Instance.RegisterCollection(collection);
```

**After:** Collections are discovered via Addressables labels. There is no
singleton.

```csharp
// v3.0
var all = ScriptableObjectCollection.FindAll();            // label: "soc_collections"
var one = ScriptableObjectCollection.LoadByGUID(longGuid); // address: "soc_collection_<base64>"
```

**Migration steps:**
- Delete any `CollectionsRegistry.asset` from your Resources folder.
- Delete the Resources folder if it is now empty.
- Replace all `CollectionsRegistry.Instance` calls with the static methods on
  `ScriptableObjectCollection`.

---

### 2. IndirectReference is gone

**Before:** Every collection item type had a generated `*IndirectReference`
class that stored two GUIDs (collection + item) for serialization-safe
references.

```csharp
// v2.x
[SerializeField] private WeaponIndirectReference weaponRef;
WeaponItem weapon = weaponRef.Ref;
```

**After:** Use Unity's `AssetReference` for serializable references, or use the
generated static accessors for compile-time-safe access.

```csharp
// v3.0 - Option A: AssetReference (inspector-assignable, Addressables-native)
[SerializeField] private AssetReferenceT<WeaponItem> weaponRef;

// v3.0 - Option B: static accessor (generated code)
WeaponItem weapon = WeaponItem.Sword;
```

**Migration steps:**
- Delete all `*IndirectReference.g.cs` files from your project.
- Replace `[SerializeField] SomeIndirectReference field` with either
  `AssetReferenceT<SomeItem>` or a direct `SomeItem` reference.
- The property drawer handles both `AssetReferenceT<ISOCItem>` and direct
  `ISOCItem` fields with the same dropdown UI.
- Re-serialize any assets that stored IndirectReference data (the old GUID pair
  format is not compatible).

---

### 3. Items no longer store collection/GUID references

**Before:** `ScriptableObjectCollectionItem` had serialized fields for the
parent collection reference and a per-item GUID.

**After:** `ScriptableObjectCollectionItem` is an empty class (just implements
`ISOCItem`). The parent collection is inferred from the folder structure, and
items are identified by their Unity asset GUID.

**Migration steps:**
- Existing item assets may still have orphaned serialized data for the old
  fields. Unity will ignore these harmlessly, but you can re-serialize to
  clean them up.
- If you had code that accessed `item.Collection` or `item.GUID`, those
  properties no longer exist. Use the generated static accessors or
  `ScriptableObjectCollection.FindByItemType()` to find the parent collection.

---

### 4. Collections use folder-based item discovery

**Before:** Collections serialized a `List<ScriptableObject> items` directly.
Items could live anywhere in the project.

**After:** A collection's items are the assets in its `Items/` subfolder (and
sub-subfolders). The collection itself does not serialize item references.
Items are loaded at runtime via Addressables using the collection's label.

**Example folder structure:**
```
Database/
  Weapons/
    WeaponCollection.asset          <-- the collection
    Items/
      Sword.asset                   <-- items
      Shield.asset
      Bows/
        Longbow.asset               <-- subfolder items included too
```

**Migration steps:**
- Create an `Items/` subfolder next to each collection asset.
- Move all items belonging to that collection into its `Items/` folder.
- The `SOCAddressablePostprocessor` will automatically label them.
- Alternatively, use the collection inspector's move functionality.

---

### 5. Addressables is a hard dependency

**Before:** Addressables was optional. Code was guarded with
`#if ADDRESSABLES_ENABLED`.

**After:** `com.unity.addressables >= 1.19.0` is a required dependency
declared in `package.json`. All conditional compilation guards have been
removed.

**Migration steps:**
- Install Addressables via Package Manager if not already present.
- Run `SOCAddressableUtility.SyncAllAddressables()` (or just modify any
  collection/item asset to trigger the postprocessor) to populate your
  Addressable groups and labels.
- All collections are placed in a `ScriptableObjectCollections` Addressable
  group automatically.

---

### 6. Generated static code has changed

**Before:** Generated code accessed items via `CollectionsRegistry.Instance`:

```csharp
// v2.x generated
public static WeaponCollection Values
{
    get
    {
        if (!hasCachedValues)
            hasCachedValues = CollectionsRegistry.Instance
                .TryGetCollectionByGUID(new LongGuid(...), out cachedValues);
        return cachedValues;
    }
}
```

**After:** Generated code loads directly via Addressables:

```csharp
// v3.0 generated
public static WeaponCollection Values
{
    get
    {
        if (cachedValues == null)
            cachedValues = Addressables
                .LoadAssetAsync<WeaponCollection>("soc_collection_<base64_guid>")
                .WaitForCompletion();
        return cachedValues;
    }
}
```

**Migration steps:**
- Regenerate all static accessor files using the "Generate Static File"
  button on each collection's inspector.
- Delete any old generated files that don't have the `.g.cs` suffix.
- The `TryGetItemByGUID()` method no longer exists. Generated item
  accessors load each item directly by its Addressable address.

---

### 7. CollectionItemPicker stores AssetReferences

**Before:** `CollectionItemPicker<T>` stored a `List<T>` of direct
ScriptableObject references.

**After:** It stores a `List<AssetReference>` internally. Items are resolved
lazily: in the editor via `editorAsset`, at runtime via
`LoadAssetAsync().WaitForCompletion()`.

**Migration steps:**
- Existing serialized CollectionItemPicker fields will lose their data
  because the backing field type changed from `List<ScriptableObject>` to
  `List<AssetReference>`. Re-assign items through the inspector.
- The public API (`Add`, `Remove`, `Contains`, `HasAll`, indexer, etc.) is
  unchanged. Code that uses CollectionItemPicker should work without changes.

---

### 8. LongGuid replaces dual-GUID system

**Before:** Items had their own GUID. Collections also had a GUID. The
pair was used for lookups.

**After:** Only collections have a `LongGuid`. This is a custom 128-bit
struct (two `long` values) that serializes to Base64 for use in Addressable
labels and addresses.

Items are identified by their standard Unity asset GUID (as used by
`AssetReference`).

---

### 9. Settings storage moved

**Before:** Settings were stored on the collection ScriptableObject itself
and in per-generator fields.

**After:**
- **Global settings** are in `ProjectSettings/ScriptableObjectCollection.json`
  (namespace prefix, max depth, default script path).
- **Per-collection settings** (namespace, static filename, output folder,
  partial class, base class) are stored in the collection asset's
  `.meta` file as JSON in the `userData` field.

**Migration steps:**
- Open each collection in the inspector and re-configure namespace,
  static filename, and output folder as needed. Old settings are not
  automatically migrated.
- Global settings can be configured at `Project Settings > Scriptable Object Collection`.

---

### 10. Minimum Unity version is 2022.2

**Before:** Unity 2018.4+.

**After:** Unity 2022.2+. All backward-compatibility guards for older Unity
versions (e.g., `#if UNITY_2022_2_OR_NEWER`) have been removed.

---

## Removed Features

| Feature | Reason |
|---------|--------|
| `CollectionsRegistry` | Replaced by Addressables label queries |
| `CollectionItemIndirectReference` | Replaced by `AssetReference` / static accessors |
| `CollectionItemQuery<T>` | Unused utility, removed |
| `CollectionItemPicker` operators (`+`, `-`) | Unused, removed |
| `CollectionItemPicker.HasAny()` / `HasNone()` | Unused, removed |
| `EditorPreferenceInt` / `Float` / `Object<T>` | Unused subclasses, removed |
| Browser editor window | Redundant with inspector, removed |
| Sample project (`Samples~/AddressablesCollection`) | Incompatible with v3 API, removed |
| `ResourceScriptableObjectSingleton` | Resources-based pattern, removed |
| Per-item GUID / collection reference fields | Items identified by folder + asset GUID |
| `#if ADDRESSABLES_ENABLED` guards | Addressables is always-on |
| `#if UNITY_2022_2_OR_NEWER` guards | 2022.2 is the minimum |

---

## New Features

| Feature | Description |
|---------|-------------|
| Automatic Addressable labeling | `SOCAddressablePostprocessor` watches for asset changes and auto-configures Addressable groups, labels, and addresses |
| Folder-based item discovery | Items belong to whichever collection's `Items/` folder they reside in |
| Pre-build Addressable sync | `CollectionPreprocessBuild` ensures all labels are current before building |
| `ScriptableObjectCollection.FindAll()` | Load all collections via the `soc_collections` label |
| `ScriptableObjectCollection.LoadByGUID()` | Load a specific collection by its `LongGuid` |
| `ScriptableObjectCollection.FindByItemType()` | Find collections containing a given item type |
| Lazy item loading | `collection.Items` triggers `LoadSync()` on first access |
| `collection.Unload()` | Release Addressable handles to free memory |
| Per-collection settings in `.meta` | No longer pollutes the ScriptableObject with editor-only fields |
| Project-wide settings JSON | `ProjectSettings/ScriptableObjectCollection.json` |

---

## Migration Checklist

1. **Update Unity** to 2022.2 or later
2. **Install Addressables** package (1.19.0+) via Package Manager
3. **Delete** `CollectionsRegistry.asset` and its Resources folder
4. **Delete** all `*IndirectReference.g.cs` generated files
5. **Delete** all old generated `.cs` files (without the `.g.cs` suffix)
6. **Organize items** into `Items/` subfolders next to their collection assets
7. **Trigger Addressables sync**: modify any collection to invoke the
   postprocessor, or call `SOCAddressableUtility.SyncAllAddressables()` from
   a menu item or editor script
8. **Regenerate static code** by clicking "Generate Static File" on each
   collection's inspector
9. **Replace code references:**
   - `CollectionsRegistry.Instance.*` -> `ScriptableObjectCollection.FindAll()` / `LoadByGUID()`
   - `*IndirectReference` fields -> `AssetReferenceT<T>` or direct references
   - `item.Collection` / `item.GUID` -> use static accessors or `FindByItemType()`
   - `TryGetItemByGUID()` -> use generated static property or `TryGetItemByName()`
10. **Re-assign CollectionItemPicker fields** in the inspector (backing type changed)
11. **Re-configure per-collection settings** (namespace, filename, output folder)
    in each collection's inspector
12. **Build and test** - the pre-build processor will validate Addressable entries

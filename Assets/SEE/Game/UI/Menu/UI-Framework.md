# UI-Framework

## General
This is a basic overview on the UI-Framework.

## Classes
| Class                      | Description                                                            |
|----------------------------|------------------------------------------------------------------------|
| PlatformDependentComponent | A component with different start and update methods for each platform. |
| ListMenu                   | A abstract menu where the user can choose between different entries.   |
| SimpleMenu                 | A list menu which instantiates a prefab for each menu entry.           |

## PlatformDependentComponent
> A component with different start and update methods for each platform.

### Attributes
| Attribute | Description                                                 |
|-----------|-------------------------------------------------------------|
| Canvas    | The canvas on which UI elements are placed.                 |
| Platform  | The current platform. (doesn't change after initialization) |
| HasStarted | Whether the UI is initialized |

### Start and Update methods
* Start: Calls the start method for the current platform.
  * An unimplemented start method for a platform means that the platform isn't supported.
  * *StartDesktop | StartVR | StartTouchGamepad*
* Update: Calls the update method for the current platform.
  * *UpdateDesktop | UpdateVR | UpdateTouchGamepad*

### OnStartFinished
At the end of the start method *OnStartFinished* is called.<br/>
This can be used to update the UI and add listeners to events.

## ListMenu
> A abstract menu where the user can choose between different entries.

### Properties
> Properties to modify the menu.

| Property           | Description                                         |
|--------------------|-----------------------------------------------------|
| Title              | The menu title                                      |
| Description        | The menu description                                |
| Icon               | The menu icon.                                      |
| CloseMenuCommand   | The keyword to close the menu.                      |
| ShowMenu           | Whether to show the menu.                           |
| AllowNoSelection   | Whether the menu can be closed without a selection. |
| HideAfterSelection | Whether to hide the menu after a selection.         |
| Entries            | The menu entries. (read-only)                       |
| Menu               | The menu game object.                               |

### Methods
> Methods to modify the menu.

| Method          | Description                               |
|-----------------|-------------------------------------------|
| EntryGameObject | Returns the game object of an menu entry. |
| ToggleMenu      | Toggles the menu.                         |
| AddEntry        | Adds a menu entry.                        |
| RemoveEntry     | Removes a menu entry.                     |
| SelectEntry     | Selects a menu entry.                     |

### Events
> Events that are triggered when the menu changes.

| Event                       | Description                              |
|-----------------------------|------------------------------------------|
| OnTitleChanged              | When the Title was changed.              |
| OnDescriptionChanged        | When the Description was changed.        |
| OnIconChanged               | When the Icon was changed.               |
| OnShowMenuChanged           | When the ShowMenu was changed.           |
| OnAllowNoSelectionChanged   | When the AllowNoSelection was changed.   |
| OnHideAfterSelectionChanged | When the HideAfterSelection was changed. |
| OnCloseMenuCommandChanged   | When the CloseMenuCommand was changed.   |
| OnEntryAdded                | When an entry was added.                 |
| OnEntryRemoved              | When an entry was removed.               |
| OnEntrySelected             | When an entry was selected.              |
| OnKeywordRecognized         | When an keyword were recognized.         |

## SimpleMenu
> A list menu which instantiates a prefab for each menu entry.

### Prefabs
> The prefabs used to instantiate the menu or buttons for menu entries.

| Prefab      | Description                                                                                                    |
|-------------|----------------------------------------------------------------------------------------------------------------|
| MenuPrefab  | The prefab used for instantiating the menu.                                                                    |
| EntryPrefab | The prefab used for each added menu entry. <br> (Only used if no game object is not found at *EntryListPath*.) |

### Paths
> Paths to specific parts of the menu.

| Path            | Description                                                                                                                                                             |
|-----------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| IconTitle       | The path to the game object containing the icon and the title.                                                                                                          |
| CloseButtonPath | The path to the game object containing the close button.                                                                                                                |
| ContentPath     | The path to the game object containing the menu content.                                                                                                                |
| EntryListPath   | The path to the game object containing the menu entry game objects.<br>Starts at the *Content* game object.<br>Can point to a game object inside the *EntryListPrefab*. |

### Hierarchy
> This is the default hierarchy.<br>
> The corresponding path and prefab attributes are in parentheses.

* Menu (MenuPrefab)
  * Main Content
    * Icon Title Mask
      * Content (IconTitlePath)
    * Buttons (CloseButtonPath)
    * Content Mask
      * Content (ContentPath)
        * Menu Entries (MenuListPrefab, MenuListPath)
          * Example Entry (EntryPrefab)

## Advanced Menus
* SelectionMenu: A list menu where only one entry can be active at a time.
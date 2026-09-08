# Material3.Avalonia
A modern Material Design 3 theme library for Avalonia applications.

> **Status:** Work in progress / early preview.  
> Controls coverage is still limited; the M3 theming foundation is mostly in place.

## Requirements
- **Avalonia:** 12.1.2+
- **Target Framework:** .NET 8.0

Progress on implemented controls is tracked in [issue #1](https://github.com/klorman/Material3.Avalonia/issues/1).

## Getting started

### 1) Add the library to your app
### 2) Wire up styles, resources, and the theme (App.axaml)

Add the XML namespace for the theme type (adjust if you changed the CLR namespace):
```xaml
<Application xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:theme="clr-namespace:Material3.Avalonia.Theme;assembly=Material3.Avalonia">
    <Application.Styles>
        <StyleInclude Source="avares://Material3.Avalonia/Theme/MaterialThemeStyles.axaml" />
        <theme:MaterialTheme Mode="Light"
                             Variant="TonalSpot"
                             SourceColor="#6750A4"
                             Contrast="Standard"
                             MotionScheme="Standard" />
    </Application.Styles>
    
    <Application.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <ResourceInclude Source="avares://Material3.Avalonia/Theme/MaterialThemeResources.axaml" />
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </Application.Resources>
</Application>
```

## Acknowledgements
Based on the [Material Design 3 guidelines](https://m3.material.io/) by Google.  
This is an **independent implementation** for Avalonia and is **not affiliated with or endorsed by Google**.

## License
This project is licensed under the [MIT License](LICENSE).

## Third-party assets
Embedded assets (e.g., fonts) are licensed by their respective owners under their own terms.
See [THIRD-PARTY-NOTICES.txt](THIRD-PARTY-NOTICES.txt)
and the files under third_party/ for details.

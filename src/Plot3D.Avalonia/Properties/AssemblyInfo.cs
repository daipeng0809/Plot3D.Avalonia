using Avalonia.Metadata;

// 允许在 AXAML 中通过 xmlns:plot3d="https://plot3d.avalonia.dev" 引用控件。
[assembly: XmlnsDefinition("https://plot3d.avalonia.dev", "Plot3D.Avalonia.Controls")]
[assembly: XmlnsDefinition("https://plot3d.avalonia.dev", "Plot3D.Avalonia.Modeling")]
[assembly: XmlnsPrefix("https://plot3d.avalonia.dev", "plot3d")]

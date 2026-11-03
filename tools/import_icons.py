"""Convert the vendored Freepik UIcons SVG paths to WPF geometry resources."""
from pathlib import Path
import xml.etree.ElementTree as ET

root = Path(__file__).resolve().parents[1]
icons = {"Aperture": "aperture", "Plus": "plus",
         "Up": "angle-small-up", "Down": "angle-small-down", "Edit": "pencil", "Trash": "trash",
         "Right": "angle-small-right", "App": "apps", "Close": "cross", "Minimize": "minus", "Info": "info"}
lines = ['<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">']
for key, name in icons.items():
    svg = ET.parse(root / "Assets" / "UIcons" / f"{name}.svg").getroot()
    paths = [node.attrib["d"] for node in svg.iter() if node.tag.endswith("}path")]
    if not paths:
        raise ValueError(f"No paths: {name}")
    lines.append(f'  <Geometry x:Key="Icon{key}">F1 {" ".join(paths)}</Geometry>')
for key, name in {"Sun": "sun-fill", "Moon": "moon-fill"}.items():
    svg = ET.parse(root / "Assets" / "Bootstrap" / f"{name}.svg").getroot()
    paths = [node.attrib["d"] for node in svg.iter() if node.tag.endswith("}path")]
    lines.append(f'<GeometryGroup x:Key="Icon{key}" Transform="16,0,0,16,0,0"><PathGeometry Figures="{" ".join(paths)}" FillRule="Nonzero"/></GeometryGroup>')
lines.append('</ResourceDictionary>')
(root / "Assets" / "IconGeometry.xaml").write_text("\n".join(lines) + "\n", encoding="utf-8")

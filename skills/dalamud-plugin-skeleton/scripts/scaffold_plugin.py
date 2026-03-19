#!/usr/bin/env python3
"""
Render a minimal Dalamud plugin skeleton from the bundled starter assets.
"""

from __future__ import annotations

import argparse
import html
import json
import re
import sys
from pathlib import Path


DEFAULT_SDK_VERSION = "14.0.1"
DEFAULT_TARGET_FRAMEWORK = "net10.0-windows"
DEFAULT_VERSION = "0.0.1.0"
INTERNAL_NAME_PATTERN = re.compile(r"^[A-Za-z0-9_.-]+$")
NAMESPACE_PATTERN = re.compile(r"^[_A-Za-z][_A-Za-z0-9]*(\.[_A-Za-z][_A-Za-z0-9]*)*$")


def split_words(value: str) -> list[str]:
    collapsed = re.sub(r"[^A-Za-z0-9]+", " ", value).strip()
    if not collapsed:
        return []
    chunks = []
    for chunk in collapsed.split():
        parts = re.findall(r"[A-Z]+(?=[A-Z][a-z]|\d|$)|[A-Z]?[a-z]+|\d+", chunk)
        chunks.extend(parts or [chunk])
    return chunks


def default_plugin_name(internal_name: str) -> str:
    words = split_words(internal_name)
    if not words:
        return internal_name
    return " ".join(word.upper() if word.isupper() else word.capitalize() for word in words)


def default_namespace(internal_name: str) -> str:
    cleaned = re.sub(r"[^A-Za-z0-9.]+", "", internal_name)
    if not cleaned:
        return "PluginSkeleton"
    if cleaned[0].isdigit():
        cleaned = f"_{cleaned}"
    return cleaned


def normalize_command(command: str | None, internal_name: str) -> str:
    if command:
        return command if command.startswith("/") else f"/{command}"
    slug = re.sub(r"[^a-z0-9]+", "", internal_name.lower())
    return f"/{slug or 'plugin'}"


def csharp_escape(value: str) -> str:
    return (
        value.replace("\\", "\\\\")
        .replace("\"", "\\\"")
        .replace("\r", "\\r")
        .replace("\n", "\\n")
    )


def xml_escape(value: str) -> str:
    return html.escape(value, quote=True)


def format_tags_json(tags: list[str]) -> str:
    if not tags:
        return "[]"
    rendered = ",\n".join(f"    {json.dumps(tag)}" for tag in tags)
    return "[\n" + rendered + "\n  ]"


def ensure_target_dir(target: Path, force: bool) -> None:
    if target.exists():
        if not target.is_dir():
            raise ValueError(f"Target exists and is not a directory: {target}")
        if any(target.iterdir()) and not force:
            raise ValueError(f"Target directory is not empty: {target}")
    else:
        target.mkdir(parents=True, exist_ok=True)


def replace_tokens(text: str, replacements: dict[str, str]) -> str:
    for key, value in replacements.items():
        text = text.replace(f"__{key}__", value)
    return text


def render_templates(template_root: Path, target_root: Path, replacements: dict[str, str]) -> list[Path]:
    created: list[Path] = []
    for source in sorted(template_root.rglob("*")):
        relative = source.relative_to(template_root)
        rendered_parts = [replace_tokens(part, replacements) for part in relative.parts]
        destination = target_root.joinpath(*rendered_parts)

        if source.is_dir():
            destination.mkdir(parents=True, exist_ok=True)
            continue

        if destination.suffix == ".template":
            destination = destination.with_suffix("")

        destination.parent.mkdir(parents=True, exist_ok=True)
        content = source.read_text(encoding="utf-8")
        destination.write_text(replace_tokens(content, replacements), encoding="utf-8")
        created.append(destination)

    return created


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description="Scaffold a minimal Dalamud plugin starter.")
    parser.add_argument("--target", required=True, help="Absolute or relative path for the output plugin root.")
    parser.add_argument("--internal-name", required=True, help="Plugin InternalName and project name.")
    parser.add_argument("--plugin-name", help="Display name used in the manifest and windows.")
    parser.add_argument("--namespace", help="C# namespace for generated files.")
    parser.add_argument("--author", default="REPLACE_ME", help="Plugin author for manifest/csproj.")
    parser.add_argument(
        "--repo-url",
        default="https://example.com/replace-me",
        help="Repository or project URL for the manifest and csproj.",
    )
    parser.add_argument("--command", help="Slash command. Defaults to /<internal-name>.")
    parser.add_argument(
        "--sdk-version",
        default=DEFAULT_SDK_VERSION,
        help=f"Dalamud.NET.Sdk version. Defaults to {DEFAULT_SDK_VERSION}.",
    )
    parser.add_argument(
        "--target-framework",
        default=DEFAULT_TARGET_FRAMEWORK,
        help=f"Target framework. Defaults to {DEFAULT_TARGET_FRAMEWORK}.",
    )
    parser.add_argument("--version", default=DEFAULT_VERSION, help=f"Plugin version. Defaults to {DEFAULT_VERSION}.")
    parser.add_argument(
        "--punchline",
        default="Minimal starter for a new Dalamud plugin.",
        help="One-line manifest punchline.",
    )
    parser.add_argument(
        "--description",
        default="Replace this description with a real explanation of what the plugin does.",
        help="Longer manifest and project description.",
    )
    parser.add_argument(
        "--tag",
        action="append",
        dest="tags",
        help="Manifest tag. Repeat for multiple tags. Defaults to one 'utility' tag.",
    )
    parser.add_argument("--force", action="store_true", help="Allow writing into an existing non-empty target directory.")
    return parser


def main() -> int:
    args = build_parser().parse_args()

    skill_root = Path(__file__).resolve().parents[1]
    template_root = skill_root / "assets" / "starter"

    target = Path(args.target).expanduser().resolve()
    internal_name = args.internal_name.strip()
    if not internal_name:
        print("[ERROR] --internal-name cannot be empty.", file=sys.stderr)
        return 1
    if not INTERNAL_NAME_PATTERN.fullmatch(internal_name):
        print(
            "[ERROR] --internal-name must use only letters, digits, dots, underscores, or hyphens.",
            file=sys.stderr,
        )
        return 1

    plugin_name = args.plugin_name or default_plugin_name(internal_name)
    namespace = args.namespace or default_namespace(internal_name)
    if not NAMESPACE_PATTERN.fullmatch(namespace):
        print("[ERROR] --namespace is not a valid C# namespace.", file=sys.stderr)
        return 1
    command = normalize_command(args.command, internal_name)
    tags = args.tags or ["utility"]

    try:
        ensure_target_dir(target, args.force)
    except ValueError as exc:
        print(f"[ERROR] {exc}", file=sys.stderr)
        return 1

    replacements = {
        "AUTHOR": args.author,
        "AUTHOR_JSON": json.dumps(args.author),
        "AUTHOR_XML": xml_escape(args.author),
        "COMMAND": command,
        "COMMAND_CSHARP": csharp_escape(command),
        "DESCRIPTION": args.description,
        "DESCRIPTION_JSON": json.dumps(args.description),
        "DESCRIPTION_XML": xml_escape(args.description),
        "INTERNAL_NAME": internal_name,
        "INTERNAL_NAME_CSHARP": csharp_escape(internal_name),
        "NAMESPACE": namespace,
        "PLUGIN_NAME": plugin_name,
        "PLUGIN_NAME_CSHARP": csharp_escape(plugin_name),
        "PLUGIN_NAME_JSON": json.dumps(plugin_name),
        "PLUGIN_VERSION": args.version,
        "PLUGIN_VERSION_XML": xml_escape(args.version),
        "PUNCHLINE": args.punchline,
        "PUNCHLINE_JSON": json.dumps(args.punchline),
        "REPO_URL": args.repo_url,
        "REPO_URL_JSON": json.dumps(args.repo_url),
        "REPO_URL_XML": xml_escape(args.repo_url),
        "SDK_VERSION": args.sdk_version,
        "TAGS_JSON": format_tags_json(tags),
        "TARGET_FRAMEWORK": args.target_framework,
    }

    created = render_templates(template_root, target, replacements)

    print(f"[OK] Rendered Dalamud plugin skeleton into {target}")
    for path in created:
        print(path)

    return 0


if __name__ == "__main__":
    raise SystemExit(main())

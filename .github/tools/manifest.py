#!/usr/bin/env python3
"""
Jellyfin Plugin Manifest Generator

Manages manifest.json for Jellyfin plugin repositories.
Supports adding new versions and creating new manifests from build.yaml.

Usage:
    # Add new version to existing manifest
    manifest.py version -f manifest.json -ver 1.0.0 -abi 10.11.6.0 -url https://... -ck abc123

    # Create new manifest from build.yaml
    manifest.py create -f manifest.json --from-build build.yaml

    # Show current manifest info
    manifest.py info -f manifest.json
"""

import argparse
import json
import sys
from datetime import datetime, timezone
from pathlib import Path

try:
    import yaml
    HAS_YAML = True
except ImportError:
    HAS_YAML = False


def load_manifest(path: Path) -> dict:
    """Load existing manifest or return empty structure."""
    if path.exists():
        with open(path, 'r', encoding='utf-8') as f:
            return json.load(f)
    return []


def save_manifest(path: Path, data: list) -> None:
    """Save manifest with consistent formatting."""
    with open(path, 'w', encoding='utf-8') as f:
        json.dump(data, f, indent=2, ensure_ascii=False)
        f.write('\n')


def load_build_yaml(path: Path) -> dict:
    """Load build.yaml configuration."""
    if not HAS_YAML:
        print("Error: PyYAML not installed. Run: pip install pyyaml", file=sys.stderr)
        sys.exit(1)

    with open(path, 'r', encoding='utf-8') as f:
        return yaml.safe_load(f)


def find_plugin_in_manifest(manifest: list, identifier: str) -> tuple[int, dict] | tuple[None, None]:
    """Find plugin by name or GUID in manifest."""
    for i, plugin in enumerate(manifest):
        if plugin.get('name') == identifier or plugin.get('guid') == identifier:
            return i, plugin
    return None, None


def create_version_entry(
    version: str,
    changelog: str,
    target_abi: str,
    source_url: str,
    checksum: str,
    timestamp: str | None = None
) -> dict:
    """Create a version entry for the manifest."""
    return {
        "version": version,
        "changelog": changelog,
        "targetAbi": target_abi,
        "sourceUrl": source_url,
        "checksum": checksum,
        "timestamp": timestamp or datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")
    }


def create_plugin_entry(
    name: str,
    guid: str,
    overview: str,
    description: str,
    owner: str,
    category: str,
    versions: list[dict] | None = None
) -> dict:
    """Create a new plugin entry for the manifest."""
    return {
        "guid": guid,
        "name": name,
        "overview": overview,
        "description": description,
        "owner": owner,
        "category": category,
        "versions": versions or []
    }


def cmd_version(args):
    """Add a new version to the manifest."""
    manifest_path = Path(args.file)
    manifest = load_manifest(manifest_path)

    # Find or create plugin entry
    idx, plugin = find_plugin_in_manifest(manifest, args.app)

    if plugin is None:
        print(f"Error: Plugin '{args.app}' not found in manifest.", file=sys.stderr)
        print("Use 'create' command first or check the app name.", file=sys.stderr)
        sys.exit(1)

    # Create version entry
    version_entry = create_version_entry(
        version=args.ver,
        changelog=args.cl,
        target_abi=args.abi,
        source_url=args.url,
        checksum=args.ck,
        timestamp=args.timestamp
    )

    # Check if version already exists
    existing_versions = {v.get('version') for v in plugin.get('versions', [])}
    if args.ver in existing_versions:
        if args.force:
            # Remove existing version
            plugin['versions'] = [v for v in plugin['versions'] if v.get('version') != args.ver]
            print(f"Replacing existing version {args.ver}")
        else:
            print(f"Error: Version {args.ver} already exists. Use --force to replace.", file=sys.stderr)
            sys.exit(1)

    # Add version (newest first)
    plugin.setdefault('versions', []).insert(0, version_entry)

    # Update manifest
    manifest[idx] = plugin
    save_manifest(manifest_path, manifest)

    print(f"Added version {args.ver} to {args.app}")
    print(f"  targetAbi: {args.abi}")
    print(f"  checksum:  {args.ck[:16]}...")


def cmd_create(args):
    """Create a new manifest or add plugin from build.yaml."""
    manifest_path = Path(args.file)
    manifest = load_manifest(manifest_path)

    # Load build.yaml
    build_path = Path(args.from_build)
    if not build_path.exists():
        print(f"Error: {build_path} not found", file=sys.stderr)
        sys.exit(1)

    build = load_build_yaml(build_path)

    # Check if plugin already exists
    idx, existing = find_plugin_in_manifest(manifest, build['guid'])

    plugin = create_plugin_entry(
        name=build['name'],
        guid=build['guid'],
        overview=build.get('overview', ''),
        description=build.get('description', ''),
        owner=build.get('owner', ''),
        category=build.get('category', 'General')
    )

    if existing:
        # Keep existing versions
        plugin['versions'] = existing.get('versions', [])
        manifest[idx] = plugin
        print(f"Updated plugin metadata for {build['name']}")
    else:
        manifest.append(plugin)
        print(f"Created new plugin entry for {build['name']}")

    save_manifest(manifest_path, manifest)


def cmd_info(args):
    """Show manifest information."""
    manifest_path = Path(args.file)

    if not manifest_path.exists():
        print(f"Manifest not found: {manifest_path}", file=sys.stderr)
        sys.exit(1)

    manifest = load_manifest(manifest_path)

    print(f"Manifest: {manifest_path}")
    print(f"Plugins:  {len(manifest)}")
    print()

    for plugin in manifest:
        versions = plugin.get('versions', [])
        latest = versions[0] if versions else None

        print(f"  {plugin.get('name')} ({plugin.get('guid')})")
        print(f"    Owner:    {plugin.get('owner')}")
        print(f"    Category: {plugin.get('category')}")
        print(f"    Versions: {len(versions)}")
        if latest:
            print(f"    Latest:   {latest.get('version')} (ABI {latest.get('targetAbi')})")
        print()


def cmd_remove_version(args):
    """Remove a version from the manifest."""
    manifest_path = Path(args.file)
    manifest = load_manifest(manifest_path)

    idx, plugin = find_plugin_in_manifest(manifest, args.app)

    if plugin is None:
        print(f"Error: Plugin '{args.app}' not found", file=sys.stderr)
        sys.exit(1)

    versions_before = len(plugin.get('versions', []))
    plugin['versions'] = [v for v in plugin.get('versions', []) if v.get('version') != args.ver]
    versions_after = len(plugin['versions'])

    if versions_before == versions_after:
        print(f"Warning: Version {args.ver} not found in {args.app}", file=sys.stderr)
    else:
        manifest[idx] = plugin
        save_manifest(manifest_path, manifest)
        print(f"Removed version {args.ver} from {args.app}")


def main():
    parser = argparse.ArgumentParser(
        description='Jellyfin Plugin Manifest Generator',
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog=__doc__
    )

    subparsers = parser.add_subparsers(dest='command', required=True)

    # version command
    ver_parser = subparsers.add_parser('version', help='Add a new version')
    ver_parser.add_argument('-f', '--file', required=True, help='Path to manifest.json')
    ver_parser.add_argument('-app', '--app', required=True, help='Plugin name or GUID')
    ver_parser.add_argument('-ver', '--ver', required=True, help='Version string')
    ver_parser.add_argument('-cl', '--cl', required=True, help='Changelog')
    ver_parser.add_argument('-abi', '--abi', required=True, help='Target Jellyfin ABI')
    ver_parser.add_argument('-url', '--url', required=True, help='Download URL')
    ver_parser.add_argument('-ck', '--ck', required=True, help='MD5 checksum')
    ver_parser.add_argument('-timestamp', '--timestamp', help='Override timestamp (ISO format)')
    ver_parser.add_argument('--force', action='store_true', help='Replace existing version')
    ver_parser.set_defaults(func=cmd_version)

    # create command
    create_parser = subparsers.add_parser('create', help='Create manifest from build.yaml')
    create_parser.add_argument('-f', '--file', required=True, help='Path to manifest.json')
    create_parser.add_argument('--from-build', required=True, help='Path to build.yaml')
    create_parser.set_defaults(func=cmd_create)

    # info command
    info_parser = subparsers.add_parser('info', help='Show manifest info')
    info_parser.add_argument('-f', '--file', required=True, help='Path to manifest.json')
    info_parser.set_defaults(func=cmd_info)

    # remove-version command
    rm_parser = subparsers.add_parser('remove-version', help='Remove a version')
    rm_parser.add_argument('-f', '--file', required=True, help='Path to manifest.json')
    rm_parser.add_argument('-app', '--app', required=True, help='Plugin name or GUID')
    rm_parser.add_argument('-ver', '--ver', required=True, help='Version to remove')
    rm_parser.set_defaults(func=cmd_remove_version)

    args = parser.parse_args()
    args.func(args)


if __name__ == '__main__':
    main()

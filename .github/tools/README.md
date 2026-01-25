# Jellyfin Plugin Manifest Tools

Standalone tools for managing Jellyfin plugin repository manifests.

## Tools

### `manifest.py` - Manifest Generator

Manages `manifest.json` for Jellyfin plugin repositories.

```bash
# Add new version to existing manifest
python manifest.py version \
  -f manifest.json \
  -app "Last.fm" \
  -ver "10.11.6.1" \
  -cl "Changelog text" \
  -abi "10.11.6.0" \
  -url "https://github.com/.../releases/download/.../plugin.zip" \
  -ck "abc123..."

# Create/update plugin entry from build.yaml
python manifest.py create -f manifest.json --from-build build.yaml

# Show manifest info
python manifest.py info -f manifest.json

# Remove a version
python manifest.py remove-version -f manifest.json -app "Last.fm" -ver "1.0.0"
```

### `md5.py` - Checksum Calculator

```bash
python md5.py -f plugin.zip
# Output: abc123def456...
```

## Integration in GitHub Actions

```yaml
- name: Calculate checksum
  run: |
    CK=$(python .github/tools/md5.py -f "plugin_${VERSION}.zip")
    echo "checksum=$CK" >> $GITHUB_OUTPUT

- name: Update manifest
  run: |
    python .github/tools/manifest.py version \
      -f manifest.json \
      -app "PluginName" \
      -ver "${{ env.VERSION }}" \
      -cl "Release ${{ env.VERSION }}" \
      -abi "${{ env.TARGET_ABI }}" \
      -url "https://github.com/${{ github.repository }}/releases/download/${{ env.VERSION }}/plugin.zip" \
      -ck "${{ steps.checksum.outputs.checksum }}"
```

## Requirements

- Python 3.10+
- PyYAML (optional, for `--from-build` feature)

```bash
pip install pyyaml
```

## Forking to Other Plugins

This branch (`tools/manifest-generator`) contains only the manifest tools and can be cherry-picked or merged into other plugin repositories:

```bash
# In another plugin repo
git remote add lastfm-tools https://github.com/lusoris/jellyfin-plugin-lastfm.git
git fetch lastfm-tools tools/manifest-generator
git cherry-pick lastfm-tools/tools/manifest-generator
```

Or copy the `.github/tools/` directory directly.

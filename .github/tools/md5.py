#!/usr/bin/env python3
"""
MD5 Checksum Calculator for Jellyfin Plugin releases.

Usage:
    md5.py -f plugin.zip
    md5.py --file path/to/file.zip
"""

import argparse
import hashlib
import sys
from pathlib import Path


def calculate_md5(file_path: Path) -> str:
    """Calculate MD5 checksum of a file."""
    md5_hash = hashlib.md5()

    with open(file_path, 'rb') as f:
        # Read in chunks for large files
        for chunk in iter(lambda: f.read(8192), b''):
            md5_hash.update(chunk)

    return md5_hash.hexdigest()


def main():
    parser = argparse.ArgumentParser(description='Calculate MD5 checksum')
    parser.add_argument('-f', '--file', required=True, help='File to checksum')

    args = parser.parse_args()
    file_path = Path(args.file)

    if not file_path.exists():
        print(f"Error: File not found: {file_path}", file=sys.stderr)
        sys.exit(1)

    checksum = calculate_md5(file_path)
    print(checksum)


if __name__ == '__main__':
    main()

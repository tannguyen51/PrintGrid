#!/usr/bin/env python3
"""Extract CuraEngine binary from an UltiMaker Cura .AppImage (v3 compressed format).

Handles New Gen AppImages where the structure is:
  [ELF binary loader prefix] [zstd-compressed squashfs filesystem]

The script scans for the zstd magic bytes (0x28, 0xb5, 0x2f, 0xfd) to find
where the compressed stream starts, then decompresses and extracts the target.
"""
import sys
import os
import subprocess


def find_pattern(filepath, pattern, max_scan=None):
    """Find first occurrence of bytes pattern in file."""
    with open(filepath, 'rb') as f:
        if max_scan:
            # Read only up to max_scan bytes (shouldn't need this much)
            chunk = f.read(max(2 << 20, max_scan))
        else:
            # Read in chunks to handle large files
            read_so_far = 0
            while True:
                chunk = f.read(1 << 20)  # Read 1MB at a time
                if not chunk:
                    break
                idx = chunk.find(pattern)
                if idx >= 0:
                    return read_so_far + idx
                read_so_far += len(chunk)
        return None


def main():
    if len(sys.argv) != 3:
        print(f"Usage: {sys.argv[0]} <appimage_path> <output_path>")
        sys.exit(1)

    appimage_path = sys.argv[1]
    output_path = sys.argv[2]

    if not os.path.exists(appimage_path):
        print(f"ERROR: File not found: {appimage_path}")
        sys.exit(1)

    filesize = os.path.getsize(appimage_path)
    print(f"[1/6] Input AppImage: {filesize} bytes")

    # Step 1: Look for zstd magic bytes (28 b5 2f fd)
    # The zstd frame starts right after the ELF loader header
    zstd_magic = b'\x28\xb5\x2f\xfd'

    # Search only in the first ~50KB (loader shouldn't be longer)
    zstd_offset = find_pattern(appimage_path, zstd_magic, max_scan=65536)

    if zstd_offset is None:
        # Fallback: search entire file for zstd magic (in case header is very large)
        print("[WARNING] zstd magic not found in first 50KB, scanning full file...")
        zstd_offset = find_pattern(appimage_path, zstd_magic)

    if zstd_offset is None:
        # Fallback: look for ISO9660 CD001 magic instead
        cd001_magic = b'CD001'
        print("[INFO] No zstd magic found, trying ISO9660 CD001 search...")
        # Check offset 32769 (standard SquashFS location) first
        std_offset = 32769
        with open(appimage_path, 'rb') as f:
            f.seek(std_offset)
            header = f.read(4)
        if header == cd001_magic:
            print(f"[INFO] Found CD001 at standard offset 32769, extracting...")
            # Old-style AppImage — extract raw squashfs starting at offset 32769
            extract_old_style(appimage_path, output_path, std_offset)
            return

        cd001_offset = find_pattern(appimage_path, cd001_magic, max_scan=filesize)
        if cd001_offset is not None:
            print(f"[INFO] Found CD001 at offset {cd001_offset}, extracting...")
            extract_old_style(appimage_path, output_path, cd001_offset)
            return

        # Last resort: dump first 32 bytes for debugging
        with open(appimage_path, 'rb') as f:
            magic = f.read(32)
        print(f"[DEBUG] First 32 bytes hex:", ' '.join(f'{b:02x}' for b in magic[:16]))
        print(f"[DEBUG] Bytes 16-31:", ' '.join(f'{b:02x}' for b in magic[16:32]))
        print("ERROR: Cannot determine AppImage format. Neither zstd nor CD001 magic found.")
        sys.exit(1)

    print(f"[2/6] Found zstd magic at offset {zstd_offset}")

    # Verify it looks like zstd by checking a few more bytes
    with open(appimage_path, 'rb') as f:
        f.seek(zstd_offset)
        frame_header = f.read(12)

    version = (frame_header[2] << 8) | frame_header[3]
    print(f"[3/6] zstd version: {version} (expected 0x0101 for v1.5.x)")

    # Step 2: Extract zstd payload
    temp_squash = '/tmp/squash.img'
    print(f"[4/6] Decompressing zstd (offset={zstd_offset}) -> {temp_squash}")
    result = subprocess.run(
        ['zstdcat', '-o', temp_squash],
        stdin=open(appimage_path, 'rb'),
        capture_output=True,
        text=False,
    )

    # Actually, zstdcat -d reads from stdin which includes the header.
    # Better: use dd to skip the header first.
    temp_payload = '/tmp/payload.bin'
    subprocess.run(
        ['dd', f'if={appimage_path}', f'of={temp_payload}', f'bs=1', f'skip={zstd_offset}', 'status=none'],
        check=True,
    )

    subprocess.run(
        ['zstdcat', temp_payload, '-o', temp_squash],
        check=True,
    )
    os.unlink(temp_payload)

    squash_size = os.path.getsize(temp_squash)
    print(f"[5/6] Decompressed squashfs: {squash_size} bytes")

    # Step 3: Extract CuraEngine from squashfs
    temp_extract = '/opt/cura/extract_root'
    os.makedirs(temp_extract, exist_ok=True)

    result = subprocess.run(
        ['bsdtar', '-xf', temp_squash, '-C', temp_extract],
        capture_output=True,
        text=True,
    )
    if result.returncode != 0:
        print(f"ERROR: bsdtar failed: {result.stderr}")
        print(f"Contents of squashfs:")
        list_result = subprocess.run(
            ['bsdtar', '-tf', temp_squash],
            capture_output=True,
            text=True,
        )
        print(list_result.stdout[:2000])
        sys.exit(1)

    # Find CuraEngine anywhere in extracted tree
    print(f"[6/6] Locating CuraEngine binary...")
    cura_paths = []
    for root, dirs, files in os.walk(temp_extract):
        for fname in files:
            if 'cura' in fname.lower() or 'Cura' in fname:
                full = os.path.join(root, fname)
                cura_paths.append(full)
                print(f"  Found: {full} ({os.path.getsize(full)} bytes)")

    if not cura_paths:
        print("ERROR: Could not find any Cura-related file in extracted squashfs!")
        sys.exit(1)

    # Pick the one that looks like an executable binary
    cura_bin = None
    for p in sorted(cura_paths):
        # Prefer exact match "CuraEngine" in path
        if 'CuraEngine' in os.path.basename(p):
            cura_bin = p
            break
    if not cura_bin:
        # Fall back to first match
        cura_bin = cura_paths[0]

    # Copy to final location
    os.makedirs(os.path.dirname(output_path), exist_ok=True)
    import shutil
    shutil.copy2(cura_bin, output_path)
    os.chmod(output_path, 0o755)

    print(f"SUCCESS: CuraEngine extracted to {output_path}")

    # Cleanup
    os.unlink(temp_squash)
    shutil.rmtree(temp_extract, ignore_errors=True)


def extract_old_style(appimage_path, output_path, cd001_offset):
    """Handle old-style (type 1) AppImages with plain SquashFS payload."""
    squash_size = os.path.getsize(appimage_path) - cd001_offset
    print(f"[2/4] Extracting raw squashfs from offset {cd001_offset} ({squash_size} bytes)")

    temp_squash = '/tmp/squash.img'
    subprocess.run(
        ['dd', f'if={appimage_path}', f'of={temp_squash}', f'bs=1', f'skip={cd001_offset}', f'count={squash_size}', 'status=none'],
        check=True,
    )

    temp_extract = '/opt/cura/extract_root'
    os.makedirs(temp_extract, exist_ok=True)

    result = subprocess.run(
        ['bsdtar', '-xf', temp_squash, '-C', temp_extract],
        capture_output=True,
        text=True,
    )
    if result.returncode != 0:
        print(f"ERROR: bsdtar failed: {result.stderr}")
        sys.exit(1)

    cura_bin = None
    for root, dirs, files in os.walk(temp_extract):
        for fname in files:
            if fname == 'CuraEngine':
                cura_bin = os.path.join(root, fname)
                break
        if cura_bin:
            break

    if not cura_bin:
        print("ERROR: Could not find CuraEngine in old-style squashfs!")
        sys.exit(1)

    import shutil
    os.makedirs(os.path.dirname(output_path), exist_ok=True)
    shutil.copy2(cura_bin, output_path)
    os.chmod(output_path, 0o755)

    print(f"SUCCESS: CuraEngine extracted to {output_path}")

    os.unlink(temp_squash)
    shutil.rmtree(temp_extract, ignore_errors=True)


if __name__ == '__main__':
    main()

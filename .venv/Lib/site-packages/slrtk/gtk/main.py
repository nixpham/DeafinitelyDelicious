import argparse 
import os
import sys
import requests
import tarfile
import tempfile

def verify_unity_project(path):
    project_version_path = os.path.join(path, "ProjectSettings", "ProjectVersion.txt")
    graphics_settings_path = os.path.join(path, "ProjectSettings", "GraphicsSettings.asset")
    assets_path = os.path.join(path, "Assets")
    
    if not os.path.exists(project_version_path):
        print("Error: This location is not supported [Err: No Project Version]. Are you sure you are using a valid Unity 6000 project?")
        sys.exit(1) 
        
    if not os.path.exists(graphics_settings_path):
        print("Error: This location is not supported [Err: No Graphics Settings]. Are you sure you are using a valid Unity 6000 project?")
        sys.exit(1) 
    
    if not os.path.exists(assets_path):
        print("Error: Assets folder not found in the project [Err: No Assets]. Have you initialized this with Unity?")
        sys.exit(1)
    return assets_path, project_version_path, graphics_settings_path

def download_gtk_src(path, signdata_url="https://signdata.cc.gatech.edu"):
    url = f'{signdata_url}/res/slrtk/res/gtk/gtk-src.tar.gz'
    response = requests.get(url)
    if response.status_code == 200:
        with open(path, 'wb') as file:
            file.write(response.content)
        return path
    else:
        print("Cannot connect to signdata server")
        exit(1)
        
def extract_specific_dir_to_flat(tar_file, target_dir, dir_to_extract):
    """
    Extracts the contents of a specific directory from a tarfile into a target directory,
    flattening the structure (removing the parent directory prefix).

    Args:
        tar_file (str): Path to the tar.gz file.
        target_dir (str): Directory where the contents will be extracted.
        dir_to_extract (str): The directory inside the tarfile to extract (e.g., "gtk-src/").
    """
    with tarfile.open(tar_file) as tar:
        members = [
            m for m in tar.getmembers() 
            if m.name.startswith(dir_to_extract)
        ]
        for member in members:
            # Remove the prefix (e.g., "gtk-src/") from the filenames
            member.name = os.path.relpath(member.name, dir_to_extract)
            tar.extract(member, path=target_dir)

def install_gtk_sources(path, asset_path):
    with tempfile.TemporaryDirectory() as f:
        tar_path = download_gtk_src(os.path.join(f, "gtk-src.tar.gz"))
        extract_specific_dir_to_flat(tar_path, asset_path, "gtk-src/")

def modify_unity_graphics_settings(input_file, output_file=None):
    """
    Reads a Unity GraphicsSettings file, adds specific shader entries,
    and writes the result while preserving the original format.
    """
    if output_file is None:
        output_file = input_file
        
    # Read the entire file
    with open(input_file, 'r') as file:
        lines = file.readlines()
    
    # Find the m_AlwaysIncludedShaders section
    shaders_index = -1
    for i, line in enumerate(lines):
        if 'm_AlwaysIncludedShaders:' in line:
            shaders_index = i
            break
    
    if shaders_index == -1:
        raise ValueError("Could not find m_AlwaysIncludedShaders section in the file")
    
    # Find where to insert the new entries (before the next major section)
    insert_index = shaders_index + 1
    for i in range(shaders_index + 1, len(lines)):
        if lines[i].startswith('  m_') and not lines[i].startswith('  - '):
            insert_index = i
            break
    
    # Create the new shader entries
    new_entries = [
        '  - {fileID: 4800000, guid: 691fcbf942e4941218b462fdd73a9a27, type: 3}\n',
        '  - {fileID: 4800000, guid: d67f6f5c68884cdab996b99d3be502d4, type: 3}\n'
    ]
    
    # Insert the new entries
    lines[insert_index:insert_index] = new_entries
    
    # Write the modified content back
    with open(output_file, 'w') as file:
        file.writelines(lines)

# platforms = ["unity", "native-android", "native-ios"]
platforms = ["unity"]

# argument parser to be used in main.py 
parser = argparse.ArgumentParser(prog='gtk', description='Interact with SLR GTk.')

install_parser = argparse.ArgumentParser(prog="install", description="Install SLR GTk and dependencies onto your platform")
platform_group = install_parser.add_mutually_exclusive_group(required=True)
for platform in platforms:
    platform_group.add_argument(f"--{platform}", action='store_true', help=f"Install dependencies for {platform.replace('-', ' ')} platform.")
setup_parsers = argparse.ArgumentParser(prog="setup", description="Setup SLR GTk for into your platform")
install_parser.add_argument("--path", required=True, help="")

def install_run(args, extra_args):
    if args.unity:
        assets_path, project_version_path, graphics_settings_path = verify_unity_project(args.path)
        install_gtk_sources(args.path, assets_path)
        modify_unity_graphics_settings(graphics_settings_path)

def parse_and_run(args, extra_args):
    install_run(args, extra_args)
        
subparsers = parser.add_subparsers(dest="command", required=True)
subparsers.add_parser(
    "install",
    parents=[install_parser],
    add_help=False,
    help="Run GTk install",
).set_defaults(func=install_run)

if __name__ == "__main__":
    args = parser.parse_args()
    parse_and_run(args)


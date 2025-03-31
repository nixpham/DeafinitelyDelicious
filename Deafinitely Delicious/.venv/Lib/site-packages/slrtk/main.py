import argparse 
import os
from importlib.machinery import SourceFileLoader

def scan_directories_with_main_py(root_dir):
    for entry in os.scandir(root_dir):
        if entry.is_dir():
            if any(file.name == 'main.py' for file in os.scandir(entry.path)):
                yield entry.name, os.path.join(root_dir, entry.name)
                
def import_from_file(path):
    module = SourceFileLoader("main", os.path.join(path, "main.py")).load_module()
    return getattr(module, "parser"), getattr(module, "parse_and_run")

def main(): 
    import sys
    try:
        delimiter_index = sys.argv.index('--')
        main_args = sys.argv[1:delimiter_index]
        subcommand_args = sys.argv[delimiter_index+1:]
    except ValueError:
        main_args = sys.argv[1:]
        subcommand_args = []
        
    main_parser = argparse.ArgumentParser(
        prog="slrtk",
        description="[Note: slrtk is still under development and very unstable. Commands and their behaviours might change! Please report issues on github or in the Sign Language] Run various SLR tools."
    )

    subparsers = main_parser.add_subparsers(dest="command", required=True)
    for i, path in scan_directories_with_main_py(os.path.dirname(os.path.abspath(__file__))):
        name = i.replace("_", "-")
        try:
            parser, parse_and_run = import_from_file(path)
            subparsers.add_parser(
                name,
                parents = [parser],
                add_help=False,
                help=f"Run {name}"
            ).set_defaults(func=parse_and_run)
        except Exception as e:
            print(e)
            print(f"Unable to load {i}")
            pass

    args, extra_args = main_parser.parse_known_args(main_args) 

    if hasattr(args, "func"):
        args.func(args, [*extra_args, *subcommand_args]) # call corresponding command
    else: 
        main_parser.print_help()

if __name__ == "__main__":
    main()
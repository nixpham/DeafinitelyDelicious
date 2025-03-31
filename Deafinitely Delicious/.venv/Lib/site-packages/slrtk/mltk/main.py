import docker
import os
import argparse

def parse_and_run(args, extra_args={}):
    client = docker.from_env()
    
    # Prepare volume bindings
    volumes = {
        os.path.abspath(args.data_dir): {'bind': '/data', 'mode': 'ro'},
        os.path.abspath(args.meta_dir): {'bind': '/meta', 'mode': 'ro'},
        os.path.abspath(args.save_dir): {'bind': '/saves', 'mode': 'rw'}
    }
    
    # Optional models directory
    if args.models_dir:
        volumes[os.path.abspath(args.models_dir)] = {'bind': '/models/external', 'mode': 'ro'}
    
    # GPU configuration
    device_requests = None
    if args.gpu:
        device_requests = [docker.types.DeviceRequest(count=1, capabilities=[['gpu']])]
    
    # IPC mode for shared memory
    ipc_mode = 'host' if args.host_shm else None
    
    if args.branch == "isolated":
        try:
            # Run the container with extra_args passed directly
            container = client.containers.run(
                image='ananay22/islr-mltk-train',
                name='retrain-runner',
                volumes=volumes,
                device_requests=device_requests,
                ipc_mode=ipc_mode,
                command=["python3", "main.py"] + extra_args,  # Pass extra args directly to the container
                remove=True,
                detach=True
            )
            
            # Stream logs
            logs = container.logs(stream=True)
            for log in logs:
                decoded_log = log.decode('utf-8')
                print(decoded_log, end='')
                
                if "OSError" in decoded_log or "Error" in decoded_log:
                    print("Error encountered. Exiting...")
                    break            
        except Exception as e:
            print(f"Error running Docker container: {e}")
    else:
        print(f"Mode {args.branch} is not supported at the moment.")

# Main argument parser for known arguments
parser = argparse.ArgumentParser(prog='mltk', description='Run the Machine Learning Toolkit bundled for SLR.')
parser.add_argument('branch', type=str, choices=["isolated", "fingerspelling", "phrases"], 
                    help='Branch for the ML pipeline you want to run')
parser.add_argument('--gpu', type=bool, default=True, 
                    help='Use the GPU [Strongly recommended]')
parser.add_argument('--host-shm', type=bool, default=True, 
                    help='Use the host\'s ipc (will pass --ipc=host to docker) [Strongly recommended]')
parser.add_argument('--data-dir', type=str, required=True, 
                    help='The directory to be used for the datasets')
parser.add_argument('--meta-dir', type=str, required=True, 
                    help='The directory to be used for metadata input')
parser.add_argument('--models-dir', type=str, 
                    help='The directory to load in additional models')
parser.add_argument('--save-dir', type=str, required=True, 
                    help='The directory to load and save models to')

if __name__ == "__main__":
    # Use parse_known_args to separate known and unknown arguments
    args, extra_args = parser.parse_known_args()
    
    # Pass known arguments and extra arguments separately
    parse_and_run(args, extra_args)

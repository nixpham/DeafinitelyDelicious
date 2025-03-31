import docker
import argparse 
import os 

def run_docker(input_file_path, output_file_path): 
    client = docker.from_env()
    docker_img = 'ananay22/mputils'
    solution = 'hands'

    try: 
        # absolute paths of input and output files 
        abs_input = os.path.abspath(input_file_path)
        abs_output = os.path.abspath(output_file_path)

        # base names 
        base_input = os.path.basename(input_file_path)
        base_output = os.path.basename(output_file_path)

        print(f"Running Docker with input file {abs_input} and output file {abs_output}.")

        # directory path where output should be saved (create one if it doesn't exist)
        output_dir = os.path.dirname(abs_output)
        if not os.path.exists(output_dir):
            os.makedirs(output_dir)

        # docker container 
        container = client.containers.run(
            docker_img,
            f"{solution} /in/{base_input} /out/{base_output}",
            # mount files from local machine to Docker container 
            volumes={abs_input: {'bind': f'/in/{base_input}', 'mode': 'ro'},
                     output_dir: {'bind': '/out', 'mode': 'rw'}},
            detach=True,
            remove=True
        )

        logs = container.logs(stream=True)
        for log in logs:
            decoded_log = log.decode('utf-8')
            print(decoded_log)

            if "OSError" in decoded_log or "Error" in decoded_log:
                print("Error encountered. Exiting...")
                return 

        container.remove()
    except Exception as e:
        print(f"Error running Docker container: {e}")

# argument parser to be used in main.py 
parser = argparse.ArgumentParser(prog='mputils', description='Run mputils on Docker.')
parser.add_argument('input_file_path', type=str, help='Path to the input file.')
parser.add_argument('output_file_path', type=str, help='Path to save the output file.')

def parse_and_run(args, extra_args):
    run_docker(args.input_file_path, args.output_file_path)

if __name__ == "__main__":
  args = parser.parse_args()
  parse_and_run(args)


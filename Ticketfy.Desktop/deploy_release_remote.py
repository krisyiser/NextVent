import paramiko
import os
import sys

sys.stdout.reconfigure(encoding='utf-8')

def deploy_release(version):
    host = "100.109.190.105"
    user = "yersi"
    pwd = "1712"

    print(f"[AUTO-DEPLOY] Connecting to Valcore server ({host}) for version v{version}...")
    ssh = paramiko.SSHClient()
    ssh.set_missing_host_key_policy(paramiko.AutoAddPolicy())
    ssh.connect(host, username=user, password=pwd)
    sftp = ssh.open_sftp()

    base_dir = os.path.dirname(os.path.abspath(__file__))
    releases_dir = os.path.join(base_dir, "Output", "Releases")
    if not os.path.exists(releases_dir):
        releases_dir = os.path.join(base_dir, "..", "Releases")

    remote_worktree = "/opt/valcore/ticketfy-releases-worktree"
    remote_public = "/opt/valcore/valcore-site/public/downloads"

    print("[AUTO-DEPLOY] Uploading all Velopack release binaries and manifests via SFTP...")

    # Explicit x64 & x86 setups
    x64_setup = os.path.join(releases_dir, "x64", "Ticketfy.Desktop-win-Setup.exe")
    x86_setup = os.path.join(releases_dir, "x86", "Ticketfy.Desktop-win-Setup.exe")
    root_setup = os.path.join(releases_dir, "Ticketfy.Desktop-win-Setup.exe")

    if os.path.exists(x64_setup):
        print(f"  -> Uploading x64 setup for v{version}...")
        sftp.put(x64_setup, f"{remote_public}/Ticketfy-Setup-v{version}-x64.exe")
        sftp.put(x64_setup, f"{remote_public}/Ticketfy-Setup-v{version}.exe")
        sftp.put(x64_setup, f"{remote_public}/Ticketfy-Setup-Latest.exe")
        sftp.put(x64_setup, f"{remote_worktree}/Ticketfy-Setup-v{version}-x64.exe")
        sftp.put(x64_setup, f"{remote_worktree}/Ticketfy-Setup-v{version}.exe")
        sftp.put(x64_setup, f"{remote_worktree}/Ticketfy-Setup-Latest.exe")
    elif os.path.exists(root_setup):
        print(f"  -> Uploading root setup for v{version}...")
        sftp.put(root_setup, f"{remote_public}/Ticketfy-Setup-v{version}-x64.exe")
        sftp.put(root_setup, f"{remote_public}/Ticketfy-Setup-v{version}.exe")
        sftp.put(root_setup, f"{remote_public}/Ticketfy-Setup-Latest.exe")
        sftp.put(root_setup, f"{remote_worktree}/Ticketfy-Setup-v{version}-x64.exe")
        sftp.put(root_setup, f"{remote_worktree}/Ticketfy-Setup-v{version}.exe")
        sftp.put(root_setup, f"{remote_worktree}/Ticketfy-Setup-Latest.exe")

    if os.path.exists(x86_setup):
        print(f"  -> Uploading x86 setup for v{version}...")
        sftp.put(x86_setup, f"{remote_public}/Ticketfy-Setup-v{version}-x86.exe")
        sftp.put(x86_setup, f"{remote_worktree}/Ticketfy-Setup-v{version}-x86.exe")

    # Upload all files recursively
    if os.path.exists(releases_dir):
        for root, dirs, files in os.walk(releases_dir):
            for f in files:
                local_file = os.path.join(root, f)
                rel_path = os.path.relpath(local_file, releases_dir).replace("\\", "/")
                remote_pub_file = f"{remote_public}/{rel_path}"
                remote_wt_file = f"{remote_worktree}/{rel_path}"

                rel_dir = os.path.dirname(rel_path)
                if rel_dir and rel_dir != ".":
                    try:
                        sftp.stat(f"{remote_public}/{rel_dir}")
                    except IOError:
                        sftp.mkdir(f"{remote_public}/{rel_dir}")
                    try:
                        sftp.stat(f"{remote_worktree}/{rel_dir}")
                    except IOError:
                        sftp.mkdir(f"{remote_worktree}/{rel_dir}")

                try:
                    sftp.put(local_file, remote_pub_file)
                    sftp.put(local_file, remote_wt_file)
                    print(f"  -> Uploaded {rel_path}")

                    if f in ["RELEASES", "releases.json", "releases.win.json", "assets.win.json"] or f.endswith(".nupkg"):
                        sftp.put(local_file, f"{remote_public}/{f}")
                        sftp.put(local_file, f"{remote_worktree}/{f}")
                except Exception as ex:
                    print(f"  -> Warning uploading {rel_path}: {ex}")

    # Write releases.json
    releases_json = f'{{\n  "version": "{version}",\n  "updated_at": "{os.popen("date /t").read().strip()}",\n  "downloads": {{\n    "x64": "/downloads/Ticketfy-Setup-v{version}-x64.exe",\n    "x86": "/downloads/Ticketfy-Setup-v{version}-x86.exe",\n    "default": "/downloads/Ticketfy-Setup-v{version}.exe"\n  }}\n}}'
    with sftp.file(f"{remote_public}/releases.json", "w") as f:
        f.write(releases_json)
    with sftp.file(f"{remote_worktree}/releases.json", "w") as f:
        f.write(releases_json)

    sftp.close()

    print(f"[AUTO-DEPLOY] Triggering remote deployment script for v{version}...")
    cmd = f"/opt/valcore/auto_deploy_release.sh {version}"
    stdin, stdout, stderr = ssh.exec_command(cmd)
    out = stdout.read().decode('utf-8', errors='ignore')
    err = stderr.read().decode('utf-8', errors='ignore')

    print("STDOUT:\n", out)
    if err:
        print("STDERR:\n", err)

    ssh.close()
    print("[AUTO-DEPLOY] Release deployment finished successfully.")

if __name__ == "__main__":
    ver = sys.argv[1] if len(sys.argv) > 1 else "3.0.45"
    deploy_release(ver)

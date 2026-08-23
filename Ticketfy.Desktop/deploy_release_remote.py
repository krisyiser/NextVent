import paramiko
import os
import sys
import datetime

sys.stdout.reconfigure(encoding='utf-8')

def safe_put(sftp, local_path, remote_path):
    try:
        try:
            sftp.remove(remote_path)
        except IOError:
            pass
        sftp.put(local_path, remote_path)
    except Exception as ex:
        print(f"  -> Warning uploading {remote_path}: {ex}")

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

    remote_public = "/opt/valcore/valcore-site/public/downloads"

    print("[AUTO-DEPLOY] Uploading all Velopack release binaries and manifests via SFTP...")

    # Explicit x64 & x86 setups
    x64_setup = os.path.join(releases_dir, f"Ticketfy-Setup-v{version}-x64.exe")
    if not os.path.exists(x64_setup):
        x64_setup = os.path.join(releases_dir, "x64", "Ticketfy.Desktop-win-Setup.exe")
    if not os.path.exists(x64_setup):
        x64_setup = os.path.join(releases_dir, "Ticketfy.Desktop-win-Setup.exe")

    x86_setup = os.path.join(releases_dir, f"Ticketfy-Setup-v{version}-x86.exe")
    if not os.path.exists(x86_setup):
        x86_setup = os.path.join(releases_dir, "x86", "Ticketfy.Desktop-win-Setup.exe")

    if os.path.exists(x64_setup):
        print(f"  -> Uploading x64 setup for v{version} ({x64_setup})...")
        safe_put(sftp, x64_setup, f"{remote_public}/Ticketfy-Setup-v{version}-x64.exe")
        safe_put(sftp, x64_setup, f"{remote_public}/Ticketfy-Setup-v{version}.exe")
        safe_put(sftp, x64_setup, f"{remote_public}/Ticketfy-Setup-Latest.exe")

    if os.path.exists(x86_setup):
        print(f"  -> Uploading x86 setup for v{version} ({x86_setup})...")
        safe_put(sftp, x86_setup, f"{remote_public}/Ticketfy-Setup-v{version}-x86.exe")

    # Upload all files recursively (ignoring .git metadata)
    if os.path.exists(releases_dir):
        for root, dirs, files in os.walk(releases_dir):
            dirs[:] = [d for d in dirs if d != '.git']
            for f in files:
                if f.startswith('.git') or f == 'releases.json':
                    continue
                local_file = os.path.join(root, f)
                rel_path = os.path.relpath(local_file, releases_dir).replace("\\", "/")
                remote_pub_file = f"{remote_public}/{rel_path}"

                rel_dir = os.path.dirname(rel_path)
                if rel_dir and rel_dir != ".":
                    parts = rel_dir.split('/')
                    curr_pub = remote_public
                    for p in parts:
                        curr_pub += "/" + p
                        try:
                            sftp.stat(curr_pub)
                        except IOError:
                            try:
                                sftp.mkdir(curr_pub)
                            except IOError:
                                pass

                safe_put(sftp, local_file, remote_pub_file)
                print(f"  -> Uploaded {rel_path}")

    # Ensure root downloads directory has the primary x64 Velopack manifests and nupkg packages
    x64_releases_dir = os.path.join(releases_dir, "x64")
    if os.path.exists(x64_releases_dir):
        for f in os.listdir(x64_releases_dir):
            if f in ["RELEASES", "releases.win.json", "assets.win.json"] or f.endswith(".nupkg"):
                local_f = os.path.join(x64_releases_dir, f)
                if os.path.exists(local_f):
                    safe_put(sftp, local_f, f"{remote_public}/{f}")
                    print(f"  -> Uploaded primary x64 file {f} to root downloads")

    # Write releases.json with current target version
    releases_json = f'{{\n  "version": "{version}",\n  "updated_at": "{now_utc}",\n  "downloads": {{\n    "x64": "/downloads/Ticketfy-Instalador-v{version}-x64.zip?v={version}",\n    "exe": "/downloads/Ticketfy-Setup-v{version}-x64.exe?v={version}",\n    "x86": "/downloads/Ticketfy-Setup-v{version}-x86.exe?v={version}",\n    "default": "/downloads/Ticketfy-Instalador-v{version}-x64.zip?v={version}"\n  }}\n}}'
    
    local_releases_json_path = os.path.join(releases_dir, "releases.json")
    with open(local_releases_json_path, "w", encoding="utf-8") as f:
        f.write(releases_json)

    safe_put(sftp, local_releases_json_path, f"{remote_public}/releases.json")
    print(f"  -> Successfully updated {remote_public}/releases.json to version {version}")

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

import paramiko
import os
import sys

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
    remote_worktree = "/opt/valcore/ticketfy-releases-worktree"

    print("[AUTO-DEPLOY] Uploading release binaries via SFTP...")

    setup_x64 = os.path.join(releases_dir, f"Ticketfy-Setup-v{version}-x64.exe")
    setup_default = os.path.join(releases_dir, f"Ticketfy-Setup-v{version}.exe")
    setup_x86 = os.path.join(releases_dir, f"Ticketfy-Setup-v{version}-x86.exe")

    if os.path.exists(setup_x64):
        sftp.put(setup_x64, f"{remote_worktree}/Ticketfy-Setup-v{version}-x64.exe")
    elif os.path.exists(setup_default):
        sftp.put(setup_default, f"{remote_worktree}/Ticketfy-Setup-v{version}-x64.exe")

    if os.path.exists(setup_x86):
        sftp.put(setup_x86, f"{remote_worktree}/Ticketfy-Setup-v{version}-x86.exe")

    if os.path.exists(setup_default):
        sftp.put(setup_default, f"{remote_worktree}/Ticketfy-Setup-v{version}.exe")

    releases_json = os.path.join(releases_dir, "releases.json")
    if os.path.exists(releases_json):
        sftp.put(releases_json, f"{remote_worktree}/releases.json")

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
    ver = sys.argv[1] if len(sys.argv) > 1 else "3.0.44"
    deploy_release(ver)

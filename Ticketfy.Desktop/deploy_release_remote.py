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

    print("[AUTO-DEPLOY] Uploading all release binaries and Velopack manifests via SFTP...")

    if os.path.exists(releases_dir):
        for f in os.listdir(releases_dir):
            local_file = os.path.join(releases_dir, f)
            if os.path.isfile(local_file):
                remote_wt_file = f"{remote_worktree}/{f}"
                remote_pub_file = f"{remote_public}/{f}"
                try:
                    sftp.put(local_file, remote_wt_file)
                    sftp.put(local_file, remote_pub_file)
                    print(f"  -> Uploaded {f}")
                except Exception as ex:
                    print(f"  -> Warning uploading {f}: {ex}")

    setup_x64 = os.path.join(releases_dir, f"Ticketfy-Setup-v{version}-x64.exe")
    setup_default = os.path.join(releases_dir, "Ticketfy.Desktop-win-Setup.exe")
    if not os.path.exists(setup_default):
        setup_default = os.path.join(releases_dir, f"Ticketfy-Setup-v{version}.exe")

    if os.path.exists(setup_default):
        sftp.put(setup_default, f"{remote_public}/Ticketfy-Setup-v{version}-x64.exe")
        sftp.put(setup_default, f"{remote_public}/Ticketfy-Setup-v{version}.exe")
        sftp.put(setup_default, f"{remote_public}/Ticketfy-Setup-Latest.exe")
        sftp.put(setup_default, f"{remote_worktree}/Ticketfy-Setup-v{version}-x64.exe")
        sftp.put(setup_default, f"{remote_worktree}/Ticketfy-Setup-v{version}.exe")
        sftp.put(setup_default, f"{remote_worktree}/Ticketfy-Setup-Latest.exe")

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

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
        print(f"  -> Uploaded: {remote_path}")
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

    # Ensure remote directory exists
    try:
        sftp.stat(remote_public)
    except IOError:
        sftp.mkdir(remote_public)

    print("[AUTO-DEPLOY] Surgical upload of release binaries and manifests...")

    # 1. Main Inno Setup Installers & ZIP
    x64_setup = os.path.join(releases_dir, f"Ticketfy-Setup-v{version}-x64.exe")
    if not os.path.exists(x64_setup):
        x64_setup = os.path.join(releases_dir, "Ticketfy-Setup-v" + version + ".exe")

    if os.path.exists(x64_setup):
        safe_put(sftp, x64_setup, f"{remote_public}/Ticketfy-Setup-v{version}-x64.exe")
        safe_put(sftp, x64_setup, f"{remote_public}/Ticketfy-Setup-v{version}.exe")
        safe_put(sftp, x64_setup, f"{remote_public}/Ticketfy-Setup-Latest.exe")

    x86_setup = os.path.join(releases_dir, f"Ticketfy-Setup-v{version}-x86.exe")
    if os.path.exists(x86_setup):
        safe_put(sftp, x86_setup, f"{remote_public}/Ticketfy-Setup-v{version}-x86.exe")

    zip_setup = os.path.join(releases_dir, f"Ticketfy-Instalador-v{version}-x64.zip")
    if os.path.exists(zip_setup):
        safe_put(sftp, zip_setup, f"{remote_public}/Ticketfy-Instalador-v{version}-x64.zip")

    # 2. Upload primary Velopack OTA manifests if present
    x64_releases_dir = os.path.join(releases_dir, "x64")
    if os.path.exists(x64_releases_dir):
        for f in os.listdir(x64_releases_dir):
            if f in ["RELEASES", "releases.win.json", "assets.win.json"] or f.endswith(".nupkg"):
                local_f = os.path.join(x64_releases_dir, f)
                if os.path.exists(local_f):
                    safe_put(sftp, local_f, f"{remote_public}/{f}")

    # 3. Write and upload releases.json web manifest
    now_utc = datetime.datetime.now(datetime.timezone.utc).strftime('%Y-%m-%dT%H:%M:%SZ')
    releases_json = f'''{{
  "version": "{version}",
  "updated_at": "{now_utc}",
  "downloads": {{
    "x64": "/downloads/Ticketfy-Instalador-v{version}-x64.zip?v={version}",
    "exe": "/downloads/Ticketfy-Setup-v{version}-x64.exe?v={version}",
    "x86": "/downloads/Ticketfy-Setup-v{version}-x86.exe?v={version}",
    "default": "/downloads/Ticketfy-Instalador-v{version}-x64.zip?v={version}"
  }}
}}'''

    local_releases_json_path = os.path.join(releases_dir, "releases.json")
    with open(local_releases_json_path, "w", encoding="utf-8") as f:
        f.write(releases_json)

    safe_put(sftp, local_releases_json_path, f"{remote_public}/releases.json")
    print(f"  -> Successfully updated {remote_public}/releases.json to version {version}")

    sftp.close()

    # 4. Trigger remote deployment hook
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
    ver = sys.argv[1] if len(sys.argv) > 1 else "3.1.31"
    deploy_release(ver)

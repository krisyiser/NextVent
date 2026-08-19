import paramiko

def run_ssh_command(host, user, password, command):
    client = paramiko.SSHClient()
    client.set_missing_host_key_policy(paramiko.AutoAddPolicy())
    try:
        client.connect(host, username=user, password=password, timeout=10)
        stdin, stdout, stderr = client.exec_command(command)
        
        out = stdout.read().decode('utf-8')
        err = stderr.read().decode('utf-8')
        print(f"--- STDOUT ---\n{out}")
        print(f"--- STDERR ---\n{err}")
    except Exception as e:
        print(f"Connection failed: {e}")
    finally:
        client.close()

if __name__ == "__main__":
    commands = [
        "docker exec nextvent-panel find / -name '*public*.pem' -o -name '*private*.pem' 2>/dev/null",
        "docker exec nextvent-panel printenv | grep -i key",
        "docker exec nextvent-panel cat /app/public_key.pem 2>/dev/null",
        "docker exec nextvent-panel cat /public_key.pem 2>/dev/null",
        "docker exec nextvent-panel cat src/public_key.pem 2>/dev/null"
    ]
    for cmd in commands:
        print(f"Executing: {cmd}")
        run_ssh_command("100.109.190.105", "yersi", "1712", cmd)

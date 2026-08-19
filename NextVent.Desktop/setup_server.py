import paramiko
import sys
import time

host = '100.109.190.105'
username = 'yersi'
password = '1712'

client = paramiko.SSHClient()
client.set_missing_host_key_policy(paramiko.AutoAddPolicy())

try:
    print(f"Conectando a {host}...")
    client.connect(host, username=username, password=password)
    
    commands = [
        "sudo -S apt-get update -y",
        "sudo -S apt-get install nginx -y",
        "sudo -S systemctl enable nginx",
        "sudo -S systemctl start nginx",
        "sudo -S mkdir -p /var/www/html/updates/ticketfy",
        "sudo -S chown -R yersi:yersi /var/www/html/updates",
        "sudo -S chmod -R 755 /var/www/html/updates",
        "sudo -S systemctl status nginx --no-pager"
    ]
    
    for cmd in commands:
        print(f"Ejecutando: {cmd}")
        stdin, stdout, stderr = client.exec_command(cmd)
        if "sudo" in cmd:
            stdin.write(password + '\n')
            stdin.flush()
            time.sleep(1) # wait for prompt
        
        out = stdout.read().decode('utf-8')
        err = stderr.read().decode('utf-8')
        if out: print("STDOUT:\n", out)
        if err: print("STDERR:\n", err)
        print("-" * 40)
        
finally:
    client.close()
    print("Conexión cerrada.")

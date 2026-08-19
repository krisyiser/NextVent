import paramiko

client = paramiko.SSHClient()
client.set_missing_host_key_policy(paramiko.AutoAddPolicy())
client.connect('100.109.190.105', username='yersi', password='1712')

stdin, stdout, stderr = client.exec_command('sudo -S lsof -i :80')
stdin.write('1712\n')
stdin.flush()

print(stdout.read().decode('utf-8'))
client.close()

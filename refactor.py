import os
import glob

def refactor_file(filepath):
    with open(filepath, 'r') as f:
        content = f.read()

    original = content
    if 'ExamPrep.Shared.Constants' in content and not 'using ExamPrep.Shared.Constants;' in content:
        # Find where to insert using
        lines = content.split('\n')
        using_idx = 0
        for i, line in enumerate(lines):
            if line.startswith('using '):
                using_idx = i
        
        lines.insert(using_idx + 1, 'using ExamPrep.Shared.Constants;')
        content = '\n'.join(lines)
    
    content = content.replace('ExamPrep.Shared.Constants.KafkaTopics', 'KafkaTopics')
    content = content.replace('ExamPrep.Shared.Constants.AppConstants', 'AppConstants')
    content = content.replace('ExamPrep.Shared.Constants.ConfigKeys', 'ConfigKeys')
    
    # Also replace config keys
    content = content.replace('"Email:FromAddress"', 'ConfigKeys.Email.FromAddress')
    content = content.replace('"Email:SmtpHost"', 'ConfigKeys.Email.SmtpHost')
    content = content.replace('"Email:SmtpPort"', 'ConfigKeys.Email.SmtpPort')
    content = content.replace('"Email:Username"', 'ConfigKeys.Email.Username')
    content = content.replace('"Email:Password"', 'ConfigKeys.Email.Password')
    content = content.replace('"Kafka:BootstrapServers"', 'ConfigKeys.Kafka.BootstrapServers')
    content = content.replace('"Kafka:GroupId"', 'ConfigKeys.Kafka.GroupId')

    if content != original:
        with open(filepath, 'w') as f:
            f.write(content)

for root, _, files in os.walk('services'):
    for file in files:
        if file.endswith('.cs'):
            refactor_file(os.path.join(root, file))


KEY_NAME="cloud-course-hw2-`date +'%N'`"
KEY_PEM="$KEY_NAME.pem"

REGION="eu-central-1"

echo "create key pair $KEY_PEM to connect to instances and save locally"
aws ec2 create-key-pair --key-name $KEY_NAME \
    | jq -r ".KeyMaterial" > $KEY_PEM

# secure the key pair
chmod 400 $KEY_PEM

SEC_GRP="hw2-`date +'%N'`"

echo "setup firewall $SEC_GRP"
aws ec2 create-security-group   \
    --group-name $SEC_GRP       \
    --description "Access my instances" 

# figure out my ip
MY_IP=$(curl ipinfo.io/ip)
echo "My IP: $MY_IP"

echo "setup rule allowing SSH access to $MY_IP only"
aws ec2 authorize-security-group-ingress        \
    --group-name $SEC_GRP --port 22 --protocol tcp \
    --cidr $MY_IP/32

echo "setup rule allowing HTTP (port 5000) access to $MY_IP only"
aws ec2 authorize-security-group-ingress        \
    --group-name $SEC_GRP --port 5000 --protocol tcp \
    --cidr $MY_IP/32
	

echo "setup rule allowing HTTP (port 5000) access to $MY_IP only"
aws ec2 authorize-security-group-ingress        \
    --group-name $SEC_GRP --port 7070 --protocol tcp \
    --cidr 0.0.0.0/0
	

UBUNTU_20_04_AMI="ami-042ad9eec03638628"

RUN_INSTANCES=$(aws ec2 run-instances --image-id $UBUNTU_20_04_AMI --key-name $KEY_NAME --instance-type t3.micro --security-groups $SEC_GRP)

INSTANCE_ID=$(echo $RUN_INSTANCES | jq -r '.Instances[0].InstanceId')
echo "INSTANCE ID: $INSTANCE_ID"

echo "Waiting for instance creation..."
aws ec2 wait instance-running --instance-ids $INSTANCE_ID


RUN_INSTANCES2=$(aws ec2 run-instances --image-id $UBUNTU_20_04_AMI --key-name $KEY_NAME --instance-type t3.micro --security-groups $SEC_GRP)

INSTANCE_ID2=$(echo $RUN_INSTANCES2 | jq -r '.Instances[0].InstanceId')
echo "INSTANCE ID: $INSTANCE_ID2"

echo "Waiting for instance creation..."
aws ec2 wait instance-running --instance-ids $INSTANCE_ID2


# echo "Creating Role and IAM policies..."
# aws iam create-role --role-name "role-hw2" --assume-role-policy-document file://ec2fullaccess.json

# aws iam attach-role-policy --policy-arn arn:aws:iam::aws:policy/AmazonEC2RolePolicyForLaunchWizard --role-name "role-hw2"
# aws iam attach-role-policy --policy-arn arn:aws:iam::aws:policy/IAMFullAccess --role-name "role-hw2"
# aws iam attach-role-policy --policy-arn arn:aws:iam::aws:policy/AmazonEC2FullAccess --role-name "role-hw2"


echo "Setting role for instance..."
aws ec2 associate-iam-instance-profile --instance-id $INSTANCE_ID --iam-instance-profile Name="test-ec2-role" 
aws ec2 associate-iam-instance-profile --instance-id $INSTANCE_ID2 --iam-instance-profile Name="test-ec2-role" 

PUBLIC_IP=$(aws ec2 describe-instances  --instance-ids $INSTANCE_ID | 
    jq -r '.Reservations[0].Instances[0].PublicIpAddress'
)

PUBLIC_IP2=$(aws ec2 describe-instances  --instance-ids $INSTANCE_ID2 | 
    jq -r '.Reservations[0].Instances[0].PublicIpAddress'
)


echo $PUBLIC_IP $PUBLIC_IP2

ssh -i $KEY_PEM -o "StrictHostKeyChecking=no" -o "ConnectionAttempts=10" ubuntu@$PUBLIC_IP <<EOF
    mkdir ~/worker
	mkdir ~/queues
	mkdir ~/loadbalancer
	exit
EOF

ssh -i $KEY_PEM -o "StrictHostKeyChecking=no" -o "ConnectionAttempts=10" ubuntu@$PUBLIC_IP2 <<EOF
    mkdir ~/queues
	exit
EOF


scp -i $KEY_PEM -o "StrictHostKeyChecking=no" -o "ConnectionAttempts=60" worker/* ubuntu@$PUBLIC_IP:/home/ubuntu/worker
scp -i $KEY_PEM -o "StrictHostKeyChecking=no" -o "ConnectionAttempts=60" queues/* ubuntu@$PUBLIC_IP:/home/ubuntu/queues
scp -i $KEY_PEM -o "StrictHostKeyChecking=no" -o "ConnectionAttempts=60" loadbalancer/* ubuntu@$PUBLIC_IP:/home/ubuntu/loadbalancer

scp -i $KEY_PEM -o "StrictHostKeyChecking=no" -o "ConnectionAttempts=60" queues/* ubuntu@$PUBLIC_IP2:/home/ubuntu/queues

echo "setup production environment"
ssh -i $KEY_PEM -o "StrictHostKeyChecking=no" -o "ConnectionAttempts=10" ubuntu@$PUBLIC_IP <<EOF
    sudo apt update -y
    sudo apt install awscli -y
	sudo apt install jq -y
	sudo snap install dotnet-sdk --classic
	
    sudo chmod +x ~/loadbalancer/spawn.sh
	sudo chmod +x ~/worker/terminate.sh
	
	cd ~/queues/ 
	dotnet publish -c rel -r ubuntu.20.04-x64 --self-contained
	
	cd ~/loadbalancer/
	dotnet publish -c rel -r ubuntu.20.04-x64 --self-contained
	
	cp ~/loadbalancer/spawn.sh ~/loadbalancer/bin/rel/net6.0/ubuntu.20.04-x64/publish/
	
	exit
EOF


ssh -i $KEY_PEM -o "StrictHostKeyChecking=no" -o "ConnectionAttempts=10" ubuntu@$PUBLIC_IP2 <<EOF
    sudo apt update -y
    sudo apt install jq -y
	sudo snap install dotnet-sdk --classic
	
    
	cd ~/queues/ 
	dotnet publish -c rel -r ubuntu.20.04-x64 --self-contained
	
	
    exit
EOF

echo "running production environment"


ssh -i $KEY_PEM -o "StrictHostKeyChecking=no" -o "ConnectionAttempts=10" ubuntu@$PUBLIC_IP2 <<EOF
    cd ~/queues/bin/rel/net6.0/ubuntu.20.04-x64/publish/ 
	nohup ./queues --urls https://0.0.0.0:7070 $PUBLIC_IP:7070  &>/dev/null &
	
    exit
EOF

ssh -i $KEY_PEM -o "StrictHostKeyChecking=no" -o "ConnectionAttempts=10" ubuntu@$PUBLIC_IP <<EOF
    cd ~/queues/bin/rel/net6.0/ubuntu.20.04-x64/publish/ 
	nohup ./queues --urls https://0.0.0.0:7070 $PUBLIC_IP2:7070 &>/dev/null &
	
	exit
EOF

ssh -i $KEY_PEM -o "StrictHostKeyChecking=no" -o "ConnectionAttempts=10" ubuntu@$PUBLIC_IP <<EOF
    cd ~/loadbalancer/bin/rel/net6.0/ubuntu.20.04-x64/publish/
	nohup ./loadbalancer $PUBLIC_IP:7070 $PUBLIC_IP2:7070 
	
    exit
EOF

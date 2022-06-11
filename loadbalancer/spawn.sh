KEY_NAME="cloud-course-hw2-`date +'%N'`"
KEY_PEM="$KEY_NAME.pem"
REGION="eu-central-1"

IPServer1=$1
IPServer2=$2
echo "Server1: $IPServer1 , Server2: $IPServer2"

echo "create key pair $KEY_PEM to connect to instances and save locally"
aws ec2 create-key-pair --key-name $KEY_NAME --region $REGION | jq -r ".KeyMaterial" > $KEY_PEM

# secure the key pair
chmod 400 $KEY_PEM

SEC_GRP="hw2-worker-`date +'%N'`"

echo "setup firewall $SEC_GRP"
aws ec2 create-security-group --region $REGION   \
    --group-name $SEC_GRP       \
    --description "Access my instances" 

# figure out my ip
MY_IP=$(curl ipinfo.io/ip)
echo "My IP: $MY_IP"

echo "setup rule allowing SSH access to $MY_IP only"
aws ec2 authorize-security-group-ingress --region $REGION  \
    --group-name $SEC_GRP --port 22 --protocol tcp \
    --cidr $MY_IP/32

echo "setup rule allowing HTTP (port 5000) access to $MY_IP only"
aws ec2 authorize-security-group-ingress --region $REGION  \
    --group-name $SEC_GRP --port 5000 --protocol tcp \
    --cidr 0.0.0.0/0
	
echo "setup rule allowing HTTP (port 7070) access to any only"
aws ec2 authorize-security-group-ingress --region $REGION\
    --group-name $SEC_GRP --port 7070 --protocol tcp \
    --cidr 0.0.0.0/0
	

UBUNTU_20_04_AMI="ami-042ad9eec03638628"

RUN_INSTANCES=$(aws ec2 run-instances --image-id $UBUNTU_20_04_AMI --key-name $KEY_NAME --instance-type t3.micro --security-groups $SEC_GRP --region $REGION)

INSTANCE_ID=$(echo $RUN_INSTANCES | jq -r '.Instances[0].InstanceId')
echo "INSTANCE ID: $INSTANCE_ID"

echo "Waiting for instance creation..."
aws ec2 wait instance-running --instance-ids $INSTANCE_ID --region $REGION 

echo "Setting role for instance..."
aws ec2 associate-iam-instance-profile --instance-id $INSTANCE_ID --iam-instance-profile Name="test-ec2-role" --region $REGION 

PUBLIC_IP=$(aws ec2 describe-instances  --instance-ids $INSTANCE_ID --region $REGION | 
    jq -r '.Reservations[0].Instances[0].PublicIpAddress'
)
echo $PUBLIC_IP


echo "mkdir worker"
ssh -i $KEY_PEM -o "StrictHostKeyChecking=no" -o "ConnectionAttempts=10" ubuntu@$PUBLIC_IP <<EOF
    mkdir ~/worker
	exit
EOF
echo "copy worker files"
scp -i $KEY_PEM -o "StrictHostKeyChecking=no" -o "ConnectionAttempts=60" ~/worker/* ubuntu@$PUBLIC_IP:/home/ubuntu/worker



echo "setup production environment"
ssh -i $KEY_PEM -o "StrictHostKeyChecking=no" -o "ConnectionAttempts=10" ubuntu@$PUBLIC_IP <<EOF
    sudo apt update -y
    sudo apt install awscli -y
	sudo apt install jq -y
	sudo snap install dotnet-sdk --classic
	
	
	cd ~/worker/ 
	
	chmod +x terminate.sh
	
	
	
	dotnet build
	nohup dotnet run $IPServer1 $IPServer2 &>/dev/null & 
	
    exit
EOF





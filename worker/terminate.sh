
echo "terminate-instance of worker node" 
INSTANCE_ID=$(wget -q -O - http://instance-data/latest/meta-data/instance-id)
aws ec2 terminate-instances --instance-ids $INSTANCE_ID --region eu-central-1
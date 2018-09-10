@echo off
echo Creating Images
docker build -f .\tickme\Dockerfile -t tickme:latest . 
docker build -f .\tickmepayments\Dockerfile -t tickmepayments:latest . 
docker build -f .\tickmetickets\Dockerfile -t tickmetickets:latest . 
echo Starting Swarm
docker swarm leave --force
docker swarm init
echo Deploying to Swarm
docker stack deploy --compose-file docker-compose-swarm.yml tickme > docker-swarm.log.txt
docker ps
echo Done
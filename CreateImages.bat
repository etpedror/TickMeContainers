docker build -f .\tickme\Dockerfile -t tickme:latest . > docker-swarm.log.txt
docker build -f .\tickmepayments\Dockerfile -t tickme:latest . > docker-swarm.log.txt
docker build -f .\tickmetickets\Dockerfile -t tickme:latest . > docker-swarm.log.txt
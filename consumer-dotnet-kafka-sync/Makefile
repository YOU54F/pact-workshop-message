PACTICIPANT := "pactflow-example-consumer-dotnet-message"
PACT_CLI="docker run --rm -v ${PWD}:${PWD} -e PACT_BROKER_BASE_URL -e PACT_BROKER_TOKEN pactfoundation/pact-cli:latest"

# Only deploy from master
ifeq ($(GIT_BRANCH),master)
	DEPLOY_TARGET=deploy
else
	DEPLOY_TARGET=no_deploy
endif

# Only deploy from master
ifeq ($(GITHUB_ACTIONS),true)
	WAIT_TARGET=wait
endif

all: test

## ====================
## CI tasks 
## ====================

restore:
	dotnet restore src
	dotnet restore tests

run:
	cd src && dotnet run

ci: run_tests can_i_deploy $(DEPLOY_TARGET)

start: server.PID

wait: 
	sleep 5

server.PID:
	{ dotnet run --project src & echo $$! > $@; }

stop: server.PID
	kill `cat $<` && rm $<

# Run the ci target from a developer machine with the environment variables
# set as if it was on Travis CI.
# Use this for quick feedback when playing around with your workflows.
fake_ci:
	CI=true \
	GIT_COMMIT=`git rev-parse --short HEAD`+`date +%s` \
	GIT_BRANCH=`git rev-parse --abbrev-ref HEAD` \
	make ci

ci_webhook: run_tests

## =====================
## Build/test tasks
## =====================

test:
	dotnet test tests

run_tests: restore start $(WAIT_TARGET) test stop 

## =====================
## Deploy tasks
## =====================

deploy: deploy_app record_deployment

no_deploy:
	@echo "Not deploying as not on master branch"

can_i_deploy:
	@"${PACT_CLI}" broker can-i-deploy --pacticipant ${PACTICIPANT} --version ${GIT_COMMIT} --to-environment production

deploy_app:
	@echo "Deploying to production"

record_deployment:
	@"${PACT_CLI}" broker record_deployment --pacticipant ${PACTICIPANT} --version ${GIT_COMMIT} --environment production

## ======================
## Misc
## ======================

.PHONY: start stop

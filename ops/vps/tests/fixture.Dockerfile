FROM docker:28.4.0-cli
RUN apk add --no-cache python3 restic
ENTRYPOINT ["python3"]

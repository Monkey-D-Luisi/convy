FROM python:3.13-alpine
ARG SOURCE_SHA
ARG VERSION
ARG HEALTHY=true
LABEL org.opencontainers.image.revision=$SOURCE_SHA
ENV VERSION=$VERSION HEALTHY=$HEALTHY
COPY application.py /app/application.py
USER 65534:65534
CMD ["python3", "/app/application.py"]

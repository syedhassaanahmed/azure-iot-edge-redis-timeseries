FROM alpine AS substitute
ARG REDIS_EXPORTER_HOST_NAME
ENV REDIS_EXPORTER_HOST_NAME=$REDIS_EXPORTER_HOST_NAME
ARG REDIS_TIME_SERIES_EDGE_HOST_NAME
ENV REDIS_TIME_SERIES_EDGE_HOST_NAME=$REDIS_TIME_SERIES_EDGE_HOST_NAME
RUN apk add gettext
ADD prometheus.yml /temp.yml
RUN envsubst < /temp.yml > /prometheus.yml

FROM prom/prometheus
COPY --from=substitute /prometheus.yml /etc/prometheus/
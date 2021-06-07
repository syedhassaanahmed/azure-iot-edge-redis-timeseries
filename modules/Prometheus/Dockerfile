FROM alpine AS substitute

ARG REDIS_EXPORTER_HOST
ARG REDIS_TIMESERIES_EDGE_HOST

RUN apk add gettext
COPY prometheus.yml /temp.yml
RUN envsubst < /temp.yml > /prometheus.yml

FROM prom/prometheus
COPY --from=substitute /prometheus.yml /etc/prometheus/
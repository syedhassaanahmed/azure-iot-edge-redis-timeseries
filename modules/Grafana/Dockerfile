FROM alpine AS substitute

ARG PROMETHEUS_HOST
ENV PROMETHEUS_HOST=$PROMETHEUS_HOST

ARG REDIS_UNIX_SOCKET_DIR
ENV REDIS_UNIX_SOCKET_DIR=$REDIS_UNIX_SOCKET_DIR

RUN apk add gettext
COPY datasources.yml /temp.yml
RUN envsubst < /temp.yml > /datasources.yml

FROM grafana/grafana
ENV GF_INSTALL_PLUGINS="redis-datasource"

COPY grafana.ini /etc/grafana/
COPY --from=substitute /datasources.yml /etc/grafana/provisioning/datasources/
COPY dashboards.yml /etc/grafana/provisioning/dashboards/
COPY dashboards/ /var/lib/grafana/dashboards/

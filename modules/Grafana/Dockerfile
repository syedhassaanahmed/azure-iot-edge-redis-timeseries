FROM alpine AS substitute
ARG PROMETHEUS_HOST_NAME
ARG REDIS_HOST_NAME
ENV PROMETHEUS_HOST_NAME=$PROMETHEUS_HOST_NAME
ENV REDIS_HOST_NAME=$REDIS_HOST_NAME
RUN apk add gettext
ADD datasources.yml /temp.yml
RUN envsubst < /temp.yml > /datasources.yml

FROM grafana/grafana
ENV GF_INSTALL_PLUGINS="redis-datasource"
COPY grafana.ini /etc/grafana/
COPY --from=substitute /datasources.yml /etc/grafana/provisioning/datasources/
COPY dashboards.yml /etc/grafana/provisioning/dashboards/
COPY --chown=grafana:root dashboards/ /var/lib/grafana/dashboards/

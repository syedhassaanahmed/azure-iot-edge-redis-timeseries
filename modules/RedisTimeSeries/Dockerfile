FROM redislabs/redistimeseries

ARG REDIS_UNIX_SOCKET_DIR

COPY redis.conf /temp.conf

RUN apt-get --allow-releaseinfo-change update && \
    apt-get install -y gettext && \
    envsubst < /temp.conf > /etc/redis.conf && \
    mkdir -m 777 $REDIS_UNIX_SOCKET_DIR

CMD ["redis-server", "/etc/redis.conf"]

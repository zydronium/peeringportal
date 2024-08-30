FROM alpine:latest
RUN apk add --no-cache python3 py3-pip git mariadb-client mariadb-dev postgresql-client postgresql-dev build-base python3-dev

RUN mkdir /root/peeringdb-py
WORKDIR /root/peeringdb-py

# Create and activate a virtual environment
RUN rm /usr/lib/python3*/EXTERNALLY-MANAGED && pip install setuptools git+https://github.com/peeringdb/django-peeringdb.git git+https://github.com/peeringdb/peeringdb-py.git django asgiref mysqlclient psycopg2

# Clean up unnecessary packages
RUN apk del build-base python3-dev

# Set the default command for the container
CMD ["python"]

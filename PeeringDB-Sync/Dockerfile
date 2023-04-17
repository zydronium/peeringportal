FROM debian:bullseye-slim

RUN mkdir /root/peeringdb-py && \
    cd /root/peeringdb-py && \
	apt-get update && \
    apt-get install -y python3-pip git mariadb-client libmariadb-dev && \
	pip install --upgrade pip && \
	pip install -U setuptools && \
	pip install git+https://github.com/peeringdb/django-peeringdb.git && \
	pip install git+https://github.com/peeringdb/peeringdb-py.git && \
	pip install "django" && \
	pip install asgiref && \
	pip install mysqlclient

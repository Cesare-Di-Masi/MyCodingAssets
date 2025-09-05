FROM python:3.11-slim

ENV PYTHONDONTWRITEBYTECODE=1
ENV PYTHONUNBUFFERED=1

WORKDIR /srv/backend

# system deps (for nmap subprocess if needed)
RUN apt-get update && apt-get install -y --no-install-recommends \
    nmap \
    iputils-ping \
    ca-certificates \
    && rm -rf /var/lib/apt/lists/*

COPY requirements.txt .
RUN pip install --upgrade pip
RUN pip install -r requirements.txt

COPY . .

RUN chmod +x /srv/backend/worker_entrypoint.sh /srv/backend/scripts/run_nmap.sh

EXPOSE 5000

CMD ["python","app.py"]

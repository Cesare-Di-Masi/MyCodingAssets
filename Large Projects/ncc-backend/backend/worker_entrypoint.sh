#!/usr/bin/env bash
# start celery worker
echo "Starting celery worker..."
celery -A app.celery worker --loglevel=info --concurrency=1

# backend/models.py
from sqlalchemy import Column, String, Integer, DateTime, Boolean, Text, ForeignKey
from sqlalchemy.orm import relationship
from datetime import datetime
from .db import Base

class Host(Base):
    __tablename__ = "hosts"
    id = Column(Integer, primary_key=True, index=True)
    ip = Column(String, unique=True, index=True, nullable=False)
    name = Column(String, nullable=True)
    os = Column(String, nullable=True)
    last_seen = Column(DateTime, default=datetime.utcnow)

    jobs = relationship("Job", back_populates="host")

class Job(Base):
    __tablename__ = "jobs"
    id = Column(Integer, primary_key=True, index=True)
    job_id = Column(String, unique=True, nullable=False)
    type = Column(String, nullable=False)
    target = Column(String, nullable=False)
    status = Column(String, default="queued")
    result_summary = Column(Text, nullable=True)
    created_at = Column(DateTime, default=datetime.utcnow)

    host_id = Column(Integer, ForeignKey("hosts.id"), nullable=True)
    host = relationship("Host", back_populates="jobs")

class ListEntry(Base):
    __tablename__ = "lists"
    id = Column(Integer, primary_key=True, index=True)
    type = Column(String, nullable=False)  # whitelist | blacklist
    value = Column(String, nullable=False)
    added_by = Column(String, default="system")
    added_at = Column(DateTime, default=datetime.utcnow)

class ToolStatus(Base):
    __tablename__ = "tools"
    id = Column(Integer, primary_key=True, index=True)
    name = Column(String, unique=True, nullable=False)
    local_version = Column(String, nullable=True)
    remote_version = Column(String, nullable=True)
    update_available = Column(Boolean, default=False)
    checked_at = Column(DateTime, default=datetime.utcnow)

#!/bin/sh

redis-server /redis/redis.conf
sleep 1
echo yes | redis-cli --cluster create 192.168.57.10:6379 192.168.57.11:6379 192.168.57.12:6379 192.168.57.13:6379 192.168.57.14:6379 192.168.57.15:6379
sleep infinity
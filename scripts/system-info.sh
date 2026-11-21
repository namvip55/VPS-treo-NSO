#!/bin/bash
RAM_TOTAL=$(free -m | awk '/^Mem:/{print $2}')
RAM_USED=$(free -m | awk '/^Mem:/{print $3}')
RAM_FREE=$(free -m | awk '/^Mem:/{print $4}')
SWAP_TOTAL=$(free -m | awk '/^Swap:/{print $2}')
SWAP_USED=$(free -m | awk '/^Swap:/{print $3}')
CPU_LOAD=$(uptime | awk -F'load average:' '{print $2}' | cut -d, -f1 | tr -d ' ')

echo "RAM_TOTAL:$RAM_TOTAL"
echo "RAM_USED:$RAM_USED"
echo "RAM_FREE:$RAM_FREE"
echo "SWAP_TOTAL:$SWAP_TOTAL"
echo "SWAP_USED:$SWAP_USED"
echo "CPU_LOAD:$CPU_LOAD"

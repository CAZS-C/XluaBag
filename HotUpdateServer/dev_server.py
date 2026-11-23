#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
本地热更新资源服务器（模拟 CDN）。

支持 Range/206，用来验证下载器的断点续传。
用法：
    cd HotUpdateServer
    python dev_server.py --port 8000

客户端默认请求：
    http://127.0.0.1:8000/PC/manifest.json
"""

import argparse
import functools
import http.server
import os
import re
import shutil
import socketserver
import sys

RANGE_RE = re.compile(r"bytes=(\d+)-(\d*)")


class RangeRequestHandler(http.server.SimpleHTTPRequestHandler):
    def do_GET(self):
        path = self.translate_path(self.path)
        if os.path.isdir(path):
            return super().do_GET()
        if not os.path.isfile(path):
            self.send_error(404, "File not found: " + self.path)
            return

        size = os.path.getsize(path)
        range_header = self.headers.get("Range")
        match = RANGE_RE.match(range_header) if range_header else None

        if match:
            start = int(match.group(1))
            end = int(match.group(2)) if match.group(2) else size - 1
            if start >= size or end < start:
                self.send_response(416)
                self.send_header("Content-Range", "bytes */%d" % size)
                self.end_headers()
                return
            end = min(end, size - 1)
            length = end - start + 1
            self.send_response(206)
            self.send_header("Content-Type", "application/octet-stream")
            self.send_header("Content-Range", "bytes %d-%d/%d" % (start, end, size))
            self.send_header("Content-Length", str(length))
            self.send_header("Accept-Ranges", "bytes")
            self.send_header("Cache-Control", "no-store")
            self.end_headers()
            with open(path, "rb") as handle:
                handle.seek(start)
                remaining = length
                while remaining > 0:
                    chunk = handle.read(min(64 * 1024, remaining))
                    if not chunk:
                        break
                    self.wfile.write(chunk)
                    remaining -= len(chunk)
            return

        self.send_response(200)
        self.send_header("Content-Type", "application/octet-stream")
        self.send_header("Content-Length", str(size))
        self.send_header("Accept-Ranges", "bytes")
        self.send_header("Cache-Control", "no-store")
        self.end_headers()
        with open(path, "rb") as handle:
            shutil.copyfileobj(handle, self.wfile)

    def log_message(self, fmt, *args):
        sys.stdout.write("[server] " + (fmt % args) + "\n")
        sys.stdout.flush()


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--port", type=int, default=8000)
    parser.add_argument("--root", default=os.path.dirname(os.path.abspath(__file__)))
    args = parser.parse_args()

    handler = functools.partial(RangeRequestHandler, directory=args.root)
    with socketserver.ThreadingTCPServer(("127.0.0.1", args.port), handler) as server:
        server.daemon_threads = True
        print("[server] serving %s at http://127.0.0.1:%d" % (args.root, args.port))
        try:
            server.serve_forever()
        except KeyboardInterrupt:
            print("\n[server] stopped")


if __name__ == "__main__":
    main()

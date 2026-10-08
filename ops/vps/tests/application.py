import http.server
import os

class Application(http.server.BaseHTTPRequestHandler):
    def do_GET(self):
        healthy = os.environ['HEALTHY'] == 'true'
        self.send_response(200 if healthy else 503)
        self.end_headers()
        self.wfile.write(os.environ['VERSION'].encode())

    def log_message(self, *args):
        pass

http.server.HTTPServer(('0.0.0.0', 8080), Application).serve_forever()
